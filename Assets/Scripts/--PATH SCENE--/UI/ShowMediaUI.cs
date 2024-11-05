using TMPro;
using UnityEngine;

public class ShowMediaUI : MonoBehaviour {

    [SerializeField] private TMP_Text quitHint;

    private void Start() {
        // update quit hint when key bindings get updated
        GameManager.Instance.OnButtonsRebound += (sender, args) => {
            var prefix = "quit: ";
            quitHint.text = prefix + GameManager.Instance.GetBindingText(GameManager.Bindings.QuitMedia);
        };
    }
    
}