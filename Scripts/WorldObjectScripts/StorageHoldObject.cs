using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

[RequireComponent(typeof(Outline))]
[RequireComponent(typeof(Rigidbody))]
public class StorageHoldObject : NetworkBehaviour, I_Interactable
{
    [SyncVar] private bool open;
    [SyncVar] private List<SerialisedInventoryItem> storedItems = new List<SerialisedInventoryItem>();

    InventoryManager p_Inventory;
    PlayerMenuManager p_Menu;

    [Header("Storage settings")]
    public string storagename;
    [SerializeField] private int xScale;
    [SerializeField] private int yScale;


    public void Start()
    {
        GetComponent<Outline>().enabled = false;
    }

    public void Interacted(GameObject source)
    {
        p_Inventory = source.GetComponent<InventoryManager>();
        p_Menu = source.GetComponent<PlayerMenuManager>();

        if (open == false)
        {
            p_Menu.OpenInventory();

            p_Inventory.GenerateSecondInventory(xScale, yScale, storedItems, gameObject, storagename);
        }
    }

    public void SetOpen(bool value)
    {
        open = value;
    }

    public void Setitems(List<SerialisedInventoryItem> items)
    {
        storedItems = items;
    }
    public void SetSize(int x, int y)
    {
        xScale = x;
        yScale = y;
    }
}
