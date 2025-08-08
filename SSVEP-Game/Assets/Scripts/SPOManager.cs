using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SPOManager : MonoBehaviour
{
    private GameObject SPO1, SPO2, SPO3, SPO4;

    // Stores tiles that have already been used to assign an SPO
    private HashSet<TileBase> assignedTiles = new HashSet<TileBase>();

    private Queue<string> spoQueue = new Queue<string>();

    private Dictionary<string, Vector3> positions = new Dictionary<string, Vector3>();

    // The currently active SPO that is to be placed in a corner
    private string currentSPOName = null;

    public void Initialize()
    {
        // Find and assign references to each SPO GameObject in the scene
        SPO1 = GameObject.Find("SPO 1");
        SPO2 = GameObject.Find("SPO 2");
        SPO3 = GameObject.Find("SPO 3");
        SPO4 = GameObject.Find("SPO 4");

        // If any SPOs are missing from the scene, show an error and abort initialization
        if (!SPO1 || !SPO2 || !SPO3 || !SPO4)
        {
            Debug.LogError("SPOs not found in scene.");
            return;
        }

        // Generate a new random queue of SPOs for each play of the game
        GenerateRandomSPOQueue();

        // Define the world positions of each corner
        positions["topleft"] = new Vector3(-0.5f, 0.28f, -0.1f);
        positions["topright"] = new Vector3(0.5f, 0.28f, -0.1f);
        positions["bottomleft"] = new Vector3(-0.5f, -0.28f, -0.1f);
        positions["bottomright"] = new Vector3(0.5f, -0.28f, -0.1f);

        // Move the first SPO in the queue to the "topright" corner
        ForceMoveSPO("topright"); 
    }

    // Attempt to move the current SPO to the direction based on the given tile and direction
    public void TryMoveSPO(TileBase tile, Vector3Int direction)
    {
        // Skip if the tile has already been used to assign an SPO
        if (assignedTiles.Contains(tile)) return;

        // Convert direction to a named corner
        if (string.IsNullOrEmpty(DirectionToName(direction))) return;

        // If no SPO is currently active, dequeue the next one from the queue
        if (currentSPOName == null)
        {
            if (spoQueue.Count == 0) return;
            currentSPOName = spoQueue.Dequeue();
        }

        // Move the active SPO to the correct corner
        StartCoroutine(MoveCurrentSPOTo(DirectionToName(direction)));
    }

    // Assign the current SPO to a collected tile and reset current SPO tracking
    public void AssignCurrentSPO(TileBase collectedTile)
    {
        if (currentSPOName == null) return;

        // Mark the tile as used so it's not reused
        assignedTiles.Add(collectedTile);

        // Reset current SPO
        currentSPOName = null;
    }

    // Special assignment (e.g. manually triggered)
    public void AssignCurrentSpecialSPO()
    {
        if (currentSPOName == null) return;
        currentSPOName = null;
    }

    // Forcefully move the next SPO in the queue to a specific corner
    public void ForceMoveSPO(string dir)
    {
        // Skip if the queue is empty or the direction is invalid
        if (spoQueue.Count == 0 || !positions.ContainsKey(dir)) return;

        // Move the next SPO in the queue directly to "topright" as the starting position for both maps
        if (dir == "topright")
        {
            currentSPOName = null;
            MoveSPOTo(spoQueue.Dequeue(), dir);
        }

        // If this is a special movement corner, set it as the current SPO and move it
        if (dir == "bottomleft" || dir == "topleft")
        {
            currentSPOName = spoQueue.Dequeue();
            StartCoroutine(MoveCurrentSPOTo(dir));
        }
    }

    // Move the current SPO to a direction
    private IEnumerator MoveCurrentSPOTo(string dir)
    {
        if (string.IsNullOrEmpty(currentSPOName)) Debug.Log("Current SPO Name is Null");

        // Wait for 1 second before moving the SPOs so that the correct feedback is visible
        yield return new WaitForSecondsRealtime(1f);
        MoveSPOTo(currentSPOName, dir);
    }

    // Move a given SPO to the specified corner
    private void MoveSPOTo(string spoName, string dir)
    {
        if (!positions.ContainsKey(dir)) return;

        GameObject movingSPO = GameObject.Find(spoName);

        // Check if another SPO is already at the target position
        GameObject swapSPO = FindSPOAt(positions[dir]);

        // If both SPOs are valid and different, swap their positions
        if (movingSPO && swapSPO && movingSPO != swapSPO)
        {
            Vector3 temp = movingSPO.transform.position;
            movingSPO.transform.position = positions[dir];
            swapSPO.transform.position = temp;
        }
        // If no swap needed, just move the current SPO
        else if (movingSPO)
        {
            movingSPO.transform.position = positions[dir];
        }
    }

    // Find which SPO (if any) is currently at the given world position
    private GameObject FindSPOAt(Vector3 pos)
    {
        foreach (var name in new[] { "SPO 1", "SPO 2", "SPO 3", "SPO 4" })
        {
            GameObject spo = GameObject.Find(name);
            if (spo && spo.transform.position == pos) return spo;
        }
        return null;
    }

    // Convert a directional input into a named corner
    private string DirectionToName(Vector3Int dir)
    {
        if (dir == Vector3Int.up) return "topright";
        if (dir == Vector3Int.down) return "bottomleft";
        if (dir == Vector3Int.left) return "topleft";
        if (dir == Vector3Int.right) return "bottomright";
        return null;
    }

    // Get the corner name where the given SPO is currently placed
    public string GetSPOCorner(GameObject spo)
    {
        if (spo == null) return null;

        foreach (var kvp in positions)
            if (spo.transform.position == kvp.Value) return kvp.Key;

        return null;
    }

    // Get the name of the SPO currently placed at the given corner
    public string GetSPONameFromCorner(string corner)
    {
        if (!positions.ContainsKey(corner)) return null;

        Vector3 targetPosition = positions[corner];

        foreach (var name in new[] { "SPO 1", "SPO 2", "SPO 3", "SPO 4" })
        {
            GameObject spo = GameObject.Find(name);
            if (spo != null && spo.transform.position == targetPosition) return name;
        }

        return null; // No SPO found at that corner
    }

    // Generate a randomized queue of SPOs (each used 3 times = 12 entries total)
    private void GenerateRandomSPOQueue()
    {
        List<string> spoList = new List<string>();

        // Add each SPO to the list 3 times
        for (int i = 0; i < 3; i++)
        {
            spoList.Add("SPO 1");
            spoList.Add("SPO 2");
            spoList.Add("SPO 3");
            spoList.Add("SPO 4");
        }

        // Shuffle the list using Fisher-Yates algorithm
        System.Random random = new System.Random();
        int n = spoList.Count;

        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            string value = spoList[k];
            spoList[k] = spoList[n];
            spoList[n] = value;
        }

        // Add each shuffled SPO to the queue
        foreach (string spo in spoList)
            spoQueue.Enqueue(spo);
    }
}
