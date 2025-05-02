using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class PlayerController : MonoBehaviour
{

    private PlayerMovement playerMovement;

    [SerializeField]
    private Tilemap ground_tilemap;

    [SerializeField]
    private Tilemap collision_tilemap;


    private void Awake()
    {
        playerMovement = new PlayerMovement();
    }

    private void OnEnable()
    {
        playerMovement.Enable();
    }

    private void OnDisable()
    {
        playerMovement.Disable();
    }
    
    void Start()
    {
        playerMovement.Main.Movement.performed += ctx => Move(ctx.ReadValue<Vector2>());   
    }

    private void Move(Vector2 direction)
    {
        if (CanMove(direction))
        {

            transform.position += (Vector3)direction;
        }
    }

    private bool CanMove(Vector3 direction)
    {
       Vector3Int gridPosition = ground_tilemap.WorldToCell(transform.position + (Vector3)direction);
       if(!ground_tilemap.HasTile(gridPosition) || collision_tilemap.HasTile(gridPosition))
           return false;
        return true;
    }

}
