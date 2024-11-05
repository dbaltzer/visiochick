using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
public class Door : MonoBehaviour, INteractable {

    // enum
    private enum OpenDirection {
        clockwise,
        counterClockwise,
    }


    #region FIELDS


    // fields in editor
    public bool automaticDoor = true;

    [SerializeField] private OpenDirection openDirection;
    [Range(0f, 90f)] [SerializeField] private float openToDegree = 90;


    [Space]
    [Header("Animation Controls")]
    // controls the open-speed at a specific time (ex. opens fast at start and slows down at end)
    [SerializeField]
    private AnimationCurve animationSpeedCurve = new AnimationCurve(new Keyframe[] {
        new Keyframe(0, 1, 0, 0), new Keyframe(1, 0.3f, 0, 0)
    });

    [SerializeField] private float animationSpeedScale = 5f;



    // hidden fields
    private bool _isClosed = true;
    private Rigidbody _rigidbody;

    private bool _activeAnimation = false;
    private float _animationTime;

    private float _initRotationAngle;
    private float _currentRotationAngle;
    private float _targetRotationAngle;

    #endregion


    #region MONOBEHAVIOR_METHODS

    private void Awake() {
        // set layerMask to interactables (3)
        gameObject.layer = 3;
    }

    private void Start() {
        // get components
        _rigidbody = GetComponent<Rigidbody>();

        // set rigidbody
        _rigidbody.isKinematic = true;
        
        // get init rotation
        _initRotationAngle = transform.localEulerAngles.z;

    }


    private void Update() {

        // no animation
        if (!_activeAnimation) return;

        // animation finished
        if (_animationTime >= 1f) {
            _activeAnimation = false;
            return;
        }

        // animator door rotation:
        _animationTime += Time.deltaTime * animationSpeedScale * animationSpeedCurve.Evaluate(_animationTime);

        transform.localEulerAngles = new Vector3(
            transform.localEulerAngles.x,
            transform.localEulerAngles.y,
            Mathf.LerpAngle(_currentRotationAngle, _targetRotationAngle, _animationTime));


    }

    #endregion

    
    
    #region EVENTS


    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.name == "DoorOpener" && automaticDoor) {
            Open();
        }
    }


    private void OnTriggerExit(Collider other) {
        if (other.gameObject.name == "DoorOpener" && automaticDoor) {
            Close();
        }
    }

    #endregion
    

    #region METHODS
    
    public void Interact() {
        Debug.Log(this.ToString() + " Interact()");
        Toggle();
    }

    private void Open() {
        if (!_isClosed || _activeAnimation)
            return;


        _animationTime = 0f;
        _targetRotationAngle = (openDirection == OpenDirection.clockwise) ? openToDegree : -openToDegree;
        _currentRotationAngle = transform.localEulerAngles.z;

        _activeAnimation = true;
        _isClosed = !_isClosed;

    }

    private void Close() {
        if (_isClosed || _activeAnimation)
            return;


        _animationTime = 0f;
        _targetRotationAngle = _initRotationAngle;
        _currentRotationAngle = transform.localEulerAngles.z;

        _activeAnimation = true;
        _isClosed = !_isClosed;
    }

    private void Toggle() {
        if (_isClosed) Open();
        else Close();
    }


    #endregion
    

}

