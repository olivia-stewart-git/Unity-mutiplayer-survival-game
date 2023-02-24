using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AttachOptionScript : MonoBehaviour, IPointerClickHandler
{
    [HideInInspector] public AttachmentSlotScript connectedSlot;
    [HideInInspector] public InventorySlot connectedInventorySlot;

    public Image backingImage;
    public Image iconImage;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(connectedSlot && connectedInventorySlot != null)
        {
            connectedSlot.ChangeAttachment(connectedInventorySlot);
        }
    }
}
