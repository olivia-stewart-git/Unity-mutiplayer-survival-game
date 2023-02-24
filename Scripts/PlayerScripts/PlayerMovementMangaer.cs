using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Prediction;
using UnityEngine.InputSystem;

public class PlayerMovementMangaer : NetworkBehaviour
{

    [Header("Components")]
    [SerializeField] private CharacterController charController;
    [SerializeField] private Transform playerBody;
    [SerializeField] private PlayerMouseLook p_MouseLook;
    [SerializeField] private PlayerResourcesScript p_resources;
    [SerializeField] private CrosshairManager crosshairManager;
    [SerializeField] private PlayerFootStepManager p_footstep;
    [SerializeField] private PlayerMenuManager p_Menu;
    [SerializeField] private PlayerAnimationManager p_AnimationManager;
    private PlayerBuffManager p_BuffManager;
    private WeaponSway w_Sway;

    private PlayerDamageManager p_Damage;

    [Header("Movement values")]
    [SerializeField]
    private float acceleration = 5f;
    private Vector3 moveDamper;

    private float xMove = 0;
    private float zMove = 0;
    private float useSpeed; //speed we actually use
    private float curSpeed;

    private Vector3 lastPos;
    private Vector3 curPos;

    private bool canMove = true;
    private bool overrideMovement = false;

    private Vector3 velocity;
    private Vector3 moveVelocity; //this is the velocity read value to find move direction

    [Header("Jumping")]
    private bool grounded;
    private bool lastGrounded;
    private bool overrideGravity = false;
    private bool canJump = true;

    private bool forceWalk = false;

    [SerializeField]
    private float jumpheight = 1f;
    [SerializeField]
    private float groundedCheckRadius = 0.15f;
    [SerializeField]
    private LayerMask groundedLayerMask;
    [SerializeField]
    private float gravity = 9.8f;
    [SerializeField]
    private float jumpVelMultiplier;
    [SerializeField]
    private int staminaUseOnJump = 10;

    public enum MovementState { idle, walk, run, crouch, prone, falling };
    [Header("state values")]
    private MovementState moveState = MovementState.idle;
    private MovementState desiredMove = MovementState.idle;
    private MovementState curState = MovementState.idle;
    [Space]
    [SerializeField] private float idleBobAmplitude;
    [SerializeField] private float idleBobTime;
    [Space]
    [SerializeField] private Vector2 idleCrosshairSize;
    [Space]
    [SerializeField] private float walkSpeed;
    private float calculatedwalkSpeed;
    [SerializeField] private float walkHeight;
    [SerializeField] private float walkBobAmplitude;
    [SerializeField] private float walkBobInterval;
    [SerializeField] private Vector2 walkCrosshairSize;
    [Space]
    [SerializeField] private float runSpeed;
    private float calculatedrunSpeed;
    [SerializeField] private float runBobAmplitude;
    [SerializeField] private float runBobInterval;
    [SerializeField] private Vector2 runCrosshairSize;
    [Space]
    [SerializeField] private float crouchSpeed;
    private float calculatedcrouchSpeed;
    [SerializeField] private float crouchHeight;
    [SerializeField] private float crouchBobAmplitude;
    [SerializeField] private float crouchBobInterval;
    [SerializeField] private Vector2 crouchCrosshairSize;
    [Space]
    [SerializeField] private float proneSpeed;
    private float calculatedproneSpeed;
    [SerializeField] private float proneHeight;
    [SerializeField] private float proneBobAmplitude;
    [SerializeField] private float proneBobInterval;
    [SerializeField] private Vector2 proneCrosshairSize;


    private float moveAccuracyMultiplier = 1f;

    //targetvalues
    private float targetSpeed;
    private float desiredStepOffset;

    public void InitializeValues()
    {
        if (!base.IsOwner) return;

        targetSpeed = walkSpeed;
        SetCharControllerSize(walkHeight);
        ReturnToStand();
    }

    private void Awake()
    {
        p_Damage = GetComponent<PlayerDamageManager>();
        p_BuffManager = GetComponent<PlayerBuffManager>();
        w_Sway = GetComponent<WeaponSway>();

        CalculateMoveSpeedValues();
    }

    #region handle buffs

    private bool buffSprintBlock = false;
    private bool buffJumpBlock = false;
    private float buffSpeedModifier = 1f;
    public void UpdatedBuffState()
    {
        buffJumpBlock = p_BuffManager.calculated_blockJumping;
        buffSprintBlock = p_BuffManager.calculated_blockSprinting;
        buffSpeedModifier = p_BuffManager.calculated_speedAdjustment;

        CalculateMoveSpeedValues();
    }
    #endregion

