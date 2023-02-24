using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class AttachmentData : ScriptableObject
{
    //note: flashlight and laser are considered in the same category
    public enum AttachMentSlot {grip, stock, barrel, sight, laser};
    [Header("Attachment settings")]
    public AttachMentSlot attachMentSlot;
    public GameObject attachmentObject;

    [Header("Barrel settings")]
    public bool overrideShootParticles = false;
    public string overrideShootSound;
    [Space]
    public float hipfireaccuracyMultiplier = 1f;
    public float adsAccuracyMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float rangeMultiplier = 1f;

    [Header("SightSettting")]
    public float sightFov = 1f;
    public float sightBackOffset = 0f;
}
