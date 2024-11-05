using System;
using UnityEngine;


[ExecuteAlways] [RequireComponent(typeof(MeshCollider))]
public class LineColliderInit : MonoBehaviour {

    [SerializeField] private LineRenderer lineRenderer;

    [SerializeField] private MeshCollider meshCollider;

    public ProjectOntoGround projectOntoGround;

    
    private void Start() {
        Debug.Log("LineColliderInit.Start;");
        if(!meshCollider.sharedMesh)
            SetColliderMesh();

        projectOntoGround.OnUpdateLineCollidersEditorButtonPressed += UpdateLineCollidersLineColliders;
    }

    
    private void UpdateLineCollidersLineColliders(object sender, EventArgs e) {
        // Cannot update mesh in realtime (must bake a new mesh for every change) 
        Debug.Log("LineColliderInit.UpdateColliderMesh");
        
        if (lineRenderer.positionCount <= 2) return;    // this avoids errors while baking the mesh
        
        // 1) delete old mesh
        if (meshCollider.sharedMesh) meshCollider.sharedMesh = null;
        
        // 2) bake new mesh
        SetColliderMesh();

    }


    [EditorCools.Button]
    private void SetColliderMesh() {
        
        if (lineRenderer.positionCount <= 2) return; // this avoids errors while baking the mesh

        
        var mesh = new Mesh();
        mesh.name = transform.parent.name;
        lineRenderer.BakeMesh(mesh: mesh, camera: Camera.main);
        mesh.Optimize();
        
        if(!mesh) Debug.Log(transform.name + " mesh is null!!");
        
        meshCollider.sharedMesh = mesh;

    }
    
    
}