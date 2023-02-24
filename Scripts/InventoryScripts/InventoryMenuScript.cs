using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using TMPro;
using UnityEngine.UI;

public class InventoryMenuScript : NetworkBehaviour
{
    //changing values
    private ItemInstance menuItem;

    //settings
    private ItemReference allitems;
    [SerializeField] InventoryManager inventoryManager;

    //refrenced componenets
    private GameObject panelObject;
    private GameObject stackPanelObject;

    //ui elements
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI stackText;
    private GameObject stackTextObject;
    private TextMeshProUGUI durabilityText;
    private GameObject durabilityTextObject;
    private TextMeshProUGUI weightText;

    private Image iconImage;
  
    //buttons
    private Button exitButton;
    private Button exitStackButton;
    private Button equipButton;
    private Button dropButton;
    private Button showstackButton;
    private Button salvageButton;
    private Button showStatsButton;
    private Button repairButton;

    private Button dropSelectedButton;
    private Button removeFromStackButton;


    //stack panel settings
    private bool stackMenuOpen = false;
    [SerializeField] private GameObject emptySlot;
    [SerializeField] private List<GameObject> stackSlotObjects = new List<GameObject>();
    private List<GameObject> selectedSlots = new List<GameObject>();
    private Transform stackSlotsHolder;

    [SerializeField] private Color durabiltyOutColor;
    [SerializeField] private Color durabiltyFullColor;
    public void SetItemReference(ItemReference iRef)
    {
        allitems = iRef;
    }

    public void InitialiseInventoryMenu(UiReference uiRef)
    {
        nameText = uiRef.nameText;
        descriptionText = uiRef.descriptionText;
        stackText = uiRef.stackText;
        durabilityText = uiRef.durabilityText;
        weightText = uiRef.weightText;

        exitButton = uiRef.exitButton;
        exitStackButton = uiRef.exitStackButton;
        equipButton = uiRef.equipButton;
        dropButton = uiRef.dropButton;
        showstackButton = uiRef.showstackButton;
        salvageButton = uiRef.salvageButton;
        repairButton = uiRef.repairButton;
        showStatsButton = uiRef.showStatsButton;

        iconImage = uiRef.iconImage;

        dropSelectedButton = uiRef.dropSelectedButton;
        removeFromStackButton = uiRef.fillSelectionButton;

        stackTextObject = uiRef.stackTextObject;
        durabilityTextObject = uiRef.durabilityTextObject;

        panelObject = uiRef.panelObject;
        panelObject.SetActive(false);
        stackPanelObject = uiRef.stackPanelObject;
        stackPanelObject.SetActive(false);

        stackSlotsHolder = uiRef.stackSlotsHolder;

        //align functions to buttons

        exitButton.onClick.AddListener(ExitPressed);
        exitStackButton.onClick.AddListener(ExitStackPressed);
        equipButton.onClick.AddListener(EquipPressed);

        //     dropButton.gameObject.GetComponent<ButtonFixer>().inventoryMenu = this;
        dropButton.onClick.AddListener(DropPressed);
        showstackButton.onClick.AddListener(ShowStackPressed);
        salvageButton.onClick.AddListener(SalvagePressed);
        repairButton.onClick.AddListener(RepairPressed);
        showStatsButton.onClick.AddListener(ShowStatsPressed);
        dropSelectedButton.onClick.AddListener(DropSelectedPressed);

        removeFromStackButton.onClick.AddListener(RemoveSelected); //this button changed hence the name being wrong
    }

    public void OpenContextMenu(ItemInstance itemInstance, InventorySlot slot)
    {
        stackPanelObject.SetActive(false);
        panelObject.SetActive(true);

        //we set position
        Vector3 posTarget = slot.transform.position;
        float offsetX = panelObject.GetComponent<RectTransform>().sizeDelta.x / 2;
        float offsetY = panelObject.GetComponent<RectTransform>().sizeDelta.y / 2;

        if(posTarget.y > Screen.height / 2)
        {
            offsetY *= -1;
        }

        if (posTarget.x > Screen.width / 2)
        {
            offsetX *= -1;
        }
        panelObject.transform.position = new Vector3(posTarget.x + offsetX, posTarget.y + offsetY);

        menuItem = itemInstance;
        //we apply values and such
        ItemData data = allitems.allItems[itemInstance.id];
        nameText.text = data.itemName;
        descriptionText.text = data.description;
        iconImage.sprite = data.iconSprite;

        ShowDynamicvalues(itemInstance);
        //enable disable corresponding buttons
        if (data.repairable)
        {
            repairButton.interactable = true;
        }
        else
        {
            repairButton.interactable = false;
        }

        if (data.salvagable)
        {
            salvageButton.interactable = true;
        }
        else
        {
            salvageButton.interactable = false;
        }

        if (data.equipable)
        {
            equipButton.interactable = true;
        }
        else
        {
            equipButton.interactable = false;
        }

        if(data.stack_type != ItemData.StackType.none)
        {
            showstackButton.interactable = true;
        }
        else
        {
            showstackButton.interactable = false;
        }

        if (data.hasStats)
        {
            showStatsButton.interactable = true;
        }
        else
        {
            showStatsButton.interactable = false;
        }
    }

    public void ShowDynamicvalues(ItemInstance instance)
    {
        ItemData basedata = allitems.allItems[instance.id];
        //calculate weight

        //durability
        if(basedata.useDurability == true)
        {
            durabilityTextObject.SetActive(true);
            durabilityText.text = instance.currentDurability.ToString();
            durabilityText.color = Color.Lerp(durabiltyOutColor, durabiltyFullColor, (instance.currentDurability - 1) / 1000f);
        }
        else
        {
            durabilityTextObject.SetActive(false);
        }

        //stack values
        if(instance.stackedItemIds != null && instance.stackedItemIds.Count > 0 && basedata.stack_type != ItemData.StackType.none)
        {
            stackTextObject.SetActive(true);
            int amount = instance.stackedItemIds.Count;
            if(basedata.stack_type == ItemData.StackType.standard)
            {
                amount++;
            }
            stackText.text = amount.ToString();
        }
        else
        {
            stackTextObject.SetActive(false);
        }
    }

    public void CloseContextMenu()
    {
        stackPanelObject.SetActive(false);
        panelObject.SetActive(false);

        if (stackMenuOpen == true)
        {
            CloseStackMenu();
        }
    }

    public void OnInventoryClose()
    {
        stackPanelObject.SetActive(false);
        panelObject.SetActive(false);

        //wipe created stack options
        if (stackMenuOpen == true)
        {
            CloseStackMenu();
        }

    }

    void ShowStackPressed()
    {
        if(stackMenuOpen == true)
        {
            CloseStackMenu();
        }
        else
        {
            OpenStackMenu();
        }
    }

    void EquipPressed()
    {

    }

    public void DropPressed()
    {
        Debug.Log("Drop pressed");
        inventoryManager.DropSelected();
    }

    void SalvagePressed()
    {

    }

    void RepairPressed()
    {

    }

    void ExitPressed()
    {
        inventoryManager.DeselectLastSelected();
        CloseContextMenu();
    }

    void RemoveSelected()
    {
        if (selectedSlots == null || selectedSlots.Count == 0 || inventoryManager.RetrieveCursorState() == true) return;

        if (selectedSlots.Count == stackSlotObjects.Count && allitems.allItems[menuItem.id].stack_type == ItemData.StackType.standard)
        {
            inventoryManager.PickFromSlot(inventoryManager.GetCurSelected());
            return;
        }

        ItemInstance toDrop = RetreiveSelectionInstance();

        //activate cursor
        inventoryManager.ActivateCursorByInstance(toDrop);

        if (menuItem.stackedItemIds.Count == 0)
        {
            CloseStackMenu();
            showstackButton.interactable = false;
        }
    }

    void DropSelectedPressed()
    {
        if (selectedSlots == null || selectedSlots.Count == 0) return;

        if (selectedSlots.Count == stackSlotObjects.Count && allitems.allItems[menuItem.id].stack_type == ItemData.StackType.standard)
        {
            inventoryManager.DropSelected();
            return;
        }

        ItemInstance toDrop = RetreiveSelectionInstance();

        inventoryManager.CmdDropItem(toDrop);

        if(menuItem.stackedItemIds.Count == 0)
        {
            CloseStackMenu();
            showstackButton.interactable = false;
        }
    }

