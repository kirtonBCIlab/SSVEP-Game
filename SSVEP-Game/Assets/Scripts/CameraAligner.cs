using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Camera))]
public class CameraAligner : MonoBehaviour
{
    public Tilemap tilemap;          // Reference to the Tilemap
    public float padding = 1f;       // Extra padding around the grid

    private void Start()
    {
        AlignCamera();
    }

    public void AlignCamera()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap reference is not set.");
            return;
        }

        // Get the bounds of the tilemap in cell coordinates
        BoundsInt bounds = tilemap.cellBounds;
        Vector3 cellSize = tilemap.layoutGrid.cellSize;

        int columns = bounds.size.x;
        int rows = bounds.size.y;

        // Calculate total width and height in world units
        float width = columns * cellSize.x;
        float height = rows * cellSize.y;

        // Find center in world space
        Vector3 centerCell = new Vector3(bounds.x + columns / 2f, bounds.y + rows / 2f, 0);
        Vector3 gridCenter = tilemap.layoutGrid.CellToWorld(Vector3Int.FloorToInt(centerCell)) + (Vector3)cellSize / 2f;

        // Move camera to look at center
        transform.position = new Vector3(gridCenter.x, gridCenter.y, -10f);
        transform.rotation = Quaternion.identity;

        // Adjust orthographic size to fit entire grid
        Camera cam = GetComponent<Camera>();
        if (cam.orthographic)
        {
            float verticalSize = (height / 2f) + padding;
            float horizontalSize = ((width / 2f) + padding) / cam.aspect;
            cam.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
        }
    }
}
