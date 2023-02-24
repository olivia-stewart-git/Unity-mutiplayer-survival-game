using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;

public class PlayerResourcesScript : NetworkBehaviour
{
    public enum ResourceType { health, stamina, food, water, radiation };
    private bool initialised = false;

    [SyncVar] private int curHealth;

    [SerializeField] private PlayerMovementMangaer p_Movement;
    private PlayerBuffManager p_BuffManager;

    private PlayerDamageManager p_Damage;

    [Header("Values")]
    [SerializeField] private int maxHealth;
    [Space]
   private float curStamina;
    [SerializeField] private int maxStamina;
    [SerializeField] private float staminaRegenSpeedMultiplier = 1f;
    [SerializeField] private float staminaRegenGap = 1f;
    [SerializeField] private bool canRegenStamina = true;
    [SerializeField] private float sprintStaminaReduceSpeed = 1f;
    private Coroutine staminaReduceCoroutine;
    private bool isLerpingStamina = false;
    private float lastStaminaUse;
    [Space]

   private int curFood;
    [SerializeField] private int maxFood;
    private bool doReduceFood = true;
    [SerializeField] private float desiredTimeToreachFood = 50;
    private bool isLerpingFood = false;
    private Coroutine foodCoroutine;
    [Space]

     private int curWater;
    [SerializeField] private int maxWater;
    private bool doReduceWater = true;
    [SerializeField] private float desiredTimeToreachWater = 50;
    private bool isLerpingWater = false;
    private Coroutine waterCoroutine;

    [Space]
     private int curRadiation;
    [SerializeField] private int maxRadiation;

    //ui values
    private TextMeshProUGUI healthText;
    private TextMeshProUGUI staminaText;
    private TextMeshProUGUI foodText;
    private TextMeshProUGUI waterText;
    private TextMeshProUGUI radiationText;

    private Slider healthSlider;
    private Slider staminaSlider;
    private Slider foodSlider;
    private Slider waterSlider;
    private Slider radiationSlider;
    private Slider healthFollowSlider;


    private void Awake()
    {
        CalculateCompleteRates();
    }
    private void Update()
    {
        if (!base.IsOwner) return;
        if (!initialised) return;

        //handle stamina
        if(p_Movement.RetrieveMoveState() == PlayerMovementMangaer.MovementState.run && curStamina != 0)
        {
            if (isLerpingStamina == false)
            {
                float timeToUse = (curStamina / maxStamina) * sprintStaminaReduceSpeed;
                staminaReduceCoroutine = StartCoroutine(LerpStaminaOverSeconds(curStamina, timeToUse));
                isLerpingStamina = true;
            }
            //decrease stamina
            lastStaminaUse = (float)NetworkManager.TimeManager.TicksToTime() + staminaRegenGap;
        }
        else
        {
            if(staminaReduceCoroutine != null)
            {
                StopCoroutine(staminaReduceCoroutine);
            }
            isLerpingStamina = false;
        }

        if(curStamina != maxStamina && (float)NetworkManager.TimeManager.TicksToTime() > lastStaminaUse && canRegenStamina && isLerpingStamina == false)
        {
            LerpStamina(maxStamina, staminaRegenSpeedMultiplier);
        }

        //for food reduction
        if (doReduceFood == true && curFood != 0 && isLerpingFood == false)
        {
            float timeToUse = (curFood / combinedFoodCap) * combinedFoodLoss;
            foodCoroutine = StartCoroutine(LerpFoodOverSeconds(curFood, timeToUse));
            isLerpingFood = true;
        }

        //for reducing water
        if (doReduceWater == true && curWater != 0 && isLerpingWater == false)
        {
            float timeToUse = (curWater / combinedWaterCap) * combinedWaterLoss;
            waterCoroutine = StartCoroutine(LerpWaterOverSeconds(curWater, timeToUse));
            isLerpingWater = true;
        }

        //update sldier
        if(healthFollowSlider.value != curHealth)
        {
            healthFollowSlider.value = Mathf.Lerp(healthFollowSlider.value, curHealth, Time.deltaTime * 8f);
        }

        //check health regen
        if(buffBlockPassiveRegeneration == false && curHealth < combinedHealthCap && p_Damage.isDead == false && Time.time > lastDamageTime && curFood > (combinedFoodCap * aboveRatioToRegen) && curWater > (combinedWaterCap * aboveRatioToRegen)) 
        {
            if (healthRegenInProgess == false)
            {
                StartHealthRegen();
            }
        }
        else
        {
            if (healthRegenInProgess == true)
            {
                StopHealthRegen();
            }
        }
        //actually do regen
        if(healthRegenInProgess == true && Time.time > lastRegenTime)
        {
            lastRegenTime = Time.time + 1f;
            AddHealth(passiveHealthRegenPerSecond);
        }
    }

