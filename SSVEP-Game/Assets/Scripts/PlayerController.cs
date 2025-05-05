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
        currentGridPos = ground_tilemap.WorldToCell(transform.position);
        transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);
    }

    private void Update()
    {
        Vector2Int moveDirection = Vector2Int.zero;

        if (Input.GetKeyDown(KeyCode.W)) // Up-Right
            moveDirection = new Vector2Int(1, 1);
        else if (Input.GetKeyDown(KeyCode.A)) // Up-Left
            moveDirection = new Vector2Int(-1, 1);
        else if (Input.GetKeyDown(KeyCode.S)) // Down-Right
            moveDirection = new Vector2Int(1, -1);
        else if (Input.GetKeyDown(KeyCode.D)) // Down-Left
            moveDirection = new Vector2Int(-1, -1);

        if (moveDirection != Vector2Int.zero)
        {
            Move(moveDirection);
        }
    }

    private void Move(Vector2Int direction)
    {
        Vector3Int targetGridPos = currentGridPos + new Vector3Int(direction.x, direction.y, 0);

        if (CanMove(targetGridPos))
        {
            currentGridPos = targetGridPos;
            transform.position = ground_tilemap.GetCellCenterWorld(currentGridPos);
        }
    }

    private bool CanMove(Vector3Int targetGridPos)
    {
        TileBase tile = ground_tilemap.GetTile(targetGridPos);
        return tile != null;
    }
}
