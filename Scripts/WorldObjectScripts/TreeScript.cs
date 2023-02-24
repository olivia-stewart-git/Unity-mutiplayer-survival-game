using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeScript : MonoBehaviour
{
    [Header("Falling settings")]
    [SerializeField] private Transform fallingParent;
    [SerializeField] private GameObject fallingComponent;
    [SerializeField] private Collider fallingCollider;
    [SerializeField] private ParticleSystem destroyParticle;
    [SerializeField] private AudioClip[] fallingSounds;

    [SerializeField] private Rigidbody fallingRigidbody;

    [SerializeField] private AudioSource aSource;

    private Coroutine fallCoro;

    public void NodeDeactived(Vector3 direction)
    {
        fallingCollider.enabled = true;
        fallingRigidbody.isKinematic = false;

        fallingRigidbody.AddForce(direction * 5f);

        aSource.PlayOneShot(fallingSounds[Random.Range(0, fallingSounds.Length)]);

        fallCoro = StartCoroutine(FallingCoroutine());
    }

    public void NodeActivated()
    {
        if(fallCoro != null)
        {
            StopCoroutine(fallCoro);
        }

        fallingParent.transform.localPosition = Vector3.zero;
        fallingParent.transform.localRotation = Quaternion.identity;

        fallingComponent.SetActive(true);

        fallingRigidbody.isKinematic = true;
        fallingCollider.enabled = false;

    }

    //the tree falls, then is hiddne
    private IEnumerator FallingCoroutine()
    {
        yield return new WaitForSeconds(5f);
        //deactive
        fallingRigidbody.isKinematic = true;
        fallingCollider.enabled = false;
        fallingComponent.SetActive(false);

        destroyParticle.Play();
    }
}
