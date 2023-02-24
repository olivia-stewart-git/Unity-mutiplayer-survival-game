using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Clothing_Data : ScriptableObject
{
    public int clothingId;
    public enum Clothing_Type {hat, face, shirt, vest, pants, feet, back};
    [Header("Clothing settings")]
    public Clothing_Type clothingType;
    public bool addToInventorySpace = false;
    public int addSlotx;
    public int addSloty;
    public GameObject clothingObject;
    [Space]
    public int armoring = 0;
}
