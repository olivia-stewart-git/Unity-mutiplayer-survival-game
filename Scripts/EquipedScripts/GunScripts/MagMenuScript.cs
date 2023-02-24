using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MagMenuScript : MonoBehaviour
{
    public InventorySlot referredSlot;
    public ItemInstance referredInstance;
    public Slider ammoSlider;
    public Image iconImage;
    public Image backing;
    public Color unselected;
    public Color selected;
    public TextMeshProUGUI ammoText;
    public GameObject currentLoaded;
}
