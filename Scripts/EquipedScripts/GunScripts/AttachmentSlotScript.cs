using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class AttachmentSlotScript : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public GunObject g_Object;
    [HideInInspector] public int locationid;

    public Transform createPos;

    public GameObject selectionHolder;
    public Color unselectedColor;
    public Color selectedColor;

    public Image backingImage;
    public Image iconImage;

    public GameObject removeButton;
    [Space]
    public Image arrowImage;
    public Sprite arrowSprite;
    public Sprite nonSprite;

    [HideInInspector] public List<GameObject> createdOptions = new List<GameObject>();
    public GameObject toCreateSelection;
    public void OnPointerClick(PointerEventData eventData)
    {
        if(g_Object != null)
        {
            g_Object.AttachmentSlotSelected(this);
        }
    }

    public void DeEquipCurrentAttachment()
    {
        g_Object.DeEquipFromAttachmentSlot(this);
    }

    public void ChangeAttachment(InventorySlot slot)
    {
        if (g_Object != null)
        {
            g_Object.ChangeGunAttachment(slot, this);
        }
    }
}
