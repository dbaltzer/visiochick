using UnityEngine;


[RequireComponent(typeof(CharacterController), typeof(PathFollower), typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour {
    
    
    // singleton
    public static PlayerController Instance { get; private set; }
    
    
    // enums
    public enum PlayerType {
        DefaultPlayer,
        VRPlayer,
    }
    
    public enum PlayerState {
        NoMovement,
        LookAround,
        FreeWalk,
        FollowPath,
    }

    #region FIELDS

        // fields in editor
        public PlayerType playerType;
        public PlayerState currentPlayerState = PlayerState.FreeWalk;
    
        // private fields
        private PlayerMovement _playerMovement;
        private PlayerInteraction _playerInteraction;     
        private bool _selectedDataSpotHasFollowPath = false;
        
    #endregion
    
    
    private void Awake() {
        // singleton:
        if (Instance != null) 
            Debug.Log("There is more than one Player instance!");
        Instance = this;
        
        
        // get components
        if (!_playerInteraction) _playerInteraction = GetComponent<PlayerInteraction>();
        if (!_playerMovement) _playerMovement = GetComponent<PlayerMovement>();
    }

    private void Start() {
        // subscribe to events
        GameManager.Instance.OnGameStateChanged += GameManagerOnGameStateChanged;
        PlayerInteraction.Instance.OnSubPathComponentSelected += PlayerInteractionOnSubPathComponentSelected;
        PerspectiveManager.Instance.OnPerspectiveChanged += PerspectiveManagerOnPerspectiveChanged;
        
        UpdatePlayerState(PlayerState.FreeWalk);
    }
    
    
    #region EVENT_FUNCTIONS //  --------------------------------------------------------------
    
    private void PerspectiveManagerOnPerspectiveChanged(object sender, PerspectiveManager.OnPerspectiveChangedEventArgs e) {
        UpdatePlayerState();
    }

    private void PlayerInteractionOnSubPathComponentSelected(object sender,
        PlayerInteraction.OnSubPathComponentSelectedEventArgs e) {
        _selectedDataSpotHasFollowPath = e.Component.subPath.subPathPoints.Count > 1;
    }

    private void GameManagerOnGameStateChanged(object sender, GameManager.OnGameStateChangedEventArgs e) {
        switch (e.TargetGameState) {
            case GameManager.GameState.WalkAround:
                UpdatePlayerState(PlayerState.FreeWalk);
                break;
            
            case GameManager.GameState.ViewMedia:
                // depends on subPath
                var targetPlayerState = _selectedDataSpotHasFollowPath ? PlayerState.FollowPath : PlayerState.LookAround;
                UpdatePlayerState(targetPlayerState);
                break;
            
            case GameManager.GameState.Menu:
                UpdatePlayerState(PlayerState.NoMovement);
                break;
            default:
                break;
        }
    }
    
    #endregion // -----------------------------------------------------------------------------------------

    public void PathFollowPauseResume(bool pause) {
        PathFollower.Instance.PauseResume(pause);
    }

    public void UpdatePlayerState(PlayerState targetPlayerState) {
        currentPlayerState = targetPlayerState;
        
        switch (targetPlayerState) {
            case PlayerState.FreeWalk:
                _playerMovement.movementState =
                    PerspectiveManager.Instance.perspective == PerspectiveManager.PerspectiveState.FirstPerson
                        ? PlayerMovement.MovementType.FirstPerson
                        : PlayerMovement.MovementType.TopDown;
                _playerInteraction.interactionEnabled = true;
                break;
            
            case PlayerState.FollowPath:
                _playerInteraction.interactionEnabled = false;                
                _playerMovement.movementState = PlayerMovement.MovementType.NoAutonomousMovement;
                // movement handled by PathFollower
                break;
            
            case PlayerState.LookAround:
                if (PerspectiveManager.Instance.perspective == PerspectiveManager.PerspectiveState.FirstPerson)
                    _playerMovement.movementState = PlayerMovement.MovementType.FirstPersonNoTranslation;
                else _playerMovement.movementState = PlayerMovement.MovementType.NoAutonomousMovement;
                _playerInteraction.interactionEnabled = false;
                break;
            
            case PlayerState.NoMovement:
                _playerMovement.movementState = PlayerMovement.MovementType.NoAutonomousMovement;
                _playerInteraction.interactionEnabled = false;
                break;
        }
    }

    private void UpdatePlayerState() {
        UpdatePlayerState(currentPlayerState);
    }
    
}
