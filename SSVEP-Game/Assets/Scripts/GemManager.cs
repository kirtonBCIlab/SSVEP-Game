using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GemManager : MonoBehaviour
{
    // === Inspector Configuration ===

    [Header("Gem Settings")]
    // Tiles for Map 1
    [SerializeField] private List<TileBase> gemTilesMap1;
    [SerializeField] private List<TileBase> collectedTilesMap1;
    [SerializeField] private List<TileBase> smallTilesMap1;

    // Tiles for Map 2
    [SerializeField] private List<TileBase> gemTilesMap2;
    [SerializeField] private List<TileBase> collectedTilesMap2;
    [SerializeField] private List<TileBase> smallTilesMap2;

    // Active tile references for the selected map
    private List<TileBase> gemTiles;
    private List<TileBase> collectedTiles;
    private List<TileBase> smallTiles;
    private List<GameObject> stickers;

    [SerializeField] private List<GameObject> stickersMap1;           // Sticker visuals for collected gems
    [SerializeField] private List<GameObject> stickersMap2;           // Sticker visuals for collected gems

    [SerializeField] private GameObject endScreenCanvas;          // UI shown when all gems are collected

    [SerializeField] private GameObject confettiPrefab;
    [SerializeField] private GameObject gemExplosionPrefab;       // Particle effect prefab for gem collection

    // === Dependencies ===

    private Tilemap gemTilemap;                 // Reference to the tilemap containing gems
    private Transform playerTransform;          // Reference to the player
    private SPOManager spoManager;              // Reference to SPOManager for notifying SPO completion
    private PlayerController playerController;  // Reference to the player's movement controller

    // === Mappings ===

    private Dictionary<TileBase, TileBase> gemToCollectedMap = new();    // Gem -> Collected state
    private Dictionary<TileBase, TileBase> collectedToSmallMap = new();  // Collected -> Small highlight
    private Dictionary<TileBase, TileBase> smallToCollectedMap = new();  // Small highlight -> Collected

    public HashSet<TileBase> collectedGemSet = new();   // Set of gems that have been collected
    public static bool eventTriggered = false;          // Tracks if the end-screen event was already triggered

    public void Start()
    {
        // Try to get PlayerController on the same GameObject
        if (!TryGetComponent<PlayerController>(out playerController))
            Debug.LogError("PlayerController component not found on GemManager GameObject.");
    }

    public void Initialize(Tilemap tilemap, Transform player, SPOManager spo)
    {
        gemTilemap = tilemap;
        playerTransform = player;
        spoManager = spo;

        // Select correct tile sets based on selected map
        if (playerController != null)
        {
            switch (playerController.selectedMap)
            {
                case MapSelection.Map1:
                    gemTiles = gemTilesMap1;
                    collectedTiles = collectedTilesMap1;
                    smallTiles = smallTilesMap1;
                    stickers = stickersMap1;
                    break;
                case MapSelection.Map2:
                    gemTiles = gemTilesMap2;
                    collectedTiles = collectedTilesMap2;
                    smallTiles = smallTilesMap2;
                    stickers = stickersMap2;
                    break;
            }
        }

        // Ensure all tile lists are the same length
        if (!ValidateTileListLengths())
        {
            Debug.LogError("GemManager: Tile lists do not match in size.");
            return;
        }

        // Clear old state
        gemToCollectedMap.Clear();
        collectedToSmallMap.Clear();
        smallToCollectedMap.Clear();
        collectedGemSet.Clear();

        // Populate tile mapping dictionaries
        for (int i = 0; i < gemTiles.Count; i++)
            gemToCollectedMap[gemTiles[i]] = collectedTiles[i];

        for (int i = 0; i < collectedTiles.Count; i++)
        {
            collectedToSmallMap[collectedTiles[i]] = smallTiles[i];
            smallToCollectedMap[smallTiles[i]] = collectedTiles[i];
        }

        // Hide end screen initially
        if (endScreenCanvas != null)
            endScreenCanvas.SetActive(false);

        // Start continuous tile update coroutine
        StartCoroutine(UpdateTilesCoroutine());
    }

    // Ensure tile lists are the same length to prevent index issues
    private bool ValidateTileListLengths()
    {
        return gemTiles.Count == collectedTiles.Count && collectedTiles.Count == smallTiles.Count;
    }

    // Coroutine that updates tiles in real-time based on player proximity
    private IEnumerator UpdateTilesCoroutine()
    {
        while (true)
        {
            UpdateTileBasedOnPlayerPosition();
            yield return null;
        }
    }

    // Swaps between large/small tile versions based on player location
    private void UpdateTileBasedOnPlayerPosition()
    {
        if (playerTransform == null || gemTilemap == null) return;

        // For each position in the grid
        foreach (Vector3Int gridPos in gemTilemap.cellBounds.allPositionsWithin)
        {
            if (!gemTilemap.HasTile(gridPos)) continue;

            // Get the tile associated with the current grid position
            TileBase currentTile = gemTilemap.GetTile(gridPos);

            // If the player is standing on the current grid position
            if (gridPos == gemTilemap.WorldToCell(playerTransform.position))
            {
                // If the current tile at the grid position has an associated small tile, swap it
                // There should be no large tiles without an associated small tile
                if (collectedToSmallMap.TryGetValue(currentTile, out TileBase smallTile))
                    gemTilemap.SetTile(gridPos, smallTile);
            }
            else
            {
                //If the player is not standing on the current grid position, keep the large tile
                if (smallToCollectedMap.TryGetValue(currentTile, out TileBase collectedTile))
                    gemTilemap.SetTile(gridPos, collectedTile);
            }
        }
    }

    // Called when player lands on a gem tile to attempt collection
    public void TryCollectGem(Vector3Int gridPos)
    {
        TileBase gemTile = gemTilemap.GetTile(gridPos);

        // If standing on a gem tile, swap the gem tile out for the small tile associated with it
        if (gemToCollectedMap.TryGetValue(gemTile, out TileBase smallTile))
        {
            // Update player state
            playerController.gem_collected = true;

            // Play visual effect
            Vector3 worldPos = gemTilemap.GetCellCenterWorld(gridPos);
            worldPos.z = -1f;
            GameObject effect = Instantiate(gemExplosionPrefab, worldPos, Quaternion.identity);
            ParticleSystem ps = effect.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Play();
            Destroy(effect, 1.5f);

            // Record gem collection
            collectedGemSet.Add(gemTile);
            playerController.special_pos = true;

            // Notify SPO manager that the SPO was used for a movement, and the next SPO in the queue can be assigned to the next special position
            spoManager?.AssignCurrentSPO(gemTile);

            // Activate sticker UI and update tile after 1s delay
            StartCoroutine(ReplaceTileAndStickerAfterDelay(gridPos, smallTile, 1f, gemTiles.IndexOf(gemTile)));

            // Show end screen if all gems are collected
            if (!eventTriggered && collectedGemSet.Count == gemTiles.Count)
            {
                StartCoroutine(ShowEndScreenAfterDelay(2f));
                eventTriggered = true;
                playerController.gem_collected = true;
            }
        }
        else // Player is standing on a gem tile that has already been collected
            playerController.gem_collected = false;
    }

    // Delays replacement of a tile and shows a sticker on the game console
    private IEnumerator ReplaceTileAndStickerAfterDelay(Vector3Int gridPos, TileBase smallTile, float delay, int index)
    {
        yield return new WaitForSeconds(delay);
        gemTilemap.SetTile(gridPos, smallTile);

        if (index >= 0 && index < stickers.Count && stickers[index] != null)
            stickers[index].SetActive(true);
    }

    // Show the end screen and disable the tilemap after final gem
    private IEnumerator ShowEndScreenAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Show confetti
        Vector3 confetti_loc = new Vector3(0, 0, -4.5f);
        GameObject confetti = Instantiate(confettiPrefab, confetti_loc, confettiPrefab.transform.rotation);
        ParticleSystem con = confetti.GetComponentInChildren<ParticleSystem>();
        if (con != null) con.Play();

        Destroy(confetti, 3f);

        // Deactivate current grid
        playerController.selectedGrid.SetActive(false);

        // Activate end screen
        if (endScreenCanvas != null)
            endScreenCanvas.SetActive(true);
    }

    // Checks for uncollected gems adjacent to the player's current position
    public Vector3Int? GetUncollectedAdjacentGem(Vector3Int currentGridPos)
    {
        if (gemTilemap == null) return null;

        // Get all directions to make sure there is no tile in any direction
        Vector3Int[] offsets = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (var offset in offsets)
        {
            // Get tiles next to current tile
            TileBase tile = gemTilemap.GetTile(currentGridPos + offset);

            // Check if this is a valid, uncollected gem tile
            if (tile != null && gemTiles.Contains(tile) && !collectedGemSet.Contains(tile))
                return currentGridPos + offset;
        } 

        return null; // No adjacent gem found
    }
}
