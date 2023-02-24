using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler 
{
    [HideInInspector] public InventoryHolder heldIn;
    [HideInInspector] public bool forceEquipable = false;
    [HideInInspector] public InventoryManager i_Manager;

    [Header("Values")]
    public ItemInstance storedItem;
    public int xPosition;
    public int yPosition;
    public bool isOccupied = false;
    public bool isSubSlot = false;
    public bool isSelected = false;
    public Vector2 parentSlot;
    public List<Vector2> connectedSlots = new List<Vector2>();

    [HideInInspector] public bool usePlacedCallBacks = false;
    [HideInInspector] public bool useStackCallBacks = false;
    [HideInInspector] public HotBarManager hotBarScript;

    [Header("Ui elements")]    
    public TextMeshProUGUI stackText;
    public Transform stackHolder;

    public TextMeshProUGUI durabilityText;
    public Transform durabliltyHolder;

    public Image backingImage;
    public Image itemImage;

    //stored data
    public int currentOrientation;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(i_Manager != null)
        {
            i_Manager.MouseEnterSlot(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(i_Manager != null)
        {
            i_Manager.MouseExitSlot(this);
        }
    }

    public void PlacedCallback()
    {
        if(hotBarScript != null)
        {
            hotBarScript.GenerateHotBar();
        }
    }

    public void PickedCallBack()
    {
        if (hotBarScript != null)
        {
            hotBarScript.GenerateHotBar();
        }
    }

    public void StackCallback()
    {
        if (hotBarScript != null)
        {
            hotBarScript.UpdateHotBarValues();
        }
    }
}
