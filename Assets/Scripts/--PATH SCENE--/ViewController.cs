using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewController : MonoBehaviour {
    
    // singleton
    public static ViewController Instance { get; private set; }
    
    // fields in editor
    [SerializeField] private Camera mainCam;
    [SerializeField] private Camera mediaCam;
    
    private void Awake() {
        
        // singleton
        if (Instance == null) {
            Instance = this;
        }
        else {
            Debug.Log("There is more than one ViewController!");
        }
        
        // subscribe to events
        GameManager.Instance.OnGameStateChanged += GameManagerOnGameStateChanged;
    }

    private void GameManagerOnGameStateChanged(object sender, GameManager.OnGameStateChangedEventArgs e) {
        switch (e.TargetGameState) {
            case GameManager.GameState.WalkAround:
                ShowMainCam();
                break;
            case GameManager.GameState.ViewMedia:
                ShowMediaCam();
                break;
        }
    }

    private void Start() {
        ShowMainCam();
    }

    [ContextMenu("ShowMediaCam")]
    public void ShowMediaCam() {
        mainCam.rect = new Rect(0.7f, 0.7f, 0.3f, 0.3f);
        mediaCam.enabled = true;
    }
    
    [ContextMenu("ShowMainCam")]
    public void ShowMainCam() {
        mediaCam.enabled = false;
        mainCam.rect = new Rect(0f, 0f, 1f, 1f);
    }
}