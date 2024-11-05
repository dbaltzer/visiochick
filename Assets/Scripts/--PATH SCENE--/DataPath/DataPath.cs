using System.Collections.Generic;
using PathCreation;
using UnityEngine;


[ExecuteInEditMode]
public class DataPath : MonoBehaviour {

    #region FIELDS

    [SerializeField] private GameObject subPathPrefab;
    [HideInInspector] public List<SubPath> subPathList;
    public List<Sprite> iconList;
    
    
    
    private DataPathVisual _dataPathVisual;
    [HideInInspector] public PathCreator pathCreator;
    [HideInInspector] public BezierPath bezierPath;
    
    private string _pathToParentDirectory;
    public List<int> vertexIndicesOfBezierPoints;
    
    #endregion

    private void Start() {
        // calculate indices
        vertexIndicesOfBezierPoints = GetVertexIndicesOfBezierPoints();
    }


    #region METHODS
    
    /// <summary>
    /// called after init of this GameObject.
    /// 1) calculates SubPaths 1.2) inits SubPathGameObject (holding dataSpot and opt. dataLine) 1.3) loads media files
    /// 2) creates bezierPath
    /// 3) calls meshGenerator to create mesh 
    /// </summary>
    /// <param name="dpl"> DataPointList. container object in which json file data was laoded</param>
    /// <param name="pathToParentDirectory"> path to folder which holds path data (typcally Resources/Assets/[PathName] </param>
    /// <param name="collapsedYAxis"> if enabled makes path 2d </param>
    /// <param name="searchDirectory"> loading media: enable if we will search for file if we cant find it in specified location</param>
    /// <param name="enableLog"> enables status log on media loading </param>
    public void BuildDataPath(DataPointList dpl, string pathToParentDirectory, bool collapsedYAxis, 
        bool searchDirectory, bool enableLog) {

        _pathToParentDirectory = pathToParentDirectory;
        _dataPathVisual = GetComponent<DataPathVisual>();
        pathCreator = GetComponent<PathCreator>();
        
        
        
        // 1) CALCULATE SUBPATHS
        var lastContent = "x";  // compare media content
        var vertPos = new List<Vector3>(); // collect positions for bezier path
        var pointIndex = 0;  // calculate absolute point index
        var subPathIndex = 0;
        
        // pass through all points
        foreach (var p in dpl.points) {
            
            vertPos.Add(p.location);    // add position
            
            
            // calculate subpaths:
            if (p.content == "") {
                // point contains no media file. is not part of any subpath
            }

            else if (p.content != lastContent) {
                // new meida file -> new subpath
                
                Debug.Log("\n");
                
                // 1.2) INIT SUBPATHGAMEOBJECT  
                var subPathGameObject = Instantiate(original: subPathPrefab, parent: transform);
                subPathGameObject.transform.localPosition = p.location;
                
                var fileName = p.content.Split("/")[^1];
                subPathGameObject.name = "SubPath #" + subPathIndex + " : " + fileName;
                
                // set dataSpot name
                var dataSpot = subPathGameObject.transform.GetChild(0).GetComponent<DataSpot>();
                dataSpot.name = "DataSpot #" + subPathIndex + " : " + fileName;

                
                // make sure videoImportManager is instantiated (might not be in edit mode)
                if (VideoImportManager.Instance == null) {
                    VideoImportManager.Instance = GameObject.FindObjectOfType<VideoImportManager>();
                }
                
                // init subpath
                var sp = subPathGameObject.AddComponent<SubPath>();
                sp.SetSubPath(
                    dataPath: this,
                    subPathPoints: new List <SubPathPoint>(),
                    dataSpot: dataSpot,
                    medium: FileLoader.LoadMediaFile(p.content, _pathToParentDirectory, // 1.3) LOAD MEDIA FILE
                    VideoImportManager.Instance.videoSourceType,
                    VideoImportManager.Instance.serverURL,
                    searchDirectory, enableLog),
                    iconIndex: LoadIcon(p.icon) 
                    );

                // add subpathpoint to new subpath
                SubPathPoint spp = new SubPathPoint(globalIndex: pointIndex, timeStamp: p.timestamp);
                sp.subPathPoints.Add(spp);
                
                // set dataSpot.subPath and dataspot.name
                dataSpot.subPath = sp;
                
                // save subpath
                subPathList.Add(sp);
                
                lastContent = p.content;

                subPathIndex++;
            }

            else if (p.content == lastContent) {
                // point belongs to last subPath!
                
                // add subpathpoint to last subpath
                SubPathPoint spp = new SubPathPoint(globalIndex: pointIndex, timeStamp: p.timestamp);
                subPathList[^1].subPathPoints.Add(spp);

                lastContent = p.content;
            }

            pointIndex++;
        }
        
        
        // CREATE BEZIERPATH
        var pathSpace = collapsedYAxis ? PathSpace.xz : PathSpace.xyz;
        pathCreator.bezierPath = new BezierPath(points: vertPos, isClosed: false, space: pathSpace);

        bezierPath = pathCreator.bezierPath;
        
        // calculate indices
        vertexIndicesOfBezierPoints = GetVertexIndicesOfBezierPoints();
        
        
        // update visual
        if(_dataPathVisual) _dataPathVisual.MakeDataPathAndSubPathLines();
        else Debug.Log("DataPath is missing MeshGenerator");
    }
    

    
    public int GetVertexPathIndexOfBezierAnchorPoint(int globalIndex) {
        return vertexIndicesOfBezierPoints[globalIndex];
        
    }

