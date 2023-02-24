using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.InputSystem;

public class ClothingInventoryManager : NetworkBehaviour
{
    private PlayerDamageManager p_damage;
    private PlayerScript p_Script;
    private ClothingSlotScript[] clothingSlots;

    private GameObject[] createdRepresentors = new GameObject[7];
    private GameObject[] createdRepresentorsUi = new GameObject[7];

    [SyncObject]
    private readonly SyncList<int> storedids = new SyncList<int>();

    private ClothingSlotScript currentHover = null;

    private Color baseSlotColor;

    [SerializeField] private Color highlightSlotColor;
    [SerializeField] private InventoryManager inventoryManager;
    private ItemReference allitems;
    [SerializeField] private PlayerRepresentorManager p_representor;


    public override void OnStartClient()
    {
        if (!base.IsOwner) return;
        CmdInitialiseClothValues();
    }

    private void Awake()
    {
        storedids.OnChange += UpdateClothLayers;
    }

    [SerializeField] private void CmdInitialiseClothValues()
    {
        for (int i = 0; i < 7; i++)
        {
            storedids.Add(0);
        }
    }

    public void InitializeClothingSlots(UiReference uiref)
    {
        p_Script = GetComponent<PlayerScript>();

        allitems = p_Script.GetItemReference();

        if (!base.IsOwner) return;

        clothingSlots = uiref.clothingSlots;

        foreach (ClothingSlotScript slot in clothingSlots)
        {
            slot.clothingManager = this;
            slot.durabiltyObject.SetActive(false);
            baseSlotColor = slot.slotBacking.color;
        }

        p_damage = GetComponent<PlayerDamageManager>();
        p_damage.UpdateArmoring(curArmor);
    }

    public void MouseEnterClothingSlot(ClothingSlotScript slot)
    {
        currentHover = slot;
        if (slot.slotBacking.color != highlightSlotColor && slot.isOccupied == false)
        {
            slot.slotBacking.color = highlightSlotColor;
        }
    }

    public void MouseExitClothingSlot(ClothingSlotScript slot)
    {
        currentHover = null;
        if (slot.isOccupied == false)
        {
            slot.slotBacking.color = baseSlotColor;
        }
    }

    public void ClothingSlotRightClick(InputAction.CallbackContext context)
    {
        if (!base.IsOwner) return;

        if (context.performed && currentHover != null)
        {
            if (inventoryManager.RetrieveCursorState() == true)
            {
                //place in slot
                if (currentHover.isOccupied == true)
                {

                }
                else
                {
                    bool success = inventoryManager.PlaceInClothingSlot(currentHover);
                    if (success == true)
                    {
                        AddedToSlot(currentHover);
                        currentHover.slotBacking.color = baseSlotColor;
                    }
                }
            }
            else
            {
                if (currentHover.isOccupied)
                {
                    //we pick out
                    PickedFromClothSlot(currentHover);
                    inventoryManager.PickFromClothingSlot(currentHover);
                }
            }
        }
    }

    public void InventoryClosed()
    {
        currentHover = null;
    }

    public void AddedToSlot(ClothingSlotScript slot)
    {
        if (!base.IsOwner) return;

        ItemData iData = allitems.allItems[slot.storedClothing.id];
        //    slot.isOccupied = true;

        //handle visual aspects
        switch (iData.clothingData.clothingType)
        {
            case Clothing_Data.Clothing_Type.hat:
                CmdChangeStoredId(0, slot.storedClothing.id);
                break;
            case Clothing_Data.Clothing_Type.face:
                CmdChangeStoredId(1, slot.storedClothing.id);
                break;
            case Clothing_Data.Clothing_Type.shirt:
                CmdChangeStoredId(2, slot.storedClothing.id);
                break;
            case Clothing_Data.Clothing_Type.vest:
                CmdChangeStoredId(3, slot.storedClothing.id);
                break;
            case Clothing_Data.Clothing_Type.pants:
                CmdChangeStoredId(4, slot.storedClothing.id);
                break;
            case Clothing_Data.Clothing_Type.feet:
                CmdChangeStoredId(5, slot.storedClothing.id);
                break;
            case Clothing_Data.Clothing_Type.back:
                CmdChangeStoredId(6, slot.storedClothing.id);
                break;
        }


        //we create additional inventory space
        if (iData.clothingData.addToInventorySpace == true)
        {
            int xVal = iData.clothingData.addSlotx;
            int yVal = iData.clothingData.addSloty;
            string nameToUse = iData.itemName;

            InventoryHolder holderInstance = inventoryManager.GenerateInventorySlots(yVal, xVal, nameToUse);
            slot.storedHolder = holderInstance;
        }

        RetrieveStatsFromClothing();
        p_damage.UpdateArmoring(curArmor);
    }