    void CalculateMoveSpeedValues()
    {
        calculatedproneSpeed = proneSpeed * buffSpeedModifier;
        calculatedcrouchSpeed = crouchSpeed * buffSpeedModifier;
        calculatedwalkSpeed = walkSpeed * buffSpeedModifier;
        calculatedrunSpeed = runSpeed * buffSpeedModifier;
    }

    public float GetWalkHeight()
    {
        return walkHeight;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawSphere(transform.position, groundedCheckRadius);
    }

    public bool IsGrounded()
    {
        return grounded;
    }

    public void UpdateMovement()
    {
        if (!base.IsOwner && !IsServer) return;

        if (canMove == true)
        {
            MovePlayer();
        }

        //update values
        grounded = Physics.CheckSphere(transform.position, groundedCheckRadius, groundedLayerMask);
        p_AnimationManager.AssignAnimatorGrounded(grounded);

        //work gravity baby!
        if (overrideGravity == false)
        {
            velocity.y += gravity * Time.deltaTime;
            charController.Move(velocity * Time.deltaTime);
        }

        //stop accel ramping
        if (grounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        //accelarate to speed
        useSpeed = Mathf.Lerp(useSpeed, targetSpeed, acceleration * Time.deltaTime);

        //check if running allowed
        if (desiredMove == MovementState.run && zMove <= 0 && moveVelocity != Vector3.zero && grounded || buffSprintBlock == true)
        {
            ReturnToStand();
        }

        if(moveState == MovementState.run && p_resources.RetrieveCurStamine() <= 1f)
        {
            ReturnToStand();
        }

        if (moveVelocity == Vector3.zero || overrideMovement == true || curSpeed == 0)
        {
            moveState = MovementState.idle;
            moveVal = Vector3.zero;
        }
        else
        {
            if (grounded == false)
            {
                moveState = MovementState.falling;
            }
            else
            {
                moveState = desiredMove;
            }
        }

        //fixes weird edge jitter
        if (grounded == false)
        {
            charController.stepOffset = 0f;
        }
        else
        {
            if (moveState == MovementState.prone)
            {
                charController.stepOffset = 0.05f;
            }
            else
            {
                charController.stepOffset = desiredStepOffset;
            }
        }    

        UpdateMoveValues();
    }

    private void UpdateMoveValues()
    {
        switch (moveState)
        {
            case MovementState.idle:

                p_MouseLook.SetHeadbobValues(idleBobAmplitude, idleBobTime);
                if (desiredMove == MovementState.run)
                {
                    desiredMove = MovementState.walk;
                }

                p_AnimationManager.SetMoveTarget(0, 0);
                crosshairManager.SetCrosshairTarget(idleCrosshairSize);

                switch (desiredMove)
                {
                    case MovementState.idle:
                        moveAccuracyMultiplier = 1f;

                       p_AnimationManager.SetAnimatorCrouching(false);
                        p_AnimationManager.SetAnimatorProne(false);

                        break;
                    case MovementState.walk:
                        moveAccuracyMultiplier = 1f;


                        p_AnimationManager.SetAnimatorCrouching(false);
                        p_AnimationManager.SetAnimatorProne(false);

                        break;
                    case MovementState.run:
                        moveAccuracyMultiplier = 1f;

                        p_AnimationManager.SetAnimatorCrouching(false);
                        p_AnimationManager.SetAnimatorProne(false);

                        break;
                    case MovementState.crouch:
                        moveAccuracyMultiplier = 0.7f;

                        p_AnimationManager.SetAnimatorCrouching(true);
                        p_AnimationManager.SetAnimatorProne(false);

                        break;
                    case MovementState.prone:
                        moveAccuracyMultiplier = 0.5f;

                        p_AnimationManager.SetAnimatorCrouching(false);
                        p_AnimationManager.SetAnimatorProne(true);

                        break;
                    case MovementState.falling:
                        moveAccuracyMultiplier = 2f;


                        p_AnimationManager.SetAnimatorCrouching(false);
                        p_AnimationManager.SetAnimatorProne(false);

                        break;
                }

                break;
            case MovementState.walk:
                targetSpeed = calculatedwalkSpeed;
                SetCharControllerSize(walkHeight);
                canJump = true;
                p_MouseLook.SetHeadbobValues(walkBobAmplitude, walkBobInterval);
                p_MouseLook.SyncToCharacterController();
                curState = MovementState.idle;

                crosshairManager.SetCrosshairTarget(walkCrosshairSize);

                moveAccuracyMultiplier = 1.2f;

                p_AnimationManager.SetMoveTarget(xMove, zMove);


                p_AnimationManager.SetAnimatorCrouching(false);
                p_AnimationManager.SetAnimatorProne(false);

                break;
            case MovementState.run:
                targetSpeed = calculatedrunSpeed;
                SetCharControllerSize(walkHeight);
                canJump = true;
                p_MouseLook.SetHeadbobValues(runBobAmplitude, runBobInterval);
                p_MouseLook.SyncToCharacterController();
                curState = MovementState.run;

                crosshairManager.SetCrosshairTarget(runCrosshairSize);

                moveAccuracyMultiplier = 1.7f;

                p_AnimationManager.SetMoveTarget(xMove * 2f, zMove * 2f);


                p_AnimationManager.SetAnimatorCrouching(false);
                p_AnimationManager.SetAnimatorProne(false);

                break;
            case MovementState.prone:
                targetSpeed = calculatedproneSpeed;
                SetCharControllerSize(proneHeight);
                canJump = false;
                p_MouseLook.SetHeadbobValues(proneBobAmplitude, proneBobInterval);
                p_MouseLook.SyncToCharacterController();
                curState = MovementState.prone;

                crosshairManager.SetCrosshairTarget(proneCrosshairSize);
                moveAccuracyMultiplier = 0.8f;

                p_AnimationManager.SetMoveTarget(xMove, zMove);


                p_AnimationManager.SetAnimatorCrouching(false);
                p_AnimationManager.SetAnimatorProne(true);

                break;
            case MovementState.falling:
                targetSpeed = Mathf.Lerp(targetSpeed, walkSpeed * 0.8f, 5f * Time.deltaTime);
                SetCharControllerSize(walkHeight);
                canJump = false;
                p_MouseLook.SetHeadbobValues(idleBobAmplitude, idleBobTime);
                p_MouseLook.SyncToCharacterController();
                curState = MovementState.falling;

                crosshairManager.SetCrosshairTarget(new Vector2(runCrosshairSize.x * 1.3f, runCrosshairSize.y * 1.3f));

                moveAccuracyMultiplier = 1.5f;

                p_AnimationManager.SetMoveTarget(0, 0);


                p_AnimationManager.SetAnimatorCrouching(false);
                p_AnimationManager.SetAnimatorProne(false);

                break;
            case MovementState.crouch:
                targetSpeed = calculatedcrouchSpeed;
                SetCharControllerSize(crouchHeight);
                canJump = false;
                p_MouseLook.SetHeadbobValues(crouchBobAmplitude, crouchBobInterval);
                p_MouseLook.SyncToCharacterController();
                curState = MovementState.crouch;

                crosshairManager.SetCrosshairTarget(crouchCrosshairSize);

                moveAccuracyMultiplier = 1.1f;

                p_AnimationManager.SetMoveTarget(xMove, zMove);

                p_AnimationManager.SetAnimatorCrouching(true);
                p_AnimationManager.SetAnimatorProne(false);
                break;
        }
    }

    public Vector3 LocalMoveVelocity()
    {
        Vector3 mVel = new Vector3(moveVal.x, moveVelocity.y, moveVal.z);
        return mVel;
    }

    public float GetAccuracyMultiplier()
    {
        return moveAccuracyMultiplier;
    }

    private void FixedUpdate()
    {
        if (!base.IsOwner)
        {
            return;
        }

        //handle speed
        lastPos = curPos;

        curSpeed = Vector3.Distance(lastPos, transform.position) / Time.fixedDeltaTime;
        curSpeed = Mathf.RoundToInt(curSpeed);

        curPos = transform.position;

        moveVelocity = curPos - lastPos;

        //for grounded 
        if (grounded && lastGrounded != grounded)
        {
            //hit ground
            CalculateFallDamage();
        }

        if(!grounded && lastGrounded == true)
        {
            leaveGroundPos = transform.position;
            Debug.Log(leaveGroundPos + " jumped at");
        }
        lastGrounded = grounded;
        
    }

    private Vector3 moveVal;

    public void MovePlayer()
    {
        //performmoving
        if (!base.IsOwner) return;

            if (overrideMovement == true || p_Menu.paused || p_Damage.isDead == true) return;

        Vector3 move = playerBody.transform.right * xMove + playerBody.transform.forward * zMove;
        move.y = 0f;
        moveVal = move;
        moveDamper = Vector3.Lerp(moveDamper, move, acceleration * Time.deltaTime);
        charController.Move(moveDamper * useSpeed * Time.deltaTime);
    }

    public void RetrieveMovementValues(InputAction.CallbackContext iMove)
    {
        if (!base.IsOwner) return;

        xMove = iMove.ReadValue<Vector2>().x;
        zMove = iMove.ReadValue<Vector2>().y;
    }

    private void SetCharControllerSize(float desiredHeight)
    {
        charController.height = desiredHeight;
        Vector3 desiredpos = new Vector3(0, desiredHeight / 2, 0);
        charController.center = desiredpos;
    }

    public void Jump(InputAction.CallbackContext context)
    {
        if (context.performed && grounded == true && overrideGravity == false && canJump && p_resources.RetrieveCurStamine() - staminaUseOnJump > 0 && p_Menu.paused == false && buffJumpBlock == false)
        {
            p_AnimationManager.JumpAnim();//plays jump animation

            velocity.y = Mathf.Sqrt(jumpheight * -2f * gravity);
            velocity += (jumpVelMultiplier * moveVelocity);
            p_resources.AddValue(-staminaUseOnJump, PlayerResourcesScript.ResourceType.stamina);

            w_Sway.JumpSway();
            p_footstep.PlayJumpSound();

            grounded = false;
        }
    }

    //for fall damage
    private Vector3 leaveGroundPos;
    private Vector3 landGroundPos;
    [Header("Fall damage settings")]
    [SerializeField]private float fallDamageMultiplier = 1.5f;
    [SerializeField] private float fallDamageDistanceThreshold = 5f;
    [SerializeField] private float maxFallVelocityAtDistance = 50f;
    [SerializeField] private AnimationCurve fallDamageSampleCurve;
    [Space]
    [SerializeField] private float breakLegThreshold = 10f;

    private void CalculateFallDamage()
    {    
        //we hit the ground
        velocity.x = 0;
        velocity.z = 0;

        //play land sound
        p_footstep.PlayJumpLandSound();
        w_Sway.LandSway();

        //do damage
        landGroundPos = transform.position;
        float yDiference = leaveGroundPos.y - landGroundPos.y;
        Debug.Log("hit ground with height difference of " + yDiference);
        if(yDiference > 0 && yDiference > fallDamageDistanceThreshold)
        {
            float damageToDo = ((yDiference - fallDamageDistanceThreshold) * fallDamageMultiplier); //second part is to have steady ratio
            float sampleX = yDiference / maxFallVelocityAtDistance;
            float muliplier = fallDamageSampleCurve.Evaluate(sampleX);
            float fallDamageCalculated = Mathf.RoundToInt(damageToDo * muliplier);


            if(yDiference > breakLegThreshold)
            {
                p_BuffManager.ApplyBuff("Broken Leg", false);
                Debug.Log("BrokeLeg");
            }

            p_Damage.TakeDamage(fallDamageCalculated, Vector3.up, ItemData.DamageType.blunt, transform.position);
        }
    }

    public void CrouchPressed(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_Damage.isDead == true) return;
        if (context.performed)
        {
            if (desiredMove == MovementState.crouch)
            {
                ReturnToStand();
            }
            else
            {
                desiredMove = MovementState.crouch;
                moveState = MovementState.crouch;
                UpdateMoveValues();
            }
        }
    }

