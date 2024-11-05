using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using PathCreation;


[ExecuteInEditMode]
[RequireComponent(typeof(ToggleFlow.Data), typeof(LineRenderer), typeof(PathCreator))]
public class DataPathVisual : MonoBehaviour {

    [Space]
    public GameObject lineRendererPrefab;
    public List<Material> alternatingSubPathMaterials;
    
    [Space]
    [SerializeField] private PathCreator _pathCreator;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private DataPath _dataPath;
    [SerializeField] private ProjectOntoGround _projectOntoGround;

    [SerializeField] private List<(SubPath, LineRenderer)> _subPathLineRendererList = new List<(SubPath, LineRenderer)>();
    private Mesh _mesh;
    

    private void Start() {
        Debug.Log("DataPathVisual.Start();");
        // subscribe to events
        _projectOntoGround ??= GetComponent<ProjectOntoGround>();
        
        _projectOntoGround.OnPathGeometryChanged += (sender, e) => {
            Debug.Log("DataPathVisual.OnPathGeomitryChanged called");
            // update visual:
            UpdateDataPathLine();
            UpdateSubPathLines();
        };
        
        
        /*
        EditorApplication.update += () => {
            UpdateDataPathLine();
            UpdateSubPathLines();
        };
        */

        // get components
        // syntax note: [var] ??= [obj]; <=> if([var] == null) [var] = [obj];
        lineRenderer ??= GetComponent<LineRenderer>();
        _pathCreator ??= GetComponent<PathCreator>();
        _dataPath ??= GetComponent<DataPath>();

    }
    
    

    public void MakeDataPathAndSubPathLines() {
        // 1) make line through whole path
        MakeDataPathLine();
        
        // extra lines for subpaths
        MakeSubPathLines();

    }
    

    private void MakeDataPathLine() {
        var verts = _pathCreator.path.localPoints.ToArray();
        var translation = new Vector3(0f, -0.01f, 0f);
        verts = verts.Select(x => x + translation).ToArray();
        var width = 0.05f;
        
        lineRenderer.useWorldSpace = false;
        lineRenderer.endWidth = width;
        lineRenderer.startWidth = width;
        lineRenderer.positionCount = verts.Length;
        
        lineRenderer.SetPositions(verts);
    }

    private void UpdateDataPathLine() {
        var verts = _pathCreator.path.localPoints.ToArray();
        var translation = new Vector3(0f, -0.01f, 0f);
        verts = verts.Select(x => x + translation).ToArray();
        
        lineRenderer.SetPositions(verts);
    }
    

    private void MakeSubPathLines() {

        // if we already have lines for the subpaths: clear
        /*
        if (_lineRendererList != null) {
            foreach (var lrPerSubPath in _lineRendererList) {
                DestroyImmediate(lrPerSubPath.Item2);
            }
            this._lineRendererList.Clear();
            
        }
        else
        */ 
        this._subPathLineRendererList = new List<(SubPath, LineRenderer)>();
        
        
        
        var materialIndex = 0;  // to alternate between subpath materials
        
        foreach (var subPath in _dataPath.subPathList) {

            if (subPath.subPathPoints.Count <= 1) continue;
            
            
            
            // get positions:
            var startVertex =
                _dataPath.GetVertexPathIndexOfBezierAnchorPoint(
                    subPath.subPathPoints[0].globalIndex);
            var stopVertex =
                _dataPath.GetVertexPathIndexOfBezierAnchorPoint(
                    subPath.subPathPoints[^1].globalIndex);
            

            var localVertexPositions = new List<Vector3>();
            for (int i = startVertex; i < stopVertex+1; i++) {
                localVertexPositions.Add(_pathCreator.path.localPoints[i] 
                               - subPath.gameObject.transform.localPosition);
            }
            
            
            
            // init new line Object as child of DataSpot
            var line = Instantiate(lineRendererPrefab, parent: subPath.transform);
            var subPathLineRenderer = line.GetComponent<LineRenderer>();

            line.name = "DataLine: #" + subPath.dataSpot.name.Split("#")[^1];
            
            
            // parse all values to linerenderer
            var verts = localVertexPositions.ToArray();
            var width = 0.05f;
        
            subPathLineRenderer.useWorldSpace = false;
            subPathLineRenderer.endWidth = width;
            subPathLineRenderer.startWidth = width;
            subPathLineRenderer.positionCount = verts.Length;
            
            
            // alternate between materials // TODO: one could also assign the material according to datatype
            subPathLineRenderer.material =
                alternatingSubPathMaterials[materialIndex % alternatingSubPathMaterials.Count];
            materialIndex++;
            
            // set positions
            subPathLineRenderer.SetPositions(localVertexPositions.ToArray());
            
            // pass dataLine to SubPath
            subPath.dataLine = line.GetComponent<DataLine>();
            
            // pass projectOntoGround to line
            line.GetComponent<LineColliderInit>().projectOntoGround = _projectOntoGround;
            
            
            // add new object to list
            this._subPathLineRendererList.Add((subPath, line.GetComponent<LineRenderer>()));

        }
    }


    public void UpdateSubPathLines() {
        Debug.Log("UpdateDataPathLines with " + _subPathLineRendererList.Count + " lines.");
        foreach (var subPathLineRendererPair in _subPathLineRendererList) {
            // get positions:
            var startVertex =
                _dataPath.GetVertexPathIndexOfBezierAnchorPoint(
                    subPathLineRendererPair.Item1.subPathPoints[0].globalIndex);
            var stopVertex =
                _dataPath.GetVertexPathIndexOfBezierAnchorPoint(
                    subPathLineRendererPair.Item1.subPathPoints[^1].globalIndex);
            

            var localVertexPositions = new List<Vector3>();
            for (int i = startVertex; i < stopVertex+1; i++) {
                localVertexPositions.Add(_pathCreator.path.localPoints[i] 
                                         -  subPathLineRendererPair.Item1.gameObject.transform.localPosition);
            }
            
            // set positions:
            subPathLineRendererPair.Item2.SetPositions(localVertexPositions.ToArray());
        }
    }
    
}