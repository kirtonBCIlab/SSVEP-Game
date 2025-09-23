using System;
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
        MovementLogger.IntendedMovementDirection = direction;
        Vector3Int targetPos = player.currentGridPos + new Vector3Int(direction.x, direction.y, 0);

        if (CanMove(targetPos))
        {
            // Saving parameters
            player.currentGridPos = targetPos;
            MovementLogger.NewPosition = targetPos;

            // Get the center of the target tile
            Vector3 cellCenter = groundTilemap.GetCellCenterWorld(player.currentGridPos);

            // Handle gem collection
            if (gemTilemap.HasTile(player.currentGridPos))
            {
                cellCenter.x -= 0.025f; // Edit cell center so the animal tile is visible
                gemManager?.TryCollectGem(player.currentGridPos);
            }

            // Move player to the center of the target tile
            player.transform.position = cellCenter;
        }
        else
        {
            // Move failed due to collision or invalid tile
            MovementLogger.MovementBlocked = true;
        }
        MovementLogger.LogCurrentMovement();
        
        // Check if player is now on the 2nd special position
        player.CheckSPOTrigger();
    }

    // Checks if the player can move to the target grid position.
    private bool CanMove(Vector3Int targetGridPos)
    {
        return groundTilemap.HasTile(targetGridPos) && !collisionTilemap.HasTile(targetGridPos);
    }

    // Moves the player toward an SPO location by its name.
    public void MoveToSPOByName(string spoName)
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
    => MoveFromKeypress("topright", MoveTopRight);
    public void MoveTopLeftKeyPress()
    => MoveFromKeypress("topleft", MoveTopLeft);
    public void MoveBottomLeftKeyPress()
    => MoveFromKeypress("bottomleft", MoveBottomLeft);
    public void MoveBottomRightKeyPress()
    => MoveFromKeypress("bottomright", MoveBottomRight);

    public void MoveFromKeypress(string directionCode, Action movementMethod)
    {
        MovementLogger.KeyPressUsed = true;
        CheckSPONameFromCorner(directionCode);
        movementMethod();
    }

    // Directional movement shortcuts
    public void MoveTopRight() => Move(new Vector2Int(0, 1));
    public void MoveBottomLeft() => Move(new Vector2Int(0, -1));
    public void MoveTopLeft() => Move(new Vector2Int(-1, 0));
    public void MoveBottomRight() => Move(new Vector2Int(1, 0));

    // Sets the SPO frequency value based on the selected direction/corner.
    private void CheckSPONameFromCorner(string dir)
    {
        string spoName = spoManager.GetSPONameFromCorner(dir);
        int spoIndex = int.Parse(spoName.Split(' ')[1]);
        MovementLogger.SelectedSPOIndex = spoIndex;
    }

    // Handles post-special-movement logic, ensuring SPO tasks complete properly.
    public void CheckSpecialMovement()
    {
        if (player.currentGridPos == player.nextPos && player.usedGridPos)
        {
            spoManager?.AssignCurrentSpecialSPO(); // Notify SPO manager
            MovementLogger.SpecialMovementPossible = false;
        }

        // Prevent repeat processing of the same special movement
        if (player.usedGridPos)
            player.gridPos = new Vector3Int(0, 0, -1000); // Dummy value
    }
}
