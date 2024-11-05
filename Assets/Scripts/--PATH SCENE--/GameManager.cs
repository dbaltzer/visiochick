using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

//[ExecuteAlways]
public class GameManager : MonoBehaviour {

    // singleton
    public static GameManager Instance { get; private set; }
    
    
    // game states
    public enum PlayerType {
        DefaultPlayer,
        VRPlayer,
    }
    
    public enum GameState {
        WalkAround,
        ViewMedia,
        Menu,
    }

    public enum Layers {
        Default,
        TransparentFX,
        IgnoreRaycast,
        Interactables,
        Water,
        UI,
        DoorOpener,
        Player,
    }

    public enum Bindings {
        MoveUp,
        MoveLeft,
        MoveRight,
        MoveDown,
        Jump,
        Rotation,
        Running,
        ToggleView,
        Interact,
        QuitMedia,
        ShuffleSelection,
        OpenMenu,
    }
    
    
    // events
    public event EventHandler<OnGameStateChangedEventArgs> OnGameStateChanged;

    public class OnGameStateChangedEventArgs : EventArgs {
        public GameState TargetGameState;
    }

    public event EventHandler<EventArgs> OnVideoPrepared;

    public event EventHandler<EventArgs> OnButtonsRebound;
    

    
    // Fields
    public PlayerType playerType;
    public GameState gameState;
    public bool playerCollidesWithInteractables;

    private const string REBINDS = "rebinds";
    private const string DELTA_SENSITIVITY = "delta_sensitivity";
    
    public InputActions InputActions;
    private PlayerInput _playerInput;

    private int _mouseSensitivityBuffer = 40;
    
    // for managing players
    [Space] 
    [SerializeField] private GameObject Player;
    [SerializeField] private GameObject VRPlayer;
    [SerializeField] private GameObject XRDeviceSimulator;
    [SerializeField] private GameObject XRInteractionManager;

    
    
    #region MONOBEHAVIOR_METHODS

    private void Awake() {
        
        // singleton
        if (Instance != null)
            Debug.Log("There is more than one GameManager");
        Instance = this;
        
        // initPlayer
        InitPlayerType();
        
        // subscribe to events:
        SubPathInteractableComponent.OnInteracted += PathInteractableOnInteracted;
        OnGameStateChanged += GameManagerOnGameStateChanged;
    
        
        // INPUT SYSTEM:
        InputActions = new InputActions();
        
        // load player prefs
        if (PlayerPrefs.HasKey(DELTA_SENSITIVITY))
            _mouseSensitivityBuffer = PlayerPrefs.GetInt(DELTA_SENSITIVITY);   // cant access PlayerMovement.Instance on Awake
        if(PlayerPrefs.HasKey(REBINDS))
            InputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(REBINDS));
        
