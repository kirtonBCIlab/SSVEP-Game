using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class GridPresenter : MonoBehaviour
{
    public GridGenerator gridGenerator;
    
    // Start is called before the first frame update
    void Start()
    {
        gridGenerator.GenerateGridShape(); // Generate the fan shape
    }

}
