using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using FishNet.Connection;

public class PlayerDamageManager : NetworkBehaviour, IDamageable
{
    private bool initialised = false;

    private ItemReference allitems;

    private PlayerResourcesScript p_Resources;
    private PlayerMovementMangaer p_Movement;
    private PlayerMouseLook p_Mouselook;
    private ClothingInventoryManager p_Clothing;
    private InventoryManager p_Inventory;
    private PlayerMenuManager p_Menu;
    private PlayerScript p_Script;
    private HotBarManager p_hotbar;
    private EquipManager p_Equiped;
    private PlayerBuffManager p_buffManager;

    private PlayerReferencer p_referencer;

    private RectTransform damageIndicator;
    private float damageIndOffset = 150f;


    [Header("Audio visual effects")]
    
    public GameObject[] bloodSplatterEffects;
    private GameObject bloodScreenEffect;

    public float cameraDamageImpluse = 1f;

    private int maxArmor = 200;

    private Slider armorSlider;
    private Slider menuArmorSlider;

    [Header("Audio")]
    public AudioClip dieSound;

    public AudioClip[] takeDamageSounds;
    private PlayerAudioManager p_Audio;

    [Header("Death Components")]
    public float deathPushMultiplayer = 0.7f;
    [SerializeField] private GameObject[] ragdollJoints;
    
    [Space]
    //we look at this when die
    [SerializeField] private Transform lookToBone;
    [SerializeField] private float lookToSpeed = 5f;

    [Space]
    [SerializeField] private SkinnedMeshRenderer handsMesh; //the root of all equiped objects
    public GameObject playerDropObject;

    [HideInInspector]public bool isDead = false;

    private GameObject gameUiHolder;
    private GameObject deathUiPanel;

    private GameObject[] playerCollisionSources;
    private Transform[] meshRepHolders;

    public Animator multiplayerRepAnimator;

    [Header("Respawning")]
    private Transform[] worldRespawns;
    public Transform overrideRespawn; //for if player slept in a bed etc

    [Header("Extras")]
    [SerializeField] public float bleedChance = 0.3f;

    public void InitialiseAllItems(ItemReference iref)
    {
        allitems = iref;
    }

    // Start is called before the first frame update
    void Start()
    {
        //we disable all ragdoll parts
        foreach  (GameObject g in ragdollJoints)
        {
            g.GetComponent<Collider>().enabled = false;
            g.GetComponent<Rigidbody>().isKinematic = true;
        }

        p_Resources = GetComponent<PlayerResourcesScript>();
        p_Movement = GetComponent<PlayerMovementMangaer>();
        p_Mouselook = GetComponent<PlayerMouseLook>();
        p_Clothing = GetComponent<ClothingInventoryManager>();
        p_Inventory = GetComponent<InventoryManager>();
        p_Menu = GetComponent<PlayerMenuManager>();
        p_Script = GetComponent<PlayerScript>();
        p_Equiped = GetComponent<EquipManager>();
        p_hotbar = GetComponent<HotBarManager>();
        p_Audio = GetComponent<PlayerAudioManager>();
        p_buffManager = GetComponent<PlayerBuffManager>();
        p_referencer = GetComponent<PlayerReferencer>();

        playerCollisionSources = p_Script.playerCollisionSources;
        meshRepHolders = p_Script.meshRepHolders;
        
                    //get world spawns
        GameObject[] worldPoints = GameObject.FindGameObjectsWithTag("WorldSpawn");
        List<Transform> eWorldSp = new List<Transform>();
        foreach (GameObject g in worldPoints)
        {
            eWorldSp.Add(g.transform);
        }
        worldRespawns = eWorldSp.ToArray(); 
    }

    // Update is called once per frame
    void Update()
    {
        if (!base.IsOwner) return;
        //rotate camera towards dead body
        if (isDead)
        {
            p_Mouselook.LookTowardPoint(lookToBone.position, lookToSpeed);
        }
    }

