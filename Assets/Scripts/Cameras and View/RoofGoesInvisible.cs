using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(BoxCollider))]
public class RoofGoesInvisible : MonoBehaviour {
    
    [Tooltip("if not set: script will automatically pick BoxCollider")] [SerializeField]
    private BoxCollider trigger;
    private MeshRenderer _renderer;
    [SerializeField] private List<MeshRenderer> additionalMeshes;
    private bool _playerInHouse;
    
    
    private void Start() {
        // subscribe to events
        PerspectiveManager.Instance.OnPerspectiveChanged += PerspectiveManagerOnPerspectiveChanged;
        
        // get components
        if(!trigger)
            trigger = GetComponent<BoxCollider>();
        _renderer = GetComponent<MeshRenderer>();

        // set box collider as trigger
        trigger.isTrigger = true;
        
        SetMeshVisibility(true);

    }

    private void PerspectiveManagerOnPerspectiveChanged(object sender, PerspectiveManager.OnPerspectiveChangedEventArgs e) {
        switch (e.TargetPerspectiveState) {
            case PerspectiveManager.PerspectiveState.FirstPerson:
                // always show roof in first person
                SetMeshVisibility(true);
                break;
            case PerspectiveManager.PerspectiveState.TopDown:
                // hide roof if player is in house
                SetMeshVisibility(!_playerInHouse);
                break;
        }
    }
    
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.name != "Player") return;
        
        _playerInHouse = true;
        
        if (PerspectiveManager.Instance.perspective != PerspectiveManager.PerspectiveState.TopDown)
            return;
        
        // if top down game mode & player enters house: set mesh to invisible
        SetMeshVisibility(false);
    }
    
    private void OnTriggerExit(Collider other) {
        if (other.gameObject.name != "Player") return;

        _playerInHouse = false;
        
        if (PerspectiveManager.Instance.perspective != PerspectiveManager.PerspectiveState.TopDown)
            return;
        
        // if player exits house: set mesh to visible
        SetMeshVisibility(true);
    }

    
    private void SetMeshVisibility(bool visible) {
        // main mesh (mesh renderer on this GameObject)
        _renderer.enabled = visible;
        
        // go through additional meshes (eg of child objects)
        foreach (var mesh in additionalMeshes) {
            mesh.enabled = visible;
        }
        
    }
    
}