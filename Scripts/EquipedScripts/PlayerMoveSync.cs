using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMoveSync : MonoBehaviour
{

    [SerializeField] private Animator referenceAnimator;
    private PlayerMovementMangaer p_movement;

    private bool initialised = false;

    [SerializeField] private string runBoolName;

    public void Initialise(PlayerMovementMangaer syncMove)
    {
        p_movement = syncMove;
        initialised = true;
    }

    void Update()
    {
        if (initialised)
        {
            if(p_movement.RetrieveMoveState() == PlayerMovementMangaer.MovementState.run)
            {
                referenceAnimator.SetBool(runBoolName, true);
            }
            else
            {
                referenceAnimator.SetBool(runBoolName, false);
            }
        }
    }
}
