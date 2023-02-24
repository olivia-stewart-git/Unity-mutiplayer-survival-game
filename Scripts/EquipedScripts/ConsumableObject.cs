using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConsumableObject : MonoBehaviour, I_EquipedItem
{

    private PlayerResourcesScript p_resources;
    private PlayerMovementMangaer p_Movement;
    private PlayerBuffManager p_BuffManager;
    private PlayerAudioManager p_Audio;
    private EquipManager p_Equip;
    

    [Header("Setup")]
    public Animator animator;
    public ConsumableData thisConsumable;

    public ItemData thisItem;

    [Header("Settings")]
    public float useLength;
    [Space]
    public float redrawTime = 0.5f;


    //no public
    private bool initialised = false;
    private Coroutine useCoroutine;
    private bool drawn = false;
    private bool inUse = false;

    private bool redraw = false;


    public void DeEquip()
    {
        StopAllCoroutines();

        if (thisConsumable.blockSprintDuringUse && inUse)
        {
            p_Movement.ForceWalk(false);
        }
    }

    public void Drawn()
    {
        drawn = true;
        redraw = true;

    }

    public void Intialise(GameObject player, ItemInstance instance)
    {
        drawn = false;
        inUse = false;

        p_resources = player.GetComponent<PlayerResourcesScript>();
        p_Movement = player.GetComponent<PlayerMovementMangaer>();
        p_BuffManager = player.GetComponent<PlayerBuffManager>();
        p_Audio = player.GetComponent<PlayerAudioManager>();
        p_Equip = player.GetComponent<EquipManager>();


        initialised = true;
    }

    public void LeftButtonDown()
    {
        if (inUse == false && redraw == true && drawn == true)
        {
            useCoroutine =  StartCoroutine(UseItem(useLength));
        }
    }

    public void LeftButtonUp()
    {
       
    }

    public void RightButtonDown()
    {
     
    }

    public void RightButtonUp()
    {
     
    }

    IEnumerator UseItem(float useTime)
    {
        inUse = true;
        redraw = false;

        animator.SetTrigger(thisConsumable.useTrigger);
        p_Audio.PlayLocalAudioClip(thisConsumable.useSound);

        if (thisConsumable.blockSprintDuringUse)
        {
            p_Movement.ForceWalk(true);
        }

        yield return new WaitForSeconds(useTime);

        ApplyValue();

        if (thisConsumable.blockSprintDuringUse)
        {
            p_Movement.ForceWalk(false);
        }

        inUse = false;

        StartCoroutine(ToRedraw());
    }

    IEnumerator ToRedraw()
    {
        redraw = false;
        yield return new WaitForSeconds(redrawTime);
        redraw = true;
    }

    public void ApplyValue()
    {
        if(thisConsumable.foodOnUse > 0)
        {
            p_resources.AddValue(thisConsumable.foodOnUse, PlayerResourcesScript.ResourceType.food);
        }

        if(thisConsumable.waterOnUse > 0)
        {
            p_resources.AddValue(thisConsumable.waterOnUse, PlayerResourcesScript.ResourceType.water);
        }

        if(thisConsumable.radsOnUse > 0)
        {
            p_resources.AddValue(thisConsumable.radsOnUse, PlayerResourcesScript.ResourceType.radiation);
        }

        if(thisConsumable.healthOnUse > 0)
        {
            p_resources.AddHealth(thisConsumable.healthOnUse);
        }

        if(thisConsumable.buffsToAdd.Length > 0)
        {
            foreach (string item in thisConsumable.buffsToAdd)
            {
                p_BuffManager.ApplyBuff(item, false);
            }
        }
        if (thisConsumable.buffsToRemove.Length > 0)
        {
            foreach (string item in thisConsumable.buffsToRemove)
            {
                p_BuffManager.ApplyBuff(item, true);
            }
        }

        if (thisConsumable.consumeOnUse)
        {
            p_Equip.SubtractFromHeld();
        }
    }

    public void PlaySupplementarySound()
    {
        if(thisConsumable.supplymentarySound != null)
        {
            p_Audio.PlayLocalAudioClip(thisConsumable.supplymentarySound);
        }
    }
}
