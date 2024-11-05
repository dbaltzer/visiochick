using System;
using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Video;

public class DebugController : MonoBehaviour {
    // singleton
    public static DebugController Instance { get; private set; }

    public bool debug = false;
    [Space]
    [SerializeField] private TMP_Text stats;
    [SerializeField] private TMP_Text mediaStats;
    [Space] 
    [SerializeField] private DebugStatusBar videoTimeStatusBar;
    [SerializeField] private DebugStatusBar pathTimeStatusBar;
    [Space]
    [SerializeField] private CharacterController playerCharacterController;
    [SerializeField] private VideoController videoController;
    private VideoPlayer _videoPlayer;
    
    private int _fps;
    private UnityEngine.Object _medium;
    private string _url;
    private TimeSpan _pathDuration;
    
    
    
    private void Awake() {
        // singleton
        if (Instance == null) {
            Instance = this;
        }
        else {
            Debug.Log("There is more than one DebugController Instnace");
        }
        
        
    }

    private void Start() {
        // subscribe to events
        SubPathInteractableComponent.OnInteracted += SubPathInteractableComponentOnInteracted;
        GameManager.Instance.OnVideoPrepared += GameManagerOnVideoPrepared;
        
        // get videoPlayer
        _videoPlayer = videoController.GetComponent<VideoPlayer>();
        
        // start fps coroutine
        StartCoroutine(FramesPerSecond());
        
        // debugging turned off
        if (debug) return;
        
        foreach (Transform child in transform) {
            child.gameObject.SetActive(false);
        }
        
    }
    
    private void SubPathInteractableComponentOnInteracted(object sender, SubPathInteractableComponent.OnInteractedEventArgs e) {
        this._medium = e.Medium;
        
        // get url
        if (_medium is UrlContainer c) _url = c.url;
        else _url = "";

        _pathDuration = _medium is VideoClip or UrlContainer ? e.SubPath.GetPathTimeLength() : TimeSpan.Zero;
        mediaStats.text = GetMediaStats();
        
        // wait for video to be prepared to get videoLength -> GameManagerOnVideoPrepared
    }
    
    
    private void GameManagerOnVideoPrepared(object sender, EventArgs e) {
        // set status bars // TODO this should only happen AFTER we loaded the video
        var maxTimeInSeconds = Mathf.Max((float)_pathDuration.TotalSeconds,
            (float)videoController.GetCurrentVideoDuration().TotalSeconds);  // BUG returns 0
        
        videoTimeStatusBar?.SetStatusBar(maxTimeInSeconds);
        pathTimeStatusBar?.SetStatusBar(maxTimeInSeconds);
    }

    

    private void Update() {
        if (!debug) return;
        stats.text = GetStats();
        mediaStats.text = GetMediaStats();

        if (!videoController.isActive) {
            videoTimeStatusBar?.SetVisible(false);
            pathTimeStatusBar?.SetVisible(false);
            
            return;
        }
        videoTimeStatusBar?.SetVal((float) videoController.GetVideoCurrentTime().TotalSeconds);
        if(PathFollower.Instance.subPath) pathTimeStatusBar?.SetVal((float) PathFollower.Instance.GetLastPointRelativeTime().TotalSeconds);

    }

    private string GetStats() {
        var vel = playerCharacterController.velocity;
        var stats = string.Format("{0}{1}\n" +
                                  "{2}{3:0.000}\n" +
                                  "{4}{5}\n" +
                                  "{6}{7}\n" +
                                  "{8}{9}",
            "GameState:", GameManager.Instance.gameState,
            "Velocity:          ", vel.magnitude,
            "FPS:               ", _fps,
            "last medium:   ", _medium + " | " + _url,
            "volume: ", AudioListener.volume);
        return stats;
    }

    private string GetMediaStats() {
        // get video percentage and path percentage
        var videoStats = "";
        if (!videoController.isActive) return videoStats;
        
        var videoPercentage = videoController.GetProgressRatio();
        var pathPercentage =
            PathFollower.Instance.subPath?.GetTimeRatioAtPoint(PathFollower.Instance.lastPointGlobalIndex);

        // get video duration
        var videoDuration = videoController.GetCurrentVideoDuration();

        var lastPointRelTime = PathFollower.Instance.subPath
            ? PathFollower.Instance.GetLastPointRelativeTime()
            : TimeSpan.Zero;
        
        //if(_medium is UrlContainer or VideoClip)  // BUG
        //    videoDuration = TimeSpan.FromSeconds((_videoPlayer.frameCount / _videoPlayer.frameRate));
            
        
        //videoStats = "video: " + videoPercentage.ToString() + "\n" + "path: " +  pathPercentage.ToString();
        /*
        videoStats = String.Format("<b>time progress:</b>\n" +
                                   "{0,-10}{1,5: 0.000}" + "\n" +
                                   "{2,-10}{3,5: 0.000}" + "\n",
            "video:", videoPercentage, "path:", pathPercentage);
        */
        videoStats = String.Format("{0}{1}{2}\n" +
                                   "{3,-10}{4,-15 : 0.000}{5,-15 :g}\n" +
                                   "{6,-10}{7,-15 : 0.000}{8,-15 :g}\n\n" +
                                   "{9,-10}{10,-20 :g}\n" +
                                   "{11,-10}{12,-20 :g}",
            "               ", "%", "            duration",
            "path:", pathPercentage, _pathDuration,
            "video:", videoPercentage, videoDuration,
            "pathtime:", lastPointRelTime,
            "vidtime:", videoController.GetVideoCurrentTime());
        return videoStats;
    }
    
    // get FPSs
    private IEnumerator FramesPerSecond() {
        while (true) {
            _fps = (int) (1f / Time.unscaledDeltaTime);

            yield return new WaitForSeconds(0.2f);
        }
    }
}