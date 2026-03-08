using UnityEngine;
using UnityEngine.Tilemaps;


public class MapGrid : MonoBehaviour
{
    public enum DirectionName
    { topright, topleft, bottomleft, bottomright }

    public Tilemap Ground;
    public Tilemap Collision;
    public Tilemap Spawn;
    public Tilemap Gems;
    public Tilemap MarkedTiles;
    public DirectionName StartingDirection;
    public DirectionName MarkedTileExitDirection;


    private void Start()
    {
        if (MarkedTiles.TryGetComponent(out TilemapRenderer renderer))
        {
            renderer.enabled = false;
        }
        else Debug.LogError("Special Movement tilemap not assigned!");
    }


    public bool CanMoveTo(Vector3Int gridPosition)
    => Ground.HasTile(gridPosition) && !Collision.HasTile(gridPosition);

    public Vector3 GetCellCentre(Vector3Int cellPosition)
    => Ground.GetCellCenterWorld(cellPosition);

    public bool HasGem(Vector3Int gridPosition)
    => Gems.HasTile(gridPosition);

    public TileBase GetGemTile(Vector3Int gridPosition)
    => Gems.GetTile(gridPosition);

    public bool HasMarkedTile(Vector3Int gridPosition)
    => MarkedTiles.HasTile(gridPosition);

    public void ClearMarkedTile(Vector3Int gridPosition)
    => MarkedTiles.SetTile(gridPosition, null);


    public void SetActive(bool active) => gameObject.SetActive(active);
}