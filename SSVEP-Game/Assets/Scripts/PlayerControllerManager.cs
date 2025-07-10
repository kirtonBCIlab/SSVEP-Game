using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerManager : MonoBehaviour
{
    public static PlayerControllerManager Instance { get; private set; }
    public Vector3Int SavedGridPosition { get; set; }
    private List<SaveStruct> movementLog = new List<SaveStruct>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LogMovement(SaveStruct movementData)
    {
        movementLog.Add(movementData); 
    }

    public void EndGame()
    {
        PlayerSaveData.SaveListToCSV(movementLog);
    }
}
