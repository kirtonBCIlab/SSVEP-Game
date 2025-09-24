using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public static class MovementLogger
{
    public static Vector3Int InitialPosition
    { set => _current = new(new(value.x, value.y)); }
    public static Vector3Int NewPosition
    { set => _current.NewPosition = new(value.x, value.y); }
    public static Vector2Int IntendedMovementDirection
    { set => _current.IntendedMovementDirection = value; }

    public static int SelectedSPOIndex
    {
        set => _current.SelectedSPO = value switch
        {
            1 => 6.25f,
            2 => 10,
            3 => 11.11f,
            4 => 14.28f,
            _ => 0
        };
    }

    public static bool MovementBlocked
    { set => _current.MovementBlocked = value; }
    public static bool KeyPressUsed
    { set => _current.KeypressUsed = value; }
    public static bool SpecialMovementPossible
    { set => _current.SpecialMovementPossible = value; }
    public static bool SpecialMovementAchieved
    { set => _current.SpecialMovementAchieved = value; }

    private static MovementData _current;
    private static Queue<MovementData> _movementLog = new();


    public static void RegisterFirstMovement()
    => RegisterSpecialMovement(MovementType.FirstMovement);
    public static void RegisterGemCollection()
    => RegisterSpecialMovement(MovementType.Collection);
    public static void RegisterSpecialTileMovement()
    => RegisterSpecialMovement(MovementType.MarkedLocation);
    public static void RegisterSpecialMovement(MovementType type)
    {
        _current.SpecialMovementType = type;
        _current.SpecialMovementAchieved = true;
    }


    public static void LogCurrentMovement()
    {
        _movementLog.Enqueue(_current);
        _current = new(_current.NewPosition);
    }

    public static void SaveToFile()
    {
        StringBuilder csvContentBuilder = new();
        csvContentBuilder.AppendLine(MovementData.CSVHeader);

        while (_movementLog.TryDequeue(out MovementData movement))
        {
            csvContentBuilder.AppendLine(movement.ToString());
        }

        File.WriteAllText(GenerateFilePath(), csvContentBuilder.ToString());
    }


    private static string GenerateFilePath()
    {
        DirectoryInfo savesDirectory = Directory.GetParent(
            Application.dataPath
        ).CreateSubdirectory("Saves");

        int saveFileCount = savesDirectory.GetFiles().Length;
        string fileName = $"movements_#{saveFileCount + 1}.csv";

        return Path.Combine(savesDirectory.FullName, fileName);
    }
}
