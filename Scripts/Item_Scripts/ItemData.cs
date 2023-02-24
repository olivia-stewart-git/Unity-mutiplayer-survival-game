using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class ItemData : ScriptableObject
{
    public enum DamageType { blunt, slice, penetration, explosive, misc };

    [Header("Base details")]
    public int itemId;
    public string itemName;
    [TextArea]
    public string description;
    public bool useDurability;
    public Sprite iconSprite;
    public GameObject pickupPrefab;

    public enum Item_Type {material, tool, gun, clothing, magazine, attachment, ammunition, meleeWeapon, consumable};
    [Header("Type settings")]
    public Item_Type itemType;
    [Header("Equiping")]
    public bool equipable = false;
    public float equipTime = 0.2f;
    [Space]
    public AudioClip equipSound;
    [Space]
    public GameObject equipObject;
    public GameObject multiplayerRepresentObject;
    [Tooltip("sets the animation set used on multiplayer body")]public int equipLayer = 0;
    [Space]
    [Header("DataInput")]
    public Clothing_Data clothingData;
    public GunData gunData;
    public AmmunitionData ammoData;
    public AttachmentData attachmentData;
    public MagazineData magazineData;
    public MeleeData mData;
    public ConsumableData consumeData;

    [Header("Storage settings")]
    public int slotSpaceX = 1;
    public int slotSpaceY = 1;

    public enum StackType {none, standard, container};
    [Header("Stack settings")]
    public StackType stack_type;
    public int stackCapacity;
    public int[] stackableItems;

    [Header("In progress settings")]
    public bool repairable;
    public bool salvagable;
    public bool hasStats;

}
