using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HotBarManager : NetworkBehaviour
{
    private ItemReference allitems;

    [SerializeField] private EquipManager equipManager;

    InventoryHolder holderInstance;

    private InventorySlot[] hotBarSlots;

    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private PlayerMenuManager p_Menu;
    [SerializeField] private int hotbarX;
    [SerializeField] private int hotbarY;

    [Header("Generating game hotbar")]
    [SerializeField] private GameObject emptySlotObject;
    [Space]
    [SerializeField] private Color durabiltyOutColor;
    [SerializeField] private Color durabiltyFullColor;
    [Space]
    private Transform creationPosition;

    private List<ItemInstance> hotBarInstances;
    private List<GameObject> createdHotBarSlots;

    [Header("Selection settings")]
    [SerializeField] private Color unselectedColor;
    [SerializeField] private Color selectedColor;

    private Vector2 scrollValue;
    private float lastSwitch;

    private int currentSlot;
    private int maxSlots;

    public void Update()
    {
        if (!base.IsOwner) return;
        if (scrollValue.y != 0 && p_Menu.inventoryOpen == false && createdHotBarSlots != null)
        {
            if (scrollValue.y > 0 && createdHotBarSlots.Count > 1)
            {
                if (Time.time > lastSwitch)
                {
                    lastSwitch = Time.time + 0.1f;
                    HotBarRight();
                    HotBarMoved();
                }
            }
            else
            {
                if (Time.time > lastSwitch && createdHotBarSlots.Count > 1)
                {
                    lastSwitch = Time.time + 0.1f;
                    HotBarLeft();
                    HotBarMoved();
                }
            }
        }
    }
    public bool ContainsSlot(InventorySlot slot)
    {
        foreach (InventorySlot hSlot in hotBarSlots)
        {
            if(hSlot == slot)
            {
                return true;
            }
        }
        return false;
    }


    public void HotBarRight()
    {
        if (currentSlot == maxSlots)
        {
            currentSlot = 1;
        }
        else
        {
            currentSlot++;
        }
        CalculateSlot(false);
    }

    public void HotBarLeft()
    {
        if (currentSlot == 1)
        {
            currentSlot = maxSlots;
        }
        else
        {
            currentSlot--;
        }
        CalculateSlot(false);
    }

    public void HotBarMoved()
    {
        Debug.Log("Current slot " + currentSlot);
    }

    public void RetrieveScrollvalue(InputAction.CallbackContext context)
    {
        scrollValue = context.ReadValue<Vector2>();
    }

    public void SetItemReference(ItemReference iRef)
    {
        allitems = iRef;
    }

    public void InitialiseHotBar(UiReference uiRef)
    {
        if (!base.IsOwner) return;

        hotBarSlots = uiRef.hotBarSlots;
        creationPosition = uiRef.creationPosition;

        InventoryHolder holderInstance = new InventoryHolder();

        holderInstance.createdSlots = new List<InventorySlot>();
        holderInstance.itemsStored = new List<ItemInstance>();
        holderInstance.slots2d = new InventorySlot[hotbarX, hotbarY]; //a 2d matrix I like is epic hellllll yeah

        foreach (InventorySlot item in hotBarSlots)
        {
            holderInstance.createdSlots.Add(item);
            holderInstance.slots2d[item.xPosition, item.yPosition] = item;
            item.i_Manager = inventoryManager;

            item.durabliltyHolder.gameObject.SetActive(false);
            item.stackHolder.gameObject.SetActive(false);
            item.hotBarScript = this;
            item.forceEquipable = true;
            item.usePlacedCallBacks = true;
            item.useStackCallBacks = true;
            
            item.heldIn = holderInstance;
        }
    }

    public void CalculateSlot(bool onGeneration)
    {
        if(currentSlot > maxSlots)
        {
            currentSlot = maxSlots;
        }
        for (int i = 0; i < maxSlots; i++)
        {
            HotBarSlotScript sScript = createdHotBarSlots[i].GetComponent<HotBarSlotScript>();
            if(i + 1 == currentSlot)
            {
                sScript.slotBacking.color = selectedColor;
                createdHotBarSlots[i].transform.localScale = new Vector3(1.1f, 1.1f);
            }
            else
            {
                sScript.slotBacking.color = unselectedColor;
                createdHotBarSlots[i].transform.localScale = Vector3.one;
            }
        }

        if (createdHotBarSlots == null || createdHotBarSlots.Count == 0)
        {
            equipManager.HandsSwitch();
        }
        else
        {
            if (onGeneration == true)
            {
                equipManager.RequestItemEquip(true);
            }
            else
            {
                equipManager.RequestItemEquip(false);
            }
        }
    }

    public void GenerateHotBar()
    {
        if (!base.IsOwner) return;

        //find all instances
        if(createdHotBarSlots != null && createdHotBarSlots.Count > 0)
        {
            foreach (GameObject item in createdHotBarSlots)
            {
                Destroy(item);
            }
        }
        createdHotBarSlots = new List<GameObject>();
        hotBarInstances = new List<ItemInstance>();

        //go through each slot
        foreach (InventorySlot slot in hotBarSlots)
        {
            if(slot.isOccupied == true && slot.isSubSlot == false)
            {
                hotBarInstances.Add(slot.storedItem);
            }
        }

        if(hotBarInstances.Count > 0)
        {
            for (int i = 0; i < hotBarInstances.Count; i++)
            {
                GameObject slotInstance = Instantiate(emptySlotObject, creationPosition);
                createdHotBarSlots.Add(slotInstance);

                //size slot correctly
                HotBarSlotScript toUseScript = slotInstance.GetComponent<HotBarSlotScript>();
                toUseScript.slotNumText.text = (i + 1).ToString();
                ItemData data = allitems.allItems[hotBarInstances[i].id];
                toUseScript.slotImage.sprite = data.iconSprite;
                toUseScript.slotBacking.color = unselectedColor;
            }
        }

        UpdateHotBarValues();

        if (createdHotBarSlots.Count > 0)
        {
            maxSlots = createdHotBarSlots.Count;
        }
        else
        {
            maxSlots = 0;
        }

        if(maxSlots == 1)
        {
            currentSlot = 1;
        }

        CalculateSlot(true);
    }

    public void UpdateHotBarValues()
    {
         if(createdHotBarSlots != null)
        {
            for (int i = 0; i < createdHotBarSlots.Count; i++)
            {
                //size slot correctly
                HotBarSlotScript toUseScript = createdHotBarSlots[i].GetComponent<HotBarSlotScript>();

                ItemData data = allitems.allItems[hotBarInstances[i].id];
                if (data.useDurability == true)
                {
                    toUseScript.durabiltyHolder.SetActive(true);
                    toUseScript.durabiltyText.text = hotBarInstances[i].currentDurability.ToString();
                    toUseScript.durabiltyText.color = Color.Lerp(durabiltyOutColor, durabiltyFullColor, (hotBarInstances[i].currentDurability - 1) / 1000f);
                }
                else
                {
                    toUseScript.durabiltyHolder.SetActive(false);
                }

                if (data.stack_type != ItemData.StackType.none && hotBarInstances[i].stackedItemIds.Count > 0)
                {
                    toUseScript.stackHolder.SetActive(true);
                    int amount = hotBarInstances[i].stackedItemIds.Count;
                    if (data.stack_type == ItemData.StackType.standard)
                    {
                        amount++;
                    }
                    toUseScript.stackText.text = amount.ToString();
                }
                else
                {

                    toUseScript.stackHolder.SetActive(false);
                }
            }
        }
    }

    public ItemInstance CurrentEquipedInstance()
    {
        if(hotBarInstances == null || hotBarInstances.Count == 0)
        {
            return null;
        }

        return hotBarInstances[currentSlot - 1];
    }

    public InventorySlot GetCurrentSlot()
    {
        for (int i = 0; i < hotBarSlots.Length; i++)
        {
            if(hotBarSlots[i].isOccupied && hotBarSlots[i].isSubSlot == false)
            {
                if(hotBarSlots[i].storedItem == CurrentEquipedInstance())
                {
                    return hotBarSlots[i];
                }
            }
        }

        return hotBarSlots[currentSlot];      
    }

    public void ClearHotBar()
    {
        for (int i = 0; i < hotBarSlots.Length; i++)
        {
            if (hotBarSlots[i].isOccupied)
            {
                hotBarSlots[i].isSubSlot = false;
                hotBarSlots[i].isOccupied = false;
                hotBarSlots[i].storedItem = null;
                inventoryManager.SetSlotImage(hotBarSlots[i]);
            }
        }

        //find all instances
        if (createdHotBarSlots != null && createdHotBarSlots.Count > 0)
        {
            foreach (GameObject item in createdHotBarSlots)
            {
                Destroy(item);
            }
        }
        createdHotBarSlots = new List<GameObject>();
        hotBarInstances = new List<ItemInstance>();
    }
}
