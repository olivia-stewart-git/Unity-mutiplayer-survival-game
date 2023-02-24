using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using TMPro;
using UnityEngine.UI;

public class CraftingManager : NetworkBehaviour
{
    private Dictionary<string, int> itemQuantities;

     private ItemReference allitems;
    [SerializeField] private CraftingReference craftReference;
    [SerializeField] private InventoryManager p_Inventory;

    [Header("DisplaySettings")]
    [SerializeField] private GameObject craftingSelection;
    [SerializeField] private GameObject craftingPopupInput;
    [Space]
    [SerializeField] private Color nonIngredients;
    [SerializeField] private Color uncraftable;
    [SerializeField] private Color craftable;

    [Header("Audio")]
    private PlayerAudioManager p_Audio;

    public AudioClip[] craftingSounds;

    public AudioClip[] selectAudioSounds;

    [Header("CreationSettings")]
    public int craftingLevel = 3;

    private UiReference uiReference;

    private List<CraftingOptionScript> createdCraftInstances;

    private void Start()
    {
        p_Audio = GetComponent<PlayerAudioManager>();
    }
    public void SetItemReference(ItemReference iRef)
    {
        allitems = iRef;
    }
    public void Initialise(UiReference uiRef)
    {
        uiReference = uiRef;

        uiReference.craftingTab.onTabDeselected.AddListener(OnExitTab);
        uiReference.craftingTab.onTabSelected.AddListener(OpenedCraftingMenu);
        uiReference.closeMenuButton.onClick.AddListener(ExitCraftPopup);
        uiReference.descriptionDropdownButton.onClick.AddListener(DescriptionDropdownClicked);
        uiReference.craftingTabGroup.onTabChanged.AddListener(ExitCraftPopup);
        uiReference.craftButton.onClick.AddListener(CraftCurrentSelected);
        //create all options
        GenerateBaseCraftIcons();
    }

    public void OpenedCraftingMenu()
    {
        itemQuantities = p_Inventory.ItemDictionary();

        foreach (CraftingOptionScript item in createdCraftInstances)
        {
            bool isCraftable = CheckCraftable(item.heldRecipe);
            if (isCraftable)
            {
                item.colorBackingImage.color = craftable;
                item.transform.SetAsFirstSibling();
            }
            else
            {
                if (HasIngredients(item.heldRecipe) == false)
                {
                    item.colorBackingImage.color = uncraftable;
                }
                else
                {
                    item.colorBackingImage.color = nonIngredients;
                }
            }
        }
    }

    private void GenerateBaseCraftIcons()
    {
        createdCraftInstances = new List<CraftingOptionScript>();

        foreach (CraftingRecipe recipe in craftReference.craftingRecipes)
        {
            GameObject createdInstance = Instantiate(craftingSelection);

            CraftingOptionScript cOptionScript = createdInstance.GetComponent<CraftingOptionScript>();
            createdCraftInstances.Add(cOptionScript);

            switch (recipe.category)
            {
                case CraftingRecipe.CraftingCategory.equipment:
                    createdInstance.transform.SetParent(uiReference.equipmentHolder);
                    break;
                case CraftingRecipe.CraftingCategory.consumable:
                    createdInstance.transform.SetParent(uiReference.consumablesHolder);
                    break;
                case CraftingRecipe.CraftingCategory.tools:
                    createdInstance.transform.SetParent(uiReference.toolsHolder);
                    break;
                case CraftingRecipe.CraftingCategory.weapons:
                    createdInstance.transform.SetParent(uiReference.weaponsHolder);
                    break;
                case CraftingRecipe.CraftingCategory.construction:
                    createdInstance.transform.SetParent(uiReference.constructionHolder);
                    break;
            }

            ItemData instanceData = allitems.allItems[recipe.outPut];

            cOptionScript.colorBackingImage.color = nonIngredients;
            cOptionScript.iconImage.sprite = instanceData.iconSprite;

            cOptionScript.amountText.text = "x" + recipe.outPutQuantity.ToString();

            cOptionScript.craftManager = this;
            cOptionScript.heldRecipe = recipe;
        }
    }
    bool HasIngredients(CraftingRecipe recipe)
    {
        if(itemQuantities == null)
        {
            itemQuantities = p_Inventory.ItemDictionary();
        }

        foreach (CraftingInput input in recipe.requiredInputs)
        {
            if (itemQuantities.ContainsKey(allitems.allItems[input.id].itemName) == true)
            {
                return true;
            }
        }
        return false;
    }