    public void PronePressed(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_Damage.isDead == true) return;
        if (context.performed)
        {
            if (desiredMove == MovementState.prone)
            {
                ReturnToStand();
            }
            else
            {
                desiredMove = MovementState.prone;
                moveState = MovementState.prone;
                UpdateMoveValues();

            }
        }
    }

    public void RunPressed(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_Damage.isDead == true) return;
        if (forceWalk != true)
        {
            if (context.performed)
            {
                if (curState == MovementState.run)
                {
                    ReturnToStand();
                }
                else
                {
                    desiredMove = MovementState.run;
                    moveState = MovementState.run;
                }
            }
            if (context.canceled && moveState == MovementState.run)
            {
                ReturnToStand();
            }
        }
    }

    public void ReturnToStand()
    {
        //raycast to check no roof above head
        if (!base.IsOwner) return;
        desiredMove = MovementState.walk;
        moveState = MovementState.walk;
        UpdateMoveValues();
    }

    public MovementState RetrieveMoveState()
    {
        return curState;
    }

    public MovementState RetreivedDesired()
    {
        return desiredMove;
    }

    public void SetMovementOverride(bool value)
    {
        overrideMovement = value;
    }

    public void ForceWalk(bool value)
    {
        if (!base.IsOwner) return;
        if (value)
        {
            forceWalk = value;
            if(desiredMove == MovementState.run)
            {
                desiredMove = MovementState.walk;
                moveState = MovementState.walk;
                UpdateMoveValues();
            }
        }
        else
        {
            forceWalk = value;
        }
    }

    public Vector3 RetrieveMovementVector()
    {
        return moveVelocity;
    }

    public bool IsMoving()
    {
        if(xMove != 0 || zMove != 0)
        {
            return true;
        }
        return false;
    }
}
