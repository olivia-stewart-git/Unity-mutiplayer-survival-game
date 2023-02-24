using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;

public class PickupInitialiser : NetworkBehaviour
{

    // called when the server starts
    public override void OnStartServer()
    {
        if (IsServer)
        {
            GameObject[] allFound = GameObject.FindGameObjectsWithTag("Pickup");
            Debug.Log(allFound);
            foreach (GameObject pickup in allFound)
            {
                ItemPickupScript p_Script = pickup.GetComponent<ItemPickupScript>();
                if (p_Script != null && p_Script.GetInstance() == null)
                {
                    ItemData toUse = p_Script.thisItem;
                    ItemInstance toUseInstance = new ItemInstance();
                    toUseInstance.id = toUse.itemId;
                    toUseInstance.currentDurability = Random.Range(1000 / 2, 100);

                    toUseInstance.stackedItemIds = new List<int>();
                    switch (toUse.stack_type)
                    {
                        case ItemData.StackType.none:
                            break;
                        case ItemData.StackType.standard:
                            int total = toUse.stackCapacity - 1;
                            int amountToMake = Random.Range(0, total);

                            if (amountToMake != 0)
                            {
                                for (int i = 0; i < amountToMake; i++)
                                {
                                    toUseInstance.stackedItemIds.Add(toUse.itemId);
                                }
                            }
                            break;
                        case ItemData.StackType.container:
                            int totalCon = toUse.stackCapacity;
                            int amountToMakeCon = totalCon;

                            if (amountToMakeCon != 0)
                            {
                                for (int i = 0; i < amountToMakeCon; i++)
                                {
                                    toUseInstance.stackedItemIds.Add(toUse.stackableItems[0]);
                                }
                            }
                            break;
                    }

                    switch (toUse.itemType)
                    {
                        case ItemData.Item_Type.gun:
                            GunObject g_Object = toUse.equipObject.GetComponent<GunObject>();
                            for (int i = 0; i < g_Object.attachments.Length; i++)
                            {
                                AttachmentClass newClass = new AttachmentClass();
                                newClass.occupied = false;
                                newClass.toAttachedId = i;
                                toUseInstance.storedAttachments.Add(newClass);
                            }
                            break;
                        case ItemData.Item_Type.material:
                            break;
                        case ItemData.Item_Type.tool:
                            break;
                        case ItemData.Item_Type.clothing:
                            break;
                        case ItemData.Item_Type.magazine:
                            break;
                        case ItemData.Item_Type.attachment:
                            break;
                        case ItemData.Item_Type.ammunition:
                            break;
                    }

                    p_Script.SetInstance(toUseInstance);

                   // CmdChangeInstance(toUseInstance, p_Script);
                }

                //   GameObject newInstance = Instantiate(p_Script.thisPrefab, pickup.transform.position, pickup.transform.rotation);

                // ServerManager.Spawn(p_Script.gameObject);
            }
        }
    }

    [ServerRpc]
    void CmdChangeInstance(ItemInstance instance, ItemPickupScript pickup)
    {
        pickup.SetInstance(instance);
    }
}
