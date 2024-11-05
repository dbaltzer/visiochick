using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer), typeof(RawImage),
    typeof(AspectRatioFitter))]
public class VideoController : MonoBehaviour {
    

    #region FIELDS

    [HideInInspector] public bool isActive;

    private object _videoSource;
    private RawImage _rawImage;
    private VideoPlayer _videoPlayer;

    #endregion

    
    
    #region MONOBEHAVIOR_METHODS

    private void Start() {
        // subscribe to events
        GameManager.Instance.OnVideoPrepared += GameManagerOnVideoPrepared;
        
        
        _videoPlayer = GetComponent<VideoPlayer>();
        _rawImage = GetComponent<RawImage>();
        
        // set source type 
        _videoPlayer.isLooping = false;
        _videoPlayer.aspectRatio = VideoAspectRatio.FitInside;

    }

    #endregion
    
    
    #region EVENTS
    
    private void GameManagerOnVideoPrepared(object sender, EventArgs e) {
        _videoPlayer.Play();
    }
    
    #endregion



    #region METHODS


    private void LoadVideo(double startAtSeconds = 0d) {
        if (DebugController.Instance)
            Debug.Log("VideoController.PlayVideo startAtSeconds = " + startAtSeconds);
        
        if (_videoSource == null) {
            Debug.Log("VideoPlayer: No Video Set to play.");
            return;
        }
        
        
        switch (_videoSource) {
            case string url:
                Debug.Log("VideoController.LoadVideoRoutine() loading url: " + url);
                _videoPlayer.url = url;
                break;
            case VideoClip clip:
                _videoPlayer.clip = clip;
                break;
            default:
                Debug.Log("video source neither videoClip nor url");
                return;
        }
        
        // set start frame:
        if (startAtSeconds == 0d)
            _videoPlayer.frame = 0;
        else {
            var startFrame = _videoPlayer.frameRate * startAtSeconds;
            _videoPlayer.frame = (long)startFrame;
            
            if(DebugController.Instance)
                Debug.Log("videoPlayer starts at frame: " + startFrame + " / " + _videoPlayer.frameCount);
        }
        
        GameManager.Instance.VideoPrepared();
        
    }
    
    
    /// <summary>
    /// plays video from frame corresponding to startAtSeconds.
    /// url needs videoPlayer to prepare, thats why this is a Coroutine
    /// </summary>
    /// <param name="startAtSeconds"> timetime for video</param>
    /// <returns></returns>
    private IEnumerator LoadVideoRoutine(double startAtSeconds = 0d) {
        if (DebugController.Instance)
            Debug.Log("VideoController.PlayVideo startAtSeconds = " + startAtSeconds);
        
        if (_videoSource == null) {
            if (DebugController.Instance) Debug.Log("VideoPlayer: No Video Set to play.");
            yield return null;
        }
        else {
            switch (_videoSource) {
                case string url:
                    if (DebugController.Instance) Debug.Log("VideoController.LoadVideoRoutine() loading url: " + url);
                    _videoPlayer.url = url;
                    break;
                case VideoClip clip:
                    if (DebugController.Instance) Debug.Log("VideoController.LoadVideoRoutine() loading clip: " + clip.name);
                    _videoPlayer.clip = clip;
                    break;
                default:
                    if (DebugController.Instance) Debug.Log("video source neither videoClip nor url");
                    break;
            }

        
            _videoPlayer.Prepare();

            // wait for videoPlayer to load video
            var maxPrepareTimeInSeconds = 10f;
            var videoPrepareTimer = 0f;
            
            while (!_videoPlayer.isPrepared && videoPrepareTimer < maxPrepareTimeInSeconds && isActive){
                yield return null;
                videoPrepareTimer += Time.deltaTime;
            }
            
            // :(
            if (videoPrepareTimer > maxPrepareTimeInSeconds && DebugController.Instance)
                Debug.LogError("videoPlayer.Prepare() timed out");
            // :)
            else if (DebugController.Instance) Debug.Log("video loaded.");

        
        
            // set start frame:
            if (startAtSeconds == 0d)
                _videoPlayer.frame = 0;
            else {
                var startFrame = _videoPlayer.frameRate * startAtSeconds;
                _videoPlayer.frame = (long)startFrame;
            
                if(DebugController.Instance)
                    Debug.Log("videoPlayer starts at frame: " + startFrame + " / " + _videoPlayer.frameCount);
            }
        
            GameManager.Instance.VideoPrepared();
        }
    }
    
    
    

    public void ShowVideo(object videoSource, double startAtSeconds = 0d) {

        isActive = true;

        if (videoSource is UrlContainer urlC) _videoSource = urlC.url;
        else _videoSource = videoSource;
        
        _rawImage.enabled = true;
        StartCoroutine(LoadVideoRoutine(startAtSeconds));

    }

    public void HideVideo() {

        isActive = false;
        
        _videoPlayer.Stop();
        _rawImage.enabled = false;
    }
    
    
    public float GetProgressRatio() {
        return (float)_videoPlayer.frame / _videoPlayer.frameCount;
    }

    public TimeSpan GetVideoCurrentTime() {
        var timeInSeconds = _videoPlayer.time;
        return TimeSpan.FromSeconds(timeInSeconds);
    }

    public TimeSpan GetCurrentVideoDuration() {
        return TimeSpan.FromSeconds(_videoPlayer.length);
    }

    public void SetProgressRatio(float ratio) {
        _videoPlayer.frame = (long) (ratio * _videoPlayer.frameCount);
    }

    public void PauseResume(bool pause) {
        if(pause)
            _videoPlayer.Pause();
        else
            _videoPlayer.Play();
    }

    #endregion
}