using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEditor;


# region DataPoint

[System.Serializable]
public class DataPoint {
    public int id;
    public Vector3 location;
    public Vector3 rotation;
    public string timestamp;
    public string content;
    public string icon;
}


[System.Serializable]
public class DataPointList {
    public List<DataPoint> points;
}

# endregion

#if UNITY_EDITOR
[ExecuteInEditMode]
public class DataPathFactory : MonoBehaviour {
    // creates DataPath objects from information in json

    private enum CoordinateSystemOrientation {
        ZUp,
        YUp,
    }

    #region FIELDS
    
    // fields visible in inspector
    [SerializeField] private VideoImportManager videoImportManager;
    
    [Space]
    [SerializeField] private GameObject dataPathPrefab;

    [Space] [Header("drag & drop json file:")] 
    [SerializeField] private TextAsset jsonFile;
    
    [Space]
    [Header("geometry import settings")]
    [SerializeField] private CoordinateSystemOrientation inputCoordinateSystem;

    [Tooltip("sets all y-values to 0")]
    [SerializeField] private bool collapseYAxis;
    [SerializeField] private Collider objectToProjectOnto;

    [Tooltip("for glasses data: 0.01")]
    [SerializeField] private float scale = 0.01f;

    
    
    [Space] [Header("file importing settings")] 
    
    [Tooltip("if we cannot find file in location provided by the json file \n " +
             "we will search for the file by its name (only works with VideoClip)")]
    [SerializeField] private bool searchDirectory;
    
    [Tooltip("print importing progress to console")]
    [SerializeField] private bool enableLog;
    

    // private fields
    private DataPointList _dataPointList;
    
    #endregion

    
    
    #region METHODS
    
    [EditorCools.Button(space: 10f)]
    public void CreateDataPath() {
        // get path to
        var pathToFolder = AssetDatabase.GetAssetPath(jsonFile);
        pathToFolder = pathToFolder.Replace("/" + jsonFile.name + ".json", ""); 

        Debug.Log("pathToFolder: " + pathToFolder);
        
        LoadData();
        InstantiateDataPath(pathToFolder);
    }
    
    /// <summary>
    /// loads data from json file and saves as DataPointList ( this._dataPointList)
    /// </summary>
    private void LoadData() {
        // Load data into DataPointList object

        if (!jsonFile) {
            Debug.LogError("<color=red>JSON file not found</color>");
            return;
        }
        
        // parse json data
        if(this._dataPointList != null) this._dataPointList.points?.Clear();
        
        this._dataPointList = JsonUtility.FromJson<DataPointList>("{\"points\":" + jsonFile.text + "}");

        
        // rescale points
        if (this.scale != 1f) {
            foreach (var p in _dataPointList.points) {
                p.location *= scale;
            }
        }
        
        // adapt input data to unity coordinate system
        switch (inputCoordinateSystem)
        {
            case CoordinateSystemOrientation.ZUp:
                // z = up -> y = up
                foreach (var p in _dataPointList.points) {
                    p.location = new Vector3(p.location.x, p.location.z, p.location.y);
                    p.rotation = new Vector3(p.rotation.x, p.rotation.z, p.rotation.y);
                }
                break;
            case CoordinateSystemOrientation.YUp:
                // do nothing
                break;
        }
        
        // optionally project to plane
        if (collapseYAxis) {
            foreach (var p in _dataPointList.points) {
                p.location = new Vector3(p.location.x, 0f, p.location.z);
            }
        }
        
        
        Debug.Log("... loading json complete.");
        
    }
    
    /// <summary>
    ///  instantiates new DataPath object as child object of DataPathFactory.
    /// calls dataPath.BuildDataPath() which creates a bezierPath and inits children of its own (SubPaths)
    /// </summary>
    /// <param name="pathToFolder"></param>
    private void InstantiateDataPath(string pathToFolder) {
        Debug.Log("Instantiating DataPath...");
        
        
        var path = Instantiate(dataPathPrefab, transform);

        path.GetComponent<DataPath>().BuildDataPath(dpl: _dataPointList, pathToParentDirectory: pathToFolder,
            collapsedYAxis: collapseYAxis, searchDirectory: searchDirectory, enableLog: enableLog);

        path.name = "DataPath : " + pathToFolder.Split("/")[^1];
        
        // set projection
        var projectOntoGround = path.GetComponent<ProjectOntoGround>();
        projectOntoGround.SetTerrain(objectToProjectOnto);
        
        Debug.Log("... " + path.name +" finished!");
    }
    
    #endregion

    
}
#endif