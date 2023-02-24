using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecalManager : MonoBehaviour
{
    public SpawnableDecal[] inputDecals;

    private Dictionary<string, SpawnableDecal> spawnableDecals;

    private ObjectPooler oPooler;


    private bool initialised = false;
    public void Initialise()
    {
        oPooler = ObjectPooler.Instance;

        spawnableDecals = new Dictionary<string, SpawnableDecal>();

        foreach (SpawnableDecal dec in inputDecals)
        {
            spawnableDecals.Add(dec.name, dec);
        }

        initialised = true;
    }

    public void RenderDecal(string name, Vector3 position, Vector3 normal)
    {
        initialised = false;
    }
}

[System.Serializable]
public struct SpawnableDecal
{
    public string name;
    public float width;
    public Material decalmaterial;
}