    //for debuggin damage
    public void HKeyPressed(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Vector3 dir = transform.forward;
            int rand = Random.Range(0, 4);
            if (rand == 1)
            {
                dir = transform.right;
                Debug.Log("right");
            }
            else
            {
                if (rand == 2)
                {
                    dir = -transform.forward;
                    Debug.Log("back");
                }
                else
                {
                    if (rand == 3)
                    {
                        dir = -transform.right;
                        Debug.Log("left");
                    }
                    else
                    {
                        Debug.Log("forward");
                    }
                }
            }
            GetComponent<IDamageable>().TakeDamage(Random.Range(0f, 20f), dir, ItemData.DamageType.blunt, transform.position);
        }
    }

    public void Initialise(UiReference uiRef)
    {
        initialised = true;

        damageIndicator = uiRef.damageIndicator;
        damageIndicator.gameObject.SetActive(false);

        gameUiHolder = uiRef.gameUI;
        deathUiPanel = uiRef.deathUiPanel;

        bloodScreenEffect = uiRef.bloodScreenPanel;

        uiRef.respawnButton.onClick.AddListener(RespawnButtonPressed);

        armorSlider = uiRef.armorSlider;
        menuArmorSlider = uiRef.menuarmorSlider;
    }
    //setup damage affectors
    private int curArmorAmount;
    public void UpdateArmoring(int armorValue)
    {
        curArmorAmount = armorValue;
        armorSlider.maxValue = maxArmor;
        armorSlider.value = curArmorAmount;
        menuArmorSlider.maxValue = maxArmor;
        menuArmorSlider.value = curArmorAmount;
    }
    //taking the damage
    public void TakeDamage(float damage, Vector3 direction, ItemData.DamageType dType, Vector3 point)
    {
        //calculate the damage
        if (isDead == true) return;
        if (base.IsOwner)
        {
            float damageMultiplier = 1 - (curArmorAmount / maxArmor);
            int calculatedDamage = (int)Mathf.RoundToInt(damage * damageMultiplier);
            int curhealth = p_Resources.CurHealth();

            curhealth -= calculatedDamage;
            Debug.Log("take damage" + damage + "calculated " + calculatedDamage + "og health " + p_Resources.CurHealth() + "health val " + curhealth);
            if (curhealth < 0) curhealth = 0;

            p_Resources.CmdChangeHealth(curhealth);
            p_Resources.UpdateValues();

            //check for death
            if (curhealth <= 0)
            {
                Die(point, damage * deathPushMultiplayer);
            }

            //check for bleeding 
            if (dType == ItemData.DamageType.penetration || dType == ItemData.DamageType.slice)
            {
                float checkBleedChance = Random.Range(0, 1f);
                if (checkBleedChance < bleedChance)
                {
                    //bleed
                    p_buffManager.ApplyBuff("Bleeding", false);
                }
            }

            //create local effects
            p_Mouselook.RecoilCamera(new Vector2(Random.Range(-cameraDamageImpluse, cameraDamageImpluse), Random.Range(-cameraDamageImpluse, cameraDamageImpluse)), 8f);

            //we make network damage effects
            if (IsServer)
            {
                RpcNetworkPlayerDamage(direction, point);
            }
            else
            {
                CmdSendNetworkDamage(direction, point);
            }

            //make damage indicator
            if (damageIndicatorCoroutine != null)
            {
                StopCoroutine(damageIndicatorCoroutine);
            }

            damageIndicatorCoroutine = StartCoroutine(DamageIndicator(new Vector2(transform.forward.x, transform.forward.z), new Vector2(direction.x, direction.z)));

            //sound
            AudioClip toTakeSound = takeDamageSounds[Random.Range(0, takeDamageSounds.Length - 1)];
            p_Audio.PlayLocalAudioClip(toTakeSound);
            p_Resources.TookDamage();
        }
        else
        {
          
            //we send to base player yoooo
            if (!base.IsOwner)
            {
                 NetworkObject identity = GetComponent< NetworkObject>();
                if (IsServer)
                {
                    TargetRpcRunLocalDamage(identity.Owner, damage, direction, dType, point);
                }
                else
                {                   
                    p_referencer.GetPlayer().GetComponent<PlayerDamageManager>().RelayDamageCommandToServer(gameObject, damage, direction, dType, point);
                 //   CmdSendDamageToServer(damage, direction, dType, point);
                }
            }
        }
    }
    #region targeting correct client
    public void RelayDamageCommandToServer(GameObject target, float damage, Vector3 direction, ItemData.DamageType dType, Vector3 point)
    {
        CmdSendDamageToServer(target, damage, direction, dType, point);
    }
    [ServerRpc] void CmdSendDamageToServer(GameObject target,float damage, Vector3 direction, ItemData.DamageType dType, Vector3 point)
    {
         NetworkObject identity = target.GetComponent< NetworkObject>();
        TargetRpcRunLocalDamage(identity.Owner, damage, direction, dType, point);
    }
    [TargetRpc]
    public void TargetRpcRunLocalDamage(NetworkConnection target, float damage, Vector3 direction, ItemData.DamageType dType, Vector3 point)
    {
        p_referencer.GetPlayer().GetComponent<PlayerDamageManager>().TakeDamage(damage, direction, dType, point);
    }
    #endregion

    [ServerRpc] void CmdSendNetworkDamage(Vector3 dir, Vector3 point) { RpcNetworkPlayerDamage(dir, point); }

    [ObserversRpc]
    void RpcNetworkPlayerDamage(Vector3 direction, Vector3 point)
    {
        if (base.IsOwner) return;
        //for implementing at later point
    }

    //for damage effect
    private Coroutine damageIndicatorCoroutine;
    private IEnumerator DamageIndicator(Vector2 forwardface, Vector2 inputDirection)
    {
        //randomly change image for variation
        bloodScreenEffect.transform.localScale = Vector3.one;
        float chanceX = Random.Range(0, 1f);
        float chanceY = Random.Range(0, 1f);
        if (chanceX > 0.5f) bloodScreenEffect.transform.localScale = new Vector3(-1f, bloodScreenEffect.transform.localScale.y, 1);
        if (chanceY > 0.5f) bloodScreenEffect.transform.localScale = new Vector3(bloodScreenEffect.transform.localScale.x, -1f, 1);
        bloodScreenEffect.SetActive(true);

        //damage indicator
        damageIndicator.gameObject.SetActive(true);

        float dot = Vector2.Dot(forwardface.normalized, inputDirection.normalized);
        float rotationFactor = 0f;

        //fuck this code it sucks
        Vector2 setPosition = new Vector2();
        if (dot < 0)
        {
            setPosition.y = -damageIndOffset;
            rotationFactor = 180f;
        }
        else
        {
            setPosition.y = damageIndOffset;
        }
        int xMultiplier = 1;
        if (Vector2.Dot(inputDirection.normalized, new Vector2(transform.right.x, transform.right.z).normalized) < 0)
        {
            xMultiplier = -1;
        }
        //we check if closer to left or right
        if (Mathf.Abs(dot) < 0.5f)
        {
            setPosition.x = damageIndOffset;
            rotationFactor = -90 * xMultiplier;
            setPosition.y = 0;
        }
        setPosition.x *= xMultiplier;
        damageIndicator.localPosition = setPosition;
        damageIndicator.localRotation = Quaternion.Euler(0f, 0f, rotationFactor);

        yield return new WaitForSeconds(0.1f);

        damageIndicator.gameObject.SetActive(false);
        bloodScreenEffect.SetActive(false);
    }

    //dying
    public void Die(Vector3 point, float deathForce)
    {
        if (!initialised || !base.IsOwner) return;

        //ya deds
        isDead = true;
        //rectify inventory
        if(p_Menu.inventoryOpen == true)
        {
            p_Menu.CloseInventory();
        }
        p_Audio.PlayLocalAudioClip(dieSound);

        //ui managing
        gameUiHolder.SetActive(false);
        deathUiPanel.SetActive(true);

        //set cursor and movement states
        p_Mouselook.SetCursorLock(false);
        p_Movement.ReturnToStand();

        p_Clothing.SetClothCorpseLayer(); //not working for some reason right now
      
        //clear local things
        p_Equiped.HandsSwitch();
        SetHandsLayerVisible(false);

        p_buffManager.CancelAllBuffs();

        //handle inventory
        Debug.Log("start inventoryHandling");
        List<SerialisedInventoryItem> inventoryItems = p_Inventory.GetAllInventoryItems();

        if (inventoryItems.Count > 0)
        {
            
            //create object with all items from inventory
            SerialisedInventoryItem lastItem = inventoryItems[0];
            if (inventoryItems.Count > 1)
            {
                lastItem = inventoryItems[inventoryItems.Count - 1];
            }

            ItemData refItem = allitems.allItems[lastItem.savedInstance.id];
            int maxY = refItem.slotSpaceY;

            int xWidth = p_Inventory.GetWidth();

            if (refItem.slotSpaceY < refItem.slotSpaceX)
            {
                maxY = refItem.slotSpaceX;
            }
            int toCreateY = lastItem.yLocation + maxY; //we use this for the max height of the players droped items
            Debug.Log("Retrieved invenotry data");

            CreatePlayerDrop(xWidth, toCreateY, inventoryItems);//actually create the drop

            //resets inventory
            p_Inventory.ClearInventory();
        }
        p_hotbar.ClearHotBar();

        //make player body ragdoll
        if (IsServer)
        {
            RpcMultiplayerKillPlayer(point,  deathForce);
        }
        else
        {
            CmdDie(point, deathForce);         
        }

    } 

    //create the drop object
    [ServerRpc]
    void CreatePlayerDrop(int xVal,int yVal, List<SerialisedInventoryItem> useList)
    {
        Debug.Log("Spawning player drop");
        GameObject createdObject = Instantiate(playerDropObject, transform.position, transform.rotation);
        StorageHoldObject s_Hold = createdObject.GetComponent<StorageHoldObject>();
        s_Hold.SetSize(xVal, yVal);
        s_Hold.Setitems(useList);

        ServerManager.Spawn(createdObject);
    }

    //these are for doing the death specific funcitons in multiplayer
    [ServerRpc]
    void CmdDie(Vector3 point, float force)
    {
        RpcMultiplayerKillPlayer(point, force);
    }
    [ObserversRpc] void RpcMultiplayerKillPlayer(Vector3 point, float force)
    {
        isDead = true;

        //sets layer to corpse so we can see the player body
        foreach (Transform trans in meshRepHolders)
        {
            trans.gameObject.layer = 15;
        }

        //sets all colliders to right layer
        foreach (GameObject col in playerCollisionSources)
        {
            col.SetActive(false);
        }

        DoRagDoll( point, force);
    }

    void SetHandsLayerVisible(bool value)
    {
        if (value == true)
        {
            handsMesh.enabled = true;
        }
        else
        {
            handsMesh.enabled = false;
        }
    }

    void DoRagDoll(Vector3 point, float force)
    {
        //disables animator
        multiplayerRepAnimator.enabled = false;

        foreach (GameObject g in ragdollJoints)
        {
            g.layer = 15;
            g.GetComponent<Collider>().enabled = true;
            g.GetComponent<Rigidbody>().isKinematic = false;
        }

        //recoils colliders
        Collider[] hitColliders = Physics.OverlapSphere(point, 1f, 15);
        foreach (Collider item in hitColliders)
        {
            if(item.gameObject.GetComponent<Rigidbody>() != null)
            {
                item.gameObject.GetComponent<Rigidbody>().AddExplosionForce(force, point, 3f);
            }
        }

    }

    void CancelRagDoll()
    {
        //disables animator
        multiplayerRepAnimator.enabled = true;

        foreach (GameObject g in ragdollJoints)
        {
            g.layer = 6;
            g.GetComponent<Collider>().enabled = false;
            g.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    //we do the respawning
    public void RespawnButtonPressed()
    {
        LocalRespawn();

        if (IsServer)
        {
            RpcRespawn();
        }
        else
        {
            CmdRespawn();
        }
    }

    void LocalRespawn()
    {
        p_Clothing.ClearClothing(); //call from local beacause has separate rpc call for function
        isDead = false;

        //send to postiion
        Transform targetRPoint = worldRespawns[Random.Range(0, worldRespawns.Length - 1)];
        if (overrideRespawn != null) targetRPoint = overrideRespawn;

        transform.position = targetRPoint.position; //sets our position

        //sets health to max etc
        p_Resources.CmdSetDefaultValues();

        //ui managing
        gameUiHolder.SetActive(true);
        deathUiPanel.SetActive(false);

        //set cursor and movement states
        p_Mouselook.SetCursorLock(true);

        //for items
        SetHandsLayerVisible(true);
    }
    [ServerRpc] void CmdRespawn()
    { RpcRespawn(); }

    [ObserversRpc] void RpcRespawn()
    {
        isDead = false;
        CancelRagDoll();

        //set mesh layers
        if (base.IsOwner)
        {
            foreach (Transform trans in meshRepHolders)
            {
                trans.gameObject.layer = 7;
            }
        }
        else
        {
            foreach (Transform trans in meshRepHolders)
            {
                trans.gameObject.layer = 6;
            }
        }

        //sets all colliders to right layer
        foreach (GameObject col in playerCollisionSources)
        {
            col.SetActive(true);
        }
    }
}