    [ServerRpc] public void CmdChangeStoredId(int at, int to)
    {
        storedids[at] = to;
    }
    public void PickedFromClothSlot(ClothingSlotScript slot)
    {
        if (slot.storedClothing == null) return;
        slot.isOccupied = false;
        Debug.Log("remove excess inventory");
        ItemData iData = allitems.allItems[slot.storedClothing.id];

        //handle visual aspects
        switch (iData.clothingData.clothingType)
        {
            case Clothing_Data.Clothing_Type.hat:
                CmdChangeStoredId(0, 0);
                break;
            case Clothing_Data.Clothing_Type.face:
                CmdChangeStoredId(1, 0);
                break;
            case Clothing_Data.Clothing_Type.shirt:
                CmdChangeStoredId(2, 0);
                break;
            case Clothing_Data.Clothing_Type.vest:
                CmdChangeStoredId(3, 0);
                break;
            case Clothing_Data.Clothing_Type.pants:
                CmdChangeStoredId(4, 0);
                break;
            case Clothing_Data.Clothing_Type.feet:
                CmdChangeStoredId(5, 0);
                break;
            case Clothing_Data.Clothing_Type.back:
                CmdChangeStoredId(6, 0);
                break;
        }

        if (slot.storedHolder != null)
        {
            inventoryManager.RemoveInventorySection(slot.storedHolder);
        }

        RetrieveStatsFromClothing();
        p_damage.UpdateArmoring(curArmor);
    }

    private int curArmor;
    void RetrieveStatsFromClothing()
    {
        int toArmor = 0;
        foreach (ClothingSlotScript sScript in clothingSlots)
        {
            if (sScript.isOccupied == true && sScript.storedClothing != null)
            {
                ItemData iData = allitems.allItems[sScript.storedClothing.id];
                int armorAmount = iData.clothingData.armoring;
                toArmor += armorAmount;
            }
        }
        curArmor = toArmor;
    }

    public void ClearClothing()
    {
        foreach (ClothingSlotScript sScript in clothingSlots)
        {
            if (sScript.isOccupied == true && sScript.storedClothing != null)
            {
                PickedFromClothSlot(sScript);
                inventoryManager.PickFromClothingSlot(sScript);
            }
        }

        curArmor = 0;
        p_damage.UpdateArmoring(curArmor);
        inventoryManager.ClearCursor();
    }
    public void SetClothCorpseLayer()
    {
        Debug.Log("Set cloth layers");

        foreach (GameObject g in createdRepresentors)
        {
            if (g != null)
            { 
                g.layer = 15;
                GameObject toChange = g.GetComponent<ClotheObjectScript>().clotheMesh;
                toChange.layer = 15;
                Debug.Log(toChange + "toChange");
            }
        }
    }
    
    //updated on stored value changes
    //for creating the visual player representations
    private void UpdateClothLayers(SyncListOperation op, int index, int oldItem, int newItem, bool asServer)
    {
        Debug.Log("updated cloth layers");

        if (createdRepresentors[index] != null)
        {
            Destroy(createdRepresentors[index]); //destroys the representor
        }

        if (base.IsOwner && createdRepresentorsUi[index] != null)
        {
            Destroy(createdRepresentorsUi[index]); //destroys the representor
        }

        if (newItem == 0)
        {
            //sets the torso and bottom mesh active
            if (index == 2)
            {
                p_representor.SetTorso(true);
            }
            else
            {
                if (index == 4)
                {
                    p_representor.SetBottom(true);
                }
            }
        }
        else
        {
            ItemData bindTo = allitems.allItems[newItem];
            GameObject createdInstance = p_representor.BindObjectToMesh(bindTo.clothingData);
            createdRepresentors[index] = createdInstance;

            if (index == 2)
            {
                p_representor.SetTorso(false);
            }
            else
            {
                if (index == 4)
                {
                    p_representor.SetBottom(false);
                }
            }

            if (base.IsOwner)
            {
                GameObject uiInst = p_representor.BindMeshToUi(bindTo.clothingData);
                createdRepresentorsUi[index] = createdInstance;
            }
        }       
    }
}
