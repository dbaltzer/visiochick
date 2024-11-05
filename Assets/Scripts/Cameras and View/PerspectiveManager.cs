using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PerspectiveManager : MonoBehaviour {
    
    // singleton
    public static PerspectiveManager Instance;
    
    
    // perspective states
    public enum PerspectiveState {
        FirstPerson,
        TopDown,
    }
    
    // events
    public event EventHandler<OnPerspectiveChangedEventArgs> OnPerspectiveChanged;

    public class OnPerspectiveChangedEventArgs : EventArgs {
        public PerspectiveState TargetPerspectiveState;
    }
    
    
    // fields
    public PerspectiveState perspective;
    [SerializeField] private CinemachineVirtualCamera firstPersonCam;
    [SerializeField] private CinemachineVirtualCamera topDownCam;
    
    #region MONOBEHAVIOR_METHODS

    private void Awake() {
        
        // singleton
        if(Instance != null)
            Debug.Log("There is more than one PlayerPerspective instance!");
        Instance = this;
        
    }

    
    private void Start() {
        
        // start with perspective set in inspector:
        ChangePerspective(perspective);
        
        // subscribe to toggle view input action
        GameManager.Instance.InputActions.Default.ToggleView.performed += ToggleView;
        
    }
    

    private void OnValidate() {
        // when value gets changed in inspector:
        ChangePerspective(perspective);
    }


    private void ToggleView(InputAction.CallbackContext context) {
        
        if (perspective == PerspectiveState.FirstPerson)
            ChangePerspective(PerspectiveState.TopDown);
                
        else if (perspective == PerspectiveState.TopDown)
            ChangePerspective(PerspectiveState.FirstPerson);
        
    }
    
    

    private void ChangePerspective(PerspectiveState targetPerspective) {
        perspective = targetPerspective;
        
        switch (targetPerspective) {
            case PerspectiveState.TopDown:
                // turn off first person cam. cinemashine switches automatically to camera of next lower pririty
                firstPersonCam.enabled = false;
                // send event
                OnPerspectiveChanged?.Invoke(this,
                    new OnPerspectiveChangedEventArgs {
                        TargetPerspectiveState = targetPerspective
                    });
                
                break;
            case PerspectiveState.FirstPerson:
                firstPersonCam.enabled = true;
                // send event
                OnPerspectiveChanged?.Invoke(this,
                    new OnPerspectiveChangedEventArgs {
                        TargetPerspectiveState = targetPerspective
                    });

                break;
        }
    }

    #endregion
    
}