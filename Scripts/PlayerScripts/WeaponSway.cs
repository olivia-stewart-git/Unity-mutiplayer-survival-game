using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class WeaponSway : NetworkBehaviour
{
    private Quaternion originRotation;
    [SerializeField] private PlayerMovementMangaer p_Movement;
    [SerializeField] private PlayerMouseLook p_mouseLook;
    [SerializeField] private Transform swayTarget;
    [Space]
    [SerializeField] private float intensity;
    [SerializeField] private float zIntensity = 0.8f;
    [SerializeField] private float smooth;
    [Space]
    [SerializeField] private float j_curveMultiplier = 1f;
    [SerializeField] private AnimationCurve jumpCurve;
    [SerializeField] private AnimationCurve landCurve;
    [SerializeField] private float moveSwayDamper = 0.5f;

    private bool initialised = false;

    private Vector3 movementVector;

    private float multiplier = 1f;

    //recoil sutff
    private float positionalReturnSpeed = 16f;
    private float rotationReturnSpeed = 30f;

    private float positionalSpeed = 8f;
    private float rotationSpeed = 8f;

    private Vector3 rotationalRecoil;
    private Vector3 positionalRecoil;
    private Vector3 rot;

    [SerializeField]
    public float bobMultiplier;

    // Start is called before the first frame update
   public override void OnStartClient()
    {
        if (!base.IsOwner) return;
        originRotation = swayTarget.localRotation;
    }


    public void Initialise()
    {
        initialised = true;
    }

    private float swayVal;

    private Vector3 moveSwayVector = Vector3.zero;

    public void UpdateSway()
    {
        if (!base.IsOwner) return;
        // Get raw mouse input for a cleaner reading on more sensitive mice.
        var mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        float useIntensity = intensity * multiplier;
        float useZIntensity = zIntensity * multiplier;
        //calculate targetRotation
        Quaternion xtargetAdjustment = Quaternion.AngleAxis(-useIntensity * mouseDelta.x, Vector3.up);
        Quaternion ztargetAdjustment = Quaternion.AngleAxis(-useZIntensity * mouseDelta.x, Vector3.forward);

        Quaternion ytargetAdjustment = Quaternion.AngleAxis(useIntensity * mouseDelta.y, Vector3.right);

        Quaternion targetRotation = xtargetAdjustment * ytargetAdjustment * ztargetAdjustment * originRotation;

        float rawbAmount = p_mouseLook.GetBobValue() * bobMultiplier * targetBobAmplify;

        swayVal = Mathf.Lerp(swayVal, rawbAmount, 3 * Time.deltaTime);

        Vector3 pVelocity = Vector3.Lerp(moveSwayVector, p_Movement.LocalMoveVelocity(), 5f * Time.deltaTime);
        float upMoveDip = pVelocity.y * moveSwayDamper;
        float horizontalMoveDip = pVelocity.x * moveSwayDamper;

        Vector3 combinedMove = new Vector3(upMoveDip * multiplier * 5f, 0f, horizontalMoveDip * -50f);
        //Debug.Log("combined m" + combinedMove);

        rotationalRecoil = Vector3.Lerp(rotationalRecoil, Vector3.zero, rotationReturnSpeed * Time.deltaTime);
        Vector3 targRec = rotationalRecoil + combinedMove;

        positionalRecoil = Vector3.Lerp(positionalRecoil, new Vector3(0,swayVal, 0), positionalReturnSpeed * Time.deltaTime);

        swayTarget.localPosition = Vector3.Slerp(swayTarget.localPosition, positionalRecoil + jumpBobAdditional, positionalSpeed * Time.deltaTime);
        rot = Vector3.Slerp(rot, targRec, rotationSpeed * Time.deltaTime);

        targetRotation *= Quaternion.Euler(rot);

        swayTarget.localRotation = Quaternion.Lerp(swayTarget.localRotation, targetRotation, Time.deltaTime * smooth);
    }

    private float targetBobAmplify = 1;
    public void SetSwayDampen(bool value)
    {
        if (value)
        {
            targetBobAmplify = 0f;
            multiplier = 0.3f;
        }
        else
        {
            targetBobAmplify = 1f;
            multiplier = 1f;
        }
    }

    public void InitialiseRecoilValues(float rSpeed, float pSpeed, float rReturnSpeed, float pReturnSpeed)
    {
        rotationSpeed = rSpeed;
        rotationReturnSpeed = rReturnSpeed;

        positionalSpeed = pSpeed;
        positionalReturnSpeed = pReturnSpeed;
    }

    public void ApplyRecoil(Vector3 rotRecoil, Vector3 kickback)
    {
        rotationalRecoil += new Vector3(-rotRecoil.x, Random.Range(-rotRecoil.y, rotRecoil.y), Random.Range(-rotRecoil.z, rotRecoil.z));
        positionalRecoil += new Vector3(Random.Range(-kickback.x, kickback.x), Random.Range(-kickback.y, kickback.y), kickback.z);
    }

    public void JumpSway()
    {
        if (jCoro != null)
        {
        StopCoroutine(jCoro);
        }
        jCoro = StartCoroutine(JumpAnim(jumpCurve));
    }

    private Vector3 jumpBobAdditional = Vector3.zero;
    IEnumerator JumpAnim(AnimationCurve useCurve)
    {
        float index = 0;
        float yOffset = 0f;

        Vector3 offset = Vector3.zero;
        offset.y = yOffset;
        SetJumpBob(offset);

        do
        {
            index += 0.05f;
            yOffset = useCurve.Evaluate(index) * j_curveMultiplier;
            offset.y = yOffset;
            SetJumpBob(offset);
            yield return new WaitForSeconds(0.05f);
        } while (index < 1f);

        yOffset = 0f;
        offset.y = yOffset;
        SetJumpBob(offset);
    }

    private Coroutine jCoro;
    void SetJumpBob(Vector3 offset)
    {
        jumpBobAdditional = offset;
    }



    public void LandSway()
    {
        if (jCoro != null)
        {
            StopCoroutine(jCoro);
        }
        jCoro =  StartCoroutine(JumpAnim(landCurve));
    }
}
