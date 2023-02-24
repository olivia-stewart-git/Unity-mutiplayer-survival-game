using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class BuildData : ScriptableObject
{
    [Header("Settings")]
    public string buildName;
    public int buildId;
    public bool isFoundation;
    [Space]
    public PlayerBuildingManager.BuildType buildType;
    public Sprite buildIcon;
    [Space]
    public CraftingInput[] inputs;
    [Space]
    public GameObject createObject;
    public GameObject representObject;
    [Space]
    public bool requireGrounded = true;
    public bool freeBuild;
    public bool snapBuild = true;

    [Space]
    public bool offsetFromFloor = false;
    public float floorOffset = 0.1f;

    [Header("Damage settings etc")]
    public int maxHealth = 300;
    public int buildPenetrationDefense = 1; //this goes from 0 to 5 five
    
}