    ItemInstance RetreiveSelectionInstance()
    {
        int startId = selectedSlots[0].GetComponent<SlotStackScript>().id;
        ItemInstance toDrop = new ItemInstance();
        toDrop.id = startId;

        List<GameObject> toRemoveSlots = new List<GameObject>();
        toRemoveSlots.Add(selectedSlots[0]);

        ItemData refData = allitems.allItems[toDrop.id];
        toDrop.stackedItemIds = new List<int>();

        menuItem.stackedItemIds.Remove(startId);

        for (int i = 1; i < selectedSlots.Count; i++)
        {
            SlotStackScript i_Script = selectedSlots[i].GetComponent<SlotStackScript>();
            if (i_Script.id == startId)
            {
                if (toDrop.stackedItemIds.Count < refData.stackCapacity)
                {
                    toRemoveSlots.Add(selectedSlots[i]);

                    menuItem.stackedItemIds.Remove(i_Script.id);

                    toDrop.stackedItemIds.Add(i_Script.id);
                }
                else
                {
                    break;
                }
            }
        }

        foreach (GameObject item in toRemoveSlots)
        {
            if (selectedSlots.Contains(item))
            {
                selectedSlots.Remove(item);
            }
            Destroy(item);
        }


        inventoryManager.UpdateSelected();

        return toDrop;
    }

    void ExitStackPressed()
    {
        CloseStackMenu();
    }

    void ShowStatsPressed()
    {

    }

    void OpenStackMenu()
    {
        selectedSlots = new List<GameObject>();

        if(menuItem.stackedItemIds.Count == 0)
        {
            return;
        }
        ItemData data = allitems.allItems[menuItem.id];

        stackSlotObjects = new List<GameObject>();

        stackMenuOpen = true;
        stackPanelObject.SetActive(true);

        if(data.stack_type == ItemData.StackType.standard) //for if we are stacking on top of other thing
        {
            GameObject createdInstance = Instantiate(emptySlot, stackSlotsHolder);
            stackSlotObjects.Add(createdInstance);

            ItemData useData = data;

            SlotStackScript s_script = createdInstance.GetComponent<SlotStackScript>();
            s_script.i_Menu = this;
            s_script.nameText.text = useData.itemName.ToString();
            s_script.id = data.itemId;
        }

        if(menuItem.stackedItemIds.Count > 0)
        {
            foreach (int id in menuItem.stackedItemIds)
            {
                GameObject createdInstance = Instantiate(emptySlot, stackSlotsHolder);
                stackSlotObjects.Add(createdInstance);

                ItemData useData = allitems.allItems[id];

                SlotStackScript s_script = createdInstance.GetComponent<SlotStackScript>();
                s_script.i_Menu = this;
                s_script.nameText.text = useData.itemName.ToString();
                s_script.id = id;
            }
        }

        dropSelectedButton.interactable = false;
        removeFromStackButton.interactable = false;
    }

    void CloseStackMenu()
    {
        stackMenuOpen = false;
        stackPanelObject.SetActive(false);

        if(stackSlotObjects.Count > 0)
        {
            foreach (GameObject g in stackSlotObjects)
            {
                Destroy(g);
            }
        }
    }

    public void StackSlotClicked(GameObject slot)
    {
        //we select the slot
        if (selectedSlots.Contains(slot))
        {
            selectedSlots.Remove(slot);
            slot.GetComponent<SlotStackScript>().indicatorObject.SetActive(false);
        }
        else
        {
            selectedSlots.Add(slot);
            slot.GetComponent<SlotStackScript>().indicatorObject.SetActive(true);
        } 

        //we enable/disable button based on selection
        if(selectedSlots.Count == 0)
        {
            dropSelectedButton.interactable = false;
            removeFromStackButton.interactable = false;
        }
        else
        {
            dropSelectedButton.interactable = true;
            removeFromStackButton.interactable = true;
        }
    }
}
