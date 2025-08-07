using UnityEngine;
using UnityEngine.Tilemaps;

public class MovementHandler
{
    private PlayerController player;
    private Tilemap groundTilemap;
    private Tilemap collisionTilemap;
    private Tilemap gemTilemap;
    private SPOManager spoManager;
    private GemManager gemManager;

    // Constructor to initialize the movement handler with necessary references
    public MovementHandler(
        PlayerController player,
        Tilemap ground,
        Tilemap collision,
        Tilemap gem,
        SPOManager spo,
        GemManager gemMan)
    {
        this.player = player;
        groundTilemap = ground;
        collisionTilemap = collision;
        gemTilemap = gem;
        spoManager = spo;
        gemManager = gemMan;
    }

    // Attempts to move the player in the given direction if the move is valid
    public void Move(Vector2Int direction)
    {
        Vector3Int targetPos = player.currentGridPos + new Vector3Int(direction.x, direction.y, 0);

        // Save current position for saving
        player.prev_pos = player.currentGridPos;

        if (CanMove(targetPos))
        {
            // Saving parameters
            player.failed_movement = false;
            player.currentGridPos = targetPos;
            player.new_pos = player.currentGridPos;
            PlayerControllerManager.Instance.SavedGridPosition = player.currentGridPos;

            // Get the center of the target tile
            Vector3 cellCenter = groundTilemap.GetCellCenterWorld(player.currentGridPos);

            // Handle gem collection
            if (gemTilemap.HasTile(player.currentGridPos))
            {
                cellCenter.x -= 0.025f; // Edit cell center so the animal tile is visible
                gemManager?.TryCollectGem(player.currentGridPos);
            }
            else
                player.special_pos = false;

            // Move player to the center of the target tile
            player.transform.position = cellCenter;

            // Using WASD or SPOs
            SetDirectionFromInput(direction);

            player.SavePlayerMovement();
            player.gem_collected = false; // Reset this flag

            // Check if player is now on the 2nd special position
            player.CheckSPOTrigger();
        }
        else
        {
            // Move failed due to collision or invalid tile
            player.failed_movement = true;
            player.SavePlayerMovement();
        }
    }

    // Checks if the player can move to the target grid position.
    private bool CanMove(Vector3Int targetGridPos)
    {
        return groundTilemap.HasTile(targetGridPos) && !collisionTilemap.HasTile(targetGridPos);
    }

    // Moves the player toward an SPO location by its name.
    public void MoveToSPO(string spoName)
    {
        if (spoManager == null) return;

        GameObject spo = GameObject.Find(spoName);
        if (spo == null)
        {
            Debug.LogWarning($"{spoName} not found.");
            return;
        }

        switch  (spoManager.GetSPOCorner(spo))
        {
            case "topright":    MoveTopRight();    break;
            case "bottomright": MoveBottomRight(); break;
            case "topleft":     MoveTopLeft();     break;
            case "bottomleft":  MoveBottomLeft();  break;
            default:
                Debug.LogWarning($"Unknown corner for {spoName}. Defaulting to TopRight.");
                MoveTopRight();
                break;
        }
    }

    // Input-triggered movement methods
    public void MoveTopRightKeyPress()
    {
        CheckSPONameFromCorner("topright");
        Move(new Vector2Int(0, 1));
    }

    public void MoveBottomLeftKeyPress()
    {
        CheckSPONameFromCorner("bottomleft");
        Move(new Vector2Int(0, -1));
    }

    public void MoveTopLeftKeyPress()
    {
        CheckSPONameFromCorner("topleft");
        Move(new Vector2Int(-1, 0));
    }

    public void MoveBottomRightKeyPress()
    {
        CheckSPONameFromCorner("bottomright");
        Move(new Vector2Int(1, 0));
    }

    // Directional movement shortcuts
    public void MoveTopRight() => Move(new Vector2Int(0, 1));
    public void MoveBottomLeft() => Move(new Vector2Int(0, -1));
    public void MoveTopLeft() => Move(new Vector2Int(-1, 0));
    public void MoveBottomRight() => Move(new Vector2Int(1, 0));

    // Sets the SPO frequency value based on the selected direction/corner.
    private void CheckSPONameFromCorner(string dir)
    {
        int spoInt = int.Parse(spoManager.GetSPONameFromCorner(dir).Split(' ')[1]);
        
        if (spoInt == 1)
            player.spo_selected = 6.25f;
        else if (spoInt == 2)
            player.spo_selected = 10.0f;
        else if (spoInt == 3)
            player.spo_selected = 11.11f;
        else if (spoInt == 4)
            player.spo_selected = 14.28f;
        else
            Debug.Log("Did not get SPO out");
    }

    // Sets the movement direction string for saving/logging based on the input vector.
    private void SetDirectionFromInput(Vector2Int direction)
    {
        if (direction == new Vector2Int(0, 1))
            player.movement_dir = "topRight";
        else if (direction == new Vector2Int(0, -1))
            player.movement_dir = "bottomLeft";
        else if (direction == new Vector2Int(1, 0))
            player.movement_dir = "bottomRight";
        else if (direction == new Vector2Int(-1, 0))
            player.movement_dir = "topLeft";
        else
            Debug.Log("Could not set direction");
    }

    // Handles post-special-movement logic, ensuring SPO tasks complete properly.
    public void CheckSpecialMovement()
    {
        if (player.currentGridPos == player.nextPos && player.usedGridPos)
        {
            spoManager?.AssignCurrentSpecialSPO(); // Notify SPO manager
            player.special_pos = false;
        }

        // Prevent repeat processing of the same special movement
        if (player.usedGridPos)
            player.gridPos = new Vector3Int(0, 0, -1000); // Dummy value
    }
}
