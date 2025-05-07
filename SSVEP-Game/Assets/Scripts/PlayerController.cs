using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private Tilemap ground_tilemap;

    private Vector3Int currentGridPos;

    private void Start()
    {
        // Snap player to nearest grid cell
        currentGridPos = ground_tilemap.WorldToCell(transform.position);
        Debug.Log("Start - Current Grid Position: " + currentGridPos);
        transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);
    }

    private void Update()
    {
        Vector2Int moveDirection = Vector2Int.zero;

        // Use standard grid directions
        if (Input.GetKeyDown(KeyCode.W))        // Up
            moveDirection = new Vector2Int(0, 1);
        else if (Input.GetKeyDown(KeyCode.A))   // Left
            moveDirection = new Vector2Int(-1, 0);
        else if (Input.GetKeyDown(KeyCode.S))   // Down
            moveDirection = new Vector2Int(0, -1);
        else if (Input.GetKeyDown(KeyCode.D))   // Right
            moveDirection = new Vector2Int(1, 0);

        if (moveDirection != Vector2Int.zero)
        {
            Move(moveDirection);
        }
    }

    private void Move(Vector2Int direction)
    {
        Vector3Int targetGridPos = currentGridPos + new Vector3Int(direction.x, direction.y, 0);
        Debug.Log("Trying to move to: " + targetGridPos);

        if (CanMove(targetGridPos))
        {
            currentGridPos = targetGridPos;
            transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);
            Debug.Log("Moved to: " + currentGridPos);
        }
        else
        {
            Debug.Log("Blocked: No tile at " + targetGridPos);
        }
    }

    private bool CanMove(Vector3Int targetGridPos)
    {
        TileBase tile = ground_tilemap.GetTile(targetGridPos);
        Debug.Log($"Tile at {targetGridPos}: {(tile != null ? tile.name : "null")}");
        return tile != null;
    }
}
