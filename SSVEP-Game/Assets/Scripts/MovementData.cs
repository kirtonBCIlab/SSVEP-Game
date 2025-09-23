using UnityEngine;

public class MovementData
{
    public static readonly string CSVHeader = string.Join(',',
        "Old Position", "New Position",
        "Intended Movement Direction",
        "Selected SPO", "Movement Blocked", "Keypress Used",
        "Special Movement Possible", "Special Movement Achieved"
    );

    public Vector2Int OldPosition;
    public Vector2Int NewPosition;
    public Vector2Int IntendedMovementDirection = Vector2Int.zero;
    public float SelectedSPO = 0;
    public bool MovementBlocked = false;
    public bool KeypressUsed = false;
    public bool SpecialMovementPossible = false;
    public bool SpecialMovementAchieved = false;


    public MovementData(Vector2Int oldPosition)
    => NewPosition = OldPosition = oldPosition;


    public override string ToString()
    => string.Join(',',
        FormatVector2Int(OldPosition),
        FormatVector2Int(NewPosition),
        FormatVector2Int(IntendedMovementDirection),
        $"{SelectedSPO:F2}", 
        MovementBlocked, KeypressUsed,
        SpecialMovementPossible, SpecialMovementAchieved
    );

    private static string FormatVector2Int(Vector2Int v)
    => $"\"({v.x},{v.y})\"";
}