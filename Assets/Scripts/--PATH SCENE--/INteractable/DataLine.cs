using UnityEngine;

public class DataLine : SubPathInteractableComponent, INteractable {

    //[Header("Visual")] 

    private Material idleMaterial;
    [SerializeField] private Material selectedMaterial;
    public LineRenderer componentRenderer;
    
    
    public void Start() {
        // get components
        subPath = transform.parent.GetComponent<SubPath>();
        componentRenderer = GetComponent<LineRenderer>();
        
        // subscribe to events
        PlayerInteraction.Instance.OnSubPathComponentSelected += PlayerInteractionOnSubPathComponentSelected;
        InitVisual();
    }

    private void InitVisual() {
        // get unselected material:
        idleMaterial = componentRenderer.material;
    }
    
    public void SetThisComponentsVisualAsSelected(bool b) {
        componentRenderer.material = b ? selectedMaterial : idleMaterial;
    }

    public new void Interact() {
        // calculate nearest subPathPoint to player
        var localIndex = subPath.GetLocalPointIndexClosestToWorldPoint(
            worldPoint: PlayerController.Instance.transform.position);
        startLocalIndex = localIndex;
        
        // return relTime at which video should begin
        relStartTimeInSeconds = subPath.subPathPoints[localIndex].relTimeInSecs;
        if (DebugController.Instance)
            Debug.Log("DataLine.Interact(): relStartTimeInSeconds = " + relStartTimeInSeconds);
        
        base.Interact();
    }
    
    
}