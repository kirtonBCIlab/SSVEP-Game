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
    [SerializeField] private MapGrid grid1;
    [SerializeField] private MapGrid grid2;

    [SerializeField] private Sprite cape;             // Sprite for character with cape (forest map)
    [SerializeField] private Sprite scuba;            // Sprite for character with scuba goggles (underwater map)
    [HideInInspector] public MapGrid selectedGrid;    // The grid that is currently active based on selectedMap
    #endregion

    #region Position State
    [HideInInspector] public Vector3Int currentGridPos;         // Player's current tile position
    [HideInInspector] public bool firstMoveCompleted = false;   // Tracks if the first move has occurred
    private bool markedTileEntered = false;
    #endregion

    #region Managers
    [HideInInspector] public SPOManager spoManager;     // Manages SPOs 
    [HideInInspector] public GemManager gemManager;     // Manages gem collection
    private MovementHandler movementHandler; // Handles player movement logic
    #endregion

    private SpriteRenderer playerRenderer;  // Player sprite renderer
    private bool gameEnded = false;


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

        // Safety check: make sure selected objects are not null
        if (selectedGrid == null)
        {
            Debug.LogError("Grid not assigned!");
            return;
        }

        // Enable the selected grid
        selectedGrid.SetActive(true);
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
        spoManager?.Initialize(selectedGrid.StartingDirection.ToString()); // Initialize the SPO system

        gemManager = GetComponent<GemManager>();
        gemManager?.Initialize(selectedGrid.Gems, transform, spoManager); // Setup gem system

        movementHandler = new MovementHandler(this, selectedGrid, spoManager, gemManager); // Create movement handler
    }

    // Set player position on the start tile or load from save if available
    private void InitializePlayerPosition()
    {
        currentGridPos = GetStartTilePosition(selectedGrid.Spawn);
        MovementLogger.InitialPosition = currentGridPos;

        // Move player GameObject to the center of the spawn tile
        transform.position = selectedGrid.GetCellCentre(currentGridPos);
    }

    private void Update()
    {
        if (!firstMoveCompleted) MovementLogger.SpecialMovementPossible = true;
        HandleGemTrigger();                     // Check for adjacent gems and move SPOs if needed
        HandleMarkedTileTrigger();
        HandleMovementInput();                  // Listen for player input (WASD)

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
            MovementLogger.SpecialMovementPossible = true;
            TileBase tile = selectedGrid.GetGemTile(gemPos.Value);
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
    public void MoveToSPO1() => MoveToSPOByIndex(1);
    public void MoveToSPO2() => MoveToSPOByIndex(2);
    public void MoveToSPO3() => MoveToSPOByIndex(3);
    public void MoveToSPO4() => MoveToSPOByIndex(4);

    // Internal function to move player to the given SPO GameObject
    private void MoveToSPOByIndex(int index)
    {
        MovementLogger.SelectedSPOIndex = index;
        movementHandler.MoveToSPOByName($"SPO {index}");
    }

    // Check if the player is on a trigger tile for SPO movement and initiate it
    public void HandleMarkedTileTrigger()
    {
        if (selectedGrid.HasMarkedTile(currentGridPos))
        {
            MovementLogger.SpecialMovementPossible = true;
            if (!markedTileEntered)
            {
                markedTileEntered = true;
                spoManager?.ForceMoveSPO(selectedGrid.MarkedTileExitDirection.ToString());
            }
        }
    }

    // Handle end of game
    public void EndGame()
    {
        if (!gameEnded)
        {
            MovementLogger.SaveToFile();
            gameEnded = true;
        }
    }
}
