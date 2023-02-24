using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CraftingRecipe : ScriptableObject
{
    public enum CraftingBench {barehands, workbench, anvil, furnace}
    public enum CraftingCategory {equipment, consumable, tools, weapons, construction}

    public int outPut;
    public int outPutQuantity = 1;
    public int minCraftingLevel = 1;
    [Space]
    public CraftingBench requiredbench; //for what you need to actually craft
    public CraftingCategory category;
    [Space]
    public CraftingInput[] requiredInputs;
}

[System.Serializable]
public class CraftingInput
{
    public int id;
    public int quantity = 1;
}

