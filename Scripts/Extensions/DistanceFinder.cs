using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DistanceFinder : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;

    [Space]
    public float calculatedDistance;
    public float yDistance;
    public float xDistance;
    public float zDistance;

    public void CalculateDistance()
    {
        Vector3 vectorToUse = pointB.position - pointA.position;
        yDistance = vectorToUse.y;
        xDistance = vectorToUse.x;
        zDistance = vectorToUse.z;

        calculatedDistance = Vector3.Magnitude(vectorToUse);
    }
}
