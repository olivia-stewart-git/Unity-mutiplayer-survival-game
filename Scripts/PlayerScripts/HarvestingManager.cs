using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Connection;

public class HarvestingManager : NetworkBehaviour
{
    public enum HarvestType {wood, rock}

    [Header("Harvesting settings")]
    [SerializeField] private float harvestMultiplier;
 

    private InventoryManager inventoryMangager;
    private ItemReference itemReference;

    public void InitialiseManager(ItemReference iRef)
    {
        inventoryMangager = GetComponent<InventoryManager>();
        itemReference = iRef;
    }

    //here we initialise all pickups
    public override void OnStartServer()
    {
        GameObject[] toInit = GameObject.FindGameObjectsWithTag("HarvestNode");

        foreach (GameObject g in toInit)
        {
            HarvestNode node = g.GetComponent<HarvestNode>();
            node.Initialise(gameObject);
        }

        Debug.Log("initialised_Nodes");
    }

    public void  HarvestCall(GameObject target,float damage, Vector3 direction, Vector3 point, Vector3 normal, HarvestingManager.HarvestType hType, int sourceItemId, Vector3 from)
    {
        HarvestNode hNode = target.GetComponent<HarvestNode>();

        Debug.Log("attempt harvest");

        if (hType != hNode.harvestType) return;
        
        CmdHarvestCall(target, damage, direction, point, normal, hType, base.Owner, sourceItemId, from);
    }


    [ServerRpc]
    public void CmdHarvestCall(GameObject target, float damage, Vector3 direction, Vector3 point, Vector3 normal, HarvestingManager.HarvestType hType, NetworkConnection conn, int sourceItemId, Vector3 from)
    {
        RpcLocalHarvest(conn, target, damage, direction, point, from);
        RpcHarvestCall(target, direction, point, normal, from);

        target.GetComponent<HarvestNode>().SubtractHealth(damage); //for syncing the health 
    }

    [ObserversRpc]
    public void RpcHarvestCall(GameObject target, Vector3 direction, Vector3 point, Vector3 normal, Vector3 from)
    {
        HarvestNode hNode = target.GetComponent<HarvestNode>();
        hNode.ResourceDamage(direction, point, normal);
    }   

    [TargetRpc]//this just handles the adding to inventory
    public void RpcLocalHarvest(NetworkConnection conn, GameObject target, float damage, Vector3 direction, Vector3 point, Vector3 from)
    {
        //calculate the amount of resources to give

        HarvestNode hNode = target.GetComponent<HarvestNode>();
        
        int amountToAdd = hNode.ReturnHarvest(damage, harvestMultiplier, direction, point, from); //the multiplier is to set the damage into the right range (also handles the weak point code)

        ItemData useData = itemReference.allItems[hNode.GetHarvestingItem()];

        ItemInstance addInstance = new ItemInstance();

        addInstance.id = useData.itemId;
        addInstance.stackedItemIds = new List<int>();

        for (int i = 0; i < amountToAdd; i++)
        {
            bool added = inventoryMangager.AddToInventory(addInstance, null);
            if (added)
            {
               inventoryMangager.PlayPickupNotification(addInstance.id, addInstance.stackedItemIds.Count + 1);               
            }
        }

    }

    //we don't actually destroy, just set inactive
    public void DestroyNode(GameObject node)
    {
        if (!IsServer)
        {
            DeactivateNode(node);
        }

        HarvestNode hNode = node.GetComponent<HarvestNode>();
        hNode.ResetHealth();
        hNode.SetActiveState(false);
    }
    [ServerRpc] 
    public void DeactivateNode(GameObject node)
    {
        DestroyNode(node);
    }

    [ServerRpc]
    public void ResetResource(GameObject resource)
    {
        HarvestNode hNode = resource.GetComponent<HarvestNode>();
        hNode.ResetHealth();

        RpcResetResource(resource);
    }
    
    [ObserversRpc]
    public void RpcResetResource(GameObject hNode)
    {
        hNode.GetComponent<HarvestNode>().ResetResourceLocal();
    }
}

