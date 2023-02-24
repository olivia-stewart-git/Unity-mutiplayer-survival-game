using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AttachmentClass 
{
    public bool occupied = false;
    public int slotId;
    public int toAttachedId;
    public int attachmentDurability;
}
