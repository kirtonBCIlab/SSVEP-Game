using System.Collections.Generic;
using JetBrains.Annotations;
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
    [SerializeField] private MapSelection selectedMap;
    [SerializeField] private GameObject grid1;
    [SerializeField] private GameObject grid2;

    private Vector3Int gridPos;
    private Vector3Int nextPos;
    private bool usedGridPos = false;
    private Vector3Int currentGridPos;

    private Vector3Int previousGridPos;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap ground_tilemap;
    [SerializeField] private Tilemap collision_tilemap;
    [SerializeField] private Tilemap gem_tilemap;
    [SerializeField] private Tilemap spawn_tilemap1;
    [SerializeField] private Tilemap spawn_tilemap2;
    private Tilemap spawnTilemap;

    private SPOManager spoManager;
    private GemManager gemManager;

    //SAVING
    private Vector3Int prev_pos;
    private Vector3Int new_pos;
    private float spo_selected = 199f;
    private string movement_dir;
    private bool special_pos;

    // Public getters for save system
    public Vector3Int PrevPos { get { return prev_pos; } }
    public Vector3Int NewPos { get { return new_pos; } }
    public float SpoSelected { get { return spo_selected; } }
    public string MovementDir { get { return movement_dir; } }
    public bool SpecialPos { get { return special_pos; } }
    public bool firstMoveCompleted = false;

    private void Start()
    {
        GameObject selectedGrid = selectedMap == MapSelection.Map1 ? grid1 : grid2;
        spawnTilemap = selectedMap == MapSelection.Map1 ? spawn_tilemap1 : spawn_tilemap2;
        gridPos = selectedMap == MapSelection.Map1 ? new Vector3Int(8, 11, 0) : new Vector3Int(13, 6, 0);
        nextPos = selectedMap == MapSelection.Map1 ? new Vector3Int(8, 10, 0) : new Vector3Int(12, 6, 0);

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

        if (Input.GetKeyDown(KeyCode.W)) MoveTopRightKeyPress();
        else if (Input.GetKeyDown(KeyCode.A)) MoveTopLeftKeyPress();
        else if (Input.GetKeyDown(KeyCode.S)) MoveBottomLeftKeyPress();
        else if (Input.GetKeyDown(KeyCode.D)) MoveBottomRightKeyPress();

        if (spawnTilemap != null)
        {
            if (currentGridPos != GetStartTilePosition(spawnTilemap))
                firstMoveCompleted = true;
        }

        CheckSpecialMovement();
    }

    public void MoveToSPO1()
    {
        MoveToSPO("SPO 1");
        spo_selected = 6.25f;
    }

    public void MoveToSPO2()
    {
        MoveToSPO("SPO 2");
        spo_selected = 10.0f;
    }

    public void MoveToSPO3()
    {
        MoveToSPO("SPO 3");
        spo_selected = 11.11f;
    }

    public void MoveToSPO4()
    {
        MoveToSPO("SPO 4");
        spo_selected = 14.28f;
    }

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

    public void MoveTopRightKeyPress()
    {
        string spoName = spoManager.GetSPONameFromCorner("topright");
        int spoInt = int.Parse(spoName.Split(' ')[1]);
        if (spoInt == 1)
            spo_selected = 6.25f;
        else if (spoInt == 2)
            spo_selected = 10.0f;
        else if (spoInt == 3)
            spo_selected = 11.11f;
        else if (spoInt == 4)
            spo_selected = 14.28f;
        else
            Debug.Log("Did not get SPO out");

        Move(new Vector2Int(0, 1));
    }

    public void MoveTopRight() => Move(new Vector2Int(0, 1));
    public void MoveBottomLeft() => Move(new Vector2Int(0, -1));
    public void MoveTopLeft() => Move(new Vector2Int(-1, 0));
    public void MoveBottomRight() => Move(new Vector2Int(1, 0));


    

    public void MoveBottomLeftKeyPress()
    {
        string spoName = spoManager.GetSPONameFromCorner("bottomleft");
        int spoInt = int.Parse(spoName.Split(' ')[1]);
        if (spoInt == 1)
            spo_selected = 6.25f;
        else if (spoInt == 2)
            spo_selected = 10.0f;
        else if (spoInt == 3)
            spo_selected = 11.11f;
        else if (spoInt == 4)
            spo_selected = 14.28f;
        else
            Debug.Log("Did not get SPO out");

        Move(new Vector2Int(0, -1));
    }

    public void MoveTopLeftKeyPress()
    {
        string spoName = spoManager.GetSPONameFromCorner("topleft");
        int spoInt = int.Parse(spoName.Split(' ')[1]);
        if (spoInt == 1)
            spo_selected = 6.25f;
        else if (spoInt == 2)
            spo_selected = 10.0f;
        else if (spoInt == 3)
            spo_selected = 11.11f;
        else if (spoInt == 4)
            spo_selected = 14.28f;
        else
            Debug.Log("Did not get SPO out");

        Move(new Vector2Int(-1, 0));
    }

    public void MoveBottomRightKeyPress()
    {
        string spoName = spoManager.GetSPONameFromCorner("bottomright");
        int spoInt = int.Parse(spoName.Split(' ')[1]);
        if (spoInt == 1)
            spo_selected = 6.25f;
        else if (spoInt == 2)
            spo_selected = 10.0f;
        else if (spoInt == 3)
            spo_selected = 11.11f;
        else if (spoInt == 4)
            spo_selected = 14.28f;
        else
            Debug.Log("Did not get SPO out");

        Move(new Vector2Int(1, 0));
    }

    public void Move(Vector2Int direction)
    {
        Vector3Int targetPos = currentGridPos + new Vector3Int(direction.x, direction.y, 0);

        prev_pos = currentGridPos; //SAVING PREV POS

        if (CanMove(targetPos))
        {
            currentGridPos = targetPos;
            new_pos = currentGridPos;  //SAVING NEW POS
            PlayerControllerManager.Instance.SavedGridPosition = currentGridPos;

            Vector3 cellCenter = ground_tilemap.GetCellCenterWorld(currentGridPos);

            if (gem_tilemap.HasTile(currentGridPos))
            {
                cellCenter.x -= 3f;
                special_pos = true;
            }
            else
                special_pos = false;

            transform.position = cellCenter;

            //Get movement direction
            if (direction == new Vector2Int(0, 1))
                movement_dir = "topRight";
            else if (direction == new Vector2Int(0, -1))
                movement_dir = "bottomLeft";
            else if (direction == new Vector2Int(1, 0))
                movement_dir = "bottomRight";
            else if (direction == new Vector2Int(-1, 0))
                movement_dir = "topLeft";

            //Save movement
            //[prev tile, new tile, spo_selected(freq), movement_direction, special_pos(FLAG)]
            SavePlayerData();

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
            usedGridPos = true;
        }
    }

    private void CheckSpecialMovement()
    {
        if (currentGridPos == nextPos && usedGridPos) //In the position after completing the special movement
        {
            // Tell SPO manager that the special movement SPO was used and now it can dequeue the next SPO
            spoManager?.AssignCurrentSpecialSPO();
        }

        if (usedGridPos)
        {
            gridPos = new Vector3Int(0, 0, -1000); //dummy number to make sure SPO movement for the special position only happens once
        }
    }

    private void SavePlayerData()
    {
        if (!firstMoveCompleted)
            spo_selected = 6.25f; //first movement always uses SPO 0

        PlayerSaveData saveData = new PlayerSaveData();
        saveData.FromPlayerController(this);

        // Print each variable (replace with actual property/field names)
        Debug.Log($"PrevPos: {saveData.prev_pos}");
        Debug.Log($"NewPos: {saveData.new_pos}");
        Debug.Log($"SpoSelected: {saveData.spo_selected}");
        Debug.Log($"MovementDir: {saveData.movement_dir}");
        Debug.Log($"SpecialPos: {saveData.special_pos}");

        SaveStruct savedStruct = saveData.ToStruct();
        PlayerControllerManager.Instance.LogMovement(savedStruct);
    }

    public void EndGame()
    {
        if (PlayerControllerManager.Instance != null)
        {
            PlayerControllerManager.Instance.EndGame();
        }
        else
            Debug.Log("Save failed becuase there is no playercontrollermanager instance");
    }
}
