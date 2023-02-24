using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class InventoryHolder 
{
    public List<ItemInstance> itemsStored; //items stored here
    public List<InventorySlot> createdSlots; //list of all slots
    public InventorySlot[,] slots2d; //holds a 2d array of slots which references the position
    public GameObject linkedObject;
}
