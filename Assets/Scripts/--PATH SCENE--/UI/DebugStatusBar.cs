using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class DebugStatusBar : MonoBehaviour {
    [SerializeField] private Image fill;
    [SerializeField] private Image background;
    private float _maxValue = 1f;

    public float _currentVal = 0f;

    private bool _isVisible;

    private void Start() {
        if (!background) background = GetComponent<Image>();
        if (!fill) fill = transform.GetChild(0).GetComponent<Image>();
        
        _currentVal = 0;
        UpdateVisual();
        SetVisible(false);

    }

    private void Update() {
        if(!_isVisible) return;
        UpdateVisual();
    }


    public void SetStatusBar(float maxVal) {
        // make visible
        SetVisible(true);
        // set max
        _maxValue = maxVal;
        // init with 0
        _currentVal = 0;
    }

    public void SetVal(float val) {
        _currentVal = val;
    }
    
    public void SetVisible(bool visible) {
        _isVisible = visible;
        fill.enabled = visible;
        background.enabled = visible;
    }


    private void UpdateVisual() {
        fill.transform.localScale = new Vector3(GetFillRatio(), 1f, 1f);
    }
    
    private float GetFillRatio() {
        if (_currentVal < 0) { // only pasitive numbrs allowed!
            return 0;
        } 
        
        if (_currentVal == 0) return 0f;

        return _currentVal / _maxValue;
    }
}