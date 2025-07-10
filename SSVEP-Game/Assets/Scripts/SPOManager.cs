using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System.Runtime.ExceptionServices;

public class SPOManager : MonoBehaviour
{
    private GameObject SPO1, SPO2, SPO3, SPO4;
    private HashSet<TileBase> assignedTiles = new HashSet<TileBase>();
    private Queue<string> spoQueue = new Queue<string>();
    private Dictionary<string, Vector3> positions = new Dictionary<string, Vector3>();
    private string currentSPOName = null;

    public void Initialize()
    {
        SPO1 = GameObject.Find("SPO 1");
        SPO2 = GameObject.Find("SPO 2");
        SPO3 = GameObject.Find("SPO 3");
        SPO4 = GameObject.Find("SPO 4");

        if (!SPO1 || !SPO2 || !SPO3 || !SPO4)
        {
            Debug.LogError("SPOs not found in scene.");
            return;
        }

        for (int i = 0; i < 4; i++)
        {
            spoQueue.Enqueue("SPO 1");
            spoQueue.Enqueue("SPO 2");
            spoQueue.Enqueue("SPO 3");
            spoQueue.Enqueue("SPO 4");
        }

        positions["topleft"] = new Vector3(16, 16, 0);
        positions["topright"] = new Vector3(130, 16, 0);
        positions["bottomleft"] = new Vector3(16, -36, 0);
        positions["bottomright"] = new Vector3(130, -36, 0);

        ForceMoveSPO("topright"); // First position for both maps
    }

    public void TryMoveSPO(TileBase tile, Vector3Int direction)
    {
        if (assignedTiles.Contains(tile)) return;

        string dir = DirectionToName(direction);
        if (string.IsNullOrEmpty(dir)) return;

        if (currentSPOName == null)
        {
            if (spoQueue.Count == 0) return;
            currentSPOName = spoQueue.Dequeue();
        }

        MoveCurrentSPOTo(dir);
    }

    public void AssignCurrentSPO(TileBase collectedTile)
    {
        if (currentSPOName == null)
        {
            return;
        }

        Debug.Log($"Assigning gem to SPO: {currentSPOName}");
        assignedTiles.Add(collectedTile);
        currentSPOName = null;
    }

    public void AssignCurrentSpecialSPO()
    {
        if (currentSPOName == null)
        {
            return;
        }

        Debug.Log($"Special movement completed, used {currentSPOName}");
        currentSPOName = null;
    }


    public void ForceMoveSPO(string dir)
    {
        if (spoQueue.Count == 0 || !positions.ContainsKey(dir)) return;

        string spoName = spoQueue.Dequeue();

        if (dir == "topright") // This is the starting step in both maps
        {
            Debug.Log("First position, SPO 1 to topright");
            currentSPOName = null;
            MoveSPOTo(spoName, dir);
        }

        if (dir == "bottomleft" || dir == "topleft") // These are the special movements for maps 1 and 2 respectively
        {
            currentSPOName = spoName;
            MoveCurrentSPOTo(dir);
        }
    }

    private void MoveCurrentSPOTo(string dir)
    {
        if (string.IsNullOrEmpty(currentSPOName)) return;
        MoveSPOTo(currentSPOName, dir);
    }

    private void MoveSPOTo(string spoName, string dir)
    {
        if (!positions.ContainsKey(dir)) return;

        GameObject movingSPO = GameObject.Find(spoName);
        Vector3 targetPos = positions[dir];

        GameObject swapSPO = FindSPOAt(targetPos);
        if (movingSPO && swapSPO && movingSPO != swapSPO)
        {
            Vector3 temp = movingSPO.transform.position;
            movingSPO.transform.position = targetPos;
            swapSPO.transform.position = temp;

            Debug.Log($"Moving SPO '{currentSPOName}' to {dir}");
        }
        else if (movingSPO)
        {
            movingSPO.transform.position = targetPos;
        }
    }

    private GameObject FindSPOAt(Vector3 pos)
    {
        foreach (var name in new[] { "SPO 1", "SPO 2", "SPO 3", "SPO 4" })
        {
            GameObject spo = GameObject.Find(name);
            if (spo && spo.transform.position == pos)
                return spo;
        }
        return null;
    }

    private string DirectionToName(Vector3Int dir)
    {
        if (dir == Vector3Int.up) return "topright";
        if (dir == Vector3Int.down) return "bottomleft";
        if (dir == Vector3Int.left) return "topleft";
        if (dir == Vector3Int.right) return "bottomright";
        return null;
    }

    public string GetSPOCorner(GameObject spo)
    {
        if (spo == null) return null;

        foreach (var kvp in positions)
        {
            if (spo.transform.position == kvp.Value)
                return kvp.Key;
        }

        return null;
    }
     public string GetSPONameFromCorner(string corner)
    {
        if (!positions.ContainsKey(corner)) return null;

        Vector3 targetPosition = positions[corner];

        foreach (var name in new[] { "SPO 1", "SPO 2", "SPO 3", "SPO 4" })
        {
            GameObject spo = GameObject.Find(name);
            if (spo != null && spo.transform.position == targetPosition)
                return name;
        }

        return null; // No SPO found at that corner
    }


}