    private CraftingOptionScript currentSelectedOption;
    //we handle selection of options
    public void CraftOptionClicked(CraftingOptionScript option)
    {
        if (currentSelectedOption == null)
        {
            SelectCraftOption(option);
        }
        else if (currentSelectedOption == option)
        {
            DeselectCraftOption(option);
        }
        else if (currentSelectedOption != option)
        {
            DeselectCraftOption(currentSelectedOption);
            SelectCraftOption(option);
        }
    }

    void SelectCraftOption(CraftingOptionScript option)
    {
        CloseDropdown();

        if (p_Audio != null)
        {
            p_Audio.PlayLocalAudioClip(selectAudioSounds[Random.Range(0, selectAudioSounds.Length)]);
        }

        currentSelectedOption = option;
        option.backingImage.color = option.selectedColor;

        //set menu
        uiReference.craftingPopup.SetActive(true);

        //we set position
        Vector3 posTarget = option.transform.position;
        float offsetX = uiReference.craftingPopup.GetComponent<RectTransform>().sizeDelta.x / 2;
        float offsetY = uiReference.craftingPopup.GetComponent<RectTransform>().sizeDelta.y / 2;

        if (posTarget.y > Screen.height / 2)
        {
            offsetY *= -1;
        }

        if (posTarget.x > Screen.width / 2)
        {
            offsetX *= -1;
        }
        uiReference.craftingPopup.transform.position = new Vector3(posTarget.x + offsetX, posTarget.y + offsetY);

        //set popup data
        SetCraftingPopupInfo(option.heldRecipe);
    }
    public Color32 canColor;
    private List<CraftingPopupInputScript> popupInputInstances;
    void SetCraftingPopupInfo(CraftingRecipe recipe)
    {
        ItemData outputData = allitems.allItems[recipe.outPut];

        uiReference.craftingPopupTitleText.text = outputData.itemName;
        uiReference.craftingPopupMainIcon.sprite = outputData.iconSprite;
        uiReference.outPutAmountText.text = "x" + recipe.outPutQuantity.ToString();

        uiReference.craftingPopupCraftingLevelText.faceColor = craftable;
        uiReference.craftingPopupCraftingLevelText.text = "Required crafting level " + recipe.minCraftingLevel.ToString();
        uiReference.craftingPopupDescriptionText.text = outputData.description;


        bool isCraftable = CheckCraftable(recipe);
        if (isCraftable)
        {
            uiReference.craftButton.interactable = true;
        }
        else
        {
            uiReference.craftButton.interactable = false;
        }

        if(recipe.minCraftingLevel > craftingLevel)
        {
            uiReference.craftingPopupCraftingLevelText.faceColor = uncraftable;
        }

        uiReference.craftingPopupWorkbenchText.text = "Requires " + recipe.requiredbench.ToString();

        //we create the input objects
        if (popupInputInstances == null) popupInputInstances = new List<CraftingPopupInputScript>();

        if(popupInputInstances.Count > 0)
        {
            foreach (CraftingPopupInputScript item in popupInputInstances)
            {
                Destroy(item.gameObject);
            }
            popupInputInstances.Clear();
        }; //remove already made instances

        for (int i = 0; i < recipe.requiredInputs.Length; i++)
        {
            ItemData inputData = allitems.allItems[recipe.requiredInputs[i].id];
            GameObject createdInstance = Instantiate(craftingPopupInput, uiReference.craftingPopupInputField);

            CraftingPopupInputScript pScript = createdInstance.GetComponent<CraftingPopupInputScript>();

            popupInputInstances.Add(pScript);

            //set values
            pScript.iconImage.sprite = inputData.iconSprite;
            pScript.nameText.text = inputData.itemName;
            pScript.backingColorImage.color = craftable;

            if (itemQuantities != null)
            {
                int displayamount = 0;
                if (itemQuantities.ContainsKey(inputData.itemName) == true)
                {
                    displayamount = itemQuantities[inputData.itemName];
                }

                pScript.amountText.text = displayamount.ToString() + " / " + recipe.requiredInputs[i].quantity;
                if(displayamount < recipe.requiredInputs[i].quantity)
                {
                    pScript.amountText.faceColor = uncraftable;
                    pScript.backingColorImage.color = uncraftable;
                }
                else
                {
                    pScript.amountText.faceColor = canColor;              
                }
            }

        }
    }

