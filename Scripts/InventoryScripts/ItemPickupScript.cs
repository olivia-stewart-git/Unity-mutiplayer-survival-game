using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

[RequireComponent(typeof(Outline))]
[RequireComponent(typeof(Rigidbody))]
public class ItemPickupScript : NetworkBehaviour, I_Interactable
{
    //settings
    public ItemData thisItem;
    public GameObject thisPrefab;
    private bool pickupAble = true;

    [SyncVar]
    private ItemInstance item_Instance; //the instance of the item in this object   

    public void Interacted(GameObject source)
    { 
        if (pickupAble)
        {
            InventoryManager iManage = source.GetComponent<InventoryManager>();
            bool added = iManage.AddToInventory(item_Instance, gameObject);
            if (added)
            {
                iManage.PlayPickupNotification(item_Instance.id, item_Instance.stackedItemIds.Count + 1);
            }
        }
    }

    public void Start()
    {
        this.GetComponent<Outline>().enabled = false;
    }

    private void InitialiseInstance()
    {
        this.GetComponent<Outline>().enabled = false;
        ItemInstance useInstance = new ItemInstance();

        useInstance.id = thisItem.itemId;
        useInstance.stackedItemIds = new List<int>();
        useInstance.currentDurability = 1000;

        item_Instance = useInstance;
    }

    public int ReturnStack()
    {
        if(item_Instance == null)
        {
            return 0;
        }
        if(thisItem.stack_type == ItemData.StackType.none)
        {
            return 1;
        }
        int amount = item_Instance.stackedItemIds.Count;
        if(thisItem.stack_type == ItemData.StackType.standard)
        {
            amount++;
        }
        return amount;
    }


    public void SetInstance(ItemInstance instance)
    {
        item_Instance = instance;
        GetComponent<Outline>().enabled = false;
    }

    public string GetName()
    {
        return thisItem.itemName;
    }

    public ItemInstance GetInstance()
    {
        return item_Instance;
    }
}
