using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using UnityEngine.InputSystem;

public class PlayerMouseLook : NetworkBehaviour
{
    [SerializeField] private PlayerMovementMangaer p_Movement;
    [SerializeField] private PlayerMenuManager p_Menu;
    [SerializeField] private PlayerAnimationManager p_animationManager;

    private PlayerDamageManager p_Damage;

    private Camera equipedCam;
    private Camera mainCam;

    [Header("Look settings")]
    [SerializeField]
    private float targetFov = 75f;
    [SerializeField]
    private Vector2 lookSensitivity = new Vector2(2, 2);
    [SerializeField]
    private Vector2 adsSensitivty = new Vector2(1, 1);
    [SerializeField]
    private Vector2 smoothing = new Vector2(3, 3);

    [SerializeField]
    private float headOffset = 0.2f;
    [SerializeField]
    private float lerpSpeed = 1f;

    [SerializeField]
    private Transform mouseLooker;
    [SerializeField]
    private Transform playerBody;
    [SerializeField]
    private Transform toBob;
    [SerializeField]
    private PlayerFootStepManager p_Footsteps;

    [SerializeField]
    private CharacterController charController;

    private Vector2 targetSensitivity;
    private Vector2 targetDirection;
    private Vector2 targetCharacterDirection;
    private Vector2 _mouseAbsolute;
    private Vector2 _smoothMouse;

    private Vector3 targetLocalPos = new Vector3(0, 1.5f, 0);
    private Vector3 modifiedLeanPos;
    private Vector3 positionDamper;

    public Vector2 clampInDegrees = new Vector2(360, 180);

    private bool cursorLocked = true;

    private bool overrideMouseLook = false;

    //headbobbing
    private float targetSpeed;
    private float targetAmplitude;

    [SerializeField] private float transitionSpeed = 8f;
    private Vector3 targetPos;
    private float index;

    [Header("Lean settings")]
    [SerializeField] private bool allowLeaning = true;
    [SerializeField] private float leanPositionOffeset;
    [SerializeField] private float leanRotation;
    [SerializeField] private float rotSpeed;
    private float useOffset;
    private bool isLeaning = false;
    private float rotAmount;

    [Header("recoil settings")]
    private float userecoilSpeed;
    [SerializeField] private Transform recoilHolder;

    private Quaternion comparitiveQuarternion = Quaternion.identity;
    private Quaternion extraCompare;

    private float lastFootstep;

    private float bobAmount;

    public float GetBobValue() => bobAmount;

    private Vector3 jumpBobVector = Vector3.zero;

    private void HeadBob()
    {
        if (p_Damage.isDead == true || !initialised) return;
        index += Time.deltaTime;

        bobAmount = Mathf.Sin(index * targetSpeed) * targetAmplitude; //le sin wave

        Vector3 targetVector = new Vector3(targetPos.x, bobAmount, targetPos.z);

        toBob.localPosition = Vector3.Lerp(toBob.localPosition, targetVector, transitionSpeed * Time.deltaTime);
        //reset time
        if (index * targetSpeed > ((Mathf.PI * 2 / 3) / targetSpeed))
        {
            index = index - (Mathf.PI * 2) / targetSpeed;

           //make the footstep noise
           if(p_Movement.IsGrounded() && p_Movement.IsMoving() == true && Time.time > lastFootstep && p_Movement.RetreivedDesired() != PlayerMovementMangaer.MovementState.prone)
            {
                p_Footsteps.PlayFootStepSound();
                lastFootstep = Time.time + 0.1f;
            }
        }
    }

    public void SetHeadbobValues(float amplitude, float speed)
    {
        targetAmplitude = amplitude;
        targetSpeed = speed;
    }

