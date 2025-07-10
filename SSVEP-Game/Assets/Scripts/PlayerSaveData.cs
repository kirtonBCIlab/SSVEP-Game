using System;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;

[Serializable]
public class PlayerSaveData
{
    public Vector3Int prev_pos;
    public Vector3Int new_pos;
    public float spo_selected;
    public string movement_dir;
    public bool special_pos;

    public SaveStruct ToStruct()
    {
        return new SaveStruct(prev_pos, new_pos, spo_selected, movement_dir, special_pos);
    }

    public void FromPlayerController(PlayerController controller)
    {
        prev_pos = controller.PrevPos;
        new_pos = controller.NewPos;
        spo_selected = controller.SpoSelected;
        movement_dir = controller.MovementDir;
        special_pos = controller.SpecialPos;
    }

    public static void SaveListToCSV(List<SaveStruct> dataList)
    {
        string path = "C:\\Users\\admin\\Documents\\SSVEP-Game\\SSVEP-Game\\Assets\\Saves\\movements.csv";
        StringBuilder csvContent = new StringBuilder();

        // Header
        csvContent.AppendLine("prev_pos,new_pos,spo_selected,movement_dir,special_pos");

        // Data
        foreach (var data in dataList)
        {
            //SaveStruct s = data.ToStruct();
            csvContent.AppendLine(data.ToString());
        }

        File.WriteAllText(path, csvContent.ToString());
        Debug.Log($"CSV saved to: {path}");
    }
}
