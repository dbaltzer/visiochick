using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Video;
using Object = UnityEngine.Object;

[RequireComponent(typeof(ViewController))]
public class MediaPlayer : MonoBehaviour {
    
    // singleton
    public static MediaPlayer Instance { get; private set; }

    #region FIELDS

    // fields in editor
    [SerializeField] private VideoController videoPanel;
    [SerializeField] private ImageController imagePanel;
    [SerializeField] private AudioController soundPanel;
    [SerializeField] private GameObject slider;
    [SerializeField] private GameObject pauseResumeButton;
    
    
    
    // private fields
    private Object _content;
    private SubPath _subPath;
    private Object _playerComponent;

    private bool _pauseToggle = false; // true=pause
    

    #endregion

    #region MONOBEHAVIOR_METHODS

    private void Awake() {
        // Singleton
        if (Instance == null) {
            Instance = this;
        }
        else {
            Debug.Log("There is more than one MediaPlayer!");
        }
        
        // subscribe to events
        GameManager.Instance.OnGameStateChanged += GameManagerOnGameStateChanged;
        SubPathInteractableComponent.OnInteracted += SubPathInteractableComponentOnInteracted;
    }

    #endregion
    

    #region EVENTS

    private void SubPathInteractableComponentOnInteracted(object sender,
        SubPathInteractableComponent.OnInteractedEventArgs e) {
        // interactable with subPath 
        if (sender is SubPathInteractableComponent c) {
            if (c.subPath && e.Medium) {
                _subPath = e.SubPath;
                PlayMedium(e.Medium, startAtSeconds: e.RelStartTimeInSeconds);
                return;
            }
        }

        // interactable without subPath
        if(e.Medium)
            PlayMedium(e.Medium);
    }

    private void GameManagerOnGameStateChanged(object sender, GameManager.OnGameStateChangedEventArgs e) {
        // when we quit the media view -> hide all content from canvas
        if (e.TargetGameState == GameManager.GameState.ViewMedia) return; // on change to any gameState but ViewMedia
        
        videoPanel.HideVideo();
        imagePanel.HideImage();
        soundPanel.HideSound();
        slider.gameObject.SetActive(false);
    }

    #endregion
    
    #region METHODS

    /// identifies type of medium and plays medium with correct panel
    private void PlayMedium(Object dataSpotContent, double startAtSeconds = 0d) {

        _content = dataSpotContent;

        switch (_content) {
            
            case Texture2D img:
                // display image:
                videoPanel.HideVideo();
                soundPanel.HideSound();
                ShowVideoAudioUi(false);
                
                imagePanel.ShowImage(img);
                break;
            
            case VideoClip vClip:
                // display video
                imagePanel.HideImage();
                soundPanel.HideSound();
                ShowVideoAudioUi(true);
                
                slider.gameObject.SetActive(true);
                videoPanel.ShowVideo(vClip, startAtSeconds);
                break;
            
            case UrlContainer container:
                // display video
                imagePanel.HideImage();
                soundPanel.HideSound();
                ShowVideoAudioUi(true);

                slider.gameObject.SetActive(true);
                videoPanel.ShowVideo(container.url, startAtSeconds);
                break;
            
            case AudioClip aClip:
                // play audio
                imagePanel.HideImage();
                videoPanel.HideVideo();
                ShowVideoAudioUi(true);
                
                soundPanel.PlaySound(aClip);
                break;
            
            default:
                Debug.Log("MediaPlayer cannot play medium of type " + _content.GetType());
                break;
        }
        if(_pauseToggle)
            PauseResume();
        
    }

    public float GetProgressRatio() {
        // switch expression :)
        var ratio = _content switch {
            VideoClip or UrlContainer => videoPanel.GetProgressRatio(),
            AudioClip => soundPanel.GetProgressRatio(),
            _ => 0f // <- default
        };
        
        return ratio;
    }

    private void ShowVideoAudioUi(bool show) {
        slider.SetActive(show);
        pauseResumeButton.SetActive(show);
    }

    public void PauseResume(bool forceResume = false) {
        
        // forceResume==true : resume independent of current state (paused or playing),
        // forceResume==false : toggle pause/resume
        _pauseToggle = forceResume ? false : !_pauseToggle;
        
        
        // pause resume video/audio
        switch (_content) {
            case VideoClip or UrlContainer:
                // pause resume videoController
                videoPanel.PauseResume(_pauseToggle);
                break;
            case AudioClip:
                // pause resume audioController
                soundPanel.PauseResume(_pauseToggle);
                break;
        }
        
        // pause/resume pathFollower
        PlayerController.Instance.PathFollowPauseResume(_pauseToggle);
    }

    
    
    public void SliderValueChanged(float value) {
        // 1) translate value [0.0, ..., 1.0] to relTime
        var videoTotalTimeInSeconds = videoPanel.GetCurrentVideoDuration().TotalSeconds;
        var relTime = value * videoTotalTimeInSeconds;
        
        // get a close relTime corresponding from one spp
        relTime = _subPath.GetSppClosestToRelTime(relTime).relTimeInSecs;
        
        // change video position
        value = (float)(relTime / videoTotalTimeInSeconds);
        videoPanel.SetProgressRatio(value);
        
        // change pathFollower position
        PathFollower.Instance.ResetToTime(relTime);
        
        // resume movement:
        PauseResume(forceResume: true);
    }
    
    #endregion
   
    
    
}