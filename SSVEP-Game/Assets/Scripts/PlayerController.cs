using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Tilemap ground_tilemap;

    [SerializeField]
    private Tilemap collision_tilemap;

    [SerializeField]
    private Tilemap gem_tilemap;

    [SerializeField]
    private List<TileBase> gemTiles;

    [SerializeField]
    private List<TileBase> collectedTiles;

    [SerializeField]

    private List<TileBase> smallTiles;

    [SerializeField]
    private List<GameObject> stickers;

    [SerializeField]
    private GameObject endScreenCanvas;

    [SerializeField]
    private GameObject gemExplosionPrefab;
    private Dictionary<TileBase, TileBase> gemToCollectedMap;

    private Dictionary<TileBase, TileBase> collectedToSmallMap;

    private Dictionary<TileBase, TileBase> smallToCollectedMap;

    private HashSet<TileBase> collectedGemSet;
    private bool eventTriggered = false;
    private Vector3Int currentGridPos;

    private Vector3Int previousGridPos;

    private string[] SPOOrder = new string[] { "SPO1", "SPO2", "SPO3", "SPO4",
        "SPO1", "SPO2", "SPO3", "SPO4",
        "SPO1", "SPO2", "SPO3", "SPO4",
        "SPO1", "SPO2", "SPO3", "SPO4"};

    private void Start()
    {
        if (PlayerControllerManager.Instance != null && PlayerControllerManager.Instance.SavedGridPosition != Vector3Int.zero)
        {
            currentGridPos = PlayerControllerManager.Instance.SavedGridPosition;
        }
        else
        {
            currentGridPos = ground_tilemap.WorldToCell(transform.position);
            Debug.Log("PlayerControllerManager instance not found or no saved position. Using default position.");
        }

        // Snap player to nearest grid cell
        transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);

        // Build mapping between gem tiles and collected replacements
        gemToCollectedMap = new Dictionary<TileBase, TileBase>();
        for (int i = 0; i < gemTiles.Count && i < collectedTiles.Count; i++)
        {
            gemToCollectedMap[gemTiles[i]] = collectedTiles[i];
        }

        // Initialize collected gem set
        collectedGemSet = new HashSet<TileBase>();

        // Ensure end screen is hidden at start
        if (endScreenCanvas != null)
        {
            endScreenCanvas.SetActive(false);
        }

        // Initialize collected to small and small to collected maps
        collectedToSmallMap = new Dictionary<TileBase, TileBase>();
        smallToCollectedMap = new Dictionary<TileBase, TileBase>();

        for (int i = 0; i < collectedTiles.Count; i++)
        {
            if (i < smallTiles.Count)
            {
                collectedToSmallMap[collectedTiles[i]] = smallTiles[i];
                smallToCollectedMap[smallTiles[i]] = collectedTiles[i];
            }
        }

        previousGridPos = gem_tilemap.WorldToCell(transform.position);
        StartCoroutine(UpdateTilesCoroutine());
    }

    private IEnumerator UpdateTilesCoroutine()
    {
        while (true)
        {
            UpdateTileBasedOnPlayerPosition();
            yield return null; // Wait for the next frame
        }
    }

    /*
      private IEnumerator UpdateSPO()
      {
          while (true)
          {
              if (NextToGem(currentGridPos))
              {

              }
          }
      }
      */

    private void UpdateTileBasedOnPlayerPosition()
    {
        // Get the player's current grid position
        Vector3Int playerGridPos = gem_tilemap.WorldToCell(transform.position);

        // Iterate through all tiles in the gem_tilemap
        foreach (Vector3Int gridPos in gem_tilemap.cellBounds.allPositionsWithin)
        {
            if (!gem_tilemap.HasTile(gridPos)) continue;

            TileBase currentTile = gem_tilemap.GetTile(gridPos);

            // Check if the player is standing on this tile
            if (gridPos == playerGridPos)
            {
                // Change to the smaller tile if the player is on it
                if (collectedToSmallMap.TryGetValue(currentTile, out TileBase smallTile))
                {
                    gem_tilemap.SetTile(gridPos, smallTile);
                }
            }
            else
            {
                // Change to the collected tile if the player is not on it
                if (smallToCollectedMap.TryGetValue(currentTile, out TileBase collectedTile))
                {
                    gem_tilemap.SetTile(gridPos, collectedTile);
                }
            }
        }
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
            MoveUp();
        else if (Input.GetKeyDown(KeyCode.A))
            MoveLeft();
        else if (Input.GetKeyDown(KeyCode.S))
            MoveDown();
        else if (Input.GetKeyDown(KeyCode.D))
            MoveRight();
    }

    public void MoveUp() => Move(new Vector2Int(0, 1));
    public void MoveDown() => Move(new Vector2Int(0, -1));
    public void MoveLeft() => Move(new Vector2Int(-1, 0));
    public void MoveRight() => Move(new Vector2Int(1, 0));

    public void Move(Vector2Int direction)
    {
        Vector3Int targetGridPos = currentGridPos + new Vector3Int(direction.x, direction.y, 0);

        if (CanMove(targetGridPos))
        {
            currentGridPos = targetGridPos;
            PlayerControllerManager.Instance.SavedGridPosition = currentGridPos;

            Vector3 cellCenter = ground_tilemap.GetCellCenterWorld(currentGridPos);

            if (gem_tilemap.HasTile(currentGridPos))
            {
                cellCenter.x -= 3f; //moves player to the left of the gem/critter
            }

            transform.position = cellCenter;

            if (gem_tilemap.HasTile(currentGridPos)) // checks if the gem is being picked up
            {
                CollectGem(currentGridPos);
            }
        }
    }

    private bool CanMove(Vector3Int targetGridPos)
    {
        return ground_tilemap.HasTile(targetGridPos) && !collision_tilemap.HasTile(targetGridPos);
    }

    private void CollectGem(Vector3Int gridPos)
    {
        TileBase gemTile = gem_tilemap.GetTile(gridPos);

        if (gemToCollectedMap.TryGetValue(gemTile, out TileBase smallTile))
        {
            // Spawn smoke effect
            Vector3 worldPos = gem_tilemap.GetCellCenterWorld(gridPos);
            worldPos.z = -1f;
            GameObject effect = Instantiate(gemExplosionPrefab, worldPos, Quaternion.identity);

            ParticleSystem ps = effect.GetComponentInChildren<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }

            Destroy(effect, 1f);

            // Add to collected gem set
            collectedGemSet.Add(gemTile);

            // Activate corresponding sticker
            int index = gemTiles.IndexOf(gemTile);

            StartCoroutine(ReplaceTileAndStickerAfterDelay(gridPos, smallTile, 1f, index));

            // Trigger event if all gems collected
            if (!eventTriggered && collectedGemSet.Count == gemTiles.Count)
            {
                StartCoroutine(ShowEndScreenAfterDelay(2f));
                eventTriggered = true;
            }
        }
        else
        {
            Debug.LogWarning("Gem tile found, but no replacement defined.");
        }
    }

    private IEnumerator ReplaceTileAndStickerAfterDelay(Vector3Int gridPos, TileBase smallTile, float delay, int index)
    {
        yield return new WaitForSeconds(delay);
        gem_tilemap.SetTile(gridPos, smallTile);

        if (index >= 0 && index < stickers.Count && stickers[index] != null)
        {
            stickers[index].SetActive(true);
        }
    }

    private IEnumerator ShowEndScreenAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (endScreenCanvas != null)
        {
            endScreenCanvas.SetActive(true);
        }
    }

    private bool NextToGem(Vector3Int gridPos)
    {
        // Check if the player is next to a gem tile
        Vector3Int[] adjacentPositions = new Vector3Int[]
        {
            gridPos + Vector3Int.up,
            gridPos + Vector3Int.down,
            gridPos + Vector3Int.left,
            gridPos + Vector3Int.right
        };

        foreach (var pos in adjacentPositions) // checks if gem hasn't been collected
        {
            if (gem_tilemap.HasTile(pos))
            {
                return true;
            }
        }

        return false;
    }

    // If gem is next to player, return the direction vector.
    private Vector3Int GetGemDirection(Vector3Int gridPos)
    {
        if (NextToGem(gridPos))
        {
            Vector3Int[] adjacentPositions = new Vector3Int[]
            {
                gridPos + Vector3Int.up,
                gridPos + Vector3Int.down,
                gridPos + Vector3Int.left,
                gridPos + Vector3Int.right
            };

            foreach (var pos in adjacentPositions)
            {
                if (gem_tilemap.HasTile(pos))
                {
                    return pos - gridPos; // Return the direction vector
                }
            }

        }
        return Vector3Int.zero; // No gem found next to the player
    }


    // Converting the direction vector to indices.
    // bottomLeft = 1 (moving down)
    // topLeft = 2 (moving left)
    // topRight = 3 (moving up)
    // bottomRight = 4 (moving right)
    private int DirectionToIndex(Vector3Int direction)
    {
        if (direction == Vector3Int.down) return 1;
        if (direction == Vector3Int.left) return 2;
        if (direction == Vector3Int.up) return 3;
        if (direction == Vector3Int.right) return 4; 
    }
    

}