using TMPro;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(SphereCollider))]
public class DataSpot : SubPathInteractableComponent, INteractable {
    
    
    #region FIELDS

    // fields in inspector
    [Space]
    [SerializeField] private MeshRenderer componentRenderer;
    [SerializeField] private  Material selectedMaterial;
    [SerializeField] private  Material unselectedMaterial; // element in [imageMaterial, videoMaterial, audioMaterial, idleMaterial)
    [Header("Library of used materials:")]
    [SerializeField] private Material imageMaterial;
    [SerializeField] private Material videoMaterial;
    [SerializeField] private Material audioMaterial;
    [SerializeField] private Material idleMaterial; // if media is null or type is unknown

    
    // private fields
    private SphereCollider _collider;
    

    #endregion

    
    
    #region MONOBEHAVIOR_METHODS

    private void Start() {
        // get components
        _collider = GetComponent<SphereCollider>();
        
        // subscribe to events
        PlayerInteraction.Instance.OnSubPathComponentSelected += PlayerInteractionOnSubPathComponentSelected;
        
        
        InitVisual();
        SetSubPathVisualAsSelected(false);
    }

    #endregion
    
    
    
    #region METHODS
    

    private void InitVisual() {
        /*
         * update visual according to content
         */
        
        // set material
        unselectedMaterial = idleMaterial;   // default (if medium is null)
        unselectedMaterial = subPath.medium switch {
            Texture2D => imageMaterial,     // if subPath.medium is type Texture2D => _idleMaterial = imageMaterial
            VideoClip or UrlContainer => videoMaterial,
            AudioClip => audioMaterial,
            _ => idleMaterial,
        };
    }
    

    public void SetThisComponentsVisualAsSelected(bool b) {
        // set material
        componentRenderer.material = b ? selectedMaterial : unselectedMaterial;
    }


    public new void Interact() {
        if (DebugController.Instance)
            Debug.Log("DataSpot.Interact(): " + subPath.dataSpot.name + " relStartTimeInSeconds = " + relStartTimeInSeconds);
        
        base.Interact();
    }

    #endregion

    
}