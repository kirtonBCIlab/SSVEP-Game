using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAligner : MonoBehaviour
{
    public GridGenerator gridGenerator;
    public float padding = 1f;

    private void Start()
    {
        AlignCamera();
    }

    public void AlignCamera()
    {
        if (gridGenerator == null)
        {
            Debug.LogError("FanGenerator reference is not set.");
            return;
        }

        // Get grid dimensions
        float width = (gridGenerator.columns - 1) * gridGenerator.spacing;
        float height = (gridGenerator.rows - 1) * gridGenerator.spacing;

        // Calculate center point of the grid
        Vector3 gridCenter = new Vector3(0, 3, 0); // Your grid is centered at origin already

        // Set camera position to look at center
        transform.position = new Vector3(gridCenter.x, gridCenter.y, 10f); // Z = -10 for 2D camera
        transform.rotation = Quaternion.identity;
        transform.rotation = Quaternion.Euler(0, 180, 0); // Reset rotation

        // Adjust orthographic size to fit grid height (and width, maintaining aspect ratio)
        Camera cam = GetComponent<Camera>();
        if (cam.orthographic)
        {
            float verticalSize = (height / 2f) + padding;
            float horizontalSize = ((width / 2f) + padding) / cam.aspect;
            cam.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
        }
    }
}
