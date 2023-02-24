using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface I_EquipedItem 
{
    void Intialise(GameObject player, ItemInstance instance);
    void Drawn();
    void LeftButtonDown();
    void LeftButtonUp();

    void RightButtonDown();
    void RightButtonUp();

    void DeEquip();
}
