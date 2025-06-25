using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
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

    // private Vector3Int gridPos1;
    private Vector3Int gridPos2;

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

    private Vector3 topleftPos;
    private Vector3 toprightPos;
    private Vector3 bottomleftPos;
    private Vector3 bottomrightPos;

    private Vector3Int previousGridPos;

    private GameObject SPO1;
    private GameObject SPO2;
    private GameObject SPO3;
    private GameObject SPO4;
    private HashSet<TileBase> assignedSPOGemSet = new HashSet<TileBase>();


    private Queue<string> SPOOrder = new Queue<string>(new[] {
        "SPO 1", "SPO 2", "SPO 3", "SPO 4",
        "SPO 1", "SPO 2", "SPO 3", "SPO 4",
        "SPO 1", "SPO 2", "SPO 3", "SPO 4",
        "SPO 1", "SPO 2", "SPO 3", "SPO 4"
    });

    private Dictionary<Vector3Int, string> directionToSPO = new Dictionary<Vector3Int, string>();

    private void Start()
    {
        GameObject selectedGrid;
        Tilemap spawnTilemap;

        // Depending on what map is selected, initialize the 2 positions
        if (selectedMap == MapSelection.Map1)
        {
            Debug.Log("Map 1 Selected!");
            selectedGrid = grid1;
            // gridPos1 = new Vector3Int(8, 7, 0);
            gridPos2 = new Vector3Int(8, 11, 0);
            spawnTilemap = spawn_tilemap1;
        }
        else if (selectedMap == MapSelection.Map2)
        {
            Debug.Log("Map 2 Selected!");
            selectedGrid = grid2;
            // gridPos1 = new Vector3Int(7, 3, 0);
            gridPos2 = new Vector3Int(13, 6, 0);
            spawnTilemap = spawn_tilemap2;
        }
        else
        {
            selectedGrid = null;
            spawnTilemap = null;
        }

        if (selectedGrid == null || spawnTilemap == null)
        {
            Debug.LogError("Grid or start tilemap not assigned!");
            return;
        }

        selectedGrid.SetActive(true);

        ground_tilemap = selectedGrid.transform.Find("Tilemap Ground").GetComponent<Tilemap>();
        collision_tilemap = selectedGrid.transform.Find("Tilemap Collision").GetComponent<Tilemap>();
        gem_tilemap = selectedGrid.transform.Find("Tilemap Gems").GetComponent<Tilemap>();

        // Initialize SPO GameObjects
        SPO1 = GameObject.Find("SPO 1");
        SPO2 = GameObject.Find("SPO 2");
        SPO3 = GameObject.Find("SPO 3");
        SPO4 = GameObject.Find("SPO 4");

        // Check if SPO GameObjects are found
        if (SPO1 == null || SPO2 == null || SPO3 == null || SPO4 == null)
        {
            Debug.LogError("One or more SPO GameObjects not found in the scene.");
        }

        topleftPos = new Vector3(16, 16, 0);
        toprightPos = new Vector3(130, 16, 0);
        bottomleftPos = new Vector3(16, -36, 0);
        bottomrightPos = new Vector3(130, -36, 0);

        if (PlayerControllerManager.Instance != null && PlayerControllerManager.Instance.SavedGridPosition != Vector3Int.zero)
        {
            currentGridPos = PlayerControllerManager.Instance.SavedGridPosition;
        }
        else
        {
            currentGridPos = GetStartTilePosition(spawnTilemap);
        }

        transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);
        moveSPOAtSpawn();

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
            yield return null; // Wait for the next frame
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
        var uncollectedGemPos = GetUncollectedAdjacentGem(currentGridPos);
        TileBase uncollectedGemTile;

        if (uncollectedGemPos != null)
        {
            uncollectedGemTile = gem_tilemap.GetTile(uncollectedGemPos.Value);
        }
        else
        {
            uncollectedGemTile = null;
        }

        // Only assign if next to a real, uncollected gem tile
        if (uncollectedGemTile != null
            && gemTiles.Contains(uncollectedGemTile)
            && !collectedGemSet.Contains(uncollectedGemTile)
            && !assignedSPOGemSet.Contains(uncollectedGemTile))
        {
            MoveSPOToCorner(GetDirectionString());
            assignedSPOGemSet.Add(uncollectedGemTile);
        }

        if (Input.GetKeyDown(KeyCode.W))
                MoveTopRight();
            else if (Input.GetKeyDown(KeyCode.A))
                MoveTopLeft();
            else if (Input.GetKeyDown(KeyCode.S))
                MoveBottomLeft();
            else if (Input.GetKeyDown(KeyCode.D))
                MoveBottomRight();
    }

    public void MoveTopRight() => Move(new Vector2Int(0, 1));
    public void MoveBottomLeft() => Move(new Vector2Int(0, -1));
    public void MoveTopLeft() => Move(new Vector2Int(-1, 0));
    public void MoveBottomRight() => Move(new Vector2Int(1, 0));

    public void Move(Vector2Int direction)
    {
        Vector3Int targetGridPos = currentGridPos + new Vector3Int(direction.x, direction.y, 0);

        if (CanMove(targetGridPos))
        {
            currentGridPos = targetGridPos;
            Debug.Log("Current Grid Pos" + currentGridPos);
            PlayerControllerManager.Instance.SavedGridPosition = currentGridPos;

            Vector3 cellCenter = ground_tilemap.GetCellCenterWorld(currentGridPos);

            if (gem_tilemap.HasTile(currentGridPos))
                cellCenter.x -= 3f;

            transform.position = cellCenter;

            if (gem_tilemap.HasTile(currentGridPos)) // checks if the gem is being picked up
            {
                CollectGem(currentGridPos);
            }

            // check if player is on either the 2 spots (gridPos1 or gridPos2)
            checkInPosition();
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
            assignedSPOGemSet.Remove(gemTile); // Remove from assigned SPO set

            // Activate corresponding sticker
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

    // checks if player is in the 2 positions of the grid
    private void checkInPosition()
    {
        if (selectedMap == MapSelection.Map1)
        {
            // if (currentGridPos == gridPos1)
            // {
            //     Debug.Log("Found in Position 1");
            //     MoveSPOToCorner("topright");
            // }
            if (currentGridPos == gridPos2)
            {
                Debug.Log("Found in Position 2");
                MoveSPOToCorner("bottomleft");
            }
            else
            {
                return;
            }
        }
        else if (selectedMap == MapSelection.Map2)
        {
            // if (currentGridPos == gridPos1)
            // {
            //     Debug.Log("Found in Position 1");
            //     MoveSPOToCorner("topright");
            // }
            if (currentGridPos == gridPos2)
            {
                Debug.Log("Found in Position 2");
                MoveSPOToCorner("topleft");
            }
            else
            {
                return;
            }
        }
    }

    private void moveSPOAtSpawn()
    {
        MoveSPOToCorner("topright");
    }

    private GameObject findSPOByName(string name)
    {
        GameObject[] allSPOs = { SPO1, SPO2, SPO3, SPO4 };
        foreach (GameObject spo in allSPOs)
        {
            if (spo != null && spo.name == name)
            {
                return spo;
            }
        }
        return null; // No SPO found with the given name
    }


    public string GetSPOForDirection(Vector3Int direction)
    {
        if (directionToSPO.TryGetValue(direction, out string spoName))
        {
            return spoName;
        }
        return null; // No SPO assigned for this direction
    }

    private string GetDirectionString()
    {
        // Only proceed if next to an uncollected gem
        var uncollectedGemPos = GetUncollectedAdjacentGem(currentGridPos);

        // Determine direction name based on the uncollected gem position
        Vector3Int dir = uncollectedGemPos.Value - currentGridPos;
        string dirName = null;
        if (dir == Vector3Int.up) dirName = "topright";
        else if (dir == Vector3Int.down) dirName = "bottomleft";
        else if (dir == Vector3Int.left) dirName = "topleft";
        else if (dir == Vector3Int.right) dirName = "bottomright";
        else dirName = null;
        return dirName;
    }

    private void MoveSPOToCorner(string dirName)
    {
        if (SPOOrder.Count == 0) return;

        string nextSPOName = SPOOrder.Dequeue(); // Get and remove the first SPO
        GameObject nextSPO = findSPOByName(nextSPOName);
        if (nextSPO == null)
        {
            Debug.LogWarning($"[SPO MOVE] SPO GameObject '{nextSPOName}' not found.");
            return;
        }

        Vector3 targetPos;
        Vector3 prevPos;
        GameObject SPOToSwitch;
        switch (dirName)
        {
            case "topright":
                targetPos = toprightPos;
                prevPos = nextSPO.transform.position; // store the next spo positions temporarily
                SPOToSwitch = GetSPOInPosition(targetPos);
                nextSPO.transform.position = targetPos;
                SPOToSwitch.transform.position = prevPos;
                break;
            case "bottomleft":
                targetPos = bottomleftPos;
                prevPos = nextSPO.transform.position;
                SPOToSwitch = GetSPOInPosition(targetPos);
                nextSPO.transform.position = targetPos;
                SPOToSwitch.transform.position = prevPos;
                break;
            case "topleft":
                targetPos = topleftPos;
                prevPos = nextSPO.transform.position;
                SPOToSwitch = GetSPOInPosition(targetPos);
                nextSPO.transform.position = targetPos;
                SPOToSwitch.transform.position = prevPos;
                break;
            case "bottomright":
                targetPos = bottomrightPos; 
                prevPos = nextSPO.transform.position;
                SPOToSwitch = GetSPOInPosition(targetPos);
                nextSPO.transform.position = targetPos;
                SPOToSwitch.transform.position = prevPos;
                break;
            default:
                Debug.LogWarning("[SPO MOVE] Invalid direction name.");
                return;
        }

        Debug.Log($"[SPO MOVE] Moved {nextSPOName} to {dirName} corner at {targetPos}");
    }

    // determines what SPO is in specified corner.
    private GameObject GetSPOInPosition(Vector3 position)
    {
        Vector3 bottomLeftPos = SPO4.transform.position;
        Vector3 topRightPos = SPO3.transform.position;
        Vector3 topLeftPos = SPO2.transform.position;
        Vector3 bottomRightPos = SPO1.transform.position;

        if (position == bottomLeftPos)
        {
            Debug.Log("SPO 4 found");
            return GameObject.Find("SPO 4");
        }
        else if (position == topRightPos)
        {
            Debug.Log("SPO 3 found");
            return GameObject.Find("SPO 3");
        }
        else if (position == topLeftPos)
        {
            Debug.Log("SPO 2 found");
            return GameObject.Find("SPO 2");
        }
        else if (position == bottomRightPos)
        {
            Debug.Log("SPO 1 found");
            return GameObject.Find("SPO 1");
        }
        Debug.Log("No SPO replacement found...");
        return null;
    }

    private Vector3Int? GetUncollectedAdjacentGem(Vector3Int gridPos)
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
            TileBase tile = gem_tilemap.GetTile(pos);
            if (tile != null && !collectedGemSet.Contains(tile))
            {
                return pos;
            }
        }
        return null;
    }
        

}