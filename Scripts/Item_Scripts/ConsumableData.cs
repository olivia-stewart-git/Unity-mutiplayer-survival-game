using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ConsumableData : ScriptableObject
{
    [Header("Values")]
    public int healthOnUse = 0;
    public int radsOnUse = 0;
    public int foodOnUse = 0;
    public int waterOnUse = 0;
    [Space]
    public bool blockSprintDuringUse = false;
    public bool consumeOnUse = true;

    [Header("Sounds")]
    public AudioClip useSound;
    public AudioClip supplymentarySound;

    [Header("Buff Application")]
    public bool addBuffs = false;
    public string[] buffsToAdd;
    [Space]
    public bool removeBuffs = false;
    public string[] buffsToRemove;

    [Header("Animation settings")]
    public string useTrigger;
    public string multiplayerAnimationTrigger;
}
