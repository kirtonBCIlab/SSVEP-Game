using System;
using UnityEngine;

[Serializable]
public struct SaveStruct
{
    public Vector3Int PrevPos { get; set; }
    public Vector3Int NewPos { get; set; }
    public float SpoSelected { get; set; }
    public string MovementDir { get; set; }
    public bool SpecialPos { get; set; }
    public bool FailedMovement { get; set; }
    public bool GemCollected { get; set; }

    public SaveStruct(Vector3Int prev, Vector3Int next, float spo, string dir, bool special, bool failed, bool gem)
    {
        PrevPos = prev;
        NewPos = next;
        SpoSelected = spo;
        MovementDir = dir;
        SpecialPos = special;
        FailedMovement = failed;
        GemCollected = gem;
    }

    public override string ToString()
    {
        return $"{FormatVector3Int(PrevPos)}," +
               $"{FormatVector3Int(NewPos)}," +
               $"{SpoSelected}," +
               $"{MovementDir}," +
               $"{SpecialPos}," +
               $"{FailedMovement}," +
               $"{GemCollected}";
    }

    private static string FormatVector3Int(Vector3Int vec)
    {
        return $"\"({vec.x},{vec.y})\"";
    }
}
