using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class PlayerSaveData
{
    public Vector3Int prev_pos;
    public Vector3Int new_pos;
    public float spo_selected;
    public string movement_dir;
    public bool special_pos;

    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "prev_pos", prev_pos },
            { "new_pos", new_pos },
            { "spo_selected", spo_selected },
            { "movement_dir", movement_dir },
            { "special_pos", special_pos }
        };
    }

    public void FromPlayerController(PlayerController controller)
    {
        prev_pos = controller.PrevPos;
        new_pos = controller.NewPos;
        spo_selected = controller.SpoSelected;
        movement_dir = controller.MovementDir;
        special_pos = controller.SpecialPos;
    }
}