    public void ExitCraftPopup()
    {
        if(currentSelectedOption != null)
        {
            DeselectCraftOption(currentSelectedOption);
        }
    }

    void DeselectCraftOption(CraftingOptionScript option)
    {
        currentSelectedOption = null;
        option.backingImage.color = option.deSelectedColor;

        if (p_Audio != null)
        {
            p_Audio.PlayLocalAudioClip(selectAudioSounds[Random.Range(0, selectAudioSounds.Length)]);
        }

        uiReference.craftingPopup.SetActive(false);
    }

    public void OnExitTab()
    {
        if(currentSelectedOption != null)
        {
            DeselectCraftOption(currentSelectedOption);
        }
    }

    bool CheckCraftable(CraftingRecipe recipe)
    {
        if(recipe.minCraftingLevel > craftingLevel)
        {
            return false;
        }

        //make sure we get all items
        if(itemQuantities == null)
        {
            itemQuantities = p_Inventory.ItemDictionary();
        }

        foreach (CraftingInput input in recipe.requiredInputs)
        {
            ItemData inputdata = allitems.allItems[input.id];

            if(itemQuantities.ContainsKey(inputdata.itemName) == false)
            {
                return false;
            }
            int comparison = itemQuantities[inputdata.itemName];

            if(comparison < input.quantity)
            {
                return false;
            }

        }

        return true;
    }
    #region description panel
    private bool dropdownOpen = false;
    public void DescriptionDropdownClicked()
    {
        if(dropdownOpen == false)
        {
            OpenDropdown();
        }
        else
        {
            CloseDropdown();
        }
    }

    public void OpenDropdown()
    {
        dropdownOpen = true;
        uiReference.popupDescriptionObject.SetActive(true);
    }

    public void CloseDropdown()
    {
        dropdownOpen = false;
        uiReference.popupDescriptionObject.SetActive(false);
    }
    #endregion

    public void CraftCurrentSelected()
    {
        if (currentSelectedOption == null) return;

        ItemInstance pickupInstance = new ItemInstance();
        ItemData refData = allitems.allItems[currentSelectedOption.heldRecipe.outPut];

        pickupInstance.id = refData.itemId;
        pickupInstance.stackedItemIds = new List<int>();

        if (refData.useDurability)
        {
            pickupInstance.currentDurability = 1000;
        }

        if(currentSelectedOption.heldRecipe.outPutQuantity > 1 && refData.stack_type == ItemData.StackType.standard)
        {
            for (int i = 0; i < currentSelectedOption.heldRecipe.outPutQuantity; i++)
            {
                pickupInstance.stackedItemIds.Add(refData.itemId);
            }
        }

        switch (refData.itemType)
        {
            case ItemData.Item_Type.material:
                break;
            case ItemData.Item_Type.tool:
                break;
            case ItemData.Item_Type.gun:
                GunObject g_Object = refData.equipObject.GetComponent<GunObject>();
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

        //remove components
        foreach (CraftingInput input in currentSelectedOption.heldRecipe.requiredInputs)
        {
            for (int i = 0; i < input.quantity; i++)
            {
                p_Inventory.RemoveById(input.id, false);
            }   
        }

        //actually add to inventory
        p_Inventory.AddToInventory(pickupInstance, null);

        if (p_Audio != null)
        {
            p_Audio.PlayLocalAudioClip(craftingSounds[Random.Range(0, craftingSounds.Length)]);
        }

        //update data
        OpenedCraftingMenu();//resets data
        if(currentSelectedOption != null)
        {
            SetCraftingPopupInfo(currentSelectedOption.heldRecipe);
        }
    }
}