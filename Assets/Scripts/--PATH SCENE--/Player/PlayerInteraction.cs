using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class PlayerInteraction : MonoBehaviour {
    
    
    #region EVENTS

    public event EventHandler<OnInteractableSelectedEventArgs> OnInteractableSelected;

    public class OnInteractableSelectedEventArgs : EventArgs {
        public bool Selected;
    }

    public event EventHandler<OnSubPathComponentSelectedEventArgs> OnSubPathComponentSelected;

    public class OnSubPathComponentSelectedEventArgs : EventArgs {
        public SubPathInteractableComponent Component;
    }

    public event EventHandler<EventArgs> OnDeselected; 

    
    #endregion
    
    

    #region FIELDS

    // singleton
    public static PlayerInteraction Instance { get; private set; }
    public bool interactionEnabled;
    
    private Collider _otherCollider;
    private INteractable _interactableObject;
    [SerializeField] private List<SubPathInteractableComponent> _selectedSubPathComponents = new List<SubPathInteractableComponent>();
    private int _selectedIndex;
    private bool _nonDataSpotInteractableSelected;
    private LayerMask _rayCastLayerMask;


    #endregion

    private void Awake() {
        // singleton
        if(Instance != null)
            Debug.Log("There is more than one PlayerInteraction instance!");
        Instance = this;
        
        // set layermask
        _rayCastLayerMask = LayerMask.GetMask("Interactables");
    }

    private void Start() {
        // subscribe to input actions events 
        GameManager.Instance.InputActions.Default.ShuffleSelection.performed += ShuffleSelection;
        GameManager.Instance.InputActions.Default.Interact.performed += Interact;

    }

    private void Update() {
        if (!interactionEnabled) return;
        
        if(_selectedSubPathComponents.Count <= 0) TestForInteractableWithRayCast();
    }


    #region EVENT_FUNCTIONS
    
    
    private void OnTriggerEnter(Collider other) {
        if (!interactionEnabled) return;
        
        if (other.gameObject.TryGetComponent(out DataSpot dataSpot)) {
            // trigger == dataSpot
            AddSubPathComponentAsSelected(dataSpot);
        }else if (other.gameObject.TryGetComponent(out DataLine spLineInteractable)) {
            // trigger == line
            AddSubPathComponentAsSelected(spLineInteractable);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.gameObject.TryGetComponent(out DataSpot dataSpot)) {
            // trigger == dataSpot
            RemoveSubPathComponentAsSelected(dataSpot);
        }
        else if (other.gameObject.TryGetComponent(out DataLine spLineInteractable)) {
            // trigger == line
            RemoveSubPathComponentAsSelected(spLineInteractable);
        }
        
    }
    
    /// <summary>
    /// adds DataSpot / DataLine to PlayerInteraction._selectedSubPathComponents
    /// DataSpots get prioritized and will replace DataLine of same SubPath
    /// Sets newest object in list as selected
    /// </summary>
    /// <param name="component">SubPathInteractableComponent (DataSpot or DataLine)</param>
    private void AddSubPathComponentAsSelected(SubPathInteractableComponent component) {
        switch (component) {
            
            case DataSpot ds:
                // (remove DataLine of same subPath and) add  DataSpot
                if (!_selectedSubPathComponents.Contains(ds)) {
                    foreach (var c in _selectedSubPathComponents) {
                        if (c.subPath != ds.subPath) continue;
                        _selectedSubPathComponents.Remove(c);
                        break;
                    }   
                    
                    _selectedSubPathComponents.Add(component);
                }
                
                // set index of selected
                _selectedIndex = _selectedSubPathComponents.IndexOf(component);
                break;
            
            case DataLine l:
                // dont select line if we already have spot selected:
                
                // dont add dataLine if already selected
                if (_selectedSubPathComponents.Contains(component)) return;
                
                // dont add dataLine if corresponding DataSpot already selected
                foreach (var c in _selectedSubPathComponents) {
                    if (c.subPath != l.subPath) continue;
                    return;
                }
                
                // add to list 
                _selectedSubPathComponents.Add(component);
                // set index of selected
                _selectedIndex = _selectedSubPathComponents.IndexOf(component);
                break;
            
        }
            
        SubPathInteractableComponentSelectionUpdate();
    }

    /// <summary>
    /// removes specific SubPathInteractableComponent object form _selectedSubPathComponents list
    /// and resets selected object
    /// </summary>
    /// <param name="component">SubPathInteractableComponent (DataSpot or DataLine)</param>
    private void RemoveSubPathComponentAsSelected(SubPathInteractableComponent component) {
        
        component.CollisionExitWithPlayer();
                
        // remove from list of selected DataSpots
        if (_selectedSubPathComponents.Contains(component))
            _selectedSubPathComponents.Remove(component);
            
        // reset selected index to lat
        _selectedIndex = _selectedSubPathComponents.Count - 1; // this turns into -1 if no spot is selected
            
        SubPathInteractableComponentSelectionUpdate();
    }

    /// <summary>
    /// sends events to SubPathInteractableComponent objects. so they can adjust visual as idle/selected
    /// </summary>
    private void SubPathInteractableComponentSelectionUpdate() {
        if (_selectedSubPathComponents.Count == 0) {
            OnDeselected?.Invoke(this, EventArgs.Empty);
            return;
        }
        
        // invoke event to update selected DataSpot Visual
        OnSubPathComponentSelected?.Invoke(this, new OnSubPathComponentSelectedEventArgs() {
            Component = _selectedSubPathComponents[_selectedIndex]
        });  
    }


    private void ShuffleSelection(InputAction.CallbackContext context) {
        if (!interactionEnabled || _selectedSubPathComponents.Count() <= 1) return;
        
        _selectedIndex = (_selectedIndex + 1) % (_selectedSubPathComponents.Count);
        SubPathInteractableComponentSelectionUpdate();
    }
    

    #endregion


    #region INTERACTION_FUNCTIONS
    
    

    /// <summary>
    /// call Interact() on selected GameObject implementing INteractable interface. will prioritize SubPathInteractableComponents
    /// </summary>
    private void Interact(InputAction.CallbackContext context) {
        /*
         * interaction priorities:
         *      1. SubPathComponent (DataSpot, DataLine 2. Interactable hit by RayCast
         */

        if (!interactionEnabled) return;
        
        // check for SubPathComponent
        if (_selectedSubPathComponents.Count > 0) {
            //_selectedSubPathComponents[_selectedIndex].Interact();
            switch (_selectedSubPathComponents[_selectedIndex]) {
                case DataSpot ds:
                    ds.Interact();
                    break;
                case DataLine dl:
                    dl.Interact();
                    break;
            }
            return;
        }
        
        // try interacting with possible raycast hit
        _interactableObject?.Interact();

    }


    /// <summary>
    /// send raycast through Players child gameObject RaycastTarget
    /// if raycast hits collider, check for interactable
    /// excludes SubPathInteractableComponents bc they might only be selected by walking into them
    /// </summary>
    private void TestForInteractableWithRayCast() {
        
        // raycast goes screenspace center -> forward 
        
        // direction & length of raycast
        var raycastDirection = Camera.main.transform.forward;
        var raycastMaxDist = 3f;
        
        // start raycast at camera
        Vector3 raycastOrigin = Camera.main.transform.position;
        
        
        // send Raycast & check for collision 
        if (Physics.Raycast(raycastOrigin, raycastDirection,
                out RaycastHit raycastHit, raycastMaxDist, layerMask: _rayCastLayerMask)) {
            
            
            // we are looking for an Object implementing the INteractable interface except DataSpots
            if (raycastHit.transform.TryGetComponent(out INteractable interactableObject)) {
                
                if (interactableObject is SubPathInteractableComponent) return; // ignore SubPaths
                
                
                // found INteractable
                _interactableObject = interactableObject;
                
            
                // fire event if we newly selected an INteractable
                if(_nonDataSpotInteractableSelected)
                    return;
            
                _nonDataSpotInteractableSelected = true;
                OnInteractableSelected?.Invoke(this, new OnInteractableSelectedEventArgs {
                    Selected = true
                });
                
                return;
            }
        }
        
        
        
        // Not hitting any Interactable
        if (_nonDataSpotInteractableSelected) {
            _nonDataSpotInteractableSelected = false;
            OnDeselected?.Invoke(this, EventArgs.Empty);
        }
        
        _interactableObject = null;
    }
    
    

    #endregion
}