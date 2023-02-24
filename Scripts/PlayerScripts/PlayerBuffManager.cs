using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using FishNet.Connection;
using TMPro;

public class PlayerBuffManager : NetworkBehaviour
{
    private PlayerAudioManager p_Audio;
    private PlayerResourcesScript p_Resources;
    private PlayerMouseLook p_Mouselook;
    private Transform p_Head;

    private PlayerDamageManager p_Damage;
    private PlayerReferencer p_reference;
    private PlayerMovementMangaer p_movement;

    [SerializeField] private float tickInterval;
    private float lastTick = 0f;
    private bool runTickCycle = false;

    //impleemnt this all later
    [Header("Available buffs")]
    public Buff[] allBuffs;
    private Dictionary<string, Buff> buffDictionary;

    [Header("Runtime")]
    public List<InstancedBuff> currentBuffs = new List<InstancedBuff>();

    //adjustments from buffs
    [HideInInspector]public int calculated_healthCap;
    [HideInInspector] public float calculated_speedAdjustment; //for adrenaline etc
    [HideInInspector] public float calculated_foodLossRate;
    [HideInInspector] public int calculated_foodCap;
    [HideInInspector] public float calculated_waterLossRate;
    [HideInInspector] public int calculated_waterCap;
    [HideInInspector] public bool calculated_blockSprinting;
    [HideInInspector] public bool calculated_blockJumping;
    [HideInInspector] public bool calculatedBlockPassiveRegeneration;

    [Header("Audio")]
    public AudioClip clip_buffIconEntered;
    public AudioClip clip_buffIconExit;

    // Start is called before the first frame update
    void Start()
    {
        //dictionary will allow use to access all the various components
        p_Audio = GetComponent<PlayerAudioManager>();
        p_Resources = GetComponent<PlayerResourcesScript>();
        p_Mouselook = GetComponent<PlayerMouseLook>();
        p_Damage = GetComponent<PlayerDamageManager>();
        p_reference = GetComponent<PlayerReferencer>();
        p_movement = GetComponent<PlayerMovementMangaer>();

        buffDictionary = new Dictionary<string, Buff>();
        foreach (Buff buff in allBuffs)
        {
            buffDictionary.Add(buff.buffName, buff);
        }
        //make sure everything works properly
        CalculateSetValues();
    }


    private GameObject buffUiHolder;
    private GameObject buffUiPiece;
    private GameObject inventoryBuffHolder;
    private GameObject inventoryBuffPiece;
    private GameObject buffDescriptionBox;
    private TextMeshProUGUI buffDescriptionText;
    private TextMeshProUGUI buffNameText;
    public void Initialise(UiReference uiRef)
    {
        buffUiHolder = uiRef.buffHolder;
        buffUiPiece = uiRef.buffPiece;
        inventoryBuffHolder = uiRef.inventoryBuffHolder;
        inventoryBuffPiece = uiRef.inventoryBuffPiece;
        buffDescriptionBox = uiRef.buffDescriptionBox;
        buffDescriptionText = uiRef.buffDescriptionText;
        buffNameText = uiRef.buffNameText;

        buffUiHolder.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!base.IsOwner) return;
        if (runTickCycle == true && currentBuffs.Count > 0 && Time.time > lastTick)
        {
            lastTick = Time.time + tickInterval;
            //run buff counter
            CalculateBuffValues();   
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            AddBuff("DevSpeed");
        }
    }


    void CalculateBuffValues()
    {
        if (runTickCycle == false) return;
        int toHealth = 0;
        int toRads = 0;

        foreach (InstancedBuff item in currentBuffs)
        {
            item.elapsedTime += tickInterval;
            item.tickCounter++;
            Buff useBuff = buffDictionary[item.buffName];

            item.b_Ui.durationText.text = (useBuff.duration - item.elapsedTime).ToString("0.0");
            item.inventory_b_UI.durationText.text = (useBuff.duration - item.elapsedTime).ToString("0.0");

            //check to adjust values
            if (item.tickCounter == useBuff.ticksPerEffect)
            {
                item.tickCounter = 0;

                if (useBuff.healthPerTick != 0)
                {
                    toHealth += useBuff.healthPerTick;
                }
                if (useBuff.radsPerTick != 0)
                {
                    toRads += useBuff.radsPerTick;
                }
            }

            if (item.elapsedTime >= useBuff.duration)
            {
                CancelBuff(item.buffName);
                return;
            }
        }
        if(toRads != 0)
        {
            p_Resources.AddValue(toRads, PlayerResourcesScript.ResourceType.radiation);
        }

        if (toHealth != 0)
        {
            p_Resources.AddHealth(toHealth);
        }
    }

    //these are for things that only happen on buff state change
    public void CalculateSetValues()
    { 
        if(currentBuffs.Count == 0)
        {
            calculated_blockJumping = false;
            calculated_blockSprinting = false;
            calculatedBlockPassiveRegeneration = false;

            calculated_foodCap = 0;
            calculated_foodLossRate = 1f;
            calculated_healthCap = 0;
            calculated_speedAdjustment = 1f;
            calculated_waterCap = 0;
            calculated_waterLossRate = 1f;
        }
        else
        {
            int foodCapCount = 0;
            int updatedFoodCap = 0;

            int waterCapCount = 0;
            int updatedWaterCap = 0;

            int healthCapCount = 0;
            int updatedHealthCap = 0;

            float updated_speedAdjustment = 0f;
            float updated_waterLossRate = 0f;
            float updated_foodLossRate = 0f;

            foreach (InstancedBuff item in currentBuffs)
            {
                Buff useBuff = buffDictionary[item.buffName];
                if(useBuff.foodCap != 0)
                {
                    foodCapCount++;
                    updatedFoodCap += useBuff.foodCap;
                }
                if (useBuff.waterCap != 0)
                {
                    waterCapCount++;
                    updatedWaterCap += useBuff.waterCap;
                }
                if (useBuff.healthCap != 0)
                {
                    healthCapCount++;
                    updatedHealthCap += useBuff.healthCap;
                }

                updated_foodLossRate += useBuff.foodLossRate;
                updated_speedAdjustment += useBuff.speedAdjustment;
                updated_waterLossRate += useBuff.waterLossRate;

                if (useBuff.blockJumping == true) calculated_blockJumping = true;
                if (useBuff.blockPassiveRegeneration == true) calculatedBlockPassiveRegeneration = true;
                if (useBuff.blockJumping == true) calculated_blockSprinting = true;
            }

            //averages the values
            calculated_foodLossRate = updated_foodLossRate / currentBuffs.Count;
            calculated_waterLossRate = updated_waterLossRate / currentBuffs.Count;
            calculated_speedAdjustment = updated_speedAdjustment / currentBuffs.Count;

            //calculatefinal values
            if(updatedFoodCap != 0) { calculated_foodCap = updatedFoodCap / foodCapCount; };
            if (updatedWaterCap != 0) { calculated_foodCap = updatedWaterCap / waterCapCount; };
            if (updatedHealthCap != 0) { calculated_foodCap = updatedHealthCap / healthCapCount; };
        }

        Debug.Log("calculated speed adjustment of " + calculated_speedAdjustment);

        //send notice of updated buff state
        p_Resources.UpdatedBuffState();
        p_movement.UpdatedBuffState();
    }

    public void CancelAllBuffs()
    {
        if (currentBuffs.Count == 0) return;
        PauseBuffClock();

        Debug.Log("Removed all buffs");

        foreach (InstancedBuff item in currentBuffs)
        {
            //we remove
            #region remove instanced parts
            if (item.createdUiObject != null)
            {
                Destroy(item.createdUiObject);
            }
            if (item.createdInventoryUiObject != null)
            {
                Destroy(item.createdInventoryUiObject);
            }
            if (item.instancedEffect != null)
            {
                Destroy(item.instancedEffect);
            }
            #endregion
        }
        currentBuffs = new List<InstancedBuff>();

        buffUiHolder.SetActive(false);
        inventoryBuffHolder.SetActive(false);
        buffDescriptionBox.SetActive(false);
        CalculateSetValues();
    }

    void BuffSound(AudioClip buffsound)
    {
        if (p_Audio == null || buffsound == null) return;
        p_Audio.PlayLocalAudioClip(buffsound);
    }

    public void CancelBuff(string name)
    {
        //check possible
        if (currentBuffs.Count == 0) return;

        List<InstancedBuff> cycleBuffs = currentBuffs;
        foreach (InstancedBuff item in cycleBuffs)
        {
            if(item.buffName == name)
            {
                //we remove
                if(currentBuffHover != null && currentBuffHover == buffDictionary[name])
                {
                    buffDescriptionBox.SetActive(false);
                    currentBuffHover = null;
                }

                #region remove instanced parts
                if (item.createdUiObject != null)
                {
                    Destroy(item.createdUiObject);
                }
                if (item.createdInventoryUiObject != null)
                {
                    Destroy(item.createdInventoryUiObject);
                }
                if(item.instancedEffect != null)
                {
                    Destroy(item.instancedEffect);
                }
                #endregion

                currentBuffs.Remove(item);
                BuffSound(buffDictionary[name].removedSound);
                if (currentBuffs.Count == 0)
                {
                    PauseBuffClock();
                    buffUiHolder.SetActive(false);
                    inventoryBuffHolder.SetActive(false);
                }
                break;
            }
        }
        CalculateSetValues();
    }

    public void PauseBuffClock()
    {
        runTickCycle = false;
    }

    public void PlayBuffClock()
    {
        runTickCycle = true;
    }

    public void ApplyBuff(string name, bool removeMode)
    {
        if (base.IsOwner)
        {
            if (removeMode == true)
            {
                CancelBuff(name);
            }
            else
            {
                //this is on for being called on local player
                AddBuff(name);
            }
        }
        else
        {
            if (!base.IsOwner)
            {
                 NetworkObject identity = GetComponent< NetworkObject>();
                if (IsServer)
                {
                    RpcTargetSendBuffToPlayer(identity.Owner, name, removeMode);
                }
                else
                {
                    p_reference.GetPlayer().GetComponent<PlayerBuffManager>().RelayBuffToServer(gameObject, name, removeMode);
                    //   CmdSendDamageToServer(damage, direction, dType, point);
                }
            }
        }
    }

    public void RelayBuffToServer(GameObject target, string buffName, bool removeMode)
    {
        CmdSendBuffToServer(target, buffName, removeMode);
    }

    [ServerRpc] void CmdSendBuffToServer(GameObject target, string buffName, bool removeMode)
    {
         NetworkObject identity = target.GetComponent< NetworkObject>();
        RpcTargetSendBuffToPlayer(identity.Owner, buffName, removeMode);
    }

    [TargetRpc] void RpcTargetSendBuffToPlayer(NetworkConnection target, string buffName, bool removeMode)
    {
        p_reference.GetPlayer().GetComponent<PlayerBuffManager>().ApplyBuff(buffName, removeMode);
    }

    private void AddBuff(string name)
    {
        if (buffDictionary.ContainsKey(name) == false) return;

        //if we currently have zero buffs
        if(currentBuffs.Count == 0)
        {
            PlayBuffClock();
            buffUiHolder.SetActive(true); 
            inventoryBuffHolder.SetActive(true);
        }

        //checks possible (we dont want to double up the same buff)
        if (currentBuffs.Count > 0)
        {
            foreach (InstancedBuff item in currentBuffs)
            {
                if(item.buffName == name)
                {
                    //we refresh duration
                    item.elapsedTime = 0f;
                    
                    return;
                }
            }
        }
        //add the buff
        InstancedBuff buffInstance = new InstancedBuff();
        buffInstance.buffName = name;

        Buff useBuff = buffDictionary[name];

        GameObject uiInstance = Instantiate(buffUiPiece, buffUiHolder.transform);
        buffInstance.createdUiObject = uiInstance;
        buffInstance.b_Ui = uiInstance.GetComponent<BuffPieceUiScript>();
        buffInstance.b_Ui.icon.sprite = useBuff.buffIcon;
        buffInstance.b_Ui.durationText.text = useBuff.duration.ToString();
        buffInstance.b_Ui.backingImage.color = useBuff.buffBackingColor;

        GameObject inventoryuiInstance = Instantiate(inventoryBuffPiece, inventoryBuffHolder.transform);
        buffInstance.createdInventoryUiObject = inventoryuiInstance;
        buffInstance.inventory_b_UI = inventoryuiInstance.GetComponent<BuffInventoryUiScript>();
        buffInstance.inventory_b_UI.icon.sprite = useBuff.buffIcon;
        buffInstance.inventory_b_UI.durationText.text = useBuff.duration.ToString();
        buffInstance.inventory_b_UI.backingImage.color = useBuff.buffBackingColor;
        buffInstance.inventory_b_UI.p_buffManager = this;
        buffInstance.inventory_b_UI.assotiatedBuff = name;

        //create various components
        currentBuffs.Add(buffInstance);
        CalculateSetValues();
        BuffSound(useBuff.addedSound);
    }

    private Buff currentBuffHover;
    public void BuffEnter(BuffInventoryUiScript buffPiece)
    {
        Buff useBuff = buffDictionary[buffPiece.assotiatedBuff];
        currentBuffHover = useBuff;

        buffDescriptionBox.SetActive(true);
        p_Audio.PlayLocalAudioClip(clip_buffIconExit);

        buffDescriptionText.text = useBuff.description;
        buffNameText.text = useBuff.buffName;
        buffDescriptionBox.transform.position = buffPiece.transform.position;
    }
    public void BuffExit(BuffInventoryUiScript buffPiece)
    {
        p_Audio.PlayLocalAudioClip(clip_buffIconExit);
        currentBuffHover = null;
        buffDescriptionBox.SetActive(false);
    }

    public void InventoryClosed()
    {
        buffDescriptionBox.SetActive(false);
        currentBuffHover = null;
    }
}

//an instance of a buff created
public class InstancedBuff
{
    public string buffName;
    public int tickCounter = 0;
    public float elapsedTime;

    public GameObject createdUiObject;
    public GameObject createdInventoryUiObject;

    public BuffPieceUiScript b_Ui;
    public BuffInventoryUiScript inventory_b_UI;

    public GameObject instancedEffect;
}

//note buffs can also be debuffs just so you know
[System.Serializable]
public class Buff
{
    public string buffName;
    [TextArea]
    public string description;
    [Space]
    [Header("Adjustments")]
    public int healthPerTick;
    public int radsPerTick;
    [Space]
    public int healthCap;
    public bool blockPassiveRegeneration;
    [Space]
    public float speedAdjustment = 1f; //for adrenaline etc
    public float foodLossRate = 1f;
    public int foodCap;
    public float waterLossRate = 1f;
    public int waterCap;
    [Space]
    public bool blockSprinting;
    public bool blockJumping;
    [Space]

    public bool impluseCamOnTick;
    public float impluseStrength = 3f;

    [Header("Timing")]
    public float duration;

    [Tooltip("each tick is 0.1 seconds, this is for how many until effect happens")] public float ticksPerEffect;

    [Header("Audio and Visuals")]
    public Sprite buffIcon;
    public Color buffBackingColor;
    [Space]
    public AudioClip addedSound;
    public AudioClip removedSound;
    //screen effects for later implementation
    
}