        InputActions.Default.Enable();
        
        
        // subscribe to input actions
        InputActions.Default.QuitMedia.performed += QuitMedia;
        InputActions.Default.OpenMenu.performed += OpenMenu;
    }
    

    private void Start() {
        
        gameState = GameState.WalkAround;
        SetCursor(locked: true);
        
        // set player pref
        PlayerMovement.Instance.mouseSensitivity = _mouseSensitivityBuffer;
        
        // ignore collisions between objects on layer interactable (doors)
        Physics.IgnoreLayerCollision((int) Layers.Interactables, (int) Layers.Interactables);
        // ignore collisions between objects on default layer and dooropener
        Physics.IgnoreLayerCollision((int) Layers.Default, (int) Layers.DoorOpener);
        // ignore between interactables and defualt
        Physics.IgnoreLayerCollision((int) Layers.Interactables, (int) Layers.Default);
        
        
        if(!playerCollidesWithInteractables)
            // ignore between interactables and player (doors dont ram into player)
            Physics.IgnoreLayerCollision((int) Layers.Interactables, (int) Layers.Player);
        
        
        // all assets in the Resources folder get loaded automatically on runtime.
        // clean up some space by unloading unused assets
        Resources.UnloadUnusedAssets();
        
        // get component
        if (!_playerInput) _playerInput = GetComponent<PlayerInput>();
    }

    /// <summary>
    /// editor only function that updates when script is reloaded or value is changed
    /// </summary>
    private void OnValidate() {
        InitPlayerType();
    }


    #region EVENT_FUNCTIONS
    

    private void PathInteractableOnInteracted(object sender, SubPathInteractableComponent.OnInteractedEventArgs e) {
        OnGameStateChanged?.Invoke(
            this,
            new OnGameStateChangedEventArgs {
                TargetGameState = GameState.ViewMedia
            });
    }

    
    private void GameManagerOnGameStateChanged(object sender, OnGameStateChangedEventArgs e) {
        gameState = e.TargetGameState;
        switch (e.TargetGameState) {
            case GameState.WalkAround:
                SetCursor(locked: true);
                break;
            case GameState.ViewMedia:
                SetCursor(locked: false);
                break;
            case GameState.Menu:
                SetCursor(locked: false);
                break;
            default:
                SetCursor(locked: true);
                break;
        }
        
    }
    
    #endregion


    private void InitPlayerType() {
        switch (playerType) {
            case PlayerType.VRPlayer:
                Player.SetActive(false);
                VRPlayer.SetActive(true);
                XRDeviceSimulator.SetActive(true);
                XRInteractionManager.SetActive(true);
                break;
            case PlayerType.DefaultPlayer:
                Player.SetActive(true);
                VRPlayer.SetActive(false);
                XRDeviceSimulator.SetActive(false);
                XRInteractionManager.SetActive(false);
                break;
        }
    }

    public void SetGameStateTo(GameState targetGameState) {
        OnGameStateChanged?.Invoke(this, new OnGameStateChangedEventArgs {
            TargetGameState = targetGameState
        });
    }
    

    private void QuitMedia(InputAction.CallbackContext context) {
        if (gameState != GameState.ViewMedia) return;
        
        OnGameStateChanged?.Invoke(
            this,
            new OnGameStateChangedEventArgs {
                TargetGameState = GameState.WalkAround
            });
    }


    private void OpenMenu(InputAction.CallbackContext context) {
        OnGameStateChanged?.Invoke(this, new OnGameStateChangedEventArgs {
            TargetGameState = GameState.Menu
        });
    }
    
    
    private void SetCursor(bool locked) {
        if(locked) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }

    public void VideoPrepared() {
        OnVideoPrepared?.Invoke(this, EventArgs.Empty);
    }

    public void QuitGame() {
        Application.Quit();
    }
    
    
    public string GetBindingText(Bindings binding) {
        switch (binding) {
            case Bindings.Interact:
                return InputActions.Default.Interact.bindings[0].ToDisplayString();
            case Bindings.OpenMenu:
                return InputActions.Default.OpenMenu.bindings[0].ToDisplayString();
            case Bindings.QuitMedia:
                return InputActions.Default.QuitMedia.bindings[0].ToDisplayString();
            case Bindings.Jump:
                return InputActions.Default.Jump.bindings[0].ToDisplayString();
            case Bindings.Running:
                return InputActions.Default.Running.bindings[0].ToDisplayString();
            case Bindings.Rotation:
                return InputActions.Default.Rotation.bindings[0].ToDisplayString();
            case Bindings.MoveUp:
                return InputActions.Default.Movement.bindings[1].ToDisplayString();
            case Bindings.MoveDown:
                return InputActions.Default.Movement.bindings[2].ToDisplayString();
            case Bindings.MoveLeft:
                return InputActions.Default.Movement.bindings[3].ToDisplayString();
            case Bindings.MoveRight:
                return InputActions.Default.Movement.bindings[4].ToDisplayString();
            case Bindings.ToggleView:
                return InputActions.Default.ToggleView.bindings[0].ToDisplayString();
            case Bindings.ShuffleSelection:
                return InputActions.Default.ShuffleSelection.bindings[0].ToDisplayString();
            default:
                return "";
        }
    }


    public void RebindBinding(Bindings binding) {
        InputActions.Default.Disable();

        InputAction inputAction;
        int bindingIndex = 0;

        switch (binding) {
            case Bindings.MoveUp:
                inputAction = InputActions.Default.Movement;
                bindingIndex = 1;
                break;
            case Bindings.MoveDown:
                inputAction = InputActions.Default.Movement;
                bindingIndex = 2;
                break;
            case Bindings.MoveRight:
                inputAction = InputActions.Default.Movement;
                bindingIndex = 4;
                break;
            case Bindings.MoveLeft:
                inputAction = InputActions.Default.Movement;
                bindingIndex = 3;
                break;
            case Bindings.OpenMenu:
                inputAction = InputActions.Default.OpenMenu;
                break;
            case Bindings.Interact:
                inputAction = InputActions.Default.Interact;
                break;
            case Bindings.Jump:
                inputAction = InputActions.Default.Jump;
                break;
            case Bindings.Running:
                inputAction = InputActions.Default.Running;
                break;
            case Bindings.ShuffleSelection:
                inputAction = InputActions.Default.ShuffleSelection;
                break;
            case Bindings.ToggleView:
                inputAction = InputActions.Default.ToggleView;
                break;
            case Bindings.Rotation:
                inputAction = InputActions.Default.Rotation;
                break;
            case Bindings.QuitMedia:
                inputAction = InputActions.Default.QuitMedia;
                break;
            default:
                inputAction = InputActions.Default.Movement;
                bindingIndex = 1;
                break;
        }

        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback => {

                callback.Dispose();
                InputActions.Default.Enable();
                
                OnButtonsRebound?.Invoke(this, EventArgs.Empty);

                // save bindings:
                PlayerPrefs.SetString(REBINDS, InputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();
            })
            .Start();
    }

    public void SaveDeltaSensitivityInPlayerPrefs() {
        PlayerPrefs.SetInt(DELTA_SENSITIVITY, PlayerMovement.Instance.mouseSensitivity);
        PlayerPrefs.Save();
    }
    
    #endregion
    
    
    
    
    
    
}