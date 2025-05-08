using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Tilemap ground_tilemap;
    
    [SerializeField]
    private Tilemap collision_tilemap;

    [SerializeField]
    private Tilemap gem_tilemap;

    [SerializeField]
    private List<TileBase> gemTiles;

    [SerializeField]
    private List<TileBase> collectedTiles;

    private Dictionary<TileBase, TileBase> gemToCollectedMap;

    private Vector3Int currentGridPos;

    private void Start()
    {
        if (PlayerControllerManager.Instance != null && PlayerControllerManager.Instance.SavedGridPosition != Vector3Int.zero)
        {
            currentGridPos = PlayerControllerManager.Instance.SavedGridPosition;
        }
        else
        {
            currentGridPos = ground_tilemap.WorldToCell(transform.position);
            Debug.Log("PlayerControllerManager instance not found or no saved position. Using default position.");
        }
        // Snap player to nearest grid cell
        //currentGridPos = ground_tilemap.WorldToCell(transform.position);
        transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);

        // Build mapping between gem tiles and collected replacements
        gemToCollectedMap = new Dictionary<TileBase, TileBase>();
        for (int i = 0; i < gemTiles.Count && i < collectedTiles.Count; i++)
        {
            gemToCollectedMap[gemTiles[i]] = collectedTiles[i];
        }
    }

    //private void Update()
    //{
        //Vector2Int moveDirection = Vector2Int.zero;

        //if (Input.GetKeyDown(KeyCode.W))        // Up
        //    moveDirection = new Vector2Int(0, 1);
        //else if (Input.GetKeyDown(KeyCode.A))   // Left
        //    moveDirection = new Vector2Int(-1, 0);
        //else if (Input.GetKeyDown(KeyCode.S))   // Down
        //    moveDirection = new Vector2Int(0, -1);
        //else if (Input.GetKeyDown(KeyCode.D))   // Right
        //    moveDirection = new Vector2Int(1, 0);

        //if (moveDirection != Vector2Int.zero)
        //{
        //    Move(moveDirection);
        //}
    //}

    public void MoveUp()
    {
        Move(new Vector2Int(0, 1));
        Debug.Log("Up key pressed");
    } 
    public void MoveDown()
    {
        Move(new Vector2Int(0, -1));
        Debug.Log("Down key pressed");
    }
    public void MoveLeft() 
    {
        Move(new Vector2Int(-1, 0));
        Debug.Log("Left key pressed");
    }

    public void MoveRight()
    {
        Move(new Vector2Int(1, 0));
        Debug.Log("Right key pressed");
    }


    public void Move(Vector2Int direction)
    {
        Vector3Int targetGridPos = currentGridPos + new Vector3Int(direction.x, direction.y, 0);

        if (CanMove(targetGridPos))
        {
            currentGridPos = targetGridPos;
            PlayerControllerManager.Instance.SavedGridPosition = currentGridPos; // Save
            Debug.Log("PlayerControllerManager instance found. Saved position updated.");
            transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);

            if (gem_tilemap.HasTile(currentGridPos))
            {
                CollectGem(currentGridPos);
            }
        }
    }
    private bool CanMove(Vector3Int targetGridPos)
    {
        // Ensure the tile exists on the ground tilemap and there's no collision
        return ground_tilemap.HasTile(targetGridPos) && !collision_tilemap.HasTile(targetGridPos);
    }


    private void CollectGem(Vector3Int gridPos)
    {
        TileBase gemTile = gem_tilemap.GetTile(gridPos);

        if (gemToCollectedMap.TryGetValue(gemTile, out TileBase collectedTile))
        {
            //change tile to the critter tile
            gem_tilemap.SetTile(gridPos, collectedTile);
        }
        else
        {
            Debug.LogWarning("Gem tile found, but no replacement defined.");
        }
    }
}
