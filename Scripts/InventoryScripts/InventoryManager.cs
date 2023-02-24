using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InventoryManager : NetworkBehaviour
{
    private List<InventoryHolder> inventoryHolders;

    private GameObject[] pickupNotifications;
    private Queue<GameObject> pickupNotifQueue;

    private PlayerAudioManager p_audio;
    private PlayerMenuManager p_menu;
    private PlayerScript p_Script;

    [SerializeField] private ClothingInventoryManager clothingManager;
    [SerializeField] private InventoryMenuScript i_ContextMenu;
    [SerializeField] private HotBarManager h_BarManager;
    private PlayerBuffManager p_BuffManager;
    
    [Header("Player base inventory data")]
    [SerializeField] private int playerInventoryX;
    [SerializeField] private int playerInventoryY;

    [Header("Audio")]
    public AudioClip slotEnterSound;
    public AudioClip closeInventorySound;
    public AudioClip closeContextSound;
    public AudioClip openInventorySound;
    public AudioClip openContextMenuSound;
    public AudioClip pickItemSound;
    public AudioClip placeItemSound;
    public AudioClip removeClothingSound;
    public AudioClip putOnClotheSound;
    public AudioClip rotateSound;

    [Header("Values")]
    [SerializeField] private float slotSpacing;
     private ItemReference allItems;
    private bool initialised = false;

    [Header("For instantiation")]
    [SerializeField] private float slotHeight; //for setting correct heigh of object
    [SerializeField] private float fromTopOffset;
    [SerializeField] private float fromXOffset;
    [SerializeField] private GameObject emptyHolder;
    [SerializeField] private GameObject emptySlot;
    private Transform holderParent;

    [Header("Colors")]
    [SerializeField] private Color backingBaseColor;
    [SerializeField] private Color backingHoverColor;
    [SerializeField] private Color backingSelectedColor;
    [SerializeField] private Color canPlaceColor;
    [SerializeField] private Color cannotPlaceColor;
    [Space]
    [SerializeField] private Color durabiltyOutColor;
    [SerializeField] private Color durabiltyFullColor;

    //cursor information
    private GameObject cursorObject;
    private Image cursorRepresentorImage;
    private Image cursorBackingImage;

    private GameObject cursorDurabilityHolder;
    private GameObject cursorStackHolder;
    private GameObject lastPickup;
    private TextMeshProUGUI cursorDurabilityText;
    private TextMeshProUGUI cursorStackText;

    private Vector2 mousePos;

    private ItemInstance currentInstance;
    private InventorySlot currentHover;
    private InventorySlot currentSelected;
    private InventorySlot currentHeld;
    private bool cursorHoldingItem;
    private int currentOrientation;

    //orientation settings
    private int useX;
    private int useY; //values for checking scale of objects
    private float rotvalue;
    private int offsetMultiplier;

    private Transform secondInventoryContent;

    //get the quantity of items
    private Transform pickupsHolder;

    public Dictionary<string, int> ItemDictionary()
    {
        Dictionary<string, int> dictionary = new Dictionary<string, int>();

        foreach (InventoryHolder holder in inventoryHolders)
        {
            foreach (InventorySlot slot in holder.createdSlots)
            {
                if(slot.isSubSlot == false && slot.isOccupied)
                {
                    string namekey = allItems.allItems[slot.storedItem.id].itemName;

                    int amount = 1;
                    if(slot.storedItem.stackedItemIds != null && allItems.allItems[slot.storedItem.id].stack_type == ItemData.StackType.standard)
                    {
                        amount += slot.storedItem.stackedItemIds.Count;
                    }

                    //check if we have an entry
                    if(dictionary.ContainsKey(namekey) != false)
                    {
                        int baseAmount = dictionary[namekey];
                        dictionary[namekey] = baseAmount + amount;
                    }
                    else
                    {
                        dictionary.Add(namekey, amount);
                    }
                }
            }
        }
        return dictionary;
    }

    private InventorySlot[] currentOutline;

    //selection settings
    private InventorySlot selectedSlot;

    public void Start()
    {
        pickupsHolder = GameObject.FindGameObjectWithTag("PickupHolder").transform;
    }

    public override void OnStartClient()
    {
        p_BuffManager = GetComponent<PlayerBuffManager>();
        p_menu = GetComponent<PlayerMenuManager>();
        p_Script = GetComponent<PlayerScript>();
        h_BarManager = GetComponent<HotBarManager>();
    }
    public void SetItemReference(ItemReference iRef)
    {
        allItems = iRef;
    }
    public void InitialiseInventoryManager(UiReference uiReference)
    {

        holderParent = uiReference.holderParent;
       
        cursorObject = uiReference.cursorObject;
        cursorBackingImage = uiReference.cursorBackingImage;
        cursorRepresentorImage = uiReference.cursorRepresentorImage;

        cursorDurabilityHolder = uiReference.cursorDurabilityHolder;
        cursorDurabilityText = uiReference.cursorDurabilityText;

        cursorStackHolder = uiReference.cursorStackHolder;
        cursorStackText = uiReference.cursorStackText;

        p_audio = GetComponent<PlayerAudioManager>();

        pickupNotifications = uiReference.pickupNotifications;
        pickupNotifQueue = new Queue<GameObject>();
        foreach (GameObject item in pickupNotifications)
        {
            pickupNotifQueue.Enqueue(item);
        }

        secondInventoryContent = uiReference.secondInventoryContent;
        //initialise slots
        GenerateInventorySlots(playerInventoryY, playerInventoryX, "Inventory");

        uiReference.inventoryTab.onTabDeselected.AddListener(OnTabDeselected);

        initialised = true;
    }

    private int lastNotifCount;
    private int lastNotifid;
    private GameObject lastNotifObj;

    public void PlayPickupNotification(int a_id, int _count)
    {
        if (!base.IsOwner) return;
        if(pickupNotifCoroutine != null)
        {
            StopCoroutine(pickupNotifCoroutine);
        }
        int count = 0;
        pickupNotifCoroutine = StartCoroutine(PlayingPickupNotificaiton());
        GameObject obj = null;
        if (lastNotifid == a_id && playingNotif)
        {
            count += lastNotifCount;
            obj = lastNotifObj;
        }
        else
        {
            obj = pickupNotifQueue.Dequeue();
            pickupNotifQueue.Enqueue(obj);
        }
        
        PickupNotificationScript pScr = obj.GetComponent<PickupNotificationScript>();
        ItemData useData = allItems.allItems[a_id];
        
        count += _count;

        lastNotifObj = obj;
        lastNotifCount = count;
        lastNotifid = a_id;

        string txt = useData.itemName + " x" + (count).ToString();

        pScr.nameText.text = txt;
        pScr.iconImage.sprite = useData.iconSprite;

        obj.transform.SetAsFirstSibling();
        obj.SetActive(true);
        pScr.Spawned();
    }
    private bool playingNotif = false;
    private Coroutine pickupNotifCoroutine;
    private IEnumerator PlayingPickupNotificaiton()
    {
        playingNotif = true;
        yield return new WaitForSeconds(1f);
        playingNotif = false;
        lastNotifCount = 0;

    }

    public void OpenedInventory()
    {
        p_audio.PlayLocalAudioClip(openInventorySound);
    }
    
    // Update is called once per frame
    void Update()
    {
        if (!base.IsOwner || !initialised) return;

        if (cursorHoldingItem == true)
        {
            cursorObject.transform.position = mousePos;
        }
    }

    public void RetrieveMousePos(InputAction.CallbackContext context)
    {
        if (!base.IsOwner) return;
        mousePos = context.ReadValue<Vector2>();
    }

    private bool shiftDown = false;
    public void ShiftInput(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || !p_menu.inventoryOpen) return;
        if (context.performed)
        {
            shiftDown = true;
        }

        if (context.canceled)
        {
            shiftDown = false;
        }
    }

    public void LeftClick(InputAction.CallbackContext context)
    {
        if (!base.IsOwner) return;
        if (context.performed && currentHover != null)
        {
            if (currentHover.isSelected == true)
            {
                PerformSlotDeselected();
            }
            else
            {
                PerformSlotSlected();
            }
        }
    }

    public void DropSelected()
    {
        if(selectedSlot != null && selectedSlot.isOccupied && selectedSlot.isSubSlot == false)
        {
            CmdDropItem(selectedSlot.storedItem);
            RemoveFromSlot(selectedSlot);
            selectedSlot = null;
            i_ContextMenu.CloseContextMenu();
        }
    }

   public void UpdateSelected()
    {
        if (selectedSlot == null) return;
        SetSlotData(selectedSlot);
    }

    public void PerformSlotSlected()
    {
        if (currentHover == null) return;

        if (currentHover.isOccupied == true && currentHover.isSubSlot == false)
        {
            if (selectedSlot != null)
            {
                selectedSlot.isSelected = false;
                selectedSlot.backingImage.color = backingBaseColor;
            }

            currentHover.isSelected = true;
            selectedSlot = currentHover;
            selectedSlot.backingImage.color = backingSelectedColor;

            //for quick item dropping
            if (shiftDown && !h_BarManager.ContainsSlot(currentSelected))
            {

                DropSelected();
                p_audio.PlayLocalAudioClip(openContextMenuSound);
                return;
            
            }

            if (selectedSlot.isOccupied && selectedSlot.isSubSlot == false)
            {
                i_ContextMenu.OpenContextMenu(selectedSlot.storedItem, selectedSlot);
                p_audio.PlayLocalAudioClip(openContextMenuSound);
            }
        }
    }

    public void PerformSlotDeselected()
    {
        if (currentHover == null || currentHover.isSelected == false) return;

        currentHover.isSelected = false;
        currentHover.backingImage.color = backingBaseColor;
        selectedSlot = null;
        i_ContextMenu.CloseContextMenu();
        p_audio.PlayLocalAudioClip(closeContextSound);
    }

    public void DeselectLastSelected()
    {
        if (selectedSlot == null) return;

        selectedSlot.isSelected = false;
        selectedSlot.backingImage.color = backingBaseColor;
        selectedSlot = null;
    }

    public void ActivateCursorByInstance(ItemInstance instance)
    {
        currentInstance = instance;
        currentOrientation = 0;
        currentHeld = selectedSlot;

        DeselectLastSelected();

        cursorHoldingItem = true;
        SetOrientation(0, currentInstance.id);
        EnableCursor();
        SetCursorImage();

        i_ContextMenu.CloseContextMenu();
    }

    public InventorySlot GetCurSelected()
    {
        return selectedSlot;
    }

    //right click pickups up to use
    public void RightClick(InputAction.CallbackContext context)
    {
        if (!base.IsOwner) return;
        if (context.performed && currentHover != null)
        {
            if (cursorHoldingItem)
            {
                //we replace item 
                if (currentHover.isOccupied == false)
                {
                    PlaceInSlot(currentHover);
                }
                else
                {
                    InventorySlot target = currentHover;
                    if (currentHover.isSubSlot == true)
                    {
                        target = currentHover.heldIn.slots2d[(int)currentHover.parentSlot.x, (int)currentHover.parentSlot.y];
                    }
                    if(TestToStack(currentHover, currentInstance) == true)
                    {
                        AddToStack(currentHover, currentInstance);
                        Debug.Log("Add");
                    }
                }
            }
            else
            {
                //we pick out item
                if (currentHover.isOccupied == true)
                {
                    InventorySlot target = currentHover;
                    if (currentHover.isSubSlot == true)
                    {
                        target = currentHover.heldIn.slots2d[(int)currentHover.parentSlot.x, (int)currentHover.parentSlot.y];
                    }
                    PickFromSlot(target);
                }
            }
        }
    }

    public void PickFromSlot(InventorySlot slot)
    {
        PerformSlotDeselected();

        if (currentHover != null)
        {
            p_audio.PlayLocalAudioClip(pickItemSound);
        }

        currentOrientation = slot.currentOrientation;
        currentInstance = slot.storedItem;
        currentHeld = slot;

        RemoveFromSlot(slot);
        if (slot.usePlacedCallBacks)
        {
            slot.PickedCallBack();
        }

        cursorHoldingItem = true;
        SetOrientation(currentOrientation, currentInstance.id);
        EnableCursor();
        SetCursorImage();
    }

    public void PlaceInSlot(InventorySlot slot)
    {
        ItemData data = allItems.allItems[currentInstance.id];

        if (slot.forceEquipable == true && data.equipable == false) return;

        if (currentHover != null)
        {
            p_audio.PlayLocalAudioClip(placeItemSound);
        }

        InventorySlot[] overslots = RetrieveLastObserved(slot, currentOrientation, currentInstance);
        bool canPlace = PlacingBlocked(overslots, data.slotSpaceX * data.slotSpaceY);
        if (!canPlace) return;

        AddToSlot(slot, currentInstance, currentOrientation, overslots);

        if (slot.usePlacedCallBacks)
        {
            slot.PlacedCallback();
        }

        cursorHoldingItem = false;
        currentInstance = null;
        currentHeld = null;
        DisableCursor();
    }

    public void PickFromClothingSlot(ClothingSlotScript slot)
    {
        currentHeld = null;
        currentInstance = slot.storedClothing;

        p_audio.PlayLocalAudioClip(removeClothingSound);

        slot.storedClothing = null;
        slot.isOccupied = false;
        slot.slotImage.sprite = slot.slotBaseSprite;
        slot.durabiltyObject.SetActive(false);

        cursorHoldingItem = true;
        SetOrientation(0, currentInstance.id);
        currentOrientation = 0;

        EnableCursor();
        SetCursorImage();
    }

    public bool PlaceInClothingSlot(ClothingSlotScript slot)
    {
        ItemData data = allItems.allItems[currentInstance.id];

        if (data.itemType != ItemData.Item_Type.clothing) return false;
        if (data.clothingData.clothingType != slot.slotType) return false;

        p_audio.PlayLocalAudioClip(putOnClotheSound);

        slot.isOccupied = true;
        slot.storedClothing = currentInstance;
        slot.durabiltyObject.SetActive(true);
        slot.durabilityText.text = slot.storedClothing.currentDurability.ToString();

        slot.slotImage.sprite = data.iconSprite;

        cursorHoldingItem = false;
        currentInstance = null;
        DisableCursor();
        return true;
    }

    //0 is up  1 is right 2 is down 3 is left

    public InventorySlot[] RetrieveLastObserved(InventorySlot pointOrigin, int orientation, ItemInstance instance)
    {
        SetOrientation(orientation, instance.id);
        int xVal = useX;
        int yVal = useY;

        InventorySlot[,] inventorySlots2d = pointOrigin.heldIn.slots2d;

        if (pointOrigin.xPosition + (xVal * offsetMultiplier) > inventorySlots2d.GetLength(0))
        {
            xVal -= pointOrigin.xPosition + (xVal * offsetMultiplier) - inventorySlots2d.GetLength(0);
        }
        if (pointOrigin.yPosition + (yVal * -1 * offsetMultiplier) > inventorySlots2d.GetLength(1))
        {
            yVal -= pointOrigin.yPosition + (yVal * -1 * offsetMultiplier) - inventorySlots2d.GetLength(1);
        }
        if (pointOrigin.xPosition + (xVal * offsetMultiplier) < -1)
        {
            xVal -= Mathf.Abs(pointOrigin.xPosition + (xVal * offsetMultiplier));
            xVal++;
        }
        if (pointOrigin.yPosition + (yVal * -1 * offsetMultiplier) < -1)
        {
            yVal -= Mathf.Abs(pointOrigin.yPosition + (yVal * -1 * offsetMultiplier));
            yVal++;
        }

        //actually generate the values
        List<InventorySlot> slotReference = new List<InventorySlot>();

        for (int horrizontal = 0; horrizontal < xVal; horrizontal++)
        {
            for (int vertical = 0; vertical < yVal; vertical++)
            {
                slotReference.Add(inventorySlots2d[pointOrigin.xPosition + (horrizontal * offsetMultiplier), pointOrigin.yPosition + (vertical * -1 * offsetMultiplier)]); //we have the extra -1 because y starts from screen top
            }
        }
        return slotReference.ToArray();
    }

    public bool PlacingBlocked(InventorySlot[] slots, int targetAmount)
    {
        if(slots.Length < targetAmount)
        {
            return false;
        }

        foreach (InventorySlot slot in slots)
        {
            if(slot.isOccupied == true)
            {
                return false;
            }
        }
        return true;
    }

    public void CreatePlacingOutline(InventorySlot[] slots, ItemInstance instance, InventorySlot over)
    {
        if(slots.Length == 0)
        {
            return;
        }

        ItemData data = allItems.allItems[instance.id];
        int targetAmount = data.slotSpaceX * data.slotSpaceY;
        bool canPlace = PlacingBlocked(slots, targetAmount);

        currentOutline = slots;

        if(canPlace == true)
        {
            foreach (InventorySlot slot in currentOutline)
            {
                if (slot.isOccupied == false)
                {
                    slot.backingImage.color = canPlaceColor;
                }
            }
        }
        else
        {
            foreach (InventorySlot slot in currentOutline)
            {
                if (slot.isOccupied == false)
                {
                    slot.backingImage.color = cannotPlaceColor;
                }
            }

        }
    }

    public void ResetPlacingOutline()
    {
        if (currentOutline != null)
        {
            foreach (InventorySlot slot in currentOutline)
            {
                slot.backingImage.color = backingBaseColor;
            }
        }
    }

    public void RemoveByInstance(ItemInstance instance)
    {

    }

    public void RotateItem(InputAction.CallbackContext context)
    {
        if (!base.IsOwner)
        {
            return;
        }

        if (context.performed && cursorHoldingItem == true && currentInstance != null)
        {
            if(currentOrientation == 3)
            {
                currentOrientation = 0;
            }
            else
            {
                currentOrientation++;
            }

            SetOrientation(currentOrientation, currentInstance.id);
            SetCursorImage();
            if (currentHover != null)
            {
                ResetPlacingOutline();
                CreatePlacingOutline(RetrieveLastObserved(currentHover, currentOrientation, currentInstance), currentInstance, currentHover);
            }
        }

        if (p_menu.inventoryOpen)
        {
            p_audio.PlayLocalAudioClip(rotateSound);
        }
    }

    #region settingvalues
    public void SetSlotImage(InventorySlot iSlot)
    {
        RectTransform imageRect = iSlot.itemImage.gameObject.GetComponent<RectTransform>();
        RectTransform backingRect = iSlot.backingImage.GetComponent<RectTransform>();
        if (iSlot.isOccupied)
        {
            if (iSlot.isSubSlot == false)
            {
                if (iSlot.itemImage != null && iSlot.backingImage != null)
                {
                    iSlot.itemImage.enabled = true; //enables  
                    iSlot.backingImage.enabled = true;

                    ItemData itemData = allItems.allItems[iSlot.storedItem.id];
                    iSlot.itemImage.sprite = itemData.iconSprite;

                    backingRect.localScale = new Vector3(useX, useY, 1);

                    backingRect.localPosition = new Vector3((((backingRect.rect.width * useX) / 2) - (backingRect.rect.height / 2)) * offsetMultiplier, (((backingRect.rect.height * useY) / 2) - (backingRect.rect.height / 2)) * offsetMultiplier);

                    imageRect.localPosition = backingRect.localPosition;
                    imageRect.localRotation = Quaternion.Euler(0, 0, rotvalue);

                    float scaleValue = itemData.slotSpaceX;
                    if (itemData.slotSpaceY > scaleValue)
                    {
                        scaleValue = itemData.slotSpaceY;
                    }
                    imageRect.localScale = new Vector3(scaleValue, scaleValue, 1);
                }
            }
            else
            {
                iSlot.itemImage.enabled = false; //enables  
                iSlot.backingImage.color = Color.clear;
                iSlot.backingImage.enabled = false;
            }
        }
        else
        {
            iSlot.backingImage.enabled = true;
            iSlot.backingImage.color = backingBaseColor;

            imageRect.localPosition = Vector3.zero;
            imageRect.localScale = Vector3.one;

            backingRect.localPosition = Vector3.zero;
            backingRect.localScale = Vector3.one;

            iSlot.itemImage.sprite = null; //gets rid of sprite
            iSlot.itemImage.enabled = false; //disables
        }

        SetSlotData(iSlot);
    }

    public void SetSlotData(InventorySlot slot)
    {
        if(slot.isOccupied == true && slot.isSubSlot == false)
        {
            ItemData useData = allItems.allItems[slot.storedItem.id];
            //set stack data
            switch (useData.stack_type)
            {
                case ItemData.StackType.none:
                    slot.stackHolder.gameObject.SetActive(false);
                    break;
                case ItemData.StackType.standard:
                    if (slot.storedItem.stackedItemIds.Count > 0)
                    {
                        slot.stackHolder.gameObject.SetActive(true);
                        slot.stackText.text = (slot.storedItem.stackedItemIds.Count + 1).ToString();
                    }
                    else
                    {
                        slot.stackHolder.gameObject.SetActive(false);
                    }
                    break;
                case ItemData.StackType.container:

                    if (slot.storedItem.stackedItemIds.Count > 0)
                    {
                        slot.stackHolder.gameObject.SetActive(true);
                        slot.stackText.text = (slot.storedItem.stackedItemIds.Count).ToString();
                    }
                    else
                    {
                        slot.stackHolder.gameObject.SetActive(false);
                    }
                    break;
            }

            //set durability data
            if (useData.useDurability)
            {
                slot.durabliltyHolder.gameObject.SetActive(true);
                slot.durabilityText.text = slot.storedItem.currentDurability.ToString();
                slot.durabilityText.color = Color.Lerp(durabiltyOutColor,durabiltyFullColor, (slot.storedItem.currentDurability - 1) / 1000f);
            }
            else
            {
                slot.durabliltyHolder.gameObject.SetActive(false);
            }

            //set positions
   //         RectTransform backingRect = slot.backingImage.GetComponent<RectTransform>();
       //     float width = backingRect.rect.width / 2f;
       //     width -= slotHeight / 2f;
       //     float height = backingRect.rect.height / 2f;
       //     height -= slotHeight / 2f;
        //    slot.stackHolder.localPosition = new Vector2(width, height);

          //  slot.durabliltyHolder.localPosition = new Vector3(-width, -height);

        }
        else
        {
            slot.durabliltyHolder.gameObject.SetActive(false);
            slot.stackHolder.gameObject.SetActive(false);
        }

        if(slot.useStackCallBacks == true)
        {
            slot.StackCallback();
        }
    }

    public void SetOrientation(int value,int byItemId)
    {
        ItemData data = allItems.allItems[byItemId];
        int xVal = data.slotSpaceX;
        int yVal = data.slotSpaceY;

        switch (value)
        {
            case 0:
                useX = xVal;
                useY = yVal;
                offsetMultiplier = 1;
                rotvalue = 0f;
                break;
            case 1:
                useX = yVal;
                useY = xVal;
                offsetMultiplier = 1;
                rotvalue = -90f;
                break;
            case 2:
                useX = xVal;
                useY = yVal;
                offsetMultiplier = -1;
                rotvalue = 180f;
                break;
            case 3:
                useX = yVal;
                useY = xVal;
                offsetMultiplier = -1;
                rotvalue = 90f;
                break;
        }
    }

    public void SetCursorData()
    {
        ItemData useData = allItems.allItems[currentInstance.id];
        //set stack data
        switch (useData.stack_type)
        {
            case ItemData.StackType.none:
                cursorStackHolder.SetActive(false);
                break;
            case ItemData.StackType.standard:
                if (currentInstance.stackedItemIds.Count > 0)
                {
                    cursorStackHolder.SetActive(true);
                    cursorStackText.text = (currentInstance.stackedItemIds.Count + 1).ToString();
                }
                else
                {
                    cursorStackHolder.SetActive(false);
                }
                break;
            case ItemData.StackType.container:

                if (currentInstance.stackedItemIds.Count > 0)
                {
                    cursorStackHolder.SetActive(true);
                    cursorStackText.text = (currentInstance.stackedItemIds.Count).ToString();
                }
                else
                {
                    cursorStackHolder.SetActive(false);
                }
                break;
        }

        //set durability data
        if (useData.useDurability)
        {
            cursorDurabilityHolder.gameObject.SetActive(true);
            cursorDurabilityText.text = currentInstance.currentDurability.ToString();
            cursorDurabilityText.color = Color.Lerp(durabiltyOutColor, durabiltyFullColor, (currentInstance.currentDurability - 1) / 1000f);
        }
        else
        {
            cursorDurabilityHolder.gameObject.SetActive(false);
        }

        //set positions
    //    RectTransform backingRect = cursorBackingImage.GetComponent<RectTransform>();
       // float width = backingRect.rect.width / 2f;
      //  width -= slotHeight / 2f;
       // float height = backingRect.rect.height / 2f;
       // height -= slotHeight / 2f;
       // cursorStackHolder.transform.localPosition = new Vector2(width, height);

       // cursorDurabilityHolder.transform.localPosition = new Vector3(-width, -height);

    }

    #endregion

    #region stacking
    public bool TestToStack(InventorySlot toTest, ItemInstance toInput)
    {
        if(toTest.isOccupied == true && toTest.isSubSlot == false)
        {
            ItemData testData = allItems.allItems[toTest.storedItem.id];

            int countUse = toInput.stackedItemIds.Count;

            //if(toInput.)
            switch (testData.stack_type)
            {
                case ItemData.StackType.standard:
                    if(testData.itemId == toInput.id && (toTest.storedItem.stackedItemIds.Count + 1) < testData.stackCapacity)
                    {
                        return true;
                    }
                    break;
                case ItemData.StackType.container:
                    if (toTest.storedItem.stackedItemIds.Count >= testData.stackCapacity)
                    {
                        return false;
                    }
                    else
                    {
                        for (int i = 0; i < testData.stackableItems.Length; i++)
                        {
                            if (testData.stackableItems[i] == toInput.id)
                            {
                                return true;
                            }
                        }
                    }
                    break;
            }
            return false;
        }

        return false;
    }
    public bool AddToStack(InventorySlot target, ItemInstance additive)
    {
        ItemData toStackItemData = allItems.allItems[target.storedItem.id];

        if (target.storedItem == null) return false;

        int capacity = toStackItemData.stackCapacity;
        if (toStackItemData.stack_type == ItemData.StackType.standard) capacity--;

        if (target.storedItem.stackedItemIds.Count == capacity) {Debug.Log("Stack error"); return false; }

        ItemData additiveData = allItems.allItems[additive.id];

        int maxStack = toStackItemData.stackCapacity;

        List<int> useIds = additive.stackedItemIds;

        int amountToAdd = additive.stackedItemIds.Count;
        if (additiveData.stack_type == ItemData.StackType.standard) amountToAdd++;

        int curCount = target.storedItem.stackedItemIds.Count;
        if (toStackItemData.stack_type == ItemData.StackType.standard) curCount++;

        bool overstacked = false;

        int difference = (amountToAdd + curCount) - maxStack;
        int amountToDifference = amountToAdd - difference;

        if (amountToAdd + curCount > maxStack)
        {
           // difference = amountToAdd - (maxStack - curCount);

            amountToAdd = maxStack - curCount;
            overstacked = true;
        }
        
        for (int i = 0; i < amountToAdd; i++)
        {
            //   int numberToRemove = itemstoAdFrom.stackdItemIds[i];
            if (additiveData.stack_type == ItemData.StackType.standard)
            {
                target.storedItem.stackedItemIds.Add(additiveData.itemId);
            }
            else
            {
                target.storedItem.stackedItemIds.Add(useIds[i]);
            }
        }

        if (overstacked)
        {
            //make stack 
            List<int> newStack = new List<int>();
            Debug.Log(amountToDifference + " difference amount ||" + difference + " difference");
            for (int i = 0; i < difference; i++)
            {
                    newStack.Add(useIds[(useIds.Count - 1) - i]);
            }
            if (additiveData.stack_type == ItemData.StackType.standard && newStack.Count > 0) newStack.RemoveAt(newStack.Count - 1);

            additive.stackedItemIds = newStack; 

            if (cursorHoldingItem)
            {
                currentInstance = additive;
                SetCursorImage();
                SetSlotData(target);             
            }
            else
            {
                AddToInventory(additive, lastPickup);
                SetSlotData(target);
                return false;
            }
            Debug.Log("overstacked");
            return false;
        }
        else
        {
            if (cursorHoldingItem)
            {
                cursorHoldingItem = false;
                currentInstance = null;
                currentHeld = null;
                DisableCursor();
            }
           // additive.stackedItemIds = new List<int>();
            SetSlotData(target);
        }
        return true;
    }
    #endregion

    #region cursor
    public void EnableCursor()
    {
        cursorObject.SetActive(true);
    }

    public void DisableCursor()
    {
        ResetPlacingOutline();
        cursorObject.SetActive(false);
    }

    public void SetCursorImage()
    {
        if (cursorHoldingItem)
        {
            ItemData itemData = allItems.allItems[currentInstance.id];;
            cursorRepresentorImage.enabled = true;
            cursorRepresentorImage.sprite = itemData.iconSprite;

            cursorBackingImage.gameObject.transform.localScale = new Vector3(useX, useY, 1);
            RectTransform cursorRect = cursorBackingImage.GetComponent<RectTransform>();

            cursorBackingImage.gameObject.transform.localPosition = new Vector3((((cursorRect.rect.width * useX) / 2) - (cursorRect.rect.width / 2)) * offsetMultiplier, (((cursorRect.rect.height * useY) / 2) - (cursorRect.rect.height / 2)) * offsetMultiplier);

            float scaleFactor = useX;
            if (useY > useX)
            {
                scaleFactor = useY;
            }

            cursorRepresentorImage.transform.localScale = new Vector3(scaleFactor, scaleFactor);

            cursorRepresentorImage.transform.localPosition = cursorBackingImage.transform.localPosition;
            cursorRepresentorImage.transform.localRotation = Quaternion.Euler(0, 0, rotvalue);

            SetCursorData();
        }
    }

    public void ClearCursor()
    {
        currentSelected = null;
        currentHeld = null;
        SetCursorImage();
        DisableCursor();
    }

    #endregion

    #region mouseHandler
    public void MouseEnterSlot(InventorySlot slot)
    {
        if (!base.IsOwner) return;
        //we highlight 
        currentHover = slot;
        if (cursorHoldingItem)
        {
            CreatePlacingOutline(RetrieveLastObserved(currentHover, currentOrientation, currentInstance), currentInstance, currentHover);
        }
        else
        {
            if (!slot.isSelected && !slot.isSubSlot)
            {
                slot.backingImage.color = backingHoverColor;
            }
        }

        p_audio.PlayLocalAudioClip(slotEnterSound);
    }

    public void MouseExitSlot(InventorySlot slot)
    {
        if (!base.IsOwner) return;

        if (currentHover == slot)
        {
            currentHover = null;
        }
        //we highlight 
        if (!slot.isSelected && !slot.isSubSlot)
        {
            slot.backingImage.color = backingBaseColor;
        }
        if (cursorHoldingItem)
        {
            ResetPlacingOutline();
        }
    }
    #endregion

    public InventoryHolder GenerateInventorySlots(int rows, int columns, string name)
    {
        //make sure we have valid list
        if(inventoryHolders == null)
        {
            inventoryHolders = new List<InventoryHolder>();
        }

        //create a new holder instance
        GameObject holderObjInstance = Instantiate(emptyHolder, holderParent);
        InventoryHolder holderInstance = new InventoryHolder();
        holderInstance.linkedObject = holderObjInstance;

        holderInstance.createdSlots = new List<InventorySlot>();
        holderInstance.itemsStored = new List<ItemInstance>();
        holderInstance.slots2d = new InventorySlot[columns, rows]; //a 2d matrix I like is epic hellllll yeah
        inventoryHolders.Add(holderInstance);

        //set content rect
        holderObjInstance.GetComponent<RectTransform>().sizeDelta = new Vector2(holderObjInstance.GetComponent<RectTransform>().rect.width, holderObjInstance.GetComponent<RectTransform>().rect.height + ((rows + 1) * (slotHeight + slotSpacing)));

        //set name
        holderObjInstance.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = name;

        //actually make the slots
        float yOrigin = (holderObjInstance.GetComponent<RectTransform>().rect.height / 2) + fromTopOffset; //where we start construction
        float xOrigin = -(holderObjInstance.GetComponent<RectTransform>().rect.width / 2) + fromXOffset;
       
        for (int i = 0; i < rows; i++)
        {
            for (int a = 0; a < columns; a++)
            {
                GameObject slotInstance = Instantiate(emptySlot, holderObjInstance.transform);

                RectTransform slotRect = slotInstance.GetComponent<RectTransform>();
                slotRect.localPosition = new Vector3((a * slotHeight + slotSpacing) + xOrigin, -(i * slotHeight + (slotSpacing / 2)) + (yOrigin));

                InventorySlot slotScript = slotInstance.GetComponent<InventorySlot>();
                slotScript.i_Manager = this;
                slotScript.isOccupied = false;
                slotScript.isSubSlot = false;
                slotScript.xPosition = a;
                slotScript.yPosition = i;
                slotScript.backingImage.color = backingBaseColor;
                slotScript.stackHolder.gameObject.SetActive(false);
                slotScript.durabliltyHolder.gameObject.SetActive(false);
                slotScript.heldIn = holderInstance;

                holderInstance.slots2d[a, i] = slotScript;
                holderInstance.createdSlots.Add(slotScript);
            }
        }
        return holderInstance;
    }

    public bool AddToInventory(ItemInstance instance, GameObject pickupObject)
    {
        Debug.Log("adding " + allItems.allItems[instance.id].itemName +" to inventory");
        if (pickupObject != null)
        {
            lastPickup = pickupObject;
        }
        foreach (InventoryHolder holder in inventoryHolders)
        {
            foreach (InventorySlot slot in holder.createdSlots)
            {
                if(slot.isOccupied == true && slot.isSubSlot == false)
                {
                    if (TestToStack(slot, instance))
                    {
                        bool added = AddToStack(slot, instance);
                        if (added && pickupObject != null) {
                            CmdDestroyPickup(pickupObject);
                        }
                        return true; // we have to bool to check if we full stacked or if there are remaining items left in stack
                    }
                }
                else
                {
                    if (slot.isSubSlot == false)
                    {
                        //we check each orientation
                        for (int i = 0; i < 3; i++)
                        {
                            SetOrientation(i, instance.id);

                            ItemData data = allItems.allItems[instance.id];
                            InventorySlot[] overslots = RetrieveLastObserved(slot, i, instance);
                            bool canPlace = PlacingBlocked(overslots, data.slotSpaceX * data.slotSpaceY);
                            if (canPlace)
                            {
                                AddToSlot(slot, instance, i, overslots);
                                if (pickupObject != null) CmdDestroyPickup(pickupObject);
                                return true;
                            }
                        }
                    }
                }
            }
        }
        if(pickupObject == null)
        {
            CmdDropItem(instance);
        }
        return false;
    }

    public void AddToSlot(InventorySlot slot, ItemInstance instance, int orientation, InventorySlot[] overslots) 
    {
        List<Vector2> slotConnections = new List<Vector2>();
        foreach (InventorySlot sUse in overslots)
        {
            slotConnections.Add(new Vector2(sUse.xPosition, sUse.yPosition));
        }
        foreach (InventorySlot overSlot in overslots)
        {
            if(overSlot != slot)
            {
                overSlot.isOccupied = true;
                overSlot.isSubSlot = true;
                overSlot.connectedSlots = slotConnections;
                overSlot.currentOrientation = orientation;
                overSlot.parentSlot = new Vector2(slot.xPosition, slot.yPosition);
                overSlot.storedItem = instance;
                SetSlotImage(overSlot);
            }
        }
        slot.isOccupied = true;
        slot.connectedSlots = slotConnections;
        slot.isSubSlot = false;
        slot.storedItem = instance;
        slot.currentOrientation = orientation;
        SetOrientation(slot.currentOrientation, instance.id);
        SetSlotImage(slot);
    }

    public void SubtractOneFromSlot(InventorySlot slot)
    {
        if(slot.storedItem.stackedItemIds.Count > 0)
        {
            slot.storedItem.stackedItemIds.RemoveAt(slot.storedItem.stackedItemIds.Count - 1);
            SetSlotImage(slot);
        }
        else
        {
            RemoveFromSlot(slot);
        }
    }

    public void RemoveFromSlot(InventorySlot slot)
    {
        if(selectedSlot != null)
        {
            DeselectLastSelected();
        }
        List<InventorySlot> removing = new List<InventorySlot>();
        foreach (Vector2 slotCords in slot.connectedSlots)
        {
            InventorySlot rSlot = slot.heldIn.slots2d[(int)slotCords.x, (int)slotCords.y];
            rSlot.isOccupied = false;
            rSlot.isSubSlot = false;
            rSlot.storedItem = null;
            removing.Add(rSlot);

            SetSlotImage(rSlot);
        }

        slot.isOccupied = false;
        slot.isSubSlot = false;
        slot.storedItem = null;

        SetSlotImage(slot);

        foreach (InventorySlot use in removing)
        {
            use.connectedSlots = new List<Vector2>();
        }     
    }

    public void RemoveById(int id, bool shownotification)
    {
        foreach (InventoryHolder holder in inventoryHolders)
        {
            foreach (InventorySlot slot in holder.createdSlots)
            {
                if(slot.isOccupied == true && slot.isSubSlot == false)
                {
                    if(slot.storedItem.id == id)
                    {
                        ItemData removedata = allItems.allItems[slot.storedItem.id];
                        if(removedata.stack_type == ItemData.StackType.standard && slot.storedItem.stackedItemIds.Count > 0)
                        {
                            slot.storedItem.stackedItemIds.RemoveAt(0);
                            SetSlotData(slot);
                            return;
                        }
                        else
                        {
                            RemoveFromSlot(slot);
                        }

                        if (shownotification)
                        {
                            PlayPickupNotification(id, -1);
                        }
                        return;
                    }
                }
            }
        }
    }
    public void InventoryClosed()
    {
        i_ContextMenu.OnInventoryClose(); //tells menu to shut itself off

        if(lastStorageAccessed != null && createdExtraHolder != null)
        {
            SaveToStorage();
        }
        if (createdExtraHolder != null)
        {
            Destroy(createdExtraHolder.linkedObject);
            lastStorageAccessed = null;
        }
        //we drop whatever has been removed and such

        cursorHoldingItem = false;
        currentHeld = null;
        if (currentInstance != null)
        {
            CmdDropItem(currentInstance);
        }
        currentInstance = null;
        DisableCursor();
        ResetPlacingOutline();
        currentHover = null;

        if (selectedSlot != null)
        {
            selectedSlot.isSelected = false;
            selectedSlot.backingImage.color = backingBaseColor;
            selectedSlot = null;
        }

        clothingManager.InventoryClosed();

        p_BuffManager.InventoryClosed();

        p_audio.PlayLocalAudioClip(closeInventorySound);
    }

    public bool RetrieveCursorState()
    {
        if (cursorHoldingItem)
        {
            return true;
        }
        return false;
    }

    [ServerRpc]
    public void CmdDropItem(ItemInstance instance)
    {
        GameObject dropInstance = Instantiate(allItems.allItems[instance.id].pickupPrefab, transform.position, transform.rotation, pickupsHolder);

        dropInstance.GetComponent<ItemPickupScript>().SetInstance(instance);

        ServerManager.Spawn(dropInstance);
    }

    [ServerRpc]
    public void CmdDestroyPickup(GameObject pickup)
    {
        Destroy(pickup);
      
    }

    public void RemoveInventorySection(InventoryHolder holder)
    {
        foreach (InventorySlot slot in holder.createdSlots)
        {
            if(slot.isOccupied == true && slot.isSubSlot == false)
            {
                CmdDropItem(slot.storedItem);
            }
       }
        inventoryHolders.Remove(holder);
        Destroy(holder.linkedObject);
    }

    public List<InventorySlot> FindAmmoOfType(string name)
    {
        List<InventorySlot> toReturn = new List<InventorySlot>();

        foreach (InventoryHolder holder in inventoryHolders)
        {
            foreach (InventorySlot slot in holder.createdSlots)
            {
                if (slot.isOccupied == true && slot.isSubSlot == false)
                {
                    ItemInstance checkInstance = slot.storedItem;
                    ItemData checkData = allItems.allItems[checkInstance.id];
                    if (checkData.itemType == ItemData.Item_Type.ammunition)
                    {
                        if (checkData.ammoData.ammoSize == name)
                        {
                            toReturn.Add(slot);
                        }
                    }
                }
            }
        }

        return toReturn;
    }

    public List<InventorySlot> FindMagazinesOfType(string name)
    {
        List<InventorySlot> toReturn = new List<InventorySlot>();

        foreach (InventoryHolder holder in inventoryHolders)
        {
            foreach (InventorySlot slot in holder.createdSlots)
            {
                if(slot.isOccupied == true && slot.isSubSlot == false)
                {
                    ItemInstance checkInstance = slot.storedItem;
                    ItemData checkData = allItems.allItems[checkInstance.id];
                    if(checkData.itemType == ItemData.Item_Type.magazine)
                    {
                        if(checkData.magazineData.ammoSize == name)
                        {
                            toReturn.Add(slot);
                        }
                    }
                }
            }
        }

        return toReturn;
    }

    public List<InventorySlot> FindAttachmentsOfType(AttachmentData.AttachMentSlot attachtype)
    {
        List<InventorySlot> toReturn = new List<InventorySlot>();

        foreach (InventoryHolder holder in inventoryHolders)
        {
            foreach (InventorySlot slot in holder.createdSlots)
            {
                if (slot.isOccupied == true && slot.isSubSlot == false)
                {
                    ItemInstance checkInstance = slot.storedItem;
                    ItemData checkData = allItems.allItems[checkInstance.id];
                    if (checkData.itemType == ItemData.Item_Type.attachment)
                    {
                        if (checkData.attachmentData.attachMentSlot == attachtype)
                        {
                            toReturn.Add(slot);
                        }
                    }
                }
            }
        }

        return toReturn;
    }

    //for when moving out of this ab
    public void OnTabDeselected()
    {
        Debug.Log("Inventory tab deselected");
        InventoryClosed();
    }

    public void ClearInventory()
    {
        foreach (InventoryHolder holder in inventoryHolders)
        {
            foreach (InventorySlot slot in holder.createdSlots)
            {
                if (slot != null && slot.isOccupied == true)
                {
                    slot.isOccupied = false;
                    slot.isSubSlot = false;
                    slot.storedItem = null;
                    if (slot.backingImage != null && slot.itemImage != null)
                    {
                        SetSlotImage(slot);
                    }
                }
            }
        }
    }

    public bool CanBuild(BuildData input)
    {
        //checks all the slots
        foreach (CraftingInput craft in input.inputs)
        {
            int count = 0;
            foreach (InventoryHolder holder in inventoryHolders)
            {
                foreach (InventorySlot slot in holder.createdSlots)
                {
                    if (slot.isOccupied == true && slot.isSubSlot == false)
                    {
                        ItemInstance checkInstance = slot.storedItem;
                        if(checkInstance.id == craft.id)
                        {
                            count += checkInstance.stackedItemIds.Count + 1;
                        }
                    }
                }
            }
            if(count < craft.quantity)
            {
                return false;
            }
        }
        return true; 
    }

    public int GetWidth()
    {
        return playerInventoryX;
    }

    public List<SerialisedInventoryItem> GetAllInventoryItems()
    {
        List<SerialisedInventoryItem> items = new List<SerialisedInventoryItem>();

        for (int i = 0; i < inventoryHolders.Count; i++)
        {
            InventoryHolder holder = inventoryHolders[i];
            Debug.Log("clearedHolder_retrieval");
            foreach (InventorySlot slot in holder.createdSlots)
            {
                if (slot.isOccupied && slot.isSubSlot == false)
                {
                    SerialisedInventoryItem serialisedItem = new SerialisedInventoryItem();
                    serialisedItem.xLocation = slot.xPosition;

                    //get y postition
                    if (i == 0)
                    {
                        serialisedItem.yLocation = slot.yPosition;                    
                    }
                    else 
                    {
                        int yBase = slot.yPosition;
                        //we add for each
                        for (int z = 0; z < i; z++)
                        {
                            InventoryHolder holderUse = inventoryHolders[z];

                            if (holder.itemsStored.Count > 0)
                            {
                                Debug.Log("clearedHolder_retrieval_2");

                                yBase += holderUse.createdSlots[holder.createdSlots.Count - 1].yPosition; //adds on position
                            }
                        }

                        serialisedItem.yLocation = yBase;
                    }

                    serialisedItem.orientation = slot.currentOrientation;
                    serialisedItem.savedInstance = slot.storedItem;
                    items.Add(serialisedItem);
                }
            }
        }
        Debug.Log("finished get items");
        return items;
    }


    #region secondInventoryHandling
    private InventoryHolder createdExtraHolder;
    private GameObject lastStorageAccessed;

    public void GenerateSecondInventory(int xSize, int ySize, List<SerialisedInventoryItem> savedItems, GameObject source, string name)
    {
        Debug.Log("opened storage of " + name);

        lastStorageAccessed = source;

        //generate
        GameObject holderObjInstance = Instantiate(emptyHolder, secondInventoryContent);
        InventoryHolder holderInstance = new InventoryHolder();
        holderInstance.linkedObject = holderObjInstance;

        holderInstance.createdSlots = new List<InventorySlot>();
        holderInstance.itemsStored = new List<ItemInstance>();
        holderInstance.slots2d = new InventorySlot[xSize, ySize]; //a 2d matrix I like is epic hellllll yeah
        inventoryHolders.Add(holderInstance);

        //set content rect
        holderObjInstance.GetComponent<RectTransform>().sizeDelta = new Vector2(holderObjInstance.GetComponent<RectTransform>().rect.width, holderObjInstance.GetComponent<RectTransform>().rect.height + ((ySize + 1) * (slotHeight + slotSpacing)));

        //set name
        holderObjInstance.transform.GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().text = name;
        createdExtraHolder = holderInstance;


        //actually make the slots
        float yOrigin = (holderObjInstance.GetComponent<RectTransform>().rect.height / 2) + fromTopOffset; //where we start construction
        float xOrigin = -(holderObjInstance.GetComponent<RectTransform>().rect.width / 2) + fromXOffset;

        for (int i = 0; i < ySize; i++)
        {
            for (int a = 0; a < xSize; a++)
            {
                Debug.Log("Made second slot");
                GameObject slotInstance = Instantiate(emptySlot, holderObjInstance.transform);

                RectTransform slotRect = slotInstance.GetComponent<RectTransform>();
                slotRect.localPosition = new Vector3((a * slotHeight + slotSpacing) + xOrigin, -(i * slotHeight + (slotSpacing / 2)) + (yOrigin));

                InventorySlot slotScript = slotInstance.GetComponent<InventorySlot>();
                slotScript.i_Manager = this;
                slotScript.isOccupied = false;
                slotScript.isSubSlot = false;
                slotScript.xPosition = a;
                slotScript.yPosition = i;
                slotScript.backingImage.color = backingBaseColor;
                slotScript.stackHolder.gameObject.SetActive(false);
                slotScript.durabliltyHolder.gameObject.SetActive(false);
                slotScript.heldIn = holderInstance;

                holderInstance.slots2d[a, i] = slotScript;
                holderInstance.createdSlots.Add(slotScript);
            }
        }
        //load items
        if (savedItems.Count > 0)
        {
            foreach (SerialisedInventoryItem item in savedItems)
            {
                //stop problems with wrong loading
                if(item.xLocation <= xSize && item.xLocation <= xSize)
                {
                    SetOrientation(item.orientation, item.savedInstance.id);

                    ItemData data = allItems.allItems[item.savedInstance.id];
                    InventorySlot slot = holderInstance.slots2d[item.xLocation, item.yLocation];
                    InventorySlot[] overslots = RetrieveLastObserved(slot, item.orientation, item.savedInstance);
                    bool canPlace = PlacingBlocked(overslots, data.slotSpaceX * data.slotSpaceY);
                    if (canPlace)
                    {
                        AddToSlot(slot, item.savedInstance, item.orientation, overslots);
                    }
                }
            }
        }
        if (lastStorageAccessed != null)
        {
            CmdSetStorageBox(true, lastStorageAccessed);
        }
    }
    
    //this just saves any inventory values to the box once we leave it yay
    void SaveToStorage()
    {
        if(lastStorageAccessed == null)
        {
            Debug.Log("No accessed last storage");
            return;
        }
        Debug.Log("saved storage");
        CmdSetStorageBox(false, lastStorageAccessed);

        List<SerialisedInventoryItem> toStoreitems = new List<SerialisedInventoryItem>();

        foreach (InventorySlot slot in createdExtraHolder.createdSlots)
        {
            if(slot.isOccupied && slot.isSubSlot == false)
            {
                SerialisedInventoryItem serialisedItem = new SerialisedInventoryItem();
                serialisedItem.xLocation = slot.xPosition;
                serialisedItem.yLocation = slot.yPosition;
                serialisedItem.orientation = slot.currentOrientation;
                serialisedItem.savedInstance = slot.storedItem;
                toStoreitems.Add(serialisedItem);
            }
        }

        SetStorageItems(toStoreitems, lastStorageAccessed);
    }

    [ServerRpc]
    void SetStorageItems(List<SerialisedInventoryItem> toStore, GameObject target)
    {
        StorageHoldObject heldStorage = target.GetComponent<StorageHoldObject>();
        heldStorage.Setitems(toStore);
    }

    [ServerRpc]
    void CmdSetStorageBox(bool value, GameObject target)
    {
            StorageHoldObject heldStorage = target.GetComponent<StorageHoldObject>();
            heldStorage.SetOpen(value);
    }
    #endregion
}

[System.Serializable]
public class SerialisedInventoryItem
{

    public int orientation;
    public int xLocation;
    public int yLocation;

    public ItemInstance savedInstance;
}