    public float GetDistanceAtPoint(int globalIndex) {
        // we dont have this function builtin to the VertexPAth class. so lets do a workaround by
        // !) get world position of vertex
        // 2) get distance by GetClosestDistanceAlongPath(worldPos)

        return pathCreator.path.GetDistanceOfVertex(GetVertexPathIndexOfBezierAnchorPoint(globalIndex));

    }
    
    
    private List<int> GetVertexIndicesOfBezierPoints() {
        var vCount = 0;
        List<int> vertexIds = new List<int> { vCount };
        for (int segmentIndex = 0; segmentIndex < bezierPath.NumSegments -1; segmentIndex++) {

            vCount += pathCreator.path.VertPerBezierSegment[segmentIndex];
            
            vertexIds.Add(vCount);
        }

        return vertexIds;
    }
    
    
    
    /// <summary>
    /// 1) load an icon into DataPath.iconList, 2) tell SubPath which icon to display
    /// </summary>
    /// <param name="rawPath"> path to icon directly copied form json file </param>
    /// <returns> index of icon in DataPath.IconList</returns>
    private int LoadIcon(string rawPath) {
        // 1) load icon into DataPath.iconList, 2) tell SubPath which icon to display

        var iconTexture = FileLoader.LoadMediaFile(rawPath, _pathToParentDirectory,
            VideoImportManager.Instance.videoSourceType, VideoImportManager.Instance.serverURL,
            enableLog: false) as Texture2D;

        if (iconTexture == null) return -1;

        var currentIcon = Sprite.Create(iconTexture, new Rect(0f, 0f, iconTexture.width, iconTexture.height),
            new Vector2(0.5f, 0.5f), 100f);
        currentIcon.name = rawPath.Split("/")[^1];
        
        // check if we already loaded icon (into DataPath Field iconList)
        foreach (var icon in iconList) {
            if (icon.name != currentIcon.name)
                continue;
            
            // ICON IS ALREADY IN LIST. return reference to this icon:
            return iconList.IndexOf(icon);
        }
        
        
        // ICON IS NOT IN LIST
        
        // add icon to list
        iconList.Add(currentIcon);

        // return reference to this icon
        return iconList.IndexOf(currentIcon);
    }

    #endregion
    
    

}