using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class BuffInventoryUiScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    // Start is called before the first frame update
    public Image icon;
    public Image backingImage;
    public TextMeshProUGUI durationText;

    public string assotiatedBuff;

    public PlayerBuffManager p_buffManager;

    public void OnPointerEnter(PointerEventData eventData)
    {
        p_buffManager.BuffEnter(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        p_buffManager.BuffExit(this);
    }
}
