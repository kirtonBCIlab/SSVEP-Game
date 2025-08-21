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
    [SerializeField] public MapSelection selectedMap; // Chosen map (Map1 or Map2)
    [SerializeField] private GameObject grid1;        // GameObject representing Grid for Map1
    [SerializeField] private GameObject grid2;        // GameObject representing Grid for Map2

    [SerializeField] private Sprite cape;             // Sprite for character with cape (forest map)
    [SerializeField] private Sprite scuba;            // Sprite for character with scuba goggles (underwater map)
    public GameObject selectedGrid;                   // The grid that is currently active based on selectedMap

    [Header("Tilemaps")]
    [SerializeField] private Tilemap spawnTilemap1;   // Spawn tilemap for Map1
    [SerializeField] private Tilemap spawnTilemap2;   // Spawn tilemap for Map2
    #endregion

    #region Tilemaps
    private Tilemap groundTilemap;     // Walkable ground layer
    private Tilemap collisionTilemap;  // Non-walkable (obstacle) layer
    private Tilemap gemTilemap;        // Gem locations
    private Tilemap spawnTilemap;      // Starting tile for player based on map
    #endregion

    #region Position State
    public Vector3Int currentGridPos;     // Player's current tile position
    private Vector3Int previousGridPos;   // Last recorded tile position, necessary for working together with PlayerControllerManager (position won't save if this isn't here)
    public Vector3Int gridPos;            // Trigger position to check for SPO movement
    public Vector3Int nextPos;            // Target position after first movement
    public bool usedGridPos = false;      // Whether SPO trigger tile has already been used
    public bool firstMoveCompleted = false; // Tracks if the first move has occurred
    #endregion

    #region Save Data
    public Vector3Int prev_pos;       // Position before a movement (used for saving)
    public Vector3Int new_pos;        // Position after a movement (used for saving)
    public float spo_selected = 199f; // ID or value representing which SPO was selected (default: 199)
    public string movement_dir;       // Direction the player moved
    public bool special_pos;          // Whether movement was a special SPO move
    public bool failed_movement;      // Whether the movement attempt failed
    public bool gem_collected;        // Whether a gem was collected in the move

    // Public property accessors for save system
    public Vector3Int PrevPos => prev_pos;
    public Vector3Int NewPos => new_pos;
    public float SpoSelected => spo_selected;
    public string MovementDir => movement_dir;
    public bool SpecialPos => special_pos;
    public bool FailedMovement => failed_movement;
    public bool GemCollected => gem_collected;
    #endregion

    #region Managers
    public SPOManager spoManager;         // Manages SPOs 
    public GemManager gemManager;         // Manages gem collection
    private MovementHandler movementHandler; // Handles player movement logic
    #endregion

    private SpriteRenderer playerRenderer;  // Player sprite renderer

    private void Start()
    {
        playerRenderer = GetComponent<SpriteRenderer>();    // Get the player sprite renderer

        InitializeMap();             // Setup map tiles and position
        InitializePlayer();          // Initialize player sprite
        InitializeManagers();        // Initialize SPO and Gem Managers
        InitializePlayerPosition();  // Set player starting position
    }

    // Initialize the map based on the selected option (Map1 or Map2)
    private void InitializeMap()
    {
        // Select the grid and spawn tilemap based on map choice
        selectedGrid = selectedMap == MapSelection.Map1 ? grid1 : grid2;
        spawnTilemap = selectedMap == MapSelection.Map1 ? spawnTilemap1 : spawnTilemap2;

        // Define trigger tile for SPO movement and the next step to move the player to
        gridPos = selectedMap == MapSelection.Map1 ? new Vector3Int(8, 11, 0) : new Vector3Int(13, 6, 0);
        nextPos = selectedMap == MapSelection.Map1 ? new Vector3Int(8, 10, 0) : new Vector3Int(12, 6, 0);

        // Safety check: make sure selected objects are not null
        if (selectedGrid == null || spawnTilemap == null)
        {
            Debug.LogError("Grid or spawn tilemap not assigned!");
            return;
        }

        // Enable the selected grid
        selectedGrid.SetActive(true);

        // Get references to tilemaps within the selected grid
        groundTilemap = selectedGrid.transform.Find("Tilemap Ground").GetComponent<Tilemap>();
        collisionTilemap = selectedGrid.transform.Find("Tilemap Collision").GetComponent<Tilemap>();
        gemTilemap = selectedGrid.transform.Find("Tilemap Gems").GetComponent<Tilemap>();
    }

    private void InitializePlayer()
    {
        if (selectedGrid == grid1)
            playerRenderer.sprite = cape;
        else
            playerRenderer.sprite = scuba;
    }

    // Initialize game managers and movement logic
    private void InitializeManagers()
    {
        spoManager = GetComponent<SPOManager>();
        spoManager?.Initialize(); // Initialize the SPO system

        gemManager = GetComponent<GemManager>();
        gemManager?.Initialize(gemTilemap, transform, spoManager); // Setup gem system

        movementHandler = new MovementHandler(this, groundTilemap, collisionTilemap, gemTilemap, spoManager, gemManager); // Create movement handler
    }

    // Set player position on the start tile or load from save if available
    private void InitializePlayerPosition()
    {
        if (PlayerControllerManager.Instance != null && PlayerControllerManager.Instance.SavedGridPosition != Vector3Int.zero)
            currentGridPos = PlayerControllerManager.Instance.SavedGridPosition;
        else
            currentGridPos = GetStartTilePosition(spawnTilemap);

        // Move player GameObject to the center of the spawn tile
        transform.position = groundTilemap.GetCellCenterWorld(currentGridPos);
        previousGridPos = gemTilemap.WorldToCell(transform.position);
    }

    private void Update()
    {
        HandleGemTrigger();                     // Check for adjacent gems and move SPOs if needed
        HandleMovementInput();                  // Listen for player input (WASD)
        movementHandler.CheckSpecialMovement(); // Handle special corner movement

        // Mark that the first movement has occurred (used for special state handling)
        if (spawnTilemap != null && currentGridPos != GetStartTilePosition(spawnTilemap))
            firstMoveCompleted = true;

        // End game condition: all gems collected
        if (gemManager.collectedGemSet.Count == 10)
            EndGame();
    }

    // Check for uncollected adjacent gem and trigger SPO movement if found
    private void HandleGemTrigger()
    {
        Vector3Int? gemPos = gemManager?.GetUncollectedAdjacentGem(currentGridPos);
        if (gemPos.HasValue)
        {
            TileBase tile = gemTilemap.GetTile(gemPos.Value);
            spoManager?.TryMoveSPO(tile, gemPos.Value - currentGridPos);
        }
    }

    // Listen for movement key input (WASD mapped to SPO-style diagonals)
    private void HandleMovementInput()
    {
        if      (Input.GetKeyDown(KeyCode.W)) movementHandler.MoveTopRightKeyPress();
        else if (Input.GetKeyDown(KeyCode.A)) movementHandler.MoveTopLeftKeyPress();
        else if (Input.GetKeyDown(KeyCode.S)) movementHandler.MoveBottomLeftKeyPress();
        else if (Input.GetKeyDown(KeyCode.D)) movementHandler.MoveBottomRightKeyPress();
    }

    // Find the first non-empty tile in the spawn tilemap (used if no save data)
    private Vector3Int GetStartTilePosition(Tilemap tilemap)
    {
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
            if (tilemap.HasTile(pos)) return pos;

        Debug.LogWarning("No tile found in Start Tilemap — using origin.");
        return Vector3Int.zero;
    }

    // These functions are called to move to a specific SPO and set the appropriate value for tracking
    public void MoveToSPO1() => MoveToSPO("SPO 1", 6.25f);
    public void MoveToSPO2() => MoveToSPO("SPO 2", 10.0f);
    public void MoveToSPO3() => MoveToSPO("SPO 3", 11.11f);
    public void MoveToSPO4() => MoveToSPO("SPO 4", 14.28f);

    // Internal function to move player to the given SPO GameObject
    private void MoveToSPO(string name, float spoValue)
    {
        movementHandler.MoveToSPO(name);
        spo_selected = spoValue; // Save the SPO ID/value for logging or scoring
    }

    // Check if the player is on a trigger tile for SPO movement and initiate it
    public void CheckSPOTrigger()
    {
        if (currentGridPos == gridPos)
        {
            string dir = selectedMap == MapSelection.Map1 ? "bottomleft" : "topleft";
            spoManager?.ForceMoveSPO(dir);
            usedGridPos = true;
        }
    }

    // Save the player’s movement
    public void SavePlayerMovement()
    {
        // If first movement hasn't occurred, force special position flag
        if (!firstMoveCompleted)
        {
            spo_selected = 6.25f;
            special_pos = true;
        }

        // Determine if player moved from the initial SPO trigger tile
        Vector3Int specialStartPos = selectedMap == MapSelection.Map1 ? new Vector3Int(8, 11, 0) : new Vector3Int(13, 6, 0);

        if (prev_pos == specialStartPos)
            special_pos = true;

        // Construct save data and log the movement to the controller manager
        PlayerSaveData saveData = new PlayerSaveData();
        saveData.FromPlayerController(this);
        SaveStruct savedStruct = saveData.ToStruct();
        PlayerControllerManager.Instance?.LogMovement(savedStruct);
    }

    // Handle end of game
    public void EndGame()
    {
        if (PlayerControllerManager.Instance != null)
            PlayerControllerManager.Instance.EndGame();
        else
            Debug.Log("Save failed because there is no PlayerControllerManager instance.");
    }
}
