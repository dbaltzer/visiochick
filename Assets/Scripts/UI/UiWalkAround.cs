using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Video;

public class UiWalkAround : MonoBehaviour {

    #region Fields

    // fields in editor
    [Header("Colors:")]
    [SerializeField] private Color dataSpotSelectedColor = Color.cyan;
    [SerializeField] private Color dataLineSelectedColor = Color.yellow;
    [SerializeField] private Color otherInteractableSelectedColor = Color.green;
    [SerializeField] private Color nonSelectedColor = Color.white;
    
    [Header("Refs to GameObjects:")]
    [SerializeField] private GameObject uiCanvas;
    [SerializeField] private Image crossHair;
    [SerializeField] private GameObject hint;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Image subPathIcon;
    [SerializeField] private GameObject subPathType;
    [SerializeField] private TMP_Text subPathTypeText;
    
    
    private readonly Dictionary<Type, string> _contentDescriptionDict = new Dictionary<Type, string> {
        { typeof(Texture2D), " show Image: " },
        { typeof(VideoClip), " play Video: " },
        { typeof(AudioClip), " play Audio: " },
        { typeof(UrlContainer), " play Video: " }
    };

    #endregion
    


    private void Start() {
        // subscirbe to event
        GameManager.Instance.OnGameStateChanged += GameManagerOnGameStateChanged;
        PlayerInteraction.Instance.OnInteractableSelected += PlayerInteractionOnInteractableSelected;
        PlayerInteraction.Instance.OnSubPathComponentSelected += PlayerInteractionOnSubPathComponentSelected;
        PlayerInteraction.Instance.OnDeselected += PlayerInteractionOnDeselected;
        PerspectiveManager.Instance.OnPerspectiveChanged += PerspectiveManagerOnPerspectiveChanged;
        GameManager.Instance.OnButtonsRebound += GameManagerOnButtonsRebound;
        
        // dont display hint
        hint.SetActive(false);
        crossHair.material.color = nonSelectedColor;
        subPathIcon.enabled = false;
        subPathType.SetActive(false);
    }

    #region EventFunctions

    private void PerspectiveManagerOnPerspectiveChanged(object sender, PerspectiveManager.OnPerspectiveChangedEventArgs e) {
        switch (e.TargetPerspectiveState) {
            case PerspectiveManager.PerspectiveState.FirstPerson:
                // set crosshair visible
                crossHair.enabled = true;
                break;
            case PerspectiveManager.PerspectiveState.TopDown:
                // set crosshair invisible
                crossHair.enabled = false;
                break;
        }
    }

    private void PlayerInteractionOnDeselected(object sender, EventArgs e) {
        // set crosshair to non selected color
        crossHair.material.color = nonSelectedColor;
        // hide all other ui
        hint.SetActive(false);
        subPathIcon.enabled = false;
        subPathType.SetActive(false);

    }

    private void PlayerInteractionOnSubPathComponentSelected(object sender,
        PlayerInteraction.OnSubPathComponentSelectedEventArgs e) {
        // set crosshair color
        var thisSelectedColor = e.Component switch {
            DataSpot => dataSpotSelectedColor,
            DataLine => dataLineSelectedColor,
            _ => otherInteractableSelectedColor
        };
        crossHair.material.color = thisSelectedColor;
        // set interaction hint visiblitity + color
        hint.SetActive(true);
        hintText.color = thisSelectedColor;

        // set icon
        var icon = e.Component switch {
            DataSpot ds => ds.subPath.GetIconSprite(),
            DataLine dl => dl.subPath.GetIconSprite(),
            _ => null
        };

        if (icon == null) {
            // no icon
            subPathIcon.enabled = false;
        }
        else {
            // set icon
            subPathIcon.sprite = icon;
            subPathIcon.enabled = true;   
        }

        
        // set subPathType
        var mType = e.Component.subPath.GetMediumType();
        
        if (mType == null) {
            // no medium type
            subPathType.SetActive(false);
            return;
        }
        
        subPathType.SetActive(true);
        subPathTypeText.text = _contentDescriptionDict[mType];
    }

    private void PlayerInteractionOnInteractableSelected(object sender,
        PlayerInteraction.OnInteractableSelectedEventArgs e) {
        // set crosshair to green 
        crossHair.material.color = otherInteractableSelectedColor;
        // display hint
        hint.SetActive(true);
        hintText.color = otherInteractableSelectedColor;
    }


    private void GameManagerOnGameStateChanged(object sender, GameManager.OnGameStateChangedEventArgs e) {
        
        switch (e.TargetGameState) {
            case GameManager.GameState.WalkAround:
                uiCanvas.SetActive(true);
                break;
            case GameManager.GameState.ViewMedia:
                uiCanvas.SetActive(false);
                break;
        }
        
    }
    
    
    private void GameManagerOnButtonsRebound(object sender, EventArgs e) {
        // change hint text to new interact binding
        hintText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.Interact);
    }

    #endregion
    
}