using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GemManager : MonoBehaviour
{
    [Header("Gem Settings")]
    [SerializeField] private List<TileBase> gemTilesMap1;
    [SerializeField] private List<TileBase> collectedTilesMap1;
    [SerializeField] private List<TileBase> smallTilesMap1;

    [SerializeField] private List<TileBase> gemTilesMap2;
    [SerializeField] private List<TileBase> collectedTilesMap2;
    [SerializeField] private List<TileBase> smallTilesMap2;

    private List<TileBase> gemTiles;
    private List<TileBase> collectedTiles;
    private List<TileBase> smallTiles;

    [SerializeField] private List<GameObject> stickers;
    [SerializeField] private GameObject endScreenCanvas;
    [SerializeField] private GameObject gemExplosionPrefab;

    private Tilemap gemTilemap;
    private Transform playerTransform;
    private SPOManager spoManager;
    private PlayerController playerController;

    private Dictionary<TileBase, TileBase> gemToCollectedMap = new();
    private Dictionary<TileBase, TileBase> collectedToSmallMap = new();
    private Dictionary<TileBase, TileBase> smallToCollectedMap = new();

    public HashSet<TileBase> collectedGemSet = new();

    public static bool eventTriggered = false;

    public void Start()
    {
        if (!TryGetComponent<PlayerController>(out playerController))
            Debug.LogError("PlayerController component not found on GemManager GameObject.");
    }

    public void Initialize(Tilemap tilemap, Transform player, SPOManager spo)
    {
        gemTilemap = tilemap;
        playerTransform = player;
        spoManager = spo;

        // Get selected map from PlayerController
        if (playerController != null)
        {
            switch (playerController.selectedMap)
            {
                case MapSelection.Map1:
                    gemTiles = gemTilesMap1;
                    collectedTiles = collectedTilesMap1;
                    smallTiles = smallTilesMap1;
                    break;
                case MapSelection.Map2:
                    gemTiles = gemTilesMap2;
                    collectedTiles = collectedTilesMap2;
                    smallTiles = smallTilesMap2;
                    break;
            }
        }

        if (!ValidateTileListLengths())
        {
            Debug.LogError("GemManager: Tile lists do not match in size.");
            return;
        }

        gemToCollectedMap.Clear();
        collectedToSmallMap.Clear();
        smallToCollectedMap.Clear();
        collectedGemSet.Clear();

        for (int i = 0; i < gemTiles.Count; i++)
            gemToCollectedMap[gemTiles[i]] = collectedTiles[i];

        for (int i = 0; i < collectedTiles.Count; i++)
        {
            collectedToSmallMap[collectedTiles[i]] = smallTiles[i];
            smallToCollectedMap[smallTiles[i]] = collectedTiles[i];
        }

        if (endScreenCanvas != null)
            endScreenCanvas.SetActive(false);

        StartCoroutine(UpdateTilesCoroutine());
    }

    private bool ValidateTileListLengths()
    {
        return gemTiles.Count == collectedTiles.Count &&
               collectedTiles.Count == smallTiles.Count;
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
        if (playerTransform == null || gemTilemap == null) return;

        Vector3Int playerGridPos = gemTilemap.WorldToCell(playerTransform.position);

        foreach (Vector3Int gridPos in gemTilemap.cellBounds.allPositionsWithin)
        {
            if (!gemTilemap.HasTile(gridPos)) continue;

            TileBase currentTile = gemTilemap.GetTile(gridPos);

            if (gridPos == playerGridPos)
            {
                if (collectedToSmallMap.TryGetValue(currentTile, out TileBase smallTile))
                    gemTilemap.SetTile(gridPos, smallTile);
            }
            else
            {
                if (smallToCollectedMap.TryGetValue(currentTile, out TileBase collectedTile))
                    gemTilemap.SetTile(gridPos, collectedTile);
            }
        }
    }

    public void TryCollectGem(Vector3Int gridPos)
    {
        TileBase gemTile = gemTilemap.GetTile(gridPos);

        if (gemToCollectedMap.TryGetValue(gemTile, out TileBase smallTile))
        {
            playerController.gem_collected = true;
            Debug.Log("Saving gem collected");

            Vector3 worldPos = gemTilemap.GetCellCenterWorld(gridPos);
            worldPos.z = -1f;
            GameObject effect = Instantiate(gemExplosionPrefab, worldPos, Quaternion.identity);

            ParticleSystem ps = effect.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play();
            Destroy(effect, 1.5f);

            collectedGemSet.Add(gemTile);
            playerController.special_pos = true;

            // Notify SPOManager that the current SPO has completed a task
            spoManager?.AssignCurrentSPO(gemTile);

            // Activate corresponding sticker
            int index = gemTiles.IndexOf(gemTile);
            StartCoroutine(ReplaceTileAndStickerAfterDelay(gridPos, smallTile, 1f, index));

            if (!eventTriggered && collectedGemSet.Count == gemTiles.Count)
            {
                StartCoroutine(ShowEndScreenAfterDelay(2f));
                eventTriggered = true;
                playerController.gem_collected = true;
            }
        }
        else
        {
            playerController.gem_collected = false;
            Debug.LogWarning("Gem tile found, but no replacement defined.");
        }
    }

    private IEnumerator ReplaceTileAndStickerAfterDelay(Vector3Int gridPos, TileBase smallTile, float delay, int index)
    {
        yield return new WaitForSeconds(delay);
        gemTilemap.SetTile(gridPos, smallTile);

        if (index >= 0 && index < stickers.Count && stickers[index] != null)
            stickers[index].SetActive(true);
    }

    private IEnumerator ShowEndScreenAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        //Deactivate grid
        playerController.selectedGrid.SetActive(false);

        //Set active
        if (endScreenCanvas != null)
            endScreenCanvas.SetActive(true);
    }

    public Vector3Int? GetUncollectedAdjacentGem(Vector3Int currentGridPos)
    {
        if (gemTilemap == null) return null;

        Vector3Int[] offsets = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (var offset in offsets)
        {
            Vector3Int pos = currentGridPos + offset;
            TileBase tile = gemTilemap.GetTile(pos);
            if (tile != null && gemTiles.Contains(tile) && !collectedGemSet.Contains(tile))
                return pos;
        }

        return null;
    }
}
