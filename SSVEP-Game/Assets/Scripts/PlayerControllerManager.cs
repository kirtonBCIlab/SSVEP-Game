using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerControllerManager : MonoBehaviour
{
    public static PlayerControllerManager Instance { get; private set; }
    public static bool eventTriggered = false;

    public Vector3Int SavedGridPosition { get; set; }

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
}
