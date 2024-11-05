using TMPro;
using UnityEngine;

public class InstructionsUI : MonoBehaviour {

    [SerializeField] private TMP_Text moveText;
    [SerializeField] private TMP_Text sprintText;
    [SerializeField] private TMP_Text interactText;
    [SerializeField] private TMP_Text selectText;
    [SerializeField] private TMP_Text perspectiveText;
    [SerializeField] private TMP_Text openMenuText;

    private string _moveTextPrefix = "<i>move:</i>\n";
    private string _sprintTextPrefix = "<i>sprint:</i>\n";
    private string _interactTextPrefix = "<i>interact:</i>\n";
    private string _selectTextPrefix = "<i>shuffle selection:</i>\n";
    private string _perspectiveTextPrefix = "<i>toggle perspective:</i>\n";
    private string _openMenuTextPrefix = "<i>menu:</i>\n";

    private void Start() {
        // subscribe to buttons rebound event
        GameManager.Instance.OnButtonsRebound += (sender, args) => { UpdateInstructionsVisual(); };
        
        UpdateInstructionsVisual();
    }

    private void UpdateInstructionsVisual() {
        moveText.text = _moveTextPrefix +
                        GameManager.Instance.GetBindingText(GameManager.Bindings.MoveUp) +
                        GameManager.Instance.GetBindingText(GameManager.Bindings.MoveLeft) +
                        GameManager.Instance.GetBindingText(GameManager.Bindings.MoveDown) +
                        GameManager.Instance.GetBindingText(GameManager.Bindings.MoveRight) +
                        " & " +
                        GameManager.Instance.GetBindingText(GameManager.Bindings.Rotation);
        sprintText.text = _sprintTextPrefix + 
                          GameManager.Instance.GetBindingText(GameManager.Bindings.Running);
        interactText.text = _interactTextPrefix +
                            GameManager.Instance.GetBindingText(GameManager.Bindings.Interact);
        selectText.text = _selectTextPrefix +
                          GameManager.Instance.GetBindingText(GameManager.Bindings.ShuffleSelection);
        perspectiveText.text = _perspectiveTextPrefix +
                               GameManager.Instance.GetBindingText(GameManager.Bindings.ToggleView);
        openMenuText.text = _openMenuTextPrefix +
                            GameManager.Instance.GetBindingText(GameManager.Bindings.OpenMenu);
    }
}