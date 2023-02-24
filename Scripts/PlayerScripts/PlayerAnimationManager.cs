using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Component.Animating;
public class PlayerAnimationManager : NetworkBehaviour
{
    public NetworkAnimator targetAnimation;

    [Header("Base settings")]
    public float moveDampSpeed = 3f;
    private float targetXMove;
    private float targetYMove;
    private float curXmove;
    private float curYmove;


    private void Update()
    {
        if (!base.IsOwner) return;
        //we calculate the move values
        curXmove = Mathf.Lerp(curXmove, targetXMove, moveDampSpeed * Time.deltaTime);
        curYmove = Mathf.Lerp(curYmove, targetYMove, moveDampSpeed * Time.deltaTime);
        targetAnimation.Animator.SetFloat("MoveX", curXmove);
        targetAnimation.Animator.SetFloat("MoveY", curYmove);

        curLookvector = Vector2.Lerp(curLookvector, targetLookVector, 5f * Time.deltaTime);
        targetAnimation.Animator.SetFloat("Lookx", curLookvector.x);
        targetAnimation.Animator.SetFloat("Looky", curLookvector.y);
    }

    public void SetMoveTarget(float xMove, float yMove)
    {
        targetXMove = xMove;
        targetYMove = yMove;
    }

    public void JumpAnim()
    {
        targetAnimation.SetTrigger("Jump");
    }

    public void AssignAnimatorGrounded(bool value)
    {
        targetAnimation.Animator.SetBool("Grounded", value);
    }

    public void SetAnimatorCrouching(bool value)
    {
        targetAnimation.Animator.SetBool("Crouching", value);
    }

    public void SetAnimatorProne(bool value)
    {
        targetAnimation.Animator.SetBool("Prone", value);
    }

    private Vector2 curLookvector = Vector2.zero;
    private Vector2 targetLookVector = Vector2.zero;
    public void UpdateLookVector(Vector2 lookVector)
    {
     //   targetAnimation.animator.SetFloat("Lookx", lookVector.x);
     //   targetAnimation.animator.SetFloat("Looky", lookVector.y);
        targetLookVector = lookVector;
    }

    private int currentstate = 1;
    public void SetUpperBodyState(int state)
    {
        currentstate = state;
        targetAnimation.Animator.SetInteger("Held", state);
    }

    public void PlayAnimation(string name)
    {
        targetAnimation.SetTrigger(name);
    }

    public void SetBool(string name, bool value)
    {
        targetAnimation.Animator.SetBool(name, value);
    }
}
