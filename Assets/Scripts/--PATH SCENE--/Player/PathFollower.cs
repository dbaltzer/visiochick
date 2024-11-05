using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using PathCreation;
using UnityEngine.Serialization;
using UnityEngine.Video;

[RequireComponent(typeof(CharacterController))]
public class PathFollower : MonoBehaviour {

    private enum RotationType {
        OnlyY,
        XYZ,
    }

    private enum PositionType {
        OnlyXZ,
        CopyHeight,
    }

    
    // singleton
    public static PathFollower Instance { get; private set; }
    
    [Header("pathFollow movement variables")]
    private float _speed = 5;
    [Tooltip("speed get calculated using timestamps and distance. scale result with this")]
    [SerializeField] [Range(0.1f, 10)] private float speedScale = 1f;
    [SerializeField] private float minSpeed = 0.0001f;
    [Space]
    [SerializeField] private RotationType rotationType = RotationType.OnlyY;
    [SerializeField] private PositionType positionType = PositionType.OnlyXZ;

    // refs
    private CharacterController _characterController;
    [HideInInspector] public SubPath subPath;
    private int _startLocalIndex;
    
    private VertexPath _vertexPath;
    private float _distanceTravelled;
    private int _stopPointGlobalIndex;
    [HideInInspector] public int lastPointGlobalIndex;
    
    private bool _follow = false;
    private bool _waitForVideoPrepared = true;
    private bool _pause = false;
    private bool _firstStep = true;


    #region MONOBEHAVIOR_METHODS

    private void Awake() {
        // singleton
        if(Instance != null)
            Debug.Log("There is more than one PathFollower instance!");
        Instance = this;
        
        // subscribe to events
        SubPathInteractableComponent.OnInteracted += SubPathInteractableComponentOnInteracted;
    }


    private void Start() {
        // get components
        this._characterController = GetComponent<CharacterController>();
        
        // subscribe to events
        GameManager.Instance.OnGameStateChanged += GameManagerOnGameStateChanged;
        GameManager.Instance.OnVideoPrepared += GameManagerOnVideoPrepared;
    }
    
    
    public void Update() {
        
        if (!subPath) return;

        if (_waitForVideoPrepared) return;
        
        if (_follow & (lastPointGlobalIndex < _stopPointGlobalIndex) & !_pause) {
            // follow subPath
            Step();
        }else if ((lastPointGlobalIndex >= _stopPointGlobalIndex) & _follow) {
            // end of path
            lastPointGlobalIndex = _stopPointGlobalIndex;
            _follow = false;
        }
    }

    #endregion

    
    
    #region EVENTS

    private void GameManagerOnGameStateChanged(object sender, GameManager.OnGameStateChangedEventArgs e) {
        if(e.TargetGameState == GameManager.GameState.WalkAround) _follow = false;
        if (e.TargetGameState != GameManager.GameState.ViewMedia) subPath = null;
    }

    private void SubPathInteractableComponentOnInteracted(object sender,
        SubPathInteractableComponent.OnInteractedEventArgs e) {
        
        // if SubPath.medium is a video. we need to wait for the VideoController to finish loading the video
        if (e.Medium is UrlContainer or VideoClip) _waitForVideoPrepared = true; // turns false when GameManagerOnVideoPrepared()
        
        switch (sender) {
            case DataSpot ds:
                FollowSubPath(e.SubPath, startAtBeginning: true);
                break;
            case DataLine dl:
                FollowSubPath(e.SubPath, startAtBeginning: false, e.StartLocalIndex);
                break;
        }
    }
    
    private void GameManagerOnVideoPrepared(object sender, EventArgs e) {
        if(subPath)
            _waitForVideoPrepared = false;
    }
    

    #endregion
    

    
    #region METHODS

    public void PauseResume(bool pause) {
        _pause = pause;
    }
    

    /// <summary>
    /// resets position to point nearest to relative Time
    /// </summary>
    /// <param name="relTime"> time ratio 0.0, ..., 1.0 , ratio at 0nth point = 0.0, ratio at last point = 1.0 </param>
    public void ResetToTime(double relTime) {
        // TODO: this doesnt work if we already finished the pat
        
        // calculate distance at new position
        var spp = subPath.GetSppClosestToRelTime(relTime);
        _distanceTravelled = spp.bezierPathDist;
        lastPointGlobalIndex = spp.globalIndex;
        
        _follow = true;
    }
    
    
    // calling this function will init player to follow subPath
    private void FollowSubPath(SubPath subPath, bool startAtBeginning = true, int startingLocalIndex = 0) {
        // get subPath & VertexPath:
        this.subPath = subPath;
        this._vertexPath = this.subPath.parentPathCreator.path;
        
        // set stopping index:
        _stopPointGlobalIndex = subPath.subPathPoints[^1].globalIndex;
        
        // set starting distance:
        if(startAtBeginning) {
            _distanceTravelled  = this.subPath.subPathPoints[0].bezierPathDist;
            lastPointGlobalIndex = this.subPath.subPathPoints[0].globalIndex;
        }
        else {
            _distanceTravelled = this.subPath.subPathPoints[startingLocalIndex].bezierPathDist;
            lastPointGlobalIndex = this.subPath.subPathPoints[startingLocalIndex].globalIndex;
        }
        
        // init walking:
        _follow = true;
        _firstStep = true;


        if (DebugController.Instance) {
            // BUG debugging -----------------------------------------------------------------------------------------------
            // print subPath info
            var msg = "<color=orange>" + "SUBPATH INFO:" + "</color>" + "\n";
            msg += String.Format("{0,-10}{2,-30}{3,-30}{4,-20}{1,-20}{5,-25}\n", 
                "point id", "path length", "global pos",
                "timestamp", "relTime", "speed");

            foreach (var spp in subPath.subPathPoints) {
                msg += string.Format("{0,-10}" + "{2,-30}" + "{3,-30}" + "{4,-20}" + "{1,-20}" + "{5,-25}" + "\n",
                    spp.globalIndex,
                    spp.bezierPathDist,
                    subPath.GetPointWorldPos(spp),
                    spp.timeStamp,
                    spp.relTimeInSecs,
                    spp.speed);

            }
        
            var videoLength = subPath.medium is VideoClip vc ? TimeSpan.FromSeconds(vc.length) : TimeSpan.Zero;
            msg += "path length: " + subPath.GetPathTimeLength() + " | video length: " + videoLength;
            Debug.Log(msg);

            // BUG debugging -----------------------------------------------------------------------------------------------   
        }
    }
    
    
    
