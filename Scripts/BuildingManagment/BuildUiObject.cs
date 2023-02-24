using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildUiObject : MonoBehaviour
{
    [Header("UiComponents")]
    public GameObject overachingUi;
    public GameObject buildMenuHolder;
    public Transform selectionOptionsHolder;

    [Space]
    public BuildCategory[] buildCategories;
    [Space]
    public GameObject allBuildOptionsMenu;
    public Transform optionHolder;
    public GameObject buildDataHolder;
    public GameObject buildDataPrefab;

    [Space]
    public GameObject editOptionsHolder;
    public GameObject editSelectionsHolder;
    public Image buildHealthRepresentor;
    public Image centreHoverSpriteObj;
    [Space]
    public GameObject editViewIndicator;
    [Space]
    public EditOptionScript[] availableEditOptions;
    [Space]
    public GameObject repairModeDataHolder;
    public GameObject destroyModeDataHolder;
    public GameObject returnHolder;
    [Space]
    public TextMeshProUGUI categoryTitle;
}

[System.Serializable]
public struct BuildCategory 
{
    public string name;
    public BuildCategoryUiObject associatedUiObject;
    public PlayerBuildingManager.BuildType associatedType;
}

