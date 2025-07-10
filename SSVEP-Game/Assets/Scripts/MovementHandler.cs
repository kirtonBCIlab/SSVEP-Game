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

    public MovementHandler(
        PlayerController player,
        Tilemap ground,
        Tilemap collision,
        Tilemap gem,
        SPOManager spo,
        GemManager gemMan)
    {
        this.player = player;
        this.groundTilemap = ground;
        this.collisionTilemap = collision;
        this.gemTilemap = gem;
        this.spoManager = spo;
        this.gemManager = gemMan;
    }

    public void Move(Vector2Int direction)
    {
        Vector3Int targetPos = player.currentGridPos + new Vector3Int(direction.x, direction.y, 0);

        player.prev_pos = player.currentGridPos;

        if (CanMove(targetPos))
        {
            player.failed_movement = false;
            player.currentGridPos = targetPos;
            player.new_pos = player.currentGridPos;
            PlayerControllerManager.Instance.SavedGridPosition = player.currentGridPos;

            Vector3 cellCenter = groundTilemap.GetCellCenterWorld(player.currentGridPos);

            if (gemTilemap.HasTile(player.currentGridPos))
            {
                cellCenter.x -= 3f;
                gemManager?.TryCollectGem(player.currentGridPos);
            }
            else
                player.special_pos = false;

            player.transform.position = cellCenter;

            SetDirectionFromInput(direction);

            player.SavePlayerMovement();
            player.gem_collected = false;

            player.CheckSPOTrigger();
        }
        else
        {
            //Tried to move into a wall
            player.failed_movement = true;
            player.SavePlayerMovement();
        }
    }

    private bool CanMove(Vector3Int targetGridPos)
    {
        return groundTilemap.HasTile(targetGridPos) && !collisionTilemap.HasTile(targetGridPos);
    }

    public void MoveToSPO(string spoName)
    {
        if (spoManager == null) return;

        GameObject spo = GameObject.Find(spoName);
        if (spo == null)
        {
            Debug.LogWarning($"{spoName} not found.");
            return;
        }

        string corner = spoManager.GetSPOCorner(spo);

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

    public void MoveTopRight() => Move(new Vector2Int(0, 1));
    public void MoveBottomLeft() => Move(new Vector2Int(0, -1));
    public void MoveTopLeft() => Move(new Vector2Int(-1, 0));
    public void MoveBottomRight() => Move(new Vector2Int(1, 0));

    private void CheckSPONameFromCorner(string dir)
    {
        string spoName = spoManager.GetSPONameFromCorner(dir);
        int spoInt = int.Parse(spoName.Split(' ')[1]);
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

    public void CheckSpecialMovement()
    {
        if (player.currentGridPos == player.nextPos && player.usedGridPos) //In the position after completing the special movement
        {
            // Tell SPO manager that the special movement SPO was used and now it can dequeue the next SPO
            spoManager?.AssignCurrentSpecialSPO();
            player.special_pos = false;
        }

        if (player.usedGridPos)
        {
            player.gridPos = new Vector3Int(0, 0, -1000); //dummy number to make sure SPO movement for the special position only happens once
        }
    }
}