    //health regen
    private bool healthRegenInProgess = false;

    void StartHealthRegen()
    {
        if (p_Damage.isDead || !base.IsOwner) return;
        healthRegenInProgess = true;
        lastRegenTime = Time.time + 1f;
    }

    void StopHealthRegen()
    {
        if (p_Damage.isDead || !base.IsOwner) return;
        healthRegenInProgess = false;
    }

    private void Start()
    {
        p_BuffManager = GetComponent<PlayerBuffManager>();
    }

    #region buffhandling

    private float combinedWaterLoss;
    private float combinedFoodLoss;

    private int combinedFoodCap;
    private int combinedWaterCap;
    private int combinedHealthCap;

    private bool buffBlockPassiveRegeneration = false;
    private float buffFoodLossRate = 1f;
    private float buffWaterLossRate = 1f;
    private int buffMaxHealth = 0;
    private int bufffoodCap = 0;
    private int buffwaterCap = 0;
    public void UpdatedBuffState()
    {
        buffBlockPassiveRegeneration = p_BuffManager.calculatedBlockPassiveRegeneration;
        buffFoodLossRate = p_BuffManager.calculated_foodLossRate;
        buffWaterLossRate = p_BuffManager.calculated_waterLossRate;
        buffMaxHealth = p_BuffManager.calculated_healthCap;
        bufffoodCap = p_BuffManager.calculated_foodCap;
        buffwaterCap = p_BuffManager.calculated_waterCap;

        if(buffwaterCap < curWater && buffwaterCap != 0)
        {
            if (waterCoroutine != null)
            {
                StopCoroutine(waterCoroutine);
            }

            isLerpingWater = false;
            curWater = buffwaterCap;
        }
        if(bufffoodCap < curFood && buffwaterCap != 0)
        {
            if (foodCoroutine != null)
            {
                StopCoroutine(foodCoroutine);
            }

            isLerpingFood = false;
            curFood = bufffoodCap;
        }

        if(buffMaxHealth != 0 && buffMaxHealth < curHealth)
        {
            CmdChangeHealth(buffMaxHealth);
        }

        CalculateCompleteRates();
    }  

    void CalculateCompleteRates()
    {
        combinedFoodLoss = (buffFoodLossRate * desiredTimeToreachFood);
        combinedWaterLoss = (buffWaterLossRate * desiredTimeToreachWater);

        if(buffwaterCap == 0)
        {
            combinedWaterCap = maxWater;
        }
        else
        {
            combinedWaterCap = buffwaterCap;
        }
        if (bufffoodCap == 0)
        {
            combinedFoodCap = maxFood;
        }
        else
        {
            combinedFoodCap = bufffoodCap;
        }
        if (buffMaxHealth == 0)
        {
            combinedHealthCap = maxHealth;
        }
        else
        {
            combinedHealthCap = buffMaxHealth;
        }
    }
    #endregion

