using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiplayerBulletRepScript : MonoBehaviour
{
    private bool spawned;
    private Vector3[] lerpPoints;

    private int count = 0;

    private float t;
    private Vector3 lastPos;

    private float toReachTime;

    private bool ended = false;

    private Vector3 target;

    public TrailRenderer lRenderer;

    public LayerMask rayMask;

    public AudioClip[] bulletWhooshes;

    void Awake()
    {
        lRenderer.Clear();
    }

    // Update is called once per frame
    void Update()
    {
        //lerps between points
        if (spawned && lerpPoints != null && lerpPoints.Length > 0)
        {
            t += Time.deltaTime / toReachTime;
            transform.position = Vector3.Lerp(lastPos, target, t);

            if (Vector3.Distance(transform.position, lerpPoints[count]) < 0.1f && count < lerpPoints.Length - 1 && ended == false)
            {
                t = 0;
                lastPos = lerpPoints[count];
                count++;
                target = lerpPoints[count];
            }
            else
            {
                if (Vector3.Distance(transform.position, target) < 0.1f && ended)
                {
                    StopBullet();
                }
            }
        }

        //raycast forward to see if we should stop
        RaycastHit hit;
        Vector3 direction = (target - transform.position).normalized;
        if (Physics.Raycast(transform.position, direction, out hit, Vector3.Distance(transform.position, target) + 0.2f, rayMask))
        {
            SetEndPoint(hit.point);
        }
    }

    public void SpawnObject(Vector3[] pointstoLerp, float lerpTime)
    {
        ended = false;
        t = 0;
        transform.position = pointstoLerp[0];
        lerpPoints = pointstoLerp;
        lastPos = lerpPoints[0];
        toReachTime = lerpTime;
        count = 1;
        target = lerpPoints[1];
        spawned = true;
        lRenderer.enabled = true;
    }

    public void StopBullet()
    {
        lRenderer.enabled = false;
        gameObject.SetActive(false);
    }

    public void OnObjectSpawn()
    {
    }

    public void SetEndPoint(Vector3 end)
    {
        target = end;
        ended = true;
    }
}
