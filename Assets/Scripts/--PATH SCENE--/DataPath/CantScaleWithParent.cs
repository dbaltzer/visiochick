using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class CantScaleWithParent : MonoBehaviour {
    [FormerlySerializedAs("_targetScale")] public float targetScale = 0.5f;
    public Vector3 parentScale;
    

    private void Start() {
        // get scale of DataPath object
        parentScale = transform.parent.transform.localScale;

        var inverseScale = Vector3.one * targetScale;
        
        // rescale DataSpot objects:
        if (parentScale != Vector3.one)
            inverseScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z) * targetScale;
        
        transform.localScale = inverseScale;
    }
}
