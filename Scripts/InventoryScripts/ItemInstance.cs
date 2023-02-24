using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemInstance 
{
    //note on item stacking: stacked items cannot store durabilty, as such 
    //for items like that looking towards treating durability as a separate
    //value might be valuable

    public int id; //item id
    public int currentDurability;

    public bool inStack = false;

    public List<int> stackedItemIds; //list of all items stacked on this

    //gun savings
    #region 
    public bool magLoaded = false;
    public int magObjectId;
    public List<int> loadedMagazineIds = new List<int>();

    //attachments
    public List<AttachmentClass> storedAttachments = new List<AttachmentClass>();
    #endregion
}
