using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CraftingOptionScript : MonoBehaviour, IPointerClickHandler
{
    public Image iconImage;
    public Image colorBackingImage;
    public Image backingImage;
    public TextMeshProUGUI amountText;
    [Space]
    public Color selectedColor;
    public Color deSelectedColor;

    [HideInInspector] public CraftingRecipe heldRecipe;
    [HideInInspector] public CraftingManager craftManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        if(craftManager != null)
        {
            craftManager.CraftOptionClicked(this);
        }
    }
}