    /// <summary>
    /// updates current location on subPath, gets speed, moves/rotates player one step further along path
    /// </summary>
    private void Step() {
        // UPDATE ACCORDING TO SUBPATH
        
        // dont instantly update index at start
        if (!_firstStep)
            UpdateLastPointIndex();
        if (_firstStep) _firstStep = false;
        
        // update speed
        var dontWait = UpdateSpeed();
        
        // if wait at point. no movement
        if (!dontWait) return;
        
        
        // MOVEMENT:
        _distanceTravelled += _speed * Time.deltaTime;


        // A) APPLY POSITION
        switch (positionType) {

            case PositionType.OnlyXZ: // walk on ground while following paths x, y positions

                // avoid floating by adding gravity if we arent standing on ground
                var gravityForce = 20f * Time.deltaTime;
                var lastYPos = transform.position.y;
                var newYPos = _characterController.isGrounded ? lastYPos : lastYPos - gravityForce;

                var nextPos = new Vector3(
                    _vertexPath.GetPointAtDistance(_distanceTravelled).x,
                    newYPos,
                    _vertexPath.GetPointAtDistance(_distanceTravelled).z);

                // apply:
                _characterController.Move(nextPos - transform.position);



                break;

            case PositionType.CopyHeight: // strictly follow path x,y,z position -> floating

                _characterController.Move(
                    _vertexPath.GetPointAtDistance(_distanceTravelled) - transform.position);

                break;
        }


        // B) APPLY ROTATION
        switch (rotationType) {
            
            case RotationType.OnlyY:
                var onlyYrotation = new Vector3(
                    0f, _vertexPath.GetRotationAtDistance(_distanceTravelled).eulerAngles.y,0f
                );
                transform.rotation = Quaternion.Euler(onlyYrotation);
                break;
            
            case RotationType.XYZ:
                transform.rotation = _vertexPath.GetRotationAtDistance(_distanceTravelled);
                break;
        }
    }
    

    
    /// <summary>
    /// checks if we surpassed distance of next lastPointGlobalIndex. yes -> update index
    /// </summary>
    private void UpdateLastPointIndex() {
        // lastPoint is always the point we just reached
        if (_distanceTravelled >=
            subPath.GetSppByIndex(index: lastPointGlobalIndex + 1, indexIsLocal: false).bezierPathDist)
            lastPointGlobalIndex++;
    }
    
    
    
    /// <summary>
    /// update speed from speed data in subpath.subpathpoints. if speed == 0 : wait for dTime between points
    /// </summary>
    /// <returns> FALSE if we have to wait at position. TRUE if speed > 0 or end of path</returns>
    private bool UpdateSpeed() {

        var speed = (float)subPath.GetSppByIndex(lastPointGlobalIndex, indexIsLocal: false).speed;

        // if speed = 0 (points at some dist but different times) : wait for time
        if (speed == 0) {
            // wait for seconds (co-routine)
            var waitTime = subPath.GetSppByIndex(lastPointGlobalIndex + 1, indexIsLocal: false).relTimeInSecs -
                           subPath.GetSppByIndex(lastPointGlobalIndex, indexIsLocal: false).relTimeInSecs;
            
            StartCoroutine(WaitCoroutine((float)waitTime));
            
            return false;
        }

        _speed = speed == Mathf.Infinity ? minSpeed : speed * speedScale;
        
        return true;
    }
    
    
    
    /// <summary>
    /// wait at point for specific amount of seconds
    /// </summary>
    /// <param name="waitForSeconds"></param>
    /// <returns></returns>
    IEnumerator WaitCoroutine(float waitForSeconds) {
        // wait
        PauseResume(pause: true);
        yield return new WaitForSeconds(waitForSeconds);
        // after we finished waiting we can resume
        PauseResume(pause: false);
    }

    
    public TimeSpan GetLastPointRelativeTime() {
        var timeStampOfPoint = subPath.GetTimeStampOfPoint(lastPointGlobalIndex, indexIsLocal: false);
        var timeStampAtSubPathStart = subPath.GetTimeStampOfPoint(0, indexIsLocal: true);
        return timeStampOfPoint - timeStampAtSubPathStart;
    }
    
    #endregion
    
    
    
}