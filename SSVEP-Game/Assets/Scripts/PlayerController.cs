using UnityEngine;
using UnityEngine.Tilemaps;

public enum MapSelection
{
    Map1,
    Map2
}
public class PlayerController : MonoBehaviour
{
    #region Inspector Fields
    [Header("Map Selection")]
    [SerializeField] private MapSelection selectedMap;
    [SerializeField] private GameObject grid1;
    [SerializeField] private GameObject grid2;

    //public int gridActive;
    public GameObject selectedGrid;

    [Header("Tilemaps")]
    [SerializeField] private Tilemap spawnTilemap1;
    [SerializeField] private Tilemap spawnTilemap2;
    #endregion

    #region Tilemaps
    private Tilemap groundTilemap;
    private Tilemap collisionTilemap;
    private Tilemap gemTilemap;
    private Tilemap spawnTilemap;
    #endregion

    #region Position State
    public Vector3Int currentGridPos;
    private Vector3Int previousGridPos;
    public Vector3Int gridPos;
    public Vector3Int nextPos;
    public bool usedGridPos = false;
    #endregion

    #region Save Data
    public Vector3Int prev_pos;
    public Vector3Int new_pos;
    public float spo_selected = 199f;
    public string movement_dir;
    public bool special_pos;
    public bool failed_movement;
    public bool gem_collected;
    public bool firstMoveCompleted = false;

    // Public getters for save system
    public Vector3Int PrevPos => prev_pos;
    public Vector3Int NewPos => new_pos;
    public float SpoSelected => spo_selected;
    public string MovementDir => movement_dir;
    public bool SpecialPos => special_pos;
    public bool FailedMovement => failed_movement;
    public bool GemCollected => gem_collected;
    #endregion

    #region Managers
    public SPOManager spoManager;
    public GemManager gemManager;
    private MovementHandler movementHandler;
    #endregion

    private void Start()
    {
        InitializeMap();
        InitializeManagers();
        InitializePlayerPosition();
    }

    private void InitializeMap()
    {
        selectedGrid = selectedMap == MapSelection.Map1 ? grid1 : grid2;
        spawnTilemap = selectedMap == MapSelection.Map1 ? spawnTilemap1 : spawnTilemap2;
        gridPos = selectedMap == MapSelection.Map1 ? new Vector3Int(8, 11, 0) : new Vector3Int(13, 6, 0);
        nextPos = selectedMap == MapSelection.Map1 ? new Vector3Int(8, 10, 0) : new Vector3Int(12, 6, 0);

        if (selectedGrid == null || spawnTilemap == null)
        {
            Debug.LogError("Grid or spawn tilemap not assigned!");
            return;
        }

        selectedGrid.SetActive(true);
        groundTilemap = selectedGrid.transform.Find("Tilemap Ground").GetComponent<Tilemap>();
        collisionTilemap = selectedGrid.transform.Find("Tilemap Collision").GetComponent<Tilemap>();
        gemTilemap = selectedGrid.transform.Find("Tilemap Gems").GetComponent<Tilemap>();
    }

    private void InitializeManagers()
    {
        spoManager = GetComponent<SPOManager>();
        spoManager?.Initialize();

        gemManager = GetComponent<GemManager>();
        gemManager?.Initialize(gemTilemap, transform, spoManager);

        movementHandler = new MovementHandler(this, groundTilemap, collisionTilemap, gemTilemap, spoManager, gemManager);
    }

    private void InitializePlayerPosition()
    {
        if (PlayerControllerManager.Instance != null && PlayerControllerManager.Instance.SavedGridPosition != Vector3Int.zero)
            currentGridPos = PlayerControllerManager.Instance.SavedGridPosition;
        else
            currentGridPos = GetStartTilePosition(spawnTilemap);

        transform.position = groundTilemap.GetCellCenterWorld(currentGridPos);
        previousGridPos = gemTilemap.WorldToCell(transform.position);
    }

    private void Update()
    {
        HandleGemTrigger();
        HandleMovementInput();
        movementHandler.CheckSpecialMovement();

        if (spawnTilemap != null && currentGridPos != GetStartTilePosition(spawnTilemap))
            firstMoveCompleted = true;

        if (gemManager.collectedGemSet.Count == 10)
            EndGame();
    }

    private void HandleGemTrigger()
    {
        Vector3Int? gemPos = gemManager?.GetUncollectedAdjacentGem(currentGridPos);
        if (gemPos.HasValue)
        {
            TileBase tile = gemTilemap.GetTile(gemPos.Value);
            spoManager?.TryMoveSPO(tile, gemPos.Value - currentGridPos);
        }
    }

    private void HandleMovementInput()
    {
        if (Input.GetKeyDown(KeyCode.W)) movementHandler.MoveTopRightKeyPress();
        else if (Input.GetKeyDown(KeyCode.A)) movementHandler.MoveTopLeftKeyPress();
        else if (Input.GetKeyDown(KeyCode.S)) movementHandler.MoveBottomLeftKeyPress();
        else if (Input.GetKeyDown(KeyCode.D)) movementHandler.MoveBottomRightKeyPress();
    }

    private Vector3Int GetStartTilePosition(Tilemap tilemap)
    {
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
                return pos;
        }

        Debug.LogWarning("No tile found in Start Tilemap — using origin.");
        return Vector3Int.zero;
    }

    #region SPO Movement Methods
    public void MoveToSPO1() => MoveToSPO("SPO 1", 6.25f);
    public void MoveToSPO2() => MoveToSPO("SPO 2", 10.0f);
    public void MoveToSPO3() => MoveToSPO("SPO 3", 11.11f);
    public void MoveToSPO4() => MoveToSPO("SPO 4", 14.28f);

    private void MoveToSPO(string name, float spoValue)
    {
        movementHandler.MoveToSPO(name);
        spo_selected = spoValue;
    }
    #endregion

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
            spo_selected = 6.25f;
            special_pos = true;
        }

        Vector3Int specialStartPos = selectedMap == MapSelection.Map1 ? new Vector3Int(8, 11, 0) : new Vector3Int(13, 6, 0);
        if (prev_pos == specialStartPos)
        {
            special_pos = true;
            Debug.Log("Setting special pos");
        }

        PlayerSaveData saveData = new PlayerSaveData();
        saveData.FromPlayerController(this);

        SaveStruct savedStruct = saveData.ToStruct();
        PlayerControllerManager.Instance?.LogMovement(savedStruct);
    }

    public void EndGame()
    {
        if (PlayerControllerManager.Instance != null)
            PlayerControllerManager.Instance.EndGame();
        else
            Debug.Log("Save failed because there is no PlayerControllerManager instance.");
    }
}