    //making stamina read only
    public float RetrieveCurStamine()
    {
        return curStamina;
    }
    public IEnumerator LerpFoodOverSeconds(float start, float lerpTime)
    {
        var currentPos = start;
        var t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / lerpTime;
            start = Mathf.Lerp(currentPos, 0, t);
            UpdateFood(start);
            yield return null;
        }
    }
    public IEnumerator LerpStaminaOverSeconds(float start, float lerpTime)
    {
        var currentPos = start;
        var t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / lerpTime;
            start = Mathf.Lerp(currentPos, 0, t);
            UpdateStamina(start);
            yield return null;
        }
    }

    public IEnumerator LerpWaterOverSeconds(float start, float lerpTime)
    {
        var currentPos = start;
        var t = 0f;
        while (t < 1)
        {
            t += Time.deltaTime / lerpTime;
            start = Mathf.Lerp(currentPos, 0, t);
            UpdateWater(start);
            yield return null;
        }
    }

    public void UpdateFood(float value)
    {
        curFood = (int)value;
        UpdateValues();
    }

    public void UpdateWater(float value)
    {
        curWater = (int)value;
        UpdateValues();
    }

    public void UpdateStamina(float value)
    {
        curStamina = (int)value;
        UpdateValues();
    }

    public float GetStamina()
    {
        return curStamina;
    }

    private void LerpStamina(float target, float time)
    {
        curStamina = Mathf.Lerp(curStamina, target, Time.deltaTime * time);
        UpdateValues();
    }

    //this is to get the ui working
    public void InitializeResources(UiReference uiReference)
    {
        p_Damage = GetComponent<PlayerDamageManager>();

        if (!base.IsOwner) return;

        foodText = uiReference.foodText;
        waterText = uiReference.waterText;
        radiationText = uiReference.radiationText;

        healthSlider = uiReference.healthSlider;
        staminaSlider = uiReference.staminaSlider;
        foodSlider = uiReference.foodSlider;
        waterSlider = uiReference.waterSlider;
        radiationSlider = uiReference.radiationSlider;
        healthFollowSlider = uiReference.healthFollowSlider;

        SetDefaultValues();
    }
    
    private void SetDefaultValues()
    {
        curHealth = maxHealth;
        curStamina = maxStamina;
        curFood = maxFood;
        curWater = maxWater;
        curRadiation = maxRadiation;

        initialised = true;
        UpdateValues();
    }

    public void UpdateValues()
    {
        if (!base.IsOwner) return;
        healthSlider.maxValue = maxHealth;
        healthFollowSlider.maxValue = maxHealth;
        healthSlider.value = curHealth;    
        staminaSlider.maxValue = maxRadiation;
        staminaSlider.value = curStamina;

        foodText.text = Mathf.RoundToInt(curFood / 10f).ToString();
        staminaSlider.maxValue = maxFood;
        foodSlider.value = curFood - 1;

        waterText.text = Mathf.RoundToInt(curWater / 10f).ToString();
        staminaSlider.maxValue = maxWater;
        waterSlider.value = curWater - 1;

        radiationText.text = curRadiation.ToString();
        staminaSlider.maxValue = maxRadiation;
        radiationSlider.value = curRadiation;
    }
    
    public void AddValue(int value, ResourceType type)
    {
        if (p_Damage.isDead) return;
        switch (type)
        {
            case ResourceType.stamina:
                if(staminaReduceCoroutine != null)
                {
                    StopCoroutine(staminaReduceCoroutine);
                }
                isLerpingStamina = false;

                curStamina += value;
                curStamina = Mathf.Clamp(curStamina,0 ,maxStamina);
                lastStaminaUse = (float)NetworkManager.TimeManager.TicksToTime() + staminaRegenGap;
                break;
            case ResourceType.food:

                Debug.Log("change food from " + curFood + "to " + (curFood + value));

                int val = curFood + value;

                isLerpingFood = false;

                if (foodCoroutine != null)
                {
                   StopCoroutine(foodCoroutine);
                }

                curFood = val;

                if (curFood > combinedFoodCap)
                {
                    curFood = combinedFoodCap;
                }
                if (curFood <= 0)
                {
                    OutOfFood();
                }

                UpdateValues();
                break;
            case ResourceType.water:

                Debug.Log("change water from " + curWater + "to " + (curWater + value) + "Combined water cap of _ " + combinedWaterCap);

                int vals = curWater + value;

                isLerpingWater = false;

                if (waterCoroutine != null)
                {
                    StopCoroutine(waterCoroutine);
                }

                curWater = vals;

                if (curWater > combinedWaterCap)
                {
                    curWater = combinedWaterCap;
                }
                if (curWater <= 0)
                {
                    OutOfWater();
                }
                UpdateValues();
 
                break;
            case ResourceType.radiation:
                curRadiation += value;
                if (curRadiation > maxRadiation)
                {
                    curRadiation = maxRadiation;
                }
                if (curRadiation <= 0)
                {
                    OutOfRadiation();
                }
                UpdateValues();
                break;
        }
    }

    public int CurHealth()
    {
        return curHealth;
    }

    public void AddHealth(int value)
    {
        int updated = curHealth + value;
        if(updated > combinedHealthCap)
        {
            updated = combinedHealthCap;
        }

        if(updated < curHealth)
        {
           //this is to notify we took damage even if not calling directly from TakeDamageFunction
           TookDamage();
        }
        CmdChangeHealth(updated);
        UpdateValues();
        if (updated <= 0)
        {
            //we kill player
            p_Damage.Die(transform.position, 0f);
        }
    }

    [ServerRpc] public void CmdChangeHealth(int value)
    {
        curHealth = value;
        Debug.Log("health changed to..." + value);
    }

    //this version is for being called externally
    [ServerRpc] public void CmdSetDefaultValues()
    {
        curHealth = maxHealth;
        curStamina = maxStamina;
        curFood = maxFood;
        curWater = maxWater;
        curRadiation = maxRadiation;
        UpdateValues();
    }

    private void OutOfWater()
    {

    }
    private void OutOfFood()
    {

    }
    private void OutOfRadiation()
    {

    }

    [Header("Health Regeneration")]
    [SerializeField] private float lastDamageToRegenTime = 10f;
    [Tooltip("when above this ratio of value in food and water we start regen automatically")][SerializeField] private float aboveRatioToRegen = 0.9f;
    [SerializeField] private int passiveHealthRegenPerSecond = 5;

    private float lastDamageTime;
    private float lastRegenTime;
    public void TookDamage()
    {
        if (!base.IsOwner) return;
        lastDamageTime = Time.time + lastDamageToRegenTime;
    }
}
