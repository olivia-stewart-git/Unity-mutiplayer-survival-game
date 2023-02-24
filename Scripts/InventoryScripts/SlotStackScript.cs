using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SlotStackScript : MonoBehaviour
{
    public InventoryMenuScript i_Menu;
    public TextMeshProUGUI nameText;
    public GameObject indicatorObject;
    public int id;


    public void Clicked()
    {
        if (i_Menu != null)
        {
            i_Menu.StackSlotClicked(gameObject);
        }
    }
}
