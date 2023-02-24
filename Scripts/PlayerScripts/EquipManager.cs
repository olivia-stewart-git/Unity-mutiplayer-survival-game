using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine.InputSystem;

public class EquipManager : NetworkBehaviour
{
    [SerializeField] private InputAction leftButtonInput;

    //multiplayer 
    [SyncVar(OnChange = nameof(UpdateHeldItem))] private int heldItemId;

    private GameObject createdHeldInstance;

    
    private bool handsEquipped = true;
    private bool inTransition = false;
    private bool itemEquiped = false;

    [SerializeField] private float toHandsTime = 0.2f;

    //equiping other items
    private GameObject equipedInstance;
    [SerializeField] private Transform itemHolder;
    private ItemReference allitems;
    [SerializeField] private HotBarManager hotBarManager;
    [SerializeField] private GameObject emptyhandsObject;
    [SerializeField] private PlayerMenuManager p_Menu;
    [SerializeField] private InventoryManager p_Inventory;
    [SerializeField] private PlayerAnimationManager p_AnimationManager;

    private PlayerMouseLook p_Mouselook;
    private PlayerAudioManager p_Audio;
    private PlayerDamageManager p_damage;

    [Space]

    [SerializeField] private Transform handSocket;

    private Coroutine switchingCoroutine;

    //header interfaces
    private I_EquipedItem equipedInterface;
    private I_ExtraInput extraInput = null;

    public Vector3 GetSocketPos()
    {
        return handSocket.position;
    }
    public void SetItemReference(ItemReference iRef)
    {
        allitems = iRef;
    }

    EZCameraShake.CameraShaker c_shake;
    private void Start()
    {
        c_shake = EZCameraShake.CameraShaker.Instance;
        p_Audio = GetComponent<PlayerAudioManager>();
    }
    public override void OnStartClient()
    {
        if (!base.IsOwner) return;
        p_damage = GetComponent<PlayerDamageManager>();
        equipedInterface = emptyhandsObject.GetComponent<I_EquipedItem>();
        equipedInterface.Intialise(gameObject, null);
        equipedInterface.Drawn();

        p_Mouselook = GetComponent<PlayerMouseLook>();
    }

