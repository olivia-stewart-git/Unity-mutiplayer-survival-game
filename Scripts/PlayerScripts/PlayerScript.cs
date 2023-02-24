using FishNet.Object;
using UnityEngine;
using FishNet.Object.Synchronizing;

public class PlayerScript : NetworkBehaviour
{
    [Header("BodyPieces")]
    public GameObject[] playerCollisionSources;
    public Transform[] meshRepHolders;

    public TextMesh playerNameText;
    public GameObject floatingInfo;

    private Material playerMaterialClone;
    private PlayerAudioManager p_Audio;

    public GameObject playerArmMesh;
    public PlayerAudioManager GetAudioManager()
    {
        return p_Audio;
    }

    [SerializeField]
    private GameObject playerRepresentor;
    [SerializeField]
    private InventoryManager playerInventory;
    [SerializeField]
    private GameObject playerInputManager;
    [SerializeField]
    private PlayerMenuManager p_Menu;
    [SerializeField]
    private PlayerResourcesScript p_Resources;
    [SerializeField]
    private InteractionInfoController p_Interactor;
    [SerializeField]
    private ClothingInventoryManager p_clothingManager;
    [SerializeField]
    private PlayerRepresentorManager p_characterRepresentor;
    [SerializeField]
    private HotBarManager p_hotbar;
    [SerializeField]
    private InventoryMenuScript i_ContextMenu;
    [SerializeField]
    private CrosshairManager crosshairManager;
    [SerializeField]
    private WeaponSway w_Sway;
    [SerializeField]
    private PlayerFootStepManager p_Footsteps;
    [SerializeField]
    private PlayerDamageManager p_Damage;
    [SerializeField]
    private PlayerBuildingManager p_BuildManager;

    private HarvestingManager h_Manager;

    [SerializeField]
    private CraftingManager craftingmanager;

    private ItemReference itemReference;

    private PlayerInteracter pInteractor;

    [SerializeField]
    private GameObject playerUi;
    private UiReference uiRef;

    [SerializeField]
    private GameObject arms;

    private PlayerProjectileManager p_Projectile;

    private PlayerBuffManager p_buffmanager;

    private EquipManager equipManager;

    [SyncVar(OnChange = nameof(OnNameChanged))]
    public string playerName;

    [SyncVar(OnChange = nameof(OnColorChanged))]
    public Color playerColor = Color.white;

    [Header("PlayerComponents")]
    [SerializeField]
    private PlayerMovementMangaer p_Movement;
    [SerializeField]
    private PlayerMouseLook p_MouseLook;
    [SerializeField]
    private Transform p_CameraPos;

    void OnNameChanged(string _Old, string _New, bool asServer)
    {
        playerNameText.text = playerName;
    }

    void OnColorChanged(Color _Old, Color _New, bool asServer)
    {
        playerNameText.color = _New;
        // playerMaterialClone = new Material(playerRepresentor.GetComponent<Renderer>().material);
        // playerMaterialClone.color = _New;
        //  playerRepresentor.GetComponent<Renderer>().material = playerMaterialClone;
    }

    private Transform createdObjectsHolder;
    public void InitialisePlayer(ItemReference iRef)
    {
        itemReference = iRef;

        p_Projectile = GetComponent<PlayerProjectileManager>();
        p_Projectile.SetItemReference(iRef);

        equipManager = GetComponent<EquipManager>();
        equipManager.SetItemReference(iRef);

        p_BuildManager.InitialiseItems(iRef);

        p_hotbar.SetItemReference(iRef);
        i_ContextMenu.SetItemReference(iRef);
        p_characterRepresentor.SetItemReference(iRef);
        playerInventory.SetItemReference(iRef);
        GetComponent<CraftingManager>().SetItemReference(iRef);

        if (base.IsOwner)
        {
            //establish ui
            createdObjectsHolder = GameObject.Find("CreatedObjectHolder").transform;
            GameObject uiInstance = Instantiate(playerUi, createdObjectsHolder);
            uiRef = uiInstance.GetComponent<UiReference>();

            //initalizes movementstate
            p_Movement.InitializeValues();
            p_MouseLook.InitialiseValues();

            p_MouseLook.SetCursorLock(true);

            p_Damage.InitialiseAllItems(iRef);
            p_Damage.Initialise(uiRef);
            p_Resources.InitializeResources(uiRef);
            p_Menu.InitialiseValues(uiRef);
            playerInventory.InitialiseInventoryManager(uiRef);

            pInteractor = GetComponent<PlayerInteracter>();
            pInteractor.Initialise();

            p_Interactor.InitialiseInteractUi(uiRef);


            p_clothingManager.InitializeClothingSlots(uiRef);
            p_characterRepresentor.InitialiseUiCharacter(uiRef);
            p_hotbar.InitialiseHotBar(uiRef);
            i_ContextMenu.InitialiseInventoryMenu(uiRef);
            crosshairManager.Initialise(uiRef);
            w_Sway.Initialise();
            p_Footsteps.Initialise(p_Audio);
            craftingmanager.Initialise(uiRef);
            p_buffmanager = GetComponent<PlayerBuffManager>();
            p_buffmanager.Initialise(uiRef);

            p_BuildManager.InitialiseUi(uiRef);

            h_Manager = GetComponent<HarvestingManager>();
            h_Manager.InitialiseManager(iRef);

        Debug.Log("initialised player");
        }
    }

