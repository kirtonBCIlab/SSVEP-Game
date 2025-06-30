using UnityEngine;
using UnityEngine.Tilemaps;

public enum MapSelection
{
    Map1,
    Map2
}

public class PlayerController2 : MonoBehaviour
{
    [Header("Map Selection")]
    [SerializeField] private MapSelection selectedMap;
    [SerializeField] private GameObject grid1;
    [SerializeField] private GameObject grid2;

    private Vector3Int gridPos;
    private Vector3Int currentGridPos;

    private Vector3Int previousGridPos;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap ground_tilemap;
    [SerializeField] private Tilemap collision_tilemap;
    [SerializeField] private Tilemap gem_tilemap;
    [SerializeField] private Tilemap spawn_tilemap1;
    [SerializeField] private Tilemap spawn_tilemap2;

    private SPOManager spoManager;
    private GemManager gemManager;

    private void Start()
    {
        GameObject selectedGrid = selectedMap == MapSelection.Map1 ? grid1 : grid2;
        Tilemap spawnTilemap = selectedMap == MapSelection.Map1 ? spawn_tilemap1 : spawn_tilemap2;
        gridPos = selectedMap == MapSelection.Map1 ? new Vector3Int(8, 11, 0) : new Vector3Int(13, 6, 0);

        if (selectedGrid == null || spawnTilemap == null)
        {
            Debug.LogError("Grid or spawn tilemap not assigned!");
            return;
        }

        selectedGrid.SetActive(true);
        ground_tilemap = selectedGrid.transform.Find("Tilemap Ground").GetComponent<Tilemap>();
        collision_tilemap = selectedGrid.transform.Find("Tilemap Collision").GetComponent<Tilemap>();
        gem_tilemap = selectedGrid.transform.Find("Tilemap Gems").GetComponent<Tilemap>();

        spoManager = GetComponent<SPOManager>();
        spoManager?.Initialize();

        gemManager = GetComponent<GemManager>();
        if (gemManager != null)
        {
            gemManager.Initialize(gem_tilemap, transform, spoManager); // assume GemManager has this method
        }

        if (PlayerControllerManager.Instance != null && PlayerControllerManager.Instance.SavedGridPosition != Vector3Int.zero)
        {
            currentGridPos = PlayerControllerManager.Instance.SavedGridPosition;
        }
        else
        {
            currentGridPos = GetStartTilePosition(spawnTilemap);
        }

        transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);
        previousGridPos = gem_tilemap.WorldToCell(transform.position);
    }

    private Vector3Int GetStartTilePosition(Tilemap spawnTilemap)
    {
        foreach (Vector3Int pos in spawnTilemap.cellBounds.allPositionsWithin)
        {
            if (spawnTilemap.HasTile(pos)) return pos;
        }
        Debug.LogWarning("No tile found in Start Tilemap — using origin.");
        return Vector3Int.zero;
    }

    private void Update()
    {
        Vector3Int? gemPos = gemManager?.GetUncollectedAdjacentGem(currentGridPos);
        if (gemPos.HasValue)
        {
            TileBase tile = gem_tilemap.GetTile(gemPos.Value);
            spoManager?.TryMoveSPO(tile, gemPos.Value - currentGridPos);
        }

        if (Input.GetKeyDown(KeyCode.W)) MoveTopRight();
        else if (Input.GetKeyDown(KeyCode.A)) MoveTopLeft();
        else if (Input.GetKeyDown(KeyCode.S)) MoveBottomLeft();
        else if (Input.GetKeyDown(KeyCode.D)) MoveBottomRight();
    }

    public void MoveToSPO1() => MoveToSPO("SPO 1");
    public void MoveToSPO2() => MoveToSPO("SPO 2");
    public void MoveToSPO3() => MoveToSPO("SPO 3");
    public void MoveToSPO4() => MoveToSPO("SPO 4");

    private void MoveToSPO(string spoName)
    {
        if (spoManager == null) return;

        GameObject spo = GameObject.Find(spoName);
        if (spo == null)
        {
            Debug.LogWarning($"{spoName} not found.");
            return;
        }

        string corner = spoManager.GetSPOCorner(spo);
        Debug.Log($"{spoName} is in corner: {corner}");

        switch (corner)
        {
            case "topright":
                MoveTopRight();
                break;
            case "bottomright":
                MoveBottomRight();
                break;
            case "topleft":
                MoveTopLeft();
                break;
            case "bottomleft":
                MoveBottomLeft();
                break;
            default:
                Debug.LogWarning($"Unknown corner for {spoName}. Defaulting to TopRight.");
                MoveTopRight();
                break;
        }
    }

    public void MoveTopRight() => Move(new Vector2Int(0, 1));
    public void MoveBottomLeft() => Move(new Vector2Int(0, -1));
    public void MoveTopLeft() => Move(new Vector2Int(-1, 0));
    public void MoveBottomRight() => Move(new Vector2Int(1, 0));

    public void Move(Vector2Int direction)
    {
        Vector3Int targetPos = currentGridPos + new Vector3Int(direction.x, direction.y, 0);
        if (CanMove(targetPos))
        {
            currentGridPos = targetPos;
            PlayerControllerManager.Instance.SavedGridPosition = currentGridPos;

            Vector3 cellCenter = ground_tilemap.GetCellCenterWorld(currentGridPos);
            if (gem_tilemap.HasTile(currentGridPos))
                cellCenter.x -= 3f;

            transform.position = cellCenter;

            if (gem_tilemap.HasTile(currentGridPos))
                gemManager?.TryCollectGem(currentGridPos);

            CheckSPOTrigger();
        }
    }

    private bool CanMove(Vector3Int targetGridPos)
    {
        return ground_tilemap.HasTile(targetGridPos) && !collision_tilemap.HasTile(targetGridPos);
    }

    private void CheckSPOTrigger()
    {
        if (currentGridPos == gridPos)
        {
            string dir = selectedMap == MapSelection.Map1 ? "bottomleft" : "topleft";
            spoManager?.ForceMoveSPO(dir);
            //NEED TO TELL SPO THAT THIS MOVEMENT WAS COMPLETED (USUALLY TRIGGERED BY SUCCESSFULLY COLLECTING A GEM)
        }
    }
}