    private void Update()
    {
        if (!base.IsOwner || !initialised) return;

        HeadBob();
        if (overrideMouseLook == false && useOffset != 0 && allowLeaning && p_Movement.RetrieveMoveState() != PlayerMovementMangaer.MovementState.run && p_Movement.RetrieveMoveState() != PlayerMovementMangaer.MovementState.falling && p_Movement.RetrieveMoveState() != PlayerMovementMangaer.MovementState.prone)
        {
            modifiedLeanPos =  targetLocalPos + (Vector3.right * leanPositionOffeset * useOffset);
            rotAmount = Mathf.Lerp(rotAmount, -leanRotation * useOffset, rotSpeed * Time.deltaTime);
            isLeaning = true;
        }
        else
        {
            modifiedLeanPos = targetLocalPos;
            rotAmount = Mathf.Lerp(rotAmount, 0, rotSpeed * Time.deltaTime);
            isLeaning = false;
        }

        if(equipedCam.fieldOfView != curViewmodelTarget)
        {
            equipedCam.fieldOfView = Mathf.Lerp(equipedCam.fieldOfView, curViewmodelTarget, Time.deltaTime * 8f);
        }

        //lerp recoil
        comparitiveQuarternion = Quaternion.Lerp(comparitiveQuarternion, Quaternion.identity, userecoilSpeed * Time.deltaTime * 0.7f);
        recoilHolder.localRotation = Quaternion.Lerp(recoilHolder.localRotation, comparitiveQuarternion, userecoilSpeed * Time.deltaTime);

        if (mainCam.fieldOfView != curFovtarget) mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, curFovtarget, Time.deltaTime * 5f);
    }

    private bool initialised = false;
    public void InitialiseValues()
    {
        SetCursorLock(true);
        SyncToCharacterController();

        SetViewModelFov(60f);

        targetDirection = mouseLooker.localRotation.eulerAngles;
        targetCharacterDirection = playerBody.transform.localRotation.eulerAngles;
        targetSensitivity = lookSensitivity;

        curFovtarget = targetFov;

        equipedCam = GameObject.Find("ItemCamera").GetComponent<Camera>();
        mainCam = equipedCam.transform.parent.gameObject.GetComponent<Camera>();
        mainCam.fieldOfView = targetFov;

        Debug.Log(equipedCam.name);

        index = 0f;

        initialised = true;
    }

    public void MouseLook()
    {
        if (!base.IsOwner || !initialised) return;
        if (overrideMouseLook || p_Menu.paused == true || p_Damage.isDead == true) return;

        LerpToTargetPos();

        // Allow the script to clamp based on a desired target value.
        var targetOrientation = Quaternion.Euler(targetDirection);
        var targetCharacterOrientation = Quaternion.Euler(targetCharacterDirection);

        // Get raw mouse input for a cleaner reading on more sensitive mice.
        var mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // Scale input against the sensitivity setting and multiply that against the smoothing value.
        mouseDelta = Vector2.Scale(mouseDelta, new Vector2(targetSensitivity.x * smoothing.x, targetSensitivity.y * smoothing.y));

        // Interpolate mouse movement over time to apply smoothing delta.
        _smoothMouse.x = Mathf.Lerp(_smoothMouse.x, mouseDelta.x, 1f / smoothing.x);
        _smoothMouse.y = Mathf.Lerp(_smoothMouse.y, mouseDelta.y, 1f / smoothing.y);

        // Find the absolute mouse movement value from point zero.
        _mouseAbsolute += _smoothMouse;

        // Clamp and apply the local x value first, so as not to be affected by world transforms.
        if (clampInDegrees.x < 360)  _mouseAbsolute.x = Mathf.Clamp(_mouseAbsolute.x, -clampInDegrees.x * 0.5f, clampInDegrees.x * 0.5f);

        // Then clamp and apply the global y value.
        if (clampInDegrees.y < 360)  _mouseAbsolute.y = Mathf.Clamp(_mouseAbsolute.y, -clampInDegrees.y * 0.5f, clampInDegrees.y * 0.5f);

        mouseLooker.localRotation = (Quaternion.AngleAxis(-_mouseAbsolute.y, targetOrientation * Vector3.right) * targetOrientation) * Quaternion.AngleAxis(rotAmount, Vector3.forward);

        // If there's a character body that acts as a parent to the camera
        var yRotation = Quaternion.AngleAxis(_mouseAbsolute.x, Vector3.up);
        playerBody.transform.rotation = yRotation * targetCharacterOrientation;

        float lookValUp = mouseLooker.transform.localEulerAngles.x;
        if(lookValUp > 100f) //this is to fix the retrieving of an obtuse angle 
        {
            lookValUp = (360f - lookValUp) * -1f;
        }

        float xVAimOffset = useOffset * 90f;
        if(isLeaning == false)
        {
            xVAimOffset = 0f;
        }
        Vector2 lookVector = new Vector2(xVAimOffset, lookValUp);

        p_animationManager.UpdateLookVector(lookVector);
    }

    //cursor locking (set true to lock)
    [HideInInspector] public bool cursorLock = false;
    public void SetCursorLock(bool value)
    {
        if (!base.IsOwner) return;

        cursorLock = value;
        if (value == true)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
        cursorLocked = value;
    }

    public void SetMouseLookOverride(bool value)
    {
        if (value == true)
        {
            overrideMouseLook = true;
        }
        else
        {
            overrideMouseLook = false;
        }
    }

    //places head from offset
    public void SyncToCharacterController()
    {
        if (!base.IsOwner) return;
        targetLocalPos.y = charController.height - headOffset;
    }

    void LerpToTargetPos()
    {
        positionDamper = Vector3.Lerp(positionDamper, modifiedLeanPos, Time.deltaTime * lerpSpeed);
        recoilHolder.localPosition = Vector3.Lerp(recoilHolder.localPosition, positionDamper, Time.deltaTime * lerpSpeed);
    }

    public void ReadLeanValue(InputAction.CallbackContext context)
    {
        if (!base.IsOwner) return;
        //make sure we can't lean when dead
        if( p_Damage.isDead == true)
        {
            useOffset = 0f;
        }
        else { 
        useOffset = context.ReadValue<Vector2>().x;
        }
    }

    private float curViewmodelTarget;
    public void SetViewModelFov(float value)
    {
        curViewmodelTarget = value;
    }

    public void SetAdsSenstivity(bool value)
    {
        if (!base.IsOwner) return;
        if (value)
        {
            targetSensitivity = adsSensitivty;
        }
        else
        {
            targetSensitivity = lookSensitivity;
        }
    }

    public Camera GetCamera()
    {
        return equipedCam;
    }

    public void RecoilCamera(Vector2 amount, float speed)
    {
        userecoilSpeed = speed;
        comparitiveQuarternion *= Quaternion.Euler(new Vector3(-amount.y, amount.x, 0f));
    }

    public Transform GetBody()
    {
        return playerBody;
    }

    private float curFovtarget;
    public void SetFov(float amount, bool isSetting)
    {
        if (isSetting)
        {
            curFovtarget = targetFov * amount;
        }
        else
        {
            curFovtarget = targetFov;
        }
    }

    public void LookTowardPoint(Vector3 point, float speed)
    {
        Quaternion lookOnLook = Quaternion.LookRotation(point - mouseLooker.position);

        mouseLooker.rotation = Quaternion.Slerp(mouseLooker.rotation, lookOnLook, Time.deltaTime * speed);
    }

    private void Awake()
    {
        p_Damage = GetComponent<PlayerDamageManager>();
    }
}
