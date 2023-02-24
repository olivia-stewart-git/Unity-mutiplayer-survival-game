using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class BuildObject : NetworkBehaviour
{
    [Header("settings")]
    public bool showEditMenu = true;
    [SerializeField] private BuildData bData;

    public Transform editCentrePosition;
    [Space]
    public LayerMask connectionCheck;
    public SnapPoint[] snapPoint;

    [SyncVar(OnChange = nameof(OnInstanceUpdated))] private BuildInstance instance;

    //who owns the object and has authority over it
    [SyncVar] private string ownerId;

    [Header("Health setting")]

    [SyncVar] private int curHealth;

    //for when the object is updated
    public void OnInstanceUpdated(BuildInstance prev, BuildInstance next, bool asServer)
    {

    }



    //must be called throught the server
    public void SetOwnership(string id)
    {
        ownerId = id;
    }

    public void SetBuildInstance(BuildInstance buildInstance) //must be called by server
    {
        instance = buildInstance;
    }

    public void Awake()
    {
        Debug.Log("Build object created!" + gameObject);


        AssignConnections();
    }

    void AssignConnections()
    {
        for (int i = 0; i < snapPoint.Length; i++)
        {
            Collider[] snapHitDetect = Physics.OverlapBox(snapPoint[i].point.position, Vector3.one * 0.5f, Quaternion.identity, connectionCheck, QueryTriggerInteraction.Collide);
            if(snapHitDetect != null && snapHitDetect.Length > 0)
            {
                foreach (Collider col in snapHitDetect)
                {
                    if(col.transform != transform && col.transform.CompareTag("Build"))
                    {
                        BuildObject targetObj = col.transform.GetComponent<BuildObject>();
                        for (int a = 0; a < targetObj.snapPoint.Length; a++)
                        {
                            if (snapPoint[i].sType == SnapPoint.SnapType.corner)
                            {
                                if(targetObj.snapPoint[a].sType == SnapPoint.SnapType.toCorner)
                                {
                                    float dist = Vector3.Distance(targetObj.snapPoint[a].point.position, snapPoint[i].point.position);
                                    if (dist < 0.1f)
                                    {
                                        targetObj.snapPoint[a].connected = true;
                                        snapPoint[i].connected = true;
                                        Debug.Log("Found connection " + snapPoint[i].point);
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                float dist = Vector3.Distance(targetObj.snapPoint[a].point.position, snapPoint[i].point.position);
                                if (dist < 0.1f)
                                {
                                    targetObj.snapPoint[a].connected = true;
                                    snapPoint[i].connected = true;
                                    Debug.Log("Found connection " + snapPoint[i].point);
                                    break;
                                }
                            }
                        }
                    
                    }
                }
            }
        }
    }

    //so we can remove adjacent connections
    public void OnBuildDestroy()
    {
      
    }



    public BuildInstance BuildInstance()
    {
        return instance;
    }

    public void OnDrawGizmosSelected()
    {
        //draw gizmos
        if (snapPoint.Length > 0)
        {
            foreach (SnapPoint sPoint in snapPoint)
            {
                if (sPoint.connected)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(sPoint.point.position, 0.15f);
                }
                else
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(sPoint.point.position, 0.1f);
                }
                Gizmos.DrawLine(sPoint.point.position, sPoint.point.position + (sPoint.point.forward * 0.2f));
            }
        }        
    }

    //these must be called from the server
    public void TakeBuildDamage(int damage, int penetration, Vector3 direction)
    {
        if (penetration > bData.buildPenetrationDefense)
        {
            curHealth -= damage;
            if(curHealth < 0)
            {
                curHealth = 0;
            }

            Debug.Log("Build taken damage " + gameObject + " health of " + curHealth);
        }
    }

    public int Health()
    {
        return curHealth;
    }

    public void SetBaseHealth()
    {
        curHealth = bData.maxHealth;
    }

    public int BuildHealth()
    {
        return curHealth;
    }

    public float HealthPercent()
    {
        return ((float)curHealth / bData.maxHealth);
    }
}

[System.Serializable]
public struct SnapPoint
{
    public bool connected;
    public bool allowConnectToConnected;
    public Transform point;
    public enum SnapType { foundation, toFoundation, wall, window, corner, other, door, toWall, toCorner, floorSocket, doorSocket}
    public bool exludeFromSnappingTo;
    public SnapType sType;
}
[System.Serializable]
public class BuildInstance
{
    public int buildId;
}


[System.Serializable]
public struct CollisionBounds
{
    public Transform topBounds;
    public Transform bottomBounds;
}
