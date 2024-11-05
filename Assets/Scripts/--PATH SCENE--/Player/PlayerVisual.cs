using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerController))]
public class PlayerVisual : MonoBehaviour {
    /*
     * handles player animation
     */

    #region FIELDS

    
    [SerializeField] private GameObject playerVisual;
    
    private CharacterController _characterController;
    private Animator _animator;

    
    #endregion

    
    
    #region MONOBEHAVIOR

    
    private void Start() {
        // get components if not already set
        _animator ??= playerVisual.GetComponent<Animator>();
        _characterController ??= gameObject.GetComponent<CharacterController>();
    }

    private void Update() {
        
        SetWalkingAnimation();

    }

    #endregion
    
    
    
    #region PRIVATE_METHODS
    
    /// <summary>
    /// sets running animation parameter according to current player speed
    /// </summary>
    private void SetWalkingAnimation() {
        // get speed as percent with runningSpeed = 100%
        var speedPercent = _characterController.velocity.magnitude / PlayerMovement.Instance.RunningSpeed;
        
        if (!_characterController.isGrounded)
            speedPercent = 0.0f;
        
        _animator.SetFloat("speed", speedPercent);
    }

    
    #endregion
    
}