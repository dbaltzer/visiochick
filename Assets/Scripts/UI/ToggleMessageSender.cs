using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class ToggleMessageSender : MonoBehaviour {
    private Toggle _toggle;
    [SerializeField] private EscapeViewController.View view;

    private void Start() {
        _toggle = GetComponent<Toggle>();
        _toggle.isOn = false;
    }

    public void ToggleThisView() {
        EscapeViewController.Instance.SetView(view, _toggle.isOn);
    }

    private void OnEnable() {
        // set toggle to off
        if(_toggle)
            _toggle.isOn = false;
    }
}