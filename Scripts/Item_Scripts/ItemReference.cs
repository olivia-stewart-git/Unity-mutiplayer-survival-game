using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemReference : MonoBehaviour
{
    public Dictionary<int, ItemData> allItems;
    public ItemData[] inputItems;

    public Dictionary<int, BuildData> allBuildItems;
    public BuildData[] inputBuilds;

    public void Start()
    {
        InitialiseItems();
    }

    public void InitialiseItems()
    {
        allItems = new Dictionary<int, ItemData>();

        foreach (ItemData data in inputItems)
        {
            if (!allItems.ContainsKey(data.itemId))
            {

                allItems.Add(data.itemId, data);
            }
        }

        allBuildItems = new Dictionary<int, BuildData>();

        foreach (BuildData item in inputBuilds)
        {
            if (!allBuildItems.ContainsKey(item.buildId))
            {
                allBuildItems.Add(item.buildId, item);
            }
        }
    }
}
