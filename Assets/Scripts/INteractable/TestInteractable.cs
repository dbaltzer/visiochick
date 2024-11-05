using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestInteractable : MonoBehaviour, INteractable {
    public void Interact() {
        gameObject.transform.GetComponent<Renderer>().material.color = Random.ColorHSV();
    }
}