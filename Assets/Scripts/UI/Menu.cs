using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour {

    public enum MenuId {
        QuickMenu,
        ControlsMenu,
    }
    
    // singleton
    public static Menu Instance { private set; get; }
    

    [SerializeField] private GameObject menuCanvas;
    [SerializeField] private GameObject quickMenu;
    [SerializeField] private GameObject controlMenu;
    [SerializeField] private GameObject rebindNotice;

    [Header("Toggle")]
    [SerializeField] private Toggle enableVRToggle;
    
    [Header("Sliders")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField] private Slider deltaSensitivitySlider;
    [SerializeField] private TMP_Text deltaSensValueText;

    [Header("Binding Buttons:")] 
    [SerializeField] private Button forwardButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button sprintButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button shuffleSelectionButton;
    [SerializeField] private Button toggleViewButton;
    [SerializeField] private Button quitMediaButton;
    [SerializeField] private Button rotationButton;
    [SerializeField] private Button menuButton;
    [Header("Binding Button Texts:")]
    [SerializeField] private TMP_Text forwardButtonText;
    [SerializeField] private TMP_Text backButtonText;
    [SerializeField] private TMP_Text rightButtonText;
    [SerializeField] private TMP_Text leftButtonText;
    [SerializeField] private TMP_Text jumpButtonText;
    [SerializeField] private TMP_Text sprintButtonText;
    [SerializeField] private TMP_Text interactButtonText;
    [SerializeField] private TMP_Text shuffleSelectionButtonText;
    [SerializeField] private TMP_Text toggleViewButtonText;
    [SerializeField] private TMP_Text quitMediaButtonText;
    [SerializeField] private TMP_Text rotationButtonText;
    [SerializeField] private TMP_Text menuButtonText;

    private void Awake() {
        // singleton
        if (!Instance) Instance = this;
        else Debug.LogError("There is more than one Menu instance!");
        
        // events
        GameManager.Instance.OnButtonsRebound += (sender, args) => {
            rebindNotice.SetActive(false);
            UpdateBindingButtonsVisual();
        };
        
        // input rebinding
        forwardButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.MoveUp); });
        backButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.MoveDown); });
        rightButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.MoveRight); });
        leftButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.MoveLeft); });
        jumpButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.Jump); });
        sprintButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.Running); });
        interactButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.Interact); });
        shuffleSelectionButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.ShuffleSelection); });
        toggleViewButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.ToggleView); });
        quitMediaButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.QuitMedia); });
        rotationButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.Rotation); });
        menuButton.onClick.AddListener(() => { RebindBinding(GameManager.Bindings.OpenMenu); });
        
        // sliders
        volumeSlider.onValueChanged.AddListener((newValue) => {
            AudioListener.volume = newValue;
            volumeValueText.text = (int)(newValue * 100)+ " %";
        });
        
        deltaSensitivitySlider.onValueChanged.AddListener((newValue) => {
            var val = (int)newValue;
            PlayerMovement.Instance.mouseSensitivity = val;
            GameManager.Instance.SaveDeltaSensitivityInPlayerPrefs();
            deltaSensValueText.text = val.ToString();
        });
    }
    
    private void Start() {
        // subscribe to events:
        GameManager.Instance.OnGameStateChanged += GameManagerOnGameStateChanged;
        
        // get canvas (if not already set)
        if (!menuCanvas) menuCanvas = transform.GetChild(0).gameObject;
        
        // initial state:
        ToggleActiveMenu(MenuId.QuickMenu);
        rebindNotice.SetActive(false);
        
        UpdateBindingButtonsVisual();
        volumeSlider.value = AudioListener.volume;
        volumeValueText.text = (int)(volumeSlider.value * 100) + " %";
        deltaSensitivitySlider.value = PlayerMovement.Instance.mouseSensitivity;
        deltaSensValueText.text = deltaSensitivitySlider.value.ToString();
        HideMenu();

    }

    private void GameManagerOnGameStateChanged(object sender, GameManager.OnGameStateChangedEventArgs e) {
        if(e.TargetGameState == GameManager.GameState.Menu) {
            ShowMenu();        
            ToggleActiveMenu(MenuId.QuickMenu);
        }
        else HideMenu();
    }

    
    #region PrivateMethods

    private void ShowMenu() {
        menuCanvas.SetActive(true);
    }

    private void HideMenu() {
        menuCanvas.SetActive(false);
    }

    private void ToggleActiveMenu(MenuId activeMenu) {
        switch (activeMenu) {
            case MenuId.ControlsMenu:
                controlMenu.SetActive(true);
                quickMenu.SetActive(false);
                break;
            case MenuId.QuickMenu:
                quickMenu.SetActive(true);
                controlMenu.SetActive(false);
                break;
        }
    }

    #endregion

    

    #region Buttons

    public void OnResumeButtonPressed() {
        GameManager.Instance.SetGameStateTo(GameManager.GameState.WalkAround);
    }

    public void OnQuitButtonPressed() {
        GameManager.Instance.QuitGame();
    }

    public void OnControlsButtonPressed() {
        ToggleActiveMenu(activeMenu: MenuId.ControlsMenu);
    }

    public void OnBackButtonPressed() {
        ToggleActiveMenu(activeMenu: MenuId.QuickMenu);
    }
    

    #endregion

    private void RebindBinding(GameManager.Bindings binding) {
        rebindNotice.SetActive(true);

        GameManager.Instance.RebindBinding(binding);

    }
    

    private void UpdateBindingButtonsVisual() {
        // update all buttons with new bindings
        forwardButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.MoveUp);
        backButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.MoveDown);
        rightButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.MoveRight);
        leftButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.MoveLeft);
        menuButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.OpenMenu);
        jumpButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.Jump);
        sprintButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.Running);
        interactButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.Interact);
        shuffleSelectionButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.ShuffleSelection);
        toggleViewButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.ToggleView);
        quitMediaButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.QuitMedia);
        rotationButtonText.text = GameManager.Instance.GetBindingText(GameManager.Bindings.Rotation);
    }
    
}