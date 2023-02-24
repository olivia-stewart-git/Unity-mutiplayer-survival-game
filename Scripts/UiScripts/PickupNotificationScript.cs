using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PickupNotificationScript : MonoBehaviour
{
    [SerializeField] public Animator thisAnimator;
    public TextMeshProUGUI nameText;
    public Image iconImage;
    public float lastTime = 1f;
    private Coroutine spawnCoroutine;
    public void Spawned()
    {
        if(spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        thisAnimator.Play("PickupNotificationAnimation", 0, 0f);

        spawnCoroutine = StartCoroutine(SpawnCoroutine());
    }

    private IEnumerator SpawnCoroutine()
    {
        yield return new WaitForSeconds(lastTime);
        gameObject.SetActive(false);
    }
}