    public ItemReference GetItemReference()
    {
        return itemReference;
    }

    public override void OnStartClient()
    {
        //player loaded
        LoadManager l_Manager = GameObject.Find("LoadManager").GetComponent<LoadManager>();
        if (l_Manager == null)
        {
            Debug.Log("null loader");
            return;
        }
        if (!base.IsOwner)
        {
            playerArmMesh.layer = 7;
            l_Manager.OnPlayerLoaded(gameObject);
            return;
        }

        //we set layer
        Debug.Log("Player created");
        SetCollisionLayers();

        Camera.main.transform.SetParent(p_CameraPos);
        Camera.main.transform.localPosition = new Vector3(0, 0, 0);
        Camera.main.transform.localRotation = Quaternion.identity;

        // floatingInfo.transform.localPosition = new Vector3(0, 3f, 0.6f);
        floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        floatingInfo.layer = 7;

        string name = "Player" + Random.Range(100, 999);
        Color color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));
        CmdSetupPlayer(name, color);

        Transform playerHolder = GameObject.Find("PlayerContainer").transform;
        transform.SetParent(playerHolder, true);

        //sets layers yo yo
        foreach (Transform item in meshRepHolders)
        {
            item.gameObject.layer = 7;
        }

        l_Manager.OnPlayerLoaded(gameObject);
    }

    [ServerRpc]
    public void CmdSetupPlayer(string _name, Color _col)
    {
        // player info sent to server, then server updates sync vars which handles it on all clients
        playerName = _name;
        playerColor = _col;
    }

    private void Awake()
    {
        //set up audio
        p_Audio = GetComponent<PlayerAudioManager>();
    }

    void Update()
    {
        if (!base.IsOwner)
        {
            // make non-local players run this
            floatingInfo.transform.LookAt(Camera.main.transform);
            return;
        }

        //updates the movement for our player
        p_Movement.UpdateMovement();
        w_Sway.UpdateSway();
        p_MouseLook.MouseLook();
    }

    void SetCollisionLayers()
    {
        if (base.IsOwner)
        {
            //SetLocalPlayerCollisionBox
            foreach (GameObject colObject in playerCollisionSources)
            {
                colObject.layer = 13;
            }
        }
        else
        {
            //SetLocalPlayerCollisionBox
            foreach (GameObject colObject in playerCollisionSources)
            {
                colObject.layer = 14;
            }
        }
    }

    public void RequestDisconnect()
    {

    }

    public void RecieveDamageCall(GameObject damageInterface, float damage, Vector3 direction, ItemData.DamageType dtype, Vector3 point)
    {
        if (damageInterface.GetComponent<NetworkObject>() == null)
        {
            damageInterface.GetComponent<IDamageable>().TakeDamage(damage, direction, dtype, point);
        }
        else
        {
            CmdSendDamage(damageInterface, damage, direction, dtype, point);
        }
    }
    [ServerRpc] void CmdSendDamage(GameObject networkId, float damage, Vector3 direction, ItemData.DamageType dtype, Vector3 point)
    {
        networkId.GetComponent<IDamageable>().TakeDamage(damage, direction, dtype, point);
    }
}



