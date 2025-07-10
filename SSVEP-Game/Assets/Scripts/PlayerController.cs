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

    public Vector3Int gridPos;
    public Vector3Int nextPos;
    public bool usedGridPos = false;
    public Vector3Int currentGridPos;

    private Vector3Int previousGridPos;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap ground_tilemap;
    [SerializeField] private Tilemap collision_tilemap;
    [SerializeField] private Tilemap gem_tilemap;
    [SerializeField] private Tilemap spawn_tilemap1;
    [SerializeField] private Tilemap spawn_tilemap2;
    private Tilemap spawnTilemap;

    public SPOManager spoManager;
    public GemManager gemManager;
    private MovementHandler movementHandler;

    //SAVING
    public Vector3Int prev_pos;
    public Vector3Int new_pos;
    public float spo_selected = 199f;
    public string movement_dir;
    public bool special_pos;
    public bool failed_movement;
    public bool gem_collected;
    public bool firstMoveCompleted = false;

    // Public getters for save system
    public Vector3Int PrevPos { get { return prev_pos; } }
    public Vector3Int NewPos { get { return new_pos; } }
    public float SpoSelected { get { return spo_selected; } }
    public string MovementDir { get { return movement_dir; } }
    public bool SpecialPos { get { return special_pos; } }
    public bool FailedMovement {get { return failed_movement; }}
    public bool GemCollected {get { return gem_collected; }}

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

        movementHandler = new MovementHandler(this, ground_tilemap, collision_tilemap, gem_tilemap, spoManager, gemManager);

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

        if (Input.GetKeyDown(KeyCode.W)) movementHandler.MoveTopRightKeyPress();
        else if (Input.GetKeyDown(KeyCode.A)) movementHandler.MoveTopLeftKeyPress();
        else if (Input.GetKeyDown(KeyCode.S)) movementHandler.MoveBottomLeftKeyPress();
        else if (Input.GetKeyDown(KeyCode.D)) movementHandler.MoveBottomRightKeyPress();

        if (spawnTilemap != null)
        {
            if (currentGridPos != GetStartTilePosition(spawnTilemap))
                firstMoveCompleted = true;
        }

        movementHandler.CheckSpecialMovement();

        if (gemManager.collectedGemSet.Count == 10)
        {
            EndGame();
        }
    }

    public void MoveToSPO1()
    {
        movementHandler.MoveToSPO("SPO 1");
        spo_selected = 6.25f;
    }

    public void MoveToSPO2()
    {
        movementHandler.MoveToSPO("SPO 2");
        spo_selected = 10.0f;
    }

    public void MoveToSPO3()
    {
        movementHandler.MoveToSPO("SPO 3");
        spo_selected = 11.11f;
    }

    public void MoveToSPO4()
    {
        movementHandler.MoveToSPO("SPO 4");
        spo_selected = 14.28f;
    }

    public void CheckSPOTrigger()
    {
        if (currentGridPos == gridPos)
        {
            string dir = selectedMap == MapSelection.Map1 ? "bottomleft" : "topleft";
            spoManager?.ForceMoveSPO(dir);
            usedGridPos = true;
        }
    }

    public void SavePlayerMovement()
    {
        if (!firstMoveCompleted)
        {
            spo_selected = 6.25f; //first movement always uses SPO 0
            special_pos = true;
        }

        if (prev_pos == new Vector3Int(8,11,0)) //hardcoded for map 1 special position
        {
            special_pos = true;
            Debug.Log("Setting special pos");
        }

        PlayerSaveData saveData = new PlayerSaveData();
        saveData.FromPlayerController(this);

        // Print each variable (replace with actual property/field names)
        //Debug.Log($"PrevPos: {saveData.prev_pos}");
        //Debug.Log($"NewPos: {saveData.new_pos}");
        //Debug.Log($"SpoSelected: {saveData.spo_selected}");
        //Debug.Log($"MovementDir: {saveData.movement_dir}");
        //Debug.Log($"SpecialPos: {saveData.special_pos}");

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