    public void DrawPressed(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_damage.isDead) return;
        if (context.performed)
        {
            if (handsEquipped)
            {
                RequestItemEquip(true);
            }
            else
            {
                HandsSwitch();
            }
        }
    }

    //dequip whatever object could be held
    public void HandsSwitch()
    {
        if (handsEquipped == true || !base.IsOwner) return;
        if(switchingCoroutine != null)
        {
            StopCoroutine(switchingCoroutine);
        }

        switchingCoroutine = StartCoroutine(SwitchToHands());
    }

    #region switch coroutines
    public IEnumerator SwitchToHands()
    {
        itemEquiped = false;
        p_AnimationManager.SetUpperBodyState(1);

        if(equipedInstance != null)
        {
            Destroy(equipedInstance);
        }
        extraInput = null;

        handsEquipped = true;
        inTransition = true;

        emptyhandsObject.SetActive(true);

        equipedInterface.DeEquip();

        equipedInterface = emptyhandsObject.GetComponent<I_EquipedItem>();
        equipedInterface.Intialise(gameObject, null);

        yield return new WaitForSeconds(toHandsTime);


         CmdSetHandHeld(0);


        inTransition = false;
        equipedInterface.Drawn();
    }
    public IEnumerator SwitchToItem(ItemInstance toUse)
    {
        //load item
        if(equipedInstance != null)
        {
            Destroy(equipedInstance);
        }
        ItemData data = allitems.allItems[toUse.id];

        if(data.equipSound != null)
        {
        p_Audio.PlayLocalAudioClip(data.equipSound);
        }

        p_AnimationManager.SetUpperBodyState(data.equipLayer);

        equipedInstance = Instantiate(data.equipObject,itemHolder);

        if (equipedInstance.GetComponent<I_ExtraInput>() != null)
        {
            extraInput = equipedInstance.GetComponent<I_ExtraInput>();
        }
        else
        {
            extraInput = null;
        }

        equipedInterface.DeEquip();

        equipedInterface = equipedInstance.GetComponent<I_EquipedItem>();
        equipedInterface.Intialise(gameObject, toUse);

        handsEquipped = false;
        inTransition = true;
        emptyhandsObject.SetActive(false);
        c_shake.ShakeOnce(1.5f, 1f, 1f, 2f);
        //creates the multiplayer held item

        CmdSetHandHeld(data.itemId);


        yield return new WaitForSeconds(toHandsTime + data.equipTime);
        inTransition = false;
        equipedInterface.Drawn();
        itemEquiped = true;
    }
    public IEnumerator SwitchFromItem(ItemInstance toUse)
    {
        //load item
        itemEquiped = true;
        if (equipedInstance != null)
        {
            Destroy(equipedInstance);
        }
        ItemData data = allitems.allItems[toUse.id];

        if (data.equipSound != null)
        {
            p_Audio.PlayLocalAudioClip(data.equipSound);
        }

        p_AnimationManager.SetUpperBodyState(data.equipLayer);

        equipedInstance = Instantiate(data.equipObject, itemHolder);

        if (equipedInstance.GetComponent<I_ExtraInput>() != null)
        {
            extraInput = equipedInstance.GetComponent<I_ExtraInput>();
        }
        else
        {
            extraInput = null;
        }

        equipedInterface.DeEquip();

        equipedInterface = equipedInstance.GetComponent<I_EquipedItem>();
        equipedInterface.Intialise(gameObject, toUse);

        handsEquipped = false;
        inTransition = true;

        c_shake.ShakeOnce(1.5f, 1f, 1f, 2f);

        //creates the multiplayer held item
 
         CmdSetHandHeld(data.itemId);
     

        yield return new WaitForSeconds(data.equipTime);
        inTransition = false;
        equipedInterface.Drawn();
    }
    #endregion

    #region multiplayerRepresenting
    [ServerRpc] void CmdSetHandHeld(int val)
    {
        heldItemId = val;
    }

    //updates dynamically
    public void UpdateHeldItem(int prev, int next, bool asServer)
    {
        // 0 for non
        if(next == 0)
        {
            if(createdHeldInstance != null)
            {
                Destroy(createdHeldInstance);
            }
        }
        else
        {
            if (createdHeldInstance != null)
            {
                Destroy(createdHeldInstance);
            }

            ItemData toSwitchData = allitems.allItems[next];

            //create the actual representor object
            GameObject createdInstance = Instantiate(toSwitchData.multiplayerRepresentObject, handSocket.position, handSocket.rotation, handSocket);

            if (base.IsOwner)
            {
                Transform[] pRepresentChildren = createdInstance.transform.GetComponentsInChildren<Transform>();
                foreach (Transform pGame in pRepresentChildren)
                {
                    pGame.gameObject.layer = 7;
                }
            }

            createdHeldInstance = createdInstance;

            Debug.Log("created representor in hand");
        }
    }

    #endregion
    public void RequestItemEquip(bool activateOnEmpty) //activate on empty is when you want to de-equip empty hands
    {
        if (!IsOwner ||hotBarManager.CurrentEquipedInstance() == null) return;
        if(handsEquipped == true && activateOnEmpty == false)
        {
            return;
        }

        ItemInstance toUseInstace = hotBarManager.CurrentEquipedInstance();
        ItemData testData = allitems.allItems[toUseInstace.id];

        if (testData.equipObject == null) return;

        if(switchingCoroutine != null)
        {
            StopCoroutine(switchingCoroutine);
        }
        if (handsEquipped == true)
        {
            switchingCoroutine = StartCoroutine(SwitchToItem(toUseInstace));
        }
        else
        {
            switchingCoroutine = StartCoroutine(SwitchFromItem(toUseInstace));
        }
    }

    public void LeftButton(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_Menu.inventoryOpen || p_Menu.paused || p_damage.isDead == true || inTransition || handAnimating) return;
       if (context.performed)
        {
            //button down
            equipedInterface.LeftButtonDown();
       }
        if (context.canceled)
        {
            //button up
            equipedInterface.LeftButtonUp();
       }
    }
    public void RightButton(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_Menu.inventoryOpen || p_Menu.paused || p_damage.isDead || inTransition || handAnimating) return;
        if (context.performed)
        {
            //button down
            equipedInterface.RightButtonDown();
        }
        if (context.canceled)
        {
            //button up
            equipedInterface.RightButtonUp();
        }
    }
    public void ExtraButton1(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_Menu.inventoryOpen || p_Menu.paused || p_damage.isDead || inTransition || handAnimating) return;
        if (context.performed)
        {
            //button down
            if(extraInput != null)
            {
                extraInput.ExtraButton1Down();
            }
        }
        if (context.canceled)
        {
            //button up
            if (extraInput != null)
            {
                extraInput.ExtraButton1Up();
            }
        }
    }
    public void ExtraButton2(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_Menu.inventoryOpen || p_Menu.paused || p_damage.isDead || inTransition || handAnimating) return;
        if (context.performed)
        {
            //button down
            if (extraInput != null)
            {
                extraInput.ExtraButton2Down();
            }
        }
        if (context.canceled)
        {
            //button up
            if (extraInput != null)
            {
                extraInput.ExtraButton2Up();
            }
        }
    }

    public void InspectButton(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_Menu.inventoryOpen || p_Menu.paused || p_damage.isDead || inTransition || handAnimating) return;
        if (context.performed)
        {
            //button down
            if (extraInput != null)
            {
                extraInput.InsectButton();
            }
        }
    }

    public void OnInventoryOpen()
    {
        if (equipedInstance == null || !base.IsOwner) return;

        if (equipedInstance.GetComponent<I_MenuEvent>() != null)
        {
            equipedInstance.GetComponent<I_MenuEvent>().OnInventoryOpen();
        }
    }

    public void DropHeld()
    {
        Debug.Log("whyyyyyyyyyy");
        p_Inventory.RemoveFromSlot(hotBarManager.GetCurrentSlot());
        hotBarManager.GenerateHotBar();
    }

    //get drop input
    public void DropInput(InputAction.CallbackContext context)
    {
        if (!base.IsOwner || p_Menu.inventoryOpen || p_Menu.paused || p_damage.isDead || inTransition) return;
        if (context.performed)
        {
            Debug.Log("drop input pressed");
            ItemInstance drop = LocalHeldDrop();
            if (drop != null)
            {
                CmdDropCurrentHeld(drop);
                PlayerHandAnimation("Anim_BareHands_ThrowAwayItem", 0.5f);
            }
        }
    }

    ItemInstance LocalHeldDrop()
    {
        InventorySlot curSlot = hotBarManager.GetCurrentSlot();
        if(curSlot != null)
        {
        ItemInstance iInstance = curSlot.storedItem;
            if (iInstance != null)
            {
                p_Inventory.RemoveFromSlot(curSlot);
                hotBarManager.GenerateHotBar();
                return iInstance;
            }
        }
        return null;//zero acts as a false value
    }

    //this actually drops the item on the ground
    [ServerRpc] public void CmdDropCurrentHeld(ItemInstance iInstance)
    {      

        ItemData data = allitems.allItems[iInstance.id];
        Debug.Log("drop item command " + data.itemName);
        GameObject dropInstance = Instantiate(data.pickupPrefab, p_Mouselook.GetCamera().transform.position, Quaternion.LookRotation(p_Mouselook.GetCamera().transform.forward));
        dropInstance.GetComponent<Rigidbody>().AddForce(p_Mouselook.GetCamera().transform.forward * 5f, ForceMode.Impulse);
        dropInstance.GetComponent<ItemPickupScript>().SetInstance(iInstance);

        ServerManager.Spawn(dropInstance);
    }

    public void SubtractFromHeld()
    {
        InventorySlot curSlot = hotBarManager.GetCurrentSlot();
        if(curSlot.storedItem.stackedItemIds.Count > 0)
        {
            curSlot.storedItem.stackedItemIds.RemoveAt(0);
            p_Inventory.SetSlotImage(curSlot);
        }
        else
        {
            p_Inventory.RemoveFromSlot(curSlot);
            hotBarManager.GenerateHotBar();
        }
    }

    private bool handAnimating = false;
    private Coroutine setAnimCoroutine;
    public void PlayerHandAnimation(string animation, float duration)
    {
        setAnimCoroutine =  StartCoroutine(SetAnimValues(duration));
        emptyhandsObject.GetComponent<Animator>().Play(animation, 0, 0);
    }

    IEnumerator SetAnimValues(float duration)
    {
        handAnimating = true;
        emptyhandsObject.SetActive(true);

        if (equipedInstance != null)
        {
            equipedInstance.SetActive(false);
        }

        yield return new WaitForSeconds(duration);

        if (equipedInstance != null)
        {
            equipedInstance.SetActive(true);
        }

        if(handsEquipped == false)
        {
            emptyhandsObject.SetActive(false);
        }

        handAnimating = false;
    }

    public void Update()
    {
        if(!base.IsOwner)

        if (!leftButtonInput.IsPressed() && itemEquiped == true)
        {
            equipedInterface.LeftButtonUp();
        }
        else
        {
            if (itemEquiped && leftButtonInput.WasPerformedThisFrame())
            {
                equipedInterface.LeftButtonDown();
            }
        }
    }

    public int GetCurrentItemDurability()
    {    
            InventorySlot curSlot = hotBarManager.GetCurrentSlot();
            return curSlot.storedItem.currentDurability;
    }
}
