using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileTesting : MonoBehaviour
{
    public float angle = 30f;
    public float startvelocity = 5.6f;
    public float gravity = 9.8f;
    public int iterations = 100;

    public LineRenderer debugRenderer;
    private void Start()
    {
        debugRenderer.positionCount = iterations;
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < iterations; i++)
        {
            float y = GetY(i);
            debugRenderer.SetPosition(i, new Vector3(i, y, 0));
        }
    }

    public float GetY(float x)
    {     
        float tanPart = x * Mathf.Tan(Mathf.Deg2Rad * angle);
        float cosPart = (gravity * x * x) / (2 * (startvelocity * Mathf.Cos(Mathf.Deg2Rad * angle)) * (startvelocity * Mathf.Cos(angle)));
        float yVal = tanPart - cosPart;
        return yVal;
    }
}
