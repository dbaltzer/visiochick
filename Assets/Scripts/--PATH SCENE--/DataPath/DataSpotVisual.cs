using System;
using UnityEngine;

[ExecuteAlways]
public class DataSpotVisual : MonoBehaviour {
    #region FIELDS

    [SerializeField] private ProjectOntoGround _projectOntoGround;
    [SerializeField] private DataSpot _dataSpot;

    #endregion


    private void Start() {
        Debug.Log("DataSpotVisual.Start();");
        // get components
        _dataSpot ??= GetComponent<DataSpot>();
        _projectOntoGround = _dataSpot.subPath.dataPath.gameObject.GetComponent<ProjectOntoGround>();
        
        // subscribe to events
        _projectOntoGround.OnPathGeometryChanged += UpdatePosition;
        //EditorApplication.update += UpdatePosition;
    }

    private void UpdatePosition(object sender, EventArgs e) {
        
        Debug.Log("DataSpot.UpdatePosition"); // BUG
        
        // get line index from subPath
        var startIndex = _dataSpot.subPath.subPathPoints[0].globalIndex;
        
        // get position of this point (globalIndex == bezierPath.anchorPoint)
        var pointLocalPosition = _dataSpot.subPath.dataPath.bezierPath.GetAnchorPointLocalPos(startIndex);
        var pointGlobalPosition = pointLocalPosition + _dataSpot.subPath.dataPath.transform.position;
        
        // set this position
        transform.position = pointGlobalPosition;
    }
    
    public void UpdatePosition() {
        
        Debug.Log("DataSpot.UpdatePosition"); // BUG
        
        // get line index from subPath
        var startIndex = _dataSpot.subPath.subPathPoints[0].globalIndex;
        
        // get position of this point (globalIndex == bezierPath.anchorPoint)
        var pointLocalPosition = _dataSpot.subPath.dataPath.bezierPath.GetAnchorPointLocalPos(startIndex);
        var pointGlobalPosition = pointLocalPosition + _dataSpot.subPath.dataPath.transform.position;
        
        // set this position
        transform.position = pointGlobalPosition;
    }
    
    
}