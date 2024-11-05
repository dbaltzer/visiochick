using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using PathCreation;
using Object = UnityEngine.Object;

# region SubPathPoint

[System.Serializable]
public class SubPathPoint {
    public int globalIndex;     // the points index in DataPath aka AnchorPoint in BezierPath
    public string timeStamp;    
    public DateTime AbsTime;    // = timeStamp but as DateTime
    [Tooltip("get calculated on Start()")] public double relTimeInSecs = -1;    // time passed since start of subPath
    [Tooltip("get calculated on Start()")] public float bezierPathDist = -1;    // dist on BezierPath (whole dataPath)
    [Tooltip("get calculated on Start()")] public double speed;                 // speed between this and next point
    
    /// <summary>
    /// constructor
    /// </summary>
    /// <param name="globalIndex"> index in DataPath aka index of AnchorPoint in BezierPath</param>
    /// <param name="timeStamp"> timestamp (string) </param>
    public SubPathPoint(int globalIndex, string timeStamp) {
        this.globalIndex = globalIndex;
        this.timeStamp = timeStamp;
    }
    
    public DateTime ParseAbsTime() {
        // cannot do this in the constructor bc it wont save
        AbsTime = DateTime.Parse(timeStamp);
        return AbsTime;
    }

    public double CalculateRelTime(DateTime startAbsTime) {
        
        if (AbsTime == DateTime.MinValue)
            Debug.LogError("SubPath.subPathPoints.CalculateRelTime: must run CalculateAbsTime before this");
        
        relTimeInSecs = (AbsTime - startAbsTime).TotalSeconds;
        return relTimeInSecs;
    }

    public void SetBezierPathDist(float dist) {
        bezierPathDist = dist;
    }

    public double CalculateSpeed(SubPathPoint nextPoint) {
        if (relTimeInSecs == -1 || bezierPathDist == -1)
            Debug.LogError(
                "SubPath.subPathPoints.CalculateSpeed: must set relTimeInSeconds and bezierPathDist before this");
        
        
        // last point -> stop
        if (nextPoint == null) {
            speed = Mathf.Infinity;
            return speed;
        }
        
        // calculate speed
        var dist = nextPoint.bezierPathDist - this.bezierPathDist;
        var dTime = nextPoint.relTimeInSecs - this.relTimeInSecs;
        
        
        // if points are at the same position but time elapses:
        if (dist == 0) {
            speed = 0;
            return speed;
        }

        speed = dist / dTime;

        return speed;
    }
    
}

# endregion



[System.Serializable]
public class SubPath : MonoBehaviour{

    #region FIELDS
    
    public DataPath dataPath;
    public PathCreator parentPathCreator;
    public List<SubPathPoint> subPathPoints;
    public List<double> relTimeAtLocalPoint = new List<double>(); 

    [Space]
    public DataSpot dataSpot;
    public DataLine dataLine;
    [Space]
    public Object medium;
    public int iconIndex;

    #endregion

    #region MONOBEHAVIOR_METHODS

    private void Awake() {
        // path to StreamingAssets has to be set on runtime (to be correct in any build)
        AddStreamingAssetsPathToVideoURL();

#if UNITY_EDITOR
        // test url before start. bc other objects are dependant on it at start
        if (medium is UrlContainer) TestIfURLValid();
#endif
    }


    private void Start() {
        SetSubPathPointsData();
    }

    #endregion
    
    #region METHODS

    
    #region SETUP_METHODS
    
    // constructor:
    public void SetSubPath(DataPath dataPath, List<SubPathPoint> subPathPoints, DataSpot dataSpot, Object medium,
        int iconIndex) {
        Debug.Log("set subpath for" + dataSpot.name + " .");
        this.dataPath = dataPath;
        this.parentPathCreator = dataPath.pathCreator;
        this.subPathPoints = subPathPoints;
        this.dataSpot = dataSpot;
        this.medium = medium;
        this.iconIndex = iconIndex;
        
        if (medium is UrlContainer) TestIfURLValid();
    }

    /// <summary>
    /// if video is stored in Assets/StreamingAssets. load path to StreamingAssets in runtime and add to video url path
    /// </summary>
    private void AddStreamingAssetsPathToVideoURL() {
        if (medium is not UrlContainer urlContainer) return;
        // dont do anything if we point to online address
        if (urlContainer.url.Contains("http")) return;

        
        if(urlContainer.url.Contains("StreamingAssets")) {
            // remove old path prefix
            urlContainer.url = urlContainer.url.Split("StreamingAssets/")[0];
        }
        // add path to StreamingAssets
        urlContainer.url = System.IO.Path.Combine(Application.streamingAssetsPath, urlContainer.url);
    }

    
        /// <summary>
    /// test if url-container has address to a valid video file. invalid address -> set medium = null
    /// </summary>
    private bool TestIfURLValid() {
        // NOTE: this does not test if the file is readable.
        // might throw WindowsMediaError (or equivalent) if video codec is incompatible

        if (medium is not UrlContainer urlContainer) return false;
        var urlAddress = urlContainer.url;
        bool fileFound = false;

        // a) url points to web address (server)
        if (urlAddress.Contains("http")) {
            // ↓ src: https://stackoverflow.com/a/12013240 ↓
            WebRequest webRequest = WebRequest.Create(urlAddress);
            webRequest.Timeout = 1200; // milliseconds
            webRequest.Method = "HEAD";

            HttpWebResponse response = null;

            try {
                response = (HttpWebResponse)webRequest.GetResponse();
                // found valid file
                fileFound = true;
            }
            catch (WebException webException) {
                Debug.Log("<color=red>" + urlAddress + " does not exist: " + webException.Message + "</color>");
                // no valid file
                fileFound = false;
            }
            finally {
                if (response != null)
                    response.Close();
            }
        }

        // b) url is complete and point to local file
        if (urlAddress.Contains("Assets")) {
            fileFound = System.IO.File.Exists(urlAddress);
        }

        /* TODO: this does not get used. remove
        // c) url point to local file but is not yet complete (StreamingAssets)
        if (!(urlAddress.Contains("Assets") || urlAddress.Contains("http"))) {
            var fileLocation = System.IO.Path.Combine(Application.streamingAssetsPath, urlAddress);
            fileFound = System.IO.File.Exists(fileLocation);
        }
        */

        // :) file extists
        if (fileFound) {
            Debug.Log("<color=green>" + ":)  | " + urlAddress + " found!" + "</color>");
            return true;
        }

        // :( could not find file at address
        Debug.Log("<color=red>" + ":(  | " + urlAddress + " not found." + "</color>");
        medium = null;
        return false;
    }


    
    /// <summary>
    /// SubPathPoint fields that can only be calculated on runtime (or depend on those)
    /// </summary>
    private void SetSubPathPointsData() {
        if (!dataLine) return;
        
        // calculate abs time
        for (int i = 0; i < subPathPoints.Count; i++) {
            subPathPoints[i].ParseAbsTime();
        }
        
        // calculate relTime for subPathPoints (and add to easily accessible list)
        subPathPoints[0].relTimeInSecs = 0d;
        relTimeAtLocalPoint.Add(subPathPoints[0].relTimeInSecs);
        for (int i = 1; i < subPathPoints.Count; i++) {
            relTimeAtLocalPoint.Add(subPathPoints[i].CalculateRelTime(subPathPoints[0].AbsTime));
        }
        
        // calculate dist by point
        foreach (var spp in subPathPoints) {
            spp.SetBezierPathDist(dataPath.GetDistanceAtPoint(spp.globalIndex));
        }
        
        // calculate speed
        for (int i = 0; i < subPathPoints.Count - 1; i++) {
            subPathPoints[i].CalculateSpeed(subPathPoints[i + 1]);
        }
        subPathPoints[^1].CalculateSpeed(null);
    }

    #endregion
    
    
    /// <summary> Returns timeStamp at point as DateTime </summary>
    /// <param name="index"> index of point </param>
    /// <param name="indexIsLocal"> true=use index in subPath (begins at 0), false= use global index of dataPath</param>
    public DateTime GetTimeStampOfPoint(int index, bool indexIsLocal = false) {

        // 1) test index
        var legalRange = indexIsLocal
            ? (0, subPathPoints.Count - 1)
            : (subPathPoints[0].globalIndex, subPathPoints[^1].globalIndex);

        if (index < legalRange.Item1 || index > legalRange.Item2) {
            // index out of range!!
            if (index == legalRange.Item2 + 1)
                index--; // <-  PathFollower might momentarily ask for 1 point beyond subPath. this is a dirty fix:
            else
                Debug.LogError("SubPath + " + name + " GetTimeStampOfPoint index out of range! index: " + index +
                               " range: " + legalRange);
        }


        // 2) return timespan
        var localIndex = indexIsLocal ? index : Mathf.Abs(index - subPathPoints[0].globalIndex);
        return subPathPoints[localIndex].AbsTime;
    }
    

    /// <summary>
    /// returns world position of subPathPoint
    /// </summary>
    /// <param name="spp"> any subPathPoint</param>
    /// <returns></returns>
    public Vector3 GetPointWorldPos(SubPathPoint spp) {
        return parentPathCreator.transform.position + dataPath.bezierPath.GetAnchorPointLocalPos(spp.globalIndex);
    }
    

    /// <summary>
    /// Get SubPathPoint in SubPath that is closest to a relTime value (time passed since beginning of path)
    /// </summary>
    /// <param name="relTimeInSeconds"></param>
    /// <returns></returns>
    public SubPathPoint GetSppClosestToRelTime(double relTimeInSeconds) {
        var minTimeDist = float.MaxValue;
        SubPathPoint minSpp = null;

        foreach (var spp in subPathPoints) {
            var thisTimeDist = Mathf.Abs((float)(spp.relTimeInSecs - relTimeInSeconds));
            if (thisTimeDist < minTimeDist) {
                minTimeDist = thisTimeDist;
                minSpp = spp;
            }
        }

        return minSpp;

    }
    

    /// <summary>
    /// calculate closest point in subPath to worldPos
    /// </summary>
    /// <param name="worldPoint"> Vector3 position in world </param>
    /// <returns> localIndex of point</returns>
    public int GetLocalPointIndexClosestToWorldPoint(Vector3 worldPoint) {
        var localIndex = -1;
        var minDist = float.MaxValue;
        for (int i = 0; i < subPathPoints.Count-1; i++) {
            var iDist = Vector3.Distance(GetPointWorldPos(subPathPoints[i]), worldPoint);
            if(iDist >= minDist) continue;
            localIndex = i;
            minDist = iDist;
        }

        if (localIndex == -1)
            Debug.LogError("Could not find point in " + name + " closest to WorldPosition: " + worldPoint);

        return localIndex;
    }
    
    
    /// <summary>
    /// get subPathPointByIndex (index local (in subpath) or global (in path)
    /// </summary>
    /// <param name="index"></param>
    /// <param name="indexIsLocal"></param>
    /// <returns>subPathPoint</returns>
    public SubPathPoint GetSppByIndex(int index, bool indexIsLocal) {

        var localIndex = indexIsLocal ? index : index - subPathPoints[0].globalIndex;
        
        if (localIndex >= subPathPoints.Count || localIndex < 0)
            Debug.LogError("SubPath.GetSPPByIndex: local index out of range with range 0-" + (subPathPoints.Count -
                1) + " and index: " + index);
        
        return subPathPoints[localIndex];
        
    }
    
    
    public TimeSpan GetPathTimeLength() {
        //return GetTimeStampOfPoint(subPathPoints.Count - 1, true) - GetTimeStampOfPoint(0, true);
        return subPathPoints[^1].AbsTime - subPathPoints[0].AbsTime;
    }
    
    
    /// <summary>
    /// Time passed at this Point in SubPath according to timestamps. used for DEBUG
    /// </summary>
    /// <param name="index"> index of point</param>
    /// <param name="indexIsLocal"> true = index in SubPath, false = index in DataPath/AnchorPoint in BezierPath</param>
    /// <returns> returns ratio ( 0.0 to 1.0 ) of how much time has passed at this point in the subPath </returns>
    public double GetTimeRatioAtPoint(int index, bool indexIsLocal = false) {
        // ONLY USED FOR DEBUG
        
        var timeAtBeginning = GetTimeStampOfPoint(0, indexIsLocal: true);
        var timeAtEnd = GetTimeStampOfPoint(subPathPoints.Count - 1, indexIsLocal: true);
        var timeAtPoint = GetTimeStampOfPoint(index, indexIsLocal);
        
        var ratio = (timeAtPoint - timeAtBeginning) / (timeAtEnd - timeAtBeginning);
        
        return ratio;
    }

    
    public Type GetMediumType() {
        if (!medium) return null;
        
        return medium.GetType();
    }

    #region VISUAL_METHODS

    public Sprite GetIconSprite() {
        if (dataPath.iconList.Count == 0) return null;   // dont have any icons
        if (iconIndex == -1) return null;               // no icon/icon didnt load
        
        var iconTexture = dataPath.iconList[iconIndex];

        return iconTexture;
    }
    
    /// <summary>
    /// sets visual cue if this SubPath is currently selected or not
    /// </summary>
    /// <param name="b"> true= selected, false= deselected</param>
    public void SetVisualAsSelected(bool b) {
        dataSpot.SetThisComponentsVisualAsSelected(b);
        if(dataLine)
            dataLine.SetThisComponentsVisualAsSelected(b);
    }

    #endregion
    
    #endregion

}
