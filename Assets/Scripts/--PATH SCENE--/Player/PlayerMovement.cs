using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour {

    private const float Gravity = -20f;

    public enum MovementType {
        FirstPerson,
        TopDown,
        FirstPersonNoTranslation,
        NoAutonomousMovement,
    }
    
    #region FIELDS // --------------------------------------------------------------------------------------------------

    // singleton
    public static PlayerMovement Instance { get; private set; }

    [SerializeField] private Transform VRCamTransform;
    [SerializeField] private Vector3 VRCamOffset;

    
    // visual in inspector
    [Header("Translation")] // ---------------------------------
    [SerializeField] private float walkingSpeed = 3f;
    public float RunningSpeed { get; private set; } = 6f;
    [SerializeField] private float jumpingSpeed = 5f;

    [SerializeField] private Vector3 topDownForward;
    [SerializeField] private Vector3 topDownRight;
    
    
    [Space(10)]
    
    
    [Header("Rotation")] // ---------------------------------
    [SerializeField] public int mouseSensitivity = 100;
    
    [Tooltip("will reset rotation if init in illegal range")]
    [SerializeField] private bool enableHardReset;
    
    
    [Space(10)]

    
    [Header("Camera")] // ---------------------------------
    [SerializeField] private Transform xRotation;
    [SerializeField] private Transform topDownCamera;

    
    // invisible in inspector
    private CharacterController _characterController;
    private PlayerController _playerController;
    private Vector3 _movementVector = Vector3.zero;

    public MovementType movementState;
    

    #endregion

    #region MONOBEHAVIOR_METHODS // ------------------------------------------------------------------------------------

    private void Awake() {
        // singleton:
        if(Instance != null)
            Debug.Log("There is more than one PlayerMovement instance!");
        Instance = this;

    }

    private void Start() {
        // get componentes
        _characterController = GetComponent<CharacterController>();
        _playerController = GetComponent<PlayerController>();
        
        // set top-down directions
        // 1) get rotation of top-down camera
        var camForward = topDownCamera.forward;
        var camRight = topDownCamera.right;
        // 2) set direction for top-down movement
        topDownForward = Vector3.Normalize(new Vector3(camForward.x, 0f, camForward.z));
        topDownRight = Vector3.Normalize(new Vector3(camRight.x, 0f, camRight.z));
    }

    private void Update() {
        
        switch (movementState) {
            case MovementType.FirstPerson:
                
                switch (_playerController.playerType) {
                    case PlayerController.PlayerType.DefaultPlayer:
                        // First Person & Default Player
                        FirstPersonMove();
                        break;
                    
                    case PlayerController.PlayerType.VRPlayer:
                        // First Person & VR Player
                        VRFirstPersonMove();
                        break;
                }
                break;
            
            
            case MovementType.TopDown:
                TopDownMove();
                break;
            case MovementType.FirstPersonNoTranslation:
                if(_playerController.playerType == PlayerController.PlayerType.DefaultPlayer)
                    FirstPersonRotation();
                break;
            case MovementType.NoAutonomousMovement:
                // dont move
                break;
        }
        
    }

    #endregion
    

    #region METHODS_DEFUALT_PLAYER

    // FIRST PERSON MOVEMENT METHODS
    
    private void FirstPersonMove() {
        FirstPersonTranslation();
        FirstPersonRotation();
    }
    
    
    private void FirstPersonTranslation() {

        var oldMovementVectorY = _movementVector.y;
        
        // get direction
        var movementInput = GameManager.Instance.InputActions.Default.Movement.ReadValue<Vector2>();
        var inputVector = transform.forward * movementInput.y +
                          transform.right * movementInput.x;
        
        
        // clamp 
        _movementVector = Vector3.ClampMagnitude(inputVector, 1f);
        
        // add speed
        var isRunning = GameManager.Instance.InputActions.Default.Running.phase == InputActionPhase.Performed;
        var speed = isRunning ? RunningSpeed : walkingSpeed;
        _movementVector *= speed;
        
        // add jumping (spacebar)
        var jumpPerformed = GameManager.Instance.InputActions.Default.Jump.phase == InputActionPhase.Performed;

        if (_characterController.isGrounded && jumpPerformed)
            _movementVector.y = jumpingSpeed;
        else
            _movementVector.y = oldMovementVectorY;

        
        // add gravity ( * deltatime bc its an acceleration )
        if (!_characterController.isGrounded)
            _movementVector += new Vector3(0, Gravity, 0) * Time.deltaTime;
        
        // apply
        _characterController.Move(_movementVector * Time.deltaTime);
        
    }


    public void FirstPersonRotation() {
        /*
         * view and direction controlled by mouse movement
         */
        
        
        // B) ROTATION BY MOUSE MOVEMENT:
        var mouseDelta = GameManager.Instance.InputActions.Default.Rotation.ReadValue<Vector2>();
        
        // 1) VERTICAL ROTATION (around y axis) applied to the whole player:
        // get input
        var dYRotation = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        // fix bug where camera abruptly turns at start:
        if (dYRotation is > 100 or < -100) dYRotation = 0;
        
        // apply delta rotation
        transform.Rotate(0f, dYRotation, 0f);
        
        
        // 2) HORIZONTAL ROTATION (around x axis) only applied to child object xRotation:
        // get input
        var dXRotation = -mouseDelta.y * mouseSensitivity * Time.deltaTime;
        // fix bug where camera abruptly turns at start:
        if (dXRotation is > 100 or < -100) dXRotation = 0;
        
        // clamp 
        var oldXAngle = xRotation.localEulerAngles.x;
        var newXAngle = oldXAngle + dXRotation;
        var minXRotation = 300;
        var maxXRotation = 60;
        
        newXAngle = ClampDegree(newXAngle, minXRotation, maxXRotation);
        
        // apply rotation
        xRotation.Rotate(newXAngle - oldXAngle, 0f, 0f);
    }
    
 
    /// <param name="value"> in range [0, ..., 360]</param>
    /// <param name="min"> in range [0, ..., 360] </param>
    /// <param name="max"> in range [0, ..., 360] </param>
    private float ClampDegree(float value, float min, float max) {
        
        // [0, ..., min, ..., max, ..., 360]
        if (min < max) return Mathf.Clamp(value, min, max);
        
        // [0, ..., max, ..., min, ..., 360]
        if (value > min || value < max) return value;
        
        var distToMin = Mathf.Abs(value - min);
        var distToMax = Mathf.Abs(value - max);
        return distToMin < distToMax ? min : max;
    }
    
    
    
    /// TOP DOWN VIEW MOVEMENT METHOD
    public void TopDownMove() {
        
        var oldMovementVectorY = _movementVector.y;
        
        // translation & rotation both controlled by wasd
        var movementInput = GameManager.Instance.InputActions.Default.Movement.ReadValue<Vector2>();
        var leftRightValue = movementInput.x;
        var upDownValue = movementInput.y;
        
        
        // add horizontal & vertical, clamp to 1 (its like normalization but without loosing the movement slowing down)
        var movementDirection = leftRightValue * topDownRight + upDownValue * topDownForward;
        movementDirection = Vector3.ClampMagnitude(movementDirection, 1f);
        
        // times speed
        var isRunning = GameManager.Instance.InputActions.Default.Running.phase == InputActionPhase.Performed;
        var speed = isRunning? RunningSpeed : walkingSpeed;
        _movementVector = movementDirection * speed;

        // add jumping
        var jumpPerformed = GameManager.Instance.InputActions.Default.Jump.phase == InputActionPhase.Performed;
        if (_characterController.isGrounded && jumpPerformed)
            _movementVector.y = jumpingSpeed;
        else
            _movementVector.y = oldMovementVectorY;

        
        // add gravity ( * deltatime bc its an acceleration )
        if (!_characterController.isGrounded)
            _movementVector += new Vector3(0, Gravity, 0) * Time.deltaTime;
        
        // apply
        _characterController.Move(_movementVector * Time.deltaTime);
        
        
        // rotate player towards movement direction
        var rotateSpeed = 10f;
        if (movementDirection != Vector3.zero)
            transform.forward = Vector3.Slerp(transform.forward, movementDirection.normalized,
                Time.deltaTime * rotateSpeed);

    }
    
    
    #endregion

    
    
    #region METHODS_VR_PLAYER

    private void VRFirstPersonMove() {
        // copy x and z translation
        transform.position = new Vector3(VRCamTransform.position.x, 0f, VRCamTransform.position.z);
        
        // copy y rotation:
        //transform.rotation.eulerAngles = new Vector3(0f, VRCamTransform.rotation.eulerAngles.y, 0f);
    }

    #endregion

    
}