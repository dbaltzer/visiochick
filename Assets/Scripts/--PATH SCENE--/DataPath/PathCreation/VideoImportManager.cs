using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;


[ExecuteAlways]
public class VideoImportManager : MonoBehaviour {

    public enum VideoSourceTypes {
        VideoClip,
        URL,
    }
    
    
    // singleton
    public static VideoImportManager Instance { get;  set; }
    
    
    // fields
    public VideoSourceTypes videoSourceType;

    [Header("URL")] 
    [Tooltip("address to parent folder (http or local)")] public string serverURL;
    [Tooltip("does not work for WebGL")] public bool useLocalFile;
    
    
    private void Awake() {
        // singleton:
        if(Instance != null)
            Debug.Log("There is more than one VideoImportManager instance!");
        Instance = this;
    }

}