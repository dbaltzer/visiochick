using System;
using System.Collections.Generic;
using Cinemachine.Editor;
using UnityEngine;
using PathCreation;
using Unity.VisualScripting;
using Object = UnityEngine.Object;

[ExecuteAlways]
public class ProjectOntoGround : MonoBehaviour {
    // take bezierpath and project onto ground. update all connected renderers and meshes
    
    // TODO: raycast to any object??
    private enum ProjectionType {
        ToGroundObject,
        ToAny,
    }
    
    #region SENDING_EVENTS
    // BUG: events dont really work in edit mode bc unity is stupid. fuck my stupid baka life
    public event EventHandler<EventArgs> OnPathGeometryChanged;
    public event EventHandler<EventArgs> OnUpdateLineCollidersEditorButtonPressed;
    
    #endregion

    #region FIELDS

    // in inspector:
    [SerializeField] private bool autoProject = true;
    [SerializeField] [Range(0, 2)] private float yOffset = 0;
    [SerializeField] private Collider terrain;
    [SerializeField] private List<Object> updatableObjects;

    
    private PathCreator _pathCreator;
    private const float GROUND_MAX_HEIGHT = 2f;

    #endregion


    #region MONOBEHAVIOR_METHODS

    private void Start() {
        // syntax note: [var] ??= [obj]; <=> if([var] == null) [var] = [obj];
        _pathCreator ??= GetComponent<PathCreator>();

        this.OnPathGeometryChanged += (sender, args) => {
            Debug.Log("OnPathGeometryChanged called ");
        };
    }

    private void Update() {
        if (transform.hasChanged & autoProject) {
            Debug.Log("ProjectOntoGround.transformHasChnaged");
            // when we move the object around
            
            // 1) update path
            ProjectPathToGround();
            
            // 2) send event to update visuals / mesh
            OnPathGeometryChanged?.Invoke(this, EventArgs.Empty);
            //UpdateObjects();
            
            // reset flag
            transform.hasChanged = false;
        }
        
    }

    private void OnValidate() {
        // when value (offset) changed in inspector
        
        if(!autoProject) return;
            
        // 1) update path
        ProjectPathToGround();
            
        // 2) send event to update visuals / mesh
        OnPathGeometryChanged?.Invoke(this, EventArgs.Empty);
        //UpdateObjects();
    }

    #endregion

    
    #region METHODS

    private void UpdateObjects() {
        foreach (var obj in updatableObjects) {
            if (obj.GameObject().TryGetComponent(out DataSpotVisual dsVisual)) {
                dsVisual.UpdatePosition();
            }else if (obj.GameObject().TryGetComponent(out DataPathVisual dpVisual)) {
                dpVisual.UpdateSubPathLines();
            }
        }
    }

    public void SetTerrain(Collider terrainCollider) {
        terrain = terrainCollider;
    }

    [EditorCools.Button]
    public void UpdateLineColliders() {
        OnUpdateLineCollidersEditorButtonPressed?.Invoke(this, EventArgs.Empty);
    }

    [EditorCools.Button]
    public void UpdateAll() {
        // 1) update path
        ProjectPathToGround();
            
        // 2) send event to update visuals / mesh
        OnPathGeometryChanged?.Invoke(this, EventArgs.Empty);
        
        // 3) update colliders
        UpdateLineColliders();
    }

    
    /// <summary>
    /// sends raycast down from position and checks for intersection with collider
    /// </summary>
    /// <param name="x"> x position</param>
    /// <param name="z"> z position</param>
    /// <returns> y position of collider at (x, z) , if no collider: returns GROUND_MAX_HEIGHT </returns>
    private float GetYPosOfCollider(float x, float z) {
        // test point with raycast
        RaycastHit hit;
        Ray ray = new Ray(new Vector3(x, GROUND_MAX_HEIGHT, z), Vector3.down);

        if (terrain.Raycast(ray, out hit, 2.0f * GROUND_MAX_HEIGHT)) {
            return hit.point.y;
        }
        
        // collider not under position
        return GROUND_MAX_HEIGHT;
    }

    
    private void ProjectPathToGround() {
        if (!_pathCreator) return;
        for (int i = 0; i < _pathCreator.bezierPath.NumPoints; i++) {
            var thisPoint = _pathCreator.bezierPath.GetPoint(i);
            var newYPos = GetYPosOfCollider(transform.position.x + thisPoint.x,
                transform.position.z + thisPoint.z) + yOffset - transform.position.y;
            var newPointPos = new Vector3(thisPoint.x, newYPos, thisPoint.z);
            _pathCreator.bezierPath.SetPoint(i, newPointPos);
        }
    }

    #endregion

}