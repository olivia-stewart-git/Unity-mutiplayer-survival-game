using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UiReference : MonoBehaviour
{
    [Header("Death components")]
    public Button respawnButton;
    public GameObject deathUiPanel;
    public GameObject bloodScreenPanel;

    [Header("Buffs")]
    public GameObject buffHolder;
    public GameObject inventoryBuffHolder;
    public GameObject buffPiece;
    public GameObject inventoryBuffPiece;
    [Space]
    public GameObject buffDescriptionBox;
    public TextMeshProUGUI buffDescriptionText;
    public TextMeshProUGUI buffNameText;

    [Header("References")]
    public GameObject gameUI;
    [Space]
    public TextMeshProUGUI foodText;
    public TextMeshProUGUI waterText;
    public TextMeshProUGUI radiationText;
    [Space]
    public GameObject crosshairImage;
    public GameObject hitmarker;
    [Space]
    [Header("PlayerData")]
    public Slider healthSlider;
    public Slider staminaSlider;
    public Slider foodSlider;
    public Slider waterSlider;
    public Slider radiationSlider;
    public Slider armorSlider;
    public Slider menuarmorSlider;
    public Slider healthFollowSlider;
    [Space]
    public RectTransform damageIndicator;
    [Space]
    public GameObject inventoryMenuObject;
    public Transform holderParent;
    public GameObject cursorObject;
    public Image cursorRepresentorImage;
    public Image cursorBackingImage;

    [Space]
    public GameObject cursorDurabilityHolder;
    public GameObject cursorStackHolder;
    public TextMeshProUGUI cursorDurabilityText;
    public TextMeshProUGUI cursorStackText;
    [Space]
    public GameObject infoPanelHolder;
    public TextMeshProUGUI infoText;
    public TextMeshProUGUI numberText;
    [Space]
    public ClothingSlotScript[] clothingSlots;
    [Space]
    public RawImage camerRenderImage;
    public Slider rotationSlider;
    [Space]
    public InventorySlot[] hotBarSlots;

    [Header("Inventory settings")]
    //refrenced componenets
    public GameObject panelObject;
    public GameObject stackPanelObject;

    //ui elements
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI stackText;
    public TextMeshProUGUI durabilityText;
    public TextMeshProUGUI weightText;

    //buttons
    public Button exitButton;
    public Button exitStackButton;
    public Button equipButton;
    public Button dropButton;
    public Button showstackButton;
    public Button salvageButton;
    public Button showStatsButton;
    public Button repairButton;

    public Image iconImage;

    public Button dropSelectedButton;
    public Button fillSelectionButton;
    [Space]
    public GameObject durabilityTextObject;
    public GameObject stackTextObject;
    public Transform stackSlotsHolder;

    [Header("hot bar")]
    public Transform creationPosition;

    [Header("Crafting")]
    public TabButtonUi craftingTab;
    public TabButtonUi inventoryTab;
    public TabGroup inventoryTabGroup;
    [Space]

    public Transform equipmentHolder;
    public Transform consumablesHolder;
    public Transform toolsHolder;
    public Transform weaponsHolder;
    public Transform constructionHolder;

    [Header("Crafting Popup")]
    public TabGroup craftingTabGroup;
    public GameObject craftingPopup;
    public TextMeshProUGUI craftingPopupTitleText;
    public TextMeshProUGUI craftingPopupWorkbenchText;
    public TextMeshProUGUI craftingPopupCraftingLevelText;
    public TextMeshProUGUI craftingPopupDescriptionText;
    public Button descriptionDropdownButton;
    public GameObject popupDescriptionObject;
    [Space]
    public Image craftingPopupMainIcon;
    public TextMeshProUGUI outPutAmountText;
    [Space]
    public Image craftingPopupWorkbenchBackingColor;
    public Image craftingPopupWorkbenchIcon;
    [Space]
    public Transform craftingPopupInputField;
    [Space]
    public Button craftButton;
    public Button closeMenuButton;

    [Header("SecondInventory")]
    public Transform secondInventoryContent;

    [Header("PickupNotifications")]
    public GameObject[] pickupNotifications;

    [Header("BuildUi")]
    public BuildUiObject buildUi;
}
