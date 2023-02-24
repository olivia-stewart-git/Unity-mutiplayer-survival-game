using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using Steamworks;

public class PlayerBuildingManager : NetworkBehaviour
{
    public enum BuildType {foundation, wall, floor, stairs, pillar, misc}

    private ItemReference itemReference;
    public void InitialiseItems(ItemReference iRef)
    {
        itemReference = iRef;
    }

    private BuildUiObject bObj;
    public void InitialiseUi(UiReference uiRef)
    {
        bObj = uiRef.buildUi;

        bObj.overachingUi.SetActive(false);
    }

    public BuildUiObject BuildUi()
    {
        return bObj;
    }

    public ItemReference GetItemReference()
    {
        return itemReference;
    }

    public void SendBuildItemRequest(int id, Vector3 position ,Quaternion rotation)
    {
        string ownerid = SteamUser.GetSteamID().ToString();
        CmdBuildObject(id, position, rotation, ownerid);
    }

    [ServerRpc] public void CmdBuildObject(int id, Vector3 position, Quaternion rotation, string ownerId)
    {
        GameObject toBuild = itemReference.allBuildItems[id].createObject;

        GameObject createdInstance = Instantiate(toBuild, position, rotation);
        BuildObject bObj = createdInstance.GetComponent<BuildObject>();
        bObj.SetOwnership(ownerId);
        BuildInstance bInstance = new BuildInstance();

        bInstance.buildId = id;

        bObj.SetBuildInstance(bInstance);
        bObj.SetBaseHealth();

        ServerManager.Spawn(createdInstance, null);
    }

    public void DamageBuild(GameObject target, int damage, int penetration, Vector3 direction)
    {
        Cmd_DamageBuild(target, damage, penetration, direction);
    }

    [ServerRpc] public void Cmd_DamageBuild(GameObject target, int damage, int penetration, Vector3 direction)
    {
        BuildObject bObject = target.GetComponent<BuildObject>();
        bObject.TakeBuildDamage(damage, penetration, direction);
        //check for the health
        int bHealth = bObject.Health();
        if(bHealth <= 0)
        {
            //desstroy build
            Cmd_DestroyBuild(target);
        }
    }

    public void DestroyBuild(GameObject target)
    {
        Cmd_DestroyBuild(target);
    }

    [ServerRpc] public void Cmd_DestroyBuild(GameObject target)
    {
        //Rpc_OnBuildDestroy(target);
        Destroy(target);
    }

    [ObserversRpc] public void Rpc_OnBuildDestroy(GameObject target)
    {
        target.GetComponent<BuildObject>().OnBuildDestroy();
    }

    public void RepairBuild(GameObject target, float amount)
    {
        Cmd_RepairBuild(target, amount);
    }

    [ServerRpc]
    public void Cmd_RepairBuild(GameObject target, float amount)
    {

    }
}
