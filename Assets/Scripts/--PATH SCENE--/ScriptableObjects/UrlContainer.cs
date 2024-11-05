using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UrlContainer", menuName = "ScriptableObjects")]
public class UrlContainer : ScriptableObject {
    public string url;

    public void SetValues(string url) {
        this.url = url;
    }
}
