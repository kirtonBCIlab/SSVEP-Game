using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum MapSelection
{
    Map1,
    Map2
}

public class PlayerController : MonoBehaviour
{
    [Header("Map Selection")]
    [SerializeField]
    private MapSelection selectedMap;

    [SerializeField]
    private GameObject grid1;

    [SerializeField]
    private GameObject grid2;

    [Header("Tilemaps")]
    [SerializeField]
    private Tilemap ground_tilemap;

    [SerializeField]
    private Tilemap collision_tilemap;

    [SerializeField]
    private Tilemap gem_tilemap;

    [SerializeField]
    private Tilemap spawn_tilemap1;

    [SerializeField]
    private Tilemap spawn_tilemap2;

    [Header("Tiles")]
    [SerializeField]
    private List<TileBase> gemTiles;

    [SerializeField]
    private List<TileBase> collectedTiles;

    [SerializeField]
    private List<TileBase> smallTiles;

    [Header("Gameplay")]
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

    private void Start()
    {
        GameObject selectedGrid = selectedMap == MapSelection.Map1 ? grid1 : grid2;
        Tilemap spawnTilemap = selectedMap == MapSelection.Map1 ? spawn_tilemap1 : spawn_tilemap2;

        if (selectedGrid == null || spawnTilemap == null)
        {
            Debug.LogError("Grid or start tilemap not assigned!");
            return;
        }

        selectedGrid.SetActive(true);

        ground_tilemap = selectedGrid.transform.Find("Tilemap Ground").GetComponent<Tilemap>();
        collision_tilemap = selectedGrid.transform.Find("Tilemap Collision").GetComponent<Tilemap>();
        gem_tilemap = selectedGrid.transform.Find("Tilemap Gems").GetComponent<Tilemap>();

        // Use saved position if available, else use the start tile
        if (PlayerControllerManager.Instance != null && PlayerControllerManager.Instance.SavedGridPosition != Vector3Int.zero)
        {
            currentGridPos = PlayerControllerManager.Instance.SavedGridPosition;
        }
        else
        {
            currentGridPos = GetStartTilePosition(spawnTilemap);
        }

        transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);

        // Initialize mappings
        gemToCollectedMap = new Dictionary<TileBase, TileBase>();
        for (int i = 0; i < gemTiles.Count && i < collectedTiles.Count; i++)
            gemToCollectedMap[gemTiles[i]] = collectedTiles[i];

        collectedGemSet = new HashSet<TileBase>();

        if (endScreenCanvas != null)
            endScreenCanvas.SetActive(false);

        collectedToSmallMap = new Dictionary<TileBase, TileBase>();
        smallToCollectedMap = new Dictionary<TileBase, TileBase>();
        for (int i = 0; i < collectedTiles.Count && i < smallTiles.Count; i++)
        {
            collectedToSmallMap[collectedTiles[i]] = smallTiles[i];
            smallToCollectedMap[smallTiles[i]] = collectedTiles[i];
        }

        previousGridPos = gem_tilemap.WorldToCell(transform.position);
        StartCoroutine(UpdateTilesCoroutine());
    }

    private Vector3Int GetStartTilePosition(Tilemap spawnTilemap)
    {
        foreach (Vector3Int pos in spawnTilemap.cellBounds.allPositionsWithin)
        {
            if (spawnTilemap.HasTile(pos))
            {
                return pos;
            }
        }

        Debug.LogWarning("No tile found in Start Tilemap — using origin.");
        return Vector3Int.zero;
    }

    private IEnumerator UpdateTilesCoroutine()
    {
        while (true)
        {
            UpdateTileBasedOnPlayerPosition();
            yield return null;
        }
    }

    private void UpdateTileBasedOnPlayerPosition()
    {
        Vector3Int playerGridPos = gem_tilemap.WorldToCell(transform.position);

        foreach (Vector3Int gridPos in gem_tilemap.cellBounds.allPositionsWithin)
        {
            if (!gem_tilemap.HasTile(gridPos)) continue;

            TileBase currentTile = gem_tilemap.GetTile(gridPos);

            if (gridPos == playerGridPos)
            {
                if (collectedToSmallMap.TryGetValue(currentTile, out TileBase smallTile))
                {
                    gem_tilemap.SetTile(gridPos, smallTile);
                }
            }
            else
            {
                if (smallToCollectedMap.TryGetValue(currentTile, out TileBase collectedTile))
                {
                    gem_tilemap.SetTile(gridPos, collectedTile);
                }
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W)) MoveUp();
        else if (Input.GetKeyDown(KeyCode.A)) MoveLeft();
        else if (Input.GetKeyDown(KeyCode.S)) MoveDown();
        else if (Input.GetKeyDown(KeyCode.D)) MoveRight();
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
                cellCenter.x -= 3f;

            transform.position = cellCenter;

            if (gem_tilemap.HasTile(currentGridPos))
                CollectGem(currentGridPos);
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
            Vector3 worldPos = gem_tilemap.GetCellCenterWorld(gridPos);
            worldPos.z = -1f;
            GameObject effect = Instantiate(gemExplosionPrefab, worldPos, Quaternion.identity);

            ParticleSystem ps = effect.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play();
            Destroy(effect, 1f);

            collectedGemSet.Add(gemTile);
            int index = gemTiles.IndexOf(gemTile);
            StartCoroutine(ReplaceTileAndStickerAfterDelay(gridPos, smallTile, 1f, index));

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
}
