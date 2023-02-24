using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class ClothingSlotScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Clothing_Data.Clothing_Type slotType;
    public Image slotImage;
    public Image slotBacking;
    [HideInInspector]  public Sprite slotBaseSprite;
    public GameObject durabiltyObject;
    public TextMeshProUGUI durabilityText;

    //for extra inventoryspace
    public InventoryHolder storedHolder;

    [HideInInspector]
    public ClothingInventoryManager clothingManager;

    public bool isOccupied = false;
    public ItemInstance storedClothing;

    private void Start()
    {
        slotBaseSprite = slotImage.sprite; //gets current sprite in image
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (clothingManager == null) return;

        clothingManager.MouseEnterClothingSlot(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (clothingManager == null) return;

        clothingManager.MouseExitClothingSlot(this);
    }
}
