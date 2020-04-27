using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, DisallowMultipleComponent]
public class DisableOnBuild : MonoBehaviour
{    
    void OnDestroy()
    {
        GameObject[] toDisableOnBuild = GameObject.FindGameObjectsWithTag("DisableOnBuild");

        foreach(GameObject disableThis in toDisableOnBuild)        
            disableThis.SetActive(false);        
    }
}