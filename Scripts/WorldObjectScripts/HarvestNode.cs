using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class HarvestNode : NetworkBehaviour
{
    [Header("node settings")]
    public MeshFilter thisMesh;
    [Space]
    public HarvestingManager.HarvestType harvestType;

    [SerializeField] private int maxHealth;

    [SyncVar(OnChange = nameof(OnHealthChange))] private int health;
    [SyncVar(OnChange = nameof(UpdateActiveState))] private bool active = false;


    [Header("harvest settings")]
    [SerializeField] private float lastIntMultiplier = 2f;
    [SerializeField] private int returnedResource; //put the item id
    [SerializeField] private float baseHarvestAmount;

    [Header("av settings")]
    [SerializeField] private GameObject turnOffObjOnDie;
    [SerializeField] private ParticleSystem hitParticles;
    [SerializeField] private ParticleSystem deactiveParticles;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip breakSound;

    private AudioSource source;

    private bool initialised = false;

    private GameObject server;
    private HarvestingManager hManager;

    //this is called from the server yo

    [Header("WeakPoint")]
    [SerializeField] private bool useWeakPoint;
    [SerializeField] private bool alignToCentre = false;
    [SerializeField] private LayerMask weakPointLayer;
    [SerializeField] private AudioClip weakPointHitSound;
    [SerializeField] private ParticleSystem weakPointParticles;
    [Space]
    [SerializeField] private GameObject weakPoint;
    [SerializeField] private float weakPointMultiplier = 2f;

    [Header("events")]

    public UnityEvent<Vector3> m_SetInactive;

    public UnityEvent m_OnReactivated;

    public UnityEvent<int> m_OnHealthChange;

    private bool weakPointactive;

    private Coroutine hitCoroutine;

    public void Initialise(GameObject _server)
    {
        health = maxHealth;
        server = _server;
        hManager = server.GetComponent<HarvestingManager>();
        active = true;

        initialised = true;
    }

    private void Start()
    {
        if (m_SetInactive == null) m_SetInactive = new UnityEvent<Vector3>();

        if (m_OnReactivated == null) m_OnReactivated = new UnityEvent();

        if (m_OnHealthChange == null) m_OnHealthChange = new UnityEvent<int>();

        source = GetComponent<AudioSource>();
        //get vertices
        InitialiseVertices();
    }

    void InitialiseVertices()
    {
        vertices = thisMesh.mesh.vertices;
        normals = thisMesh.mesh.normals;

        //transform the vertices to world position
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = transform.TransformPoint(vertices[i]);
        }
    }

    public void ResourceDamage(Vector3 direction, Vector3 point, Vector3 normal)
    {
        if (!initialised || !active) return;
        lastDir = direction;

        hitParticles.transform.rotation = Quaternion.LookRotation(normal);
        hitParticles.transform.position = point;

        hitParticles.Play();
        source.PlayOneShot(hitSounds[Random.Range(0, hitSounds.Length)]);
    }

    public void SubtractHealth(float damage)
    {
        if (!initialised || !active) return;
        health -= (int)damage;
        health = Mathf.Clamp(health, 0, maxHealth);

        if (health <= 0)
        {
            hManager.DestroyNode(gameObject);
        }
    }

    public void ResetHealth()
    {
        health = maxHealth;
    }


    public int GetHarvestingItem()
    {
        return returnedResource;
    }

    public int ReturnHarvest(float damage, float multiplier, Vector3 direction, Vector3 point, Vector3 from)
    {
        //this is actually where we get hit
        int returnValue = (int)Mathf.RoundToInt((damage + baseHarvestAmount) * multiplier);
        if (health - damage <= 0)
        {
            returnValue = (int)(returnValue * lastIntMultiplier);
        }

        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }
        hitCoroutine = StartCoroutine(SinceHitCoroutine());

        //check weakpoint
        if (useWeakPoint)
        {
            if (weakPointactive)
            {
                //check the weak point
                bool hit = CheckWeakPoint(direction, point, from);
                if (hit)
                {
                    returnValue = (int)Mathf.RoundToInt(returnValue * weakPointMultiplier);
                    EZCameraShake.CameraShaker.Instance.ShakeOnce(2f, 2f, 0f, 0.5f);
                    source.PlayOneShot(weakPointHitSound);
                }
                else
                {
                    EZCameraShake.CameraShaker.Instance.ShakeOnce(1f, 1f, 0f, 0.5f);
                }

                //set new weak point position
                GenerateWeakPoint(direction, point, from);
            }
            else
            {
                SetWeakPointActive();
                GenerateWeakPoint(direction, point, from);

                EZCameraShake.CameraShaker.Instance.ShakeOnce(1f, 1f, 0f, 0.5f);
            }
        }
        return returnValue;
    }

    private bool CheckWeakPoint(Vector3 direction, Vector3 point, Vector3 from)
    {
        float checkDist = Vector3.Distance(point, from) + 0.5f;

        RaycastHit hit;

        //Debug.DrawRay(from, direction.normalized, Color.blue, 5f);

        //check to see if hit weakpoinbt
        if (Physics.Raycast(from, direction, out hit, checkDist, weakPointLayer))
        {
            weakPointParticles.transform.position = hit.point;
            weakPointParticles.transform.rotation = Quaternion.LookRotation(hit.normal);

            weakPointParticles.Play();

            return true;
        }

        return false;
    }

    private IEnumerator SinceHitCoroutine()
    {
        weakPointactive = true;
        yield return new WaitForSeconds(10f);
        weakPointactive = false;
        SetWeakPointInactive();
    }

    private void SetWeakPointActive()
    {
        weakPoint.SetActive(true);
    }

    private void SetWeakPointInactive()
    {
        weakPoint.SetActive(false);
    }

    private Vector3[] vertices;
    private Vector3[] normals;
    //this are all the returned positions in range
    private List<int> verticeIndexes = new List<int>();
    public void GenerateWeakPoint(Vector3 direction, Vector3 hitPoint, Vector3 from)
    {
        if (!initialised) return;


        SetWeakPointActive();

        verticeIndexes.Clear();

        for (int i = 0; i < normals.Length; i++)
        {
            float dot = Vector3.Dot(direction.normalized, normals[i].normalized);
            //here make it so it wont be facing down
            float floorDot = Vector3.Dot(direction, Vector3.down);

            float yVal = from.y;//we do this so that we dont get weak points high up

            //distance check so we don't get weak point spawning at , say the top of a tree
            if (dot < -0.7f && Vector3.Distance(hitPoint, vertices[i]) < 2f && Mathf.Abs(vertices[i].y - yVal) < 1.5f)
            {
                Debug.DrawLine(vertices[i], vertices[i] + normals[i], Color.grey, 5f);

                verticeIndexes.Add(i);
            }
        }

        if (verticeIndexes.Count > 0)
        {
            int choseIndex = verticeIndexes[Random.Range(0, verticeIndexes.Count)];
            Vector3 pt = vertices[choseIndex];
            Vector3 normal = normals[choseIndex];

            //for some randomness
            Vector3 modPt = pt + new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));

            Vector3 chFrom = pt + normal;
            Vector3 dir = modPt - chFrom;

            RaycastHit hit;
            if (Physics.Raycast(chFrom, dir, out hit, Vector3.Distance(chFrom, modPt) + 0.2f, 0))
            {
                weakPoint.transform.position = hit.point;
                weakPoint.transform.rotation = Quaternion.LookRotation(-hit.normal);
                return;
            }

            weakPoint.transform.position = pt;
            if (alignToCentre)
            {
                Vector3 lookrot = transform.position - pt;
                weakPoint.transform.rotation = Quaternion.LookRotation(-lookrot);
            }
            else
            {
                weakPoint.transform.rotation = Quaternion.LookRotation(normals[choseIndex]);
            }

            Debug.Log("set weak point " + weakPoint);
        }
        else
        {
            //if no points found set inactive
            if (weakPointactive)
            {
                if (hitCoroutine != null)
                {
                    StopCoroutine(hitCoroutine);
                }
                SetWeakPointInactive();
            }
        }
    }

    public void SetActiveState(bool value)
    {
        active = value;
    }

    private Vector3 lastDir;
    public void UpdateActiveState(bool prev, bool next, bool asServer)
    {
        if (!next && prev == true) //for tree drying
        {
            if (hitCoroutine != null)
            {
                StopCoroutine(hitCoroutine);
            }
            SetWeakPointInactive();
            weakPointactive = false;

            turnOffObjOnDie.SetActive(false);
            deactiveParticles.Play();
            source.PlayOneShot(breakSound);

            //call dead 
            m_SetInactive.Invoke(lastDir);
        }
    }

    public void ResetResourceLocal()
    {
        m_OnReactivated.Invoke();
        health = maxHealth;
    }

    public void OnHealthChange(int prev, int next, bool asServer)
    {
        m_OnHealthChange.Invoke(next);
    }
}
