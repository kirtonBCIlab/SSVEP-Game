using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BCIEssentials.StimulusObjects;
using TMPro;
using UnityEngine.Rendering;

public class GridGenerator : MonoBehaviour
{
    public Material material;
    public Color colour = Color.grey;

    public int rows = 6;
    public int columns = 6;
    public float spacing = 1.0f; //same as size of the squares so it looks like one flat sheet

    public void GenerateGridShape()
    {
        DestroyGridSegments(); // Clear previous segments before generating new ones

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                float offsetX = (columns - 1) * spacing / 2f;
                float offsetY = (rows - 1) * spacing / 2f;

                Vector3 position = new Vector3(j * spacing - offsetX, -i * spacing + offsetY, 0);
                CreateGridSegment(position);
            }
        }
        //rotate the whole fan shape 45 degrees
        transform.localRotation = Quaternion.Euler(0, 0, 45);
    }

    

    public void CreateGridSegment(Vector3 position)
    {
        Mesh fanMesh = CreateMeshSquare(1, 1);
        CreateMeshObject("FanSegment", fanMesh, 0, position);
    }

    public void DestroyGridSegments()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    private GameObject CreateMeshObject(string objectName, Mesh generatedMesh, float eulerRotation = 0, Vector3? localPosition = null)
    {
        GameObject meshObject = new GameObject(objectName);
        meshObject.transform.SetParent(transform);
        meshObject.transform.localPosition = localPosition ?? Vector3.zero;
        meshObject.transform.localEulerAngles = new Vector3(0, 0, eulerRotation);
        meshObject.transform.localScale = Vector3.one;

        MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
        meshFilter.mesh = generatedMesh;

        MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = material;
        meshRenderer.material.color = colour;

        return meshObject;
    }

    private Mesh CreateMeshSquare(float width, float height)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-width / 2, -height / 2, 0),
            new Vector3(width / 2, -height / 2, 0),
            new Vector3(-width / 2, height / 2, 0),
            new Vector3(width / 2, height / 2, 0)
        };
        mesh.vertices = vertices;

        int[] triangles = new int[6]
        {
            0, 1, 2,
            1, 3, 2
        };
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}
