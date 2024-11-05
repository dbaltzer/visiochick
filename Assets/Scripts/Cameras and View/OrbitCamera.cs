using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Basic third person follower camera with adjustment options

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour {

    [SerializeField]
    [Tooltip("Object for the camera to focus on and follow.")]
    Transform focus = default;

    [SerializeField, Range(1f, 20f)]
    [Tooltip("Distance in m between camera and focus object.")]
    float distance = 5f;

    [SerializeField, Min(0f)]
    [Tooltip("Use scalable focus radius instead of object centre to allow small object movements without the camera following.")]
    float focusRadius = 1f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("Moves camera focus to object centre after a movement with adjustable speed. 0.5 means halving the distance every second.")]
    float focusCentering = 0.5f;

    [SerializeField, Range(1f, 360f)]
    [Tooltip("Speed for the camera orbiting around the object in degrees per second.")]
    float rotationSpeed = 90f;

    [SerializeField, Range(-89f, 89f)]
    [Tooltip("Camera vertical rotation limits in degrees.")]
    float minVerticalAngle = -30f, maxVerticalAngle = 60f;

    [SerializeField, Min(0f)]
    [Tooltip("Delay in seconds for the camera view to align with the last object movement direction.")]
    float alignDelay = 5f;

    [SerializeField, Range(0f, 90f)]
    [Tooltip("Scales the camera direction alignment speed for orientation differences up to the given value in degrees. For higher values the value for rotationSpeed will be used.")]
    float alignSmoothRange = 45f;

    [SerializeField]
    [Tooltip("Allows camera to ignore small geometries when performing the box cast to check if the sight on the object is free.")]
    LayerMask obstructionMask = -1;

    //3D point the camera focusses on and previous point
    Vector3 focusPoint, previousFocusPoint;
    //Angles of the camera in the orbit around the followed object
    Vector2 orbitAngles = new Vector2(45f, 0f);
    //Last point in time of a manual rotation gets saved to allow adjustable camera direction alignment speed
    float lastManualRotationTime;
    //Camera GameObject
    Camera regularCamera;
    //Calculation of camera half extends to be used for the detection of objects or terrain between focussed object and camera based on Box Cast
    Vector3 CameraHalfExtends
    {
        get
        {
            Vector3 halfExtends;
            halfExtends.y =
                regularCamera.nearClipPlane *
                Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
            halfExtends.x = halfExtends.y * regularCamera.aspect;
            halfExtends.z = 0f;
            return halfExtends;
        }
    }

    //Checks if the vertical camera angle limits are valid
    void OnValidate()
    {
        if (maxVerticalAngle < minVerticalAngle)
        {
            maxVerticalAngle = minVerticalAngle;
        }
    }

    //Constrains the angles to the given extents for vertical and 360 degrees for horizontal direction
    void ConstrainAngles()
    {
        orbitAngles.x =
            Mathf.Clamp(orbitAngles.x, minVerticalAngle, maxVerticalAngle);

        if (orbitAngles.y < 0f)
        {
            orbitAngles.y += 360f;
        }
        else if (orbitAngles.y >= 360f)
        {
            orbitAngles.y -= 360f;
        }
    }

    //Sets the initial camera focus 
    void Awake()
    {
        regularCamera = GetComponent<Camera>();
        focusPoint = focus.position;
        transform.localRotation = Quaternion.Euler(orbitAngles);
    }

    //Gets invoked at the end of every frame to take all prior movements into account and sets the new camera view for the next frame
    void LateUpdate()
    {
        UpdateFocusPoint();
        Quaternion lookRotation;
        if (ManualRotation() || AutomaticRotation())
        {
            ConstrainAngles();
            lookRotation = Quaternion.Euler(orbitAngles);
        }
        else
        {
            lookRotation = transform.localRotation;
        }
        Vector3 lookDirection = lookRotation * Vector3.forward;
        Vector3 lookPosition = focusPoint - lookDirection * distance;

        Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
        Vector3 rectPosition = lookPosition + rectOffset;
        Vector3 castFrom = focus.position;
        Vector3 castLine = rectPosition - castFrom;
        float castDistance = castLine.magnitude;
        Vector3 castDirection = castLine / castDistance;

        if (Physics.BoxCast(
            castFrom, CameraHalfExtends, castDirection, out RaycastHit hit,
            lookRotation, castDistance, obstructionMask
        ))
        {
            rectPosition = castFrom + castDirection * hit.distance;
            lookPosition = rectPosition - rectOffset;
        }
        transform.SetPositionAndRotation(lookPosition, lookRotation);
    }

    //Sets the new focus point based on the target movement
    void UpdateFocusPoint()
    {
        previousFocusPoint = focusPoint;
        Vector3 targetPoint = focus.position;
        if (focusRadius > 0f)
        {
            float distance = Vector3.Distance(targetPoint, focusPoint);
            float t = 1f;
            if (distance > 0.01f && focusCentering > 0f)
            {
                t = Mathf.Pow(1f - focusCentering, Time.unscaledDeltaTime);
            }
            if (distance > focusRadius)
            {
                t = Mathf.Min(t, focusRadius / distance);
            }
            focusPoint = Vector3.Lerp(targetPoint, focusPoint, t);
        }
        else
        {
            focusPoint = targetPoint;
        }
    }

    //Manual rotation through keyboard inputs
    //ATTENTION: inputs have to be set in the Unity project settings (Check "Input Manager" in the Unity documentation)
    //Returns true when a rotation has been executed
    bool ManualRotation()
    {
        Vector2 input = new Vector2(
            Input.GetAxis("Vertical Camera"), //has to be defined in the Input Manager, e.g. J,K as keyboard inputs
            Input.GetAxis("Horizontal Camera") //has to be defined in the Input Manager, e.g. Q,E as keyboard inputs
        );
        const float e = 0.001f;
        if (input.x < -e || input.x > e || input.y < -e || input.y > e)
        {
            orbitAngles += rotationSpeed * Time.unscaledDeltaTime * input;
            lastManualRotationTime = Time.unscaledTime;
            return true;
        }
        return false;
    }

    //Automatic rotation aligning the camera with the direction of the last object movement with respect to some constraints (e.g. no rotation when moving towards the camera)
    //Returns true when a rotation has been executed
    bool AutomaticRotation()
    {
        if (Time.unscaledTime - lastManualRotationTime < alignDelay)
        {
            return false;
        }
        Vector2 movement = new Vector2(
            focusPoint.x - previousFocusPoint.x,
            focusPoint.z - previousFocusPoint.z
        );
        float movementDeltaSqr = movement.sqrMagnitude;
        if (movementDeltaSqr < 0.0001f)
        {
            return false;
        }
        float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
        float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
        float rotationChange =
            rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
        if (deltaAbs < alignSmoothRange)
        {
            rotationChange *= deltaAbs / alignSmoothRange;
        }
        else if (180f - deltaAbs < alignSmoothRange)
        {
            rotationChange *= (180f - deltaAbs) / alignSmoothRange;
        }
        orbitAngles.y =
            Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);
        return true;
    }

    //Calculates the correct orientation angle for the object heading
    static float GetAngle(Vector2 direction)
    {
        float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
        return direction.x < 0f ? 360f - angle : angle;
    }
}
