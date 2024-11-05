using System;
using UnityEngine;
using UnityEngine.UI;

public class SliderController : MonoBehaviour {
    [HideInInspector] public bool isDragging = false;
    [SerializeField] private Slider slider;
    private float _valueBuffer;

    private void Start() {
        if (!slider)
            slider = GetComponent<Slider>();
        slider.maxValue = 1f;
        slider.minValue = 0f;
        slider.onValueChanged.AddListener(HandleSliderValueChanged);
    }

    public void SetDrag(bool dragging) {
        isDragging = dragging;
        if (!isDragging) MediaPlayer.Instance.SliderValueChanged(_valueBuffer);
    }

    public void HandleSliderValueChanged(float value) {
        if (isDragging) _valueBuffer = value;
    }

    private void Update() {
        if(!isDragging)
            slider.value = MediaPlayer.Instance.GetProgressRatio();
    }
}