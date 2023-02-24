using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class PlayerProjectileManager : NetworkBehaviour
{
    ObjectPooler objPooler;

    private ItemReference allitems;

    private PlayerAudioManager p_Audio;
    private PlayerReferencer p_Referencer;
    private EquipManager e_Manager;

    [Header("Effects library for creation")]
    public GameObject[] spawnableEffectsObjects;
    public Dictionary<string, GameObject> spawnableEffects = new Dictionary<string, GameObject>();

    [Space]
    public SpawnDecal[] useableDecals;
    private Dictionary<string, SpawnDecal> spawnableDecals = new Dictionary<string, SpawnDecal>();

    [Header("Projectiles")]
    public Dictionary<string, GameObject> registeredProjectiles = new Dictionary<string, GameObject>();
    public GameObject[] spawnableProjectiles;

    public override void OnStartClient()
    {

    }

    private void Start()
    {
        p_Audio = GetComponent<PlayerAudioManager>();
        p_Referencer = GetComponent<PlayerReferencer>();
        e_Manager = GetComponent<EquipManager>();

        objPooler = ObjectPooler.Instance;

        //we initialise all effect dictionaries
        foreach (GameObject g in spawnableEffectsObjects)
        {
            spawnableEffects.Add(g.name, g);
        }

        foreach (GameObject go in spawnableProjectiles)
        {
            registeredProjectiles.Add(go.name, go);
        }

        foreach (SpawnDecal item in useableDecals)
        {
            spawnableDecals.Add(item.decalName, item);
        }
    }

    public void SetItemReference(ItemReference iRef)
    {
        allitems = iRef;
    }

    public void InitialiseProjectileObj(string projKey, Vector3 position, Quaternion rotation, bool spawnCollided, float velocity, Vector3 direction, float damage, int dur)
    {
        CmdDoProjectileSpawn(projKey, position, rotation, spawnCollided, velocity, direction, damage, dur);
    }
    [ServerRpc] void CmdDoProjectileSpawn(string projKey, Vector3 position, Quaternion rotation, bool spawnCollided, float velocity, Vector3 direction, float damage, int dur)
    {
        GameObject projPrefab = registeredProjectiles[projKey];

        GameObject projInstance = Instantiate(projPrefab, position, rotation);
        ProjectileObject pObject = projInstance.GetComponent<ProjectileObject>();

        ServerManager.Spawn(projInstance);
        pObject.SetCollisionFrom(this);
        //set so we dont duplicate damage
        pObject.SetDamage(damage, gameObject);
        pObject.ApplyDurability(dur);
        if (spawnCollided)
        {
            pObject.CollidedAtPoint(position);
        }
        else
        {
            pObject.AddVelocity(velocity, direction);
        }

    }

    public GameObject GetProjectileByKey(string key)
    {
        return registeredProjectiles[key];
    }
    public void CreateProjectilePickup(Vector3 point, Quaternion rotation, int id, NetworkObject obj, int storedDurability)
    {
        if (!base.IsOwner) return;
        GenerateProjectile(point, rotation, id, obj, storedDurability);
    }

    [ServerRpc]
    public void GenerateProjectile(Vector3 point, Quaternion rotation, int id, NetworkObject obj, int dur)
    { 
        obj.Despawn();

        ItemData createData = allitems.allItems[id];

        GameObject pickupObjInstance = Instantiate(createData.pickupPrefab, point, rotation);

        ItemInstance pickupInstance = new ItemInstance();
        pickupInstance.id = id;
        pickupInstance.stackedItemIds = new List<int>();
        pickupInstance.currentDurability = dur;

        switch (createData.itemType)
        {
            case ItemData.Item_Type.material:
                break;
            case ItemData.Item_Type.tool:
                break;
            case ItemData.Item_Type.gun:
                GunObject g_Object = createData.equipObject.GetComponent<GunObject>();
                for (int i = 0; i < g_Object.attachments.Length; i++)
                {
                    AttachmentClass newClass = new AttachmentClass();
                    newClass.occupied = false;
                    newClass.toAttachedId = i;
                    pickupInstance.storedAttachments.Add(newClass);
                }
                break;
            case ItemData.Item_Type.clothing:
                break;
            case ItemData.Item_Type.magazine:
                break;
            case ItemData.Item_Type.attachment:
                break;
            case ItemData.Item_Type.ammunition:
                break;
            case ItemData.Item_Type.meleeWeapon:
                break;
        }

        pickupObjInstance.GetComponent<ItemPickupScript>().SetInstance(pickupInstance);

        ServerManager.Spawn(pickupObjInstance);
    }

    public void SpawnDecal(CollisionParticleData decalData, Vector3 position, Vector3 orientation, WorldSurface.SurfaceType surface)
    {
        string useKey = "";
        switch (surface)
        {
            case WorldSurface.SurfaceType.DirtyGround:
                useKey = decalData.decal_DirtyGround;
                break;
            case WorldSurface.SurfaceType.Grass:
                useKey = decalData.decal_Grass;
                break;
            case WorldSurface.SurfaceType.Gravel:
                useKey = decalData.decal_Gravel;
                break;
            case WorldSurface.SurfaceType.Leaves:
                useKey = decalData.decal_Leaves;
                break;
            case WorldSurface.SurfaceType.Metal:
                useKey = decalData.decal_Metal;
                break;
            case WorldSurface.SurfaceType.Sand:
                useKey = decalData.decal_Sand;
                break;
            case WorldSurface.SurfaceType.Wood:
                useKey = decalData.decal_Wood;
                break;
            case WorldSurface.SurfaceType.Water:
                useKey = decalData.decal_Water;
                break;
            case WorldSurface.SurfaceType.Snow:
                useKey = decalData.decal_Snow;
                break;
            case WorldSurface.SurfaceType.Tile:
                useKey = decalData.decal_Tile;
                break;
            case WorldSurface.SurfaceType.Rock:
                useKey = decalData.decal_Rock;
                break;
            case WorldSurface.SurfaceType.Mud:
                useKey = decalData.decal_Mud;
                break;
            case WorldSurface.SurfaceType.Flesh:
                useKey = decalData.decal_Flesh;
                break;
        }

        //do the spawn yo
        if (IsServer)
        {
            RpcSpawnDecal(useKey, position, orientation);
        }
        else
        {
            CmdSpawnDecal(useKey, position, orientation);
        }
    }

    [ServerRpc]
    public void CmdSpawnDecal(string key, Vector3 position, Vector3 orientation)
    {
        RpcSpawnDecal(key, position, orientation);
    }

    [ObserversRpc]
    public void RpcSpawnDecal(string key, Vector3 position, Vector3 orientation)
    {
        GameObject instance = objPooler.SpawnFromPool("Decal_Object", position, Quaternion.LookRotation(orientation));

        SpawnDecal decalData = spawnableDecals[key];
        DecalEffectScript dEffect = instance.GetComponent<DecalEffectScript>();

        if(decalData.randomRot == true)
        {
            dEffect.rotator.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 180f));
        }
        else
        {
            dEffect.rotator.localRotation = Quaternion.identity;
        }

        Material useMat = decalData.decalMats[Random.Range(0, decalData.decalMats.Length)];
        dEffect.d_Projector.material = useMat;
        dEffect.d_Projector.size.Set(decalData.size, decalData.size, dEffect.d_Projector.size.z);
        dEffect.d_Projector.fadeFactor = decalData.opacity;
    }

    //doesnt work right now
    #region not working
    public void CreateProjectileEffect(GameObject particle, Vector3 position, Quaternion direction)
    {
        DoEffect(particle, position, direction);
    }

    [ServerRpc]
    public void DoEffect(GameObject particle, Vector3 position, Quaternion direction)
    {
        GameObject pInstance = Instantiate(particle, position, direction);

        ServerManager.Spawn(pInstance);

        Destroy(pInstance, 3f);
    }
    #endregion

    public void CreateCollsionEffect(WorldSurface.SurfaceType surface, CollisionParticleData pData, Vector3 location, Vector3 orientation)
    {
        if (base.IsOwner)
        {
            string particleKey = null;
            string toSound = null;
            //get relevant values
            switch (surface)
            {
                case WorldSurface.SurfaceType.DirtyGround:
                    particleKey = pData.particlesDirtyGround[Random.Range(0, pData.particlesDirtyGround.Length)];
                    toSound = pData.clip_DirtyGround;
                    break;
                case WorldSurface.SurfaceType.Grass:
                    particleKey = pData.particlesGrass[Random.Range(0, pData.particlesGrass.Length)];
                    toSound = pData.clip_Grass;
                    break;
                case WorldSurface.SurfaceType.Gravel:
                    particleKey = pData.particlesGravel[Random.Range(0, pData.particlesGravel.Length)];
                    toSound = pData.clip_Gravel;
                    break;
                case WorldSurface.SurfaceType.Leaves:
                    particleKey = pData.particlesLeaves[Random.Range(0, pData.particlesLeaves.Length)];
                    toSound = pData.clip_Leaves;
                    break;
                case WorldSurface.SurfaceType.Metal:
                    particleKey = pData.particlesMetal[Random.Range(0, pData.particlesMetal.Length)];
                    toSound = pData.clip_Metal;
                    break;
                case WorldSurface.SurfaceType.Sand:
                    particleKey = pData.particlesSand[Random.Range(0, pData.particlesSand.Length)];
                    toSound = pData.clip_Sand;
                    break;
                case WorldSurface.SurfaceType.Wood:
                    particleKey = pData.particlesWood[Random.Range(0, pData.particlesWood.Length)];
                    toSound = pData.clip_Wood;
                    break;
                case WorldSurface.SurfaceType.Water:
                    particleKey = pData.particlesWater[Random.Range(0, pData.particlesWater.Length)];
                    toSound = pData.clip_Water;
                    break;
                case WorldSurface.SurfaceType.Snow:
                    particleKey = pData.particlesSnow[Random.Range(0, pData.particlesSnow.Length)];
                    toSound = pData.clip_Snow;
                    break;
                case WorldSurface.SurfaceType.Tile:
                    particleKey = pData.particlesTile[Random.Range(0, pData.particlesTile.Length)];
                    toSound = pData.clip_Tile;
                    break;
                case WorldSurface.SurfaceType.Rock:
                    particleKey = pData.particlesRock[Random.Range(0, pData.particlesRock.Length)];
                    toSound = pData.clip_Rock;
                    break;
                case WorldSurface.SurfaceType.Mud:
                    particleKey = pData.particlesMud[Random.Range(0, pData.particlesMud.Length)];
                    toSound = pData.clip_Mud;
                    break;
                case WorldSurface.SurfaceType.Flesh:
                    particleKey = pData.particlesFlesh[Random.Range(0, pData.particlesFlesh.Length)];
                    toSound = pData.clip_Flesh;
                    break;
            }

            if (toSound != null || toSound != "")
            {
                p_Audio.PlaySound(toSound, location);
            }

            //create effect
            if (IsServer)
            {
                RpcCreateCollisionParticle(particleKey, location, orientation);
            }
            else
            {
                CmdSendCollisionParticleEffect(particleKey, location, orientation);
            }
        }
        else
        {
            //for making commands from relevant player
            PlayerProjectileManager toFrom = p_Referencer.GetComponent<PlayerProjectileManager>();

            toFrom.CreateCollsionEffect(surface, pData, location, orientation);
        }
    }

    [ServerRpc] void CmdSendCollisionParticleEffect(string spawnableParticleKey, Vector3 position, Vector3 orienation)
    {
        RpcCreateCollisionParticle(spawnableParticleKey, position, orienation);   
    }
    [ObserversRpc] public void RpcCreateCollisionParticle(string spawnableParticleKey, Vector3 position, Vector3 orienation)
    {
        Quaternion toRot = Quaternion.LookRotation(orienation);

        //create the particle
        GameObject instance = Instantiate(spawnableEffects[spawnableParticleKey], position, toRot);

        //destroy
        Destroy(instance, 3f);
    }

    public void CreateMultiplayerBullet(Vector3[] positions, float iterationTime)
    {
        if (base.IsOwner)
        {
            if (positions.Length < 2) return;

            //there is the aim to halve the amount of iterations between the points in order to make it more optimised

            List<Vector3> updatedPositions = new List<Vector3>();
            updatedPositions.Add(e_Manager.GetSocketPos()); //we add first point

            for (int i = 1; i < positions.Length; i += 2)
            {
                //start at one so we avoid starting at barrel end position
                if (i >= positions.Length)
                {
                    updatedPositions.Add(positions[positions.Length - 1]);
                    break;
                }
                else
                {
                    updatedPositions.Add(positions[i]);
                }
            }
            float updatedTime = iterationTime * 2f;

            if (IsServer)
            {
                RpcMakeMultiplayerBullet(updatedPositions.ToArray(), updatedTime);
            }
            else
            {
                CmdSendMultiplayerBullet(updatedPositions.ToArray(), updatedTime);
            }
        }
        else
        {
            //for if not being called from desired part
            p_Referencer.GetPlayer().GetComponent<PlayerProjectileManager>().CreateMultiplayerBullet(positions, iterationTime);
        }
    }
    [ServerRpc] void CmdSendMultiplayerBullet(Vector3[] positions, float timeBetween) { RpcMakeMultiplayerBullet(positions, timeBetween); }
    [ObserversRpc] public void RpcMakeMultiplayerBullet(Vector3[] positions, float timeBetween)
    {
        if (!base.IsOwner)
        {
            GameObject bulletInstance = objPooler.SpawnFromPool("MultiplayerBullet", positions[0], Quaternion.identity);
            bulletInstance.GetComponent<MultiplayerBulletRepScript>().SpawnObject(positions, timeBetween);
        }
    }
}

[System.Serializable]
public struct SpawnDecal
{
    public string decalName;
    public float size;
    public float opacity; //out of 1
    public bool randomRot;
    public Material[] decalMats;
}
