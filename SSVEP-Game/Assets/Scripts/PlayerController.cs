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
    private List<GameObject> stickers;

    [SerializeField]
    private GameObject endScreenCanvas; // UI element to display when all gems are collected

    [SerializeField]
    private GameObject gemExplosionPrefab;
    private Dictionary<TileBase, TileBase> gemToCollectedMap;
    private HashSet<TileBase> collectedGemSet;
    private bool eventTriggered = false;
    private Vector3Int currentGridPos;

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
            transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);

            if (gem_tilemap.HasTile(currentGridPos))
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

        if (gemToCollectedMap.TryGetValue(gemTile, out TileBase collectedTile))
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

            // Delay tile replacement
            StartCoroutine(ReplaceTileAfterDelay(gridPos, collectedTile, 1f));

            // Add to collected gem set
            collectedGemSet.Add(gemTile);

            // Activate corresponding sticker
            int index = gemTiles.IndexOf(gemTile);
            if (index >= 0 && index < stickers.Count && stickers[index] != null)
            {
                stickers[index].SetActive(true);
            }

            // Trigger event if all gems collected
            if (!eventTriggered && collectedGemSet.Count == gemTiles.Count)
            {
                TriggerEndEvent();
                eventTriggered = true;
            }
        }
        else
        {
            Debug.LogWarning("Gem tile found, but no replacement defined.");
        }
    }

    private IEnumerator ReplaceTileAfterDelay(Vector3Int gridPos, TileBase collectedTile, float delay)
    {
        yield return new WaitForSeconds(delay);
        gem_tilemap.SetTile(gridPos, collectedTile);
    }



    private void TriggerEndEvent()
    {
        if (endScreenCanvas != null)
        {
            endScreenCanvas.SetActive(true);
        }
    }
}
