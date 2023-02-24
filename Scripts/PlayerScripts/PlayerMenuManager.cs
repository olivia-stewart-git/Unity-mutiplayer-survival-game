using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FishNet.Managing;

public class PlayerMenuManager : NetworkBehaviour
{
    private bool initialised = false;
    private UiReference u_Reference;

    [SerializeField] private PlayerMouseLook p_mouseLook;
    [SerializeField] private PlayerMovementMangaer p_Movement;
    [SerializeField] private InventoryManager playerInventory;
    [SerializeField] private PlayerRepresentorManager p_representor;
    [SerializeField] private EquipManager equipManager;

    private PlayerDamageManager p_damage;

    private NetworkManager netManager;

    [SerializeField] private GameObject pauseMenuPrefab;
    private GameObject pauseMenuInstance;
   [HideInInspector] public PauseMenureference p_Menu;
    
    [Header("Inventory menu settings")]
    private GameObject inventoryMenuObject;
    [HideInInspector] public bool inventoryOpen = false;
    private bool overrideInventoryOpening = false;

    private TabButtonUi inventoryTab;
    private TabGroup inventoryTabGroup;

    public bool paused = false;

    private GameObject netHud;
    public void InitialiseValues(UiReference uiReference)
    {
        p_damage = GetComponent<PlayerDamageManager>();

        if (!base.IsOwner) return;

     //   netManager = NetworkManager.singleton;
        netHud = GameObject.Find("NetworkHud");

        u_Reference = uiReference;

        inventoryMenuObject = u_Reference.inventoryMenuObject;
        inventoryTab = uiReference.inventoryTab;
        inventoryTabGroup = uiReference.inventoryTabGroup;

        inventoryMenuObject.SetActive(false);

        Transform createdObjectsHolder = GameObject.Find("CreatedObjectHolder").transform;
        pauseMenuInstance = Instantiate(pauseMenuPrefab, createdObjectsHolder);
    
        p_Menu = pauseMenuInstance.GetComponent<PauseMenureference>();
        p_Menu.unpauseButton.onClick.AddListener(Unpause);

        pauseMenuInstance.SetActive(false);

        initialised = true;
    }

    public void InventoryKeyPressed(InputAction.CallbackContext context)
    {
        if (!base.IsOwner) return;
        if (p_damage.isDead == true) return;

        if (context.performed && overrideInventoryOpening == false && paused == false)
        {
            if (inventoryOpen)
            {
                CloseInventory();
            }
            else
            {
                OpenInventory();
            }
        }
    }

    public void OpenInventory()
    {
        inventoryOpen = true;
        equipManager.OnInventoryOpen();
        inventoryMenuObject.SetActive(true);

        p_representor.InventoryOpened();

        inventoryTabGroup.OnTabSelected(inventoryTab); //resets to the inventoryTab

        p_mouseLook.SetMouseLookOverride(true);
        p_mouseLook.SetCursorLock(false);

        p_Movement.SetMovementOverride(true);

        playerInventory.OpenedInventory();

    }

    public void SetOverrideInventory(bool value)
    {
        overrideInventoryOpening = value;
    }

    public void CloseInventory()
    {
        playerInventory.InventoryClosed();

        inventoryOpen = false;
        inventoryMenuObject.SetActive(false);

        p_representor.InventoryClosed();

        p_mouseLook.SetMouseLookOverride(false);
        p_mouseLook.SetCursorLock(true);

        p_Movement.SetMovementOverride(false);
    }

    public void PauseKeyPressed(InputAction.CallbackContext context)
    {
        if (!base.IsOwner) return;

        if (context.performed)
        {
            if (paused)
            {
                Unpause();
            }
            else
            {
                Pause();
            }
        }
    }

    private bool useCursorState;
    public void Unpause()
    {
        if(useCursorState == true)
        {
            p_mouseLook.SetCursorLock(true);
        }

        paused = false;
        pauseMenuInstance.SetActive(false);
    }

    public void Pause()
    {
        useCursorState = p_mouseLook.cursorLock;

        p_mouseLook.SetCursorLock(false);

        paused = true;
        pauseMenuInstance.SetActive(true);
    }
}
