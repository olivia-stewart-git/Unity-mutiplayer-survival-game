using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildCategoryUiObject : MonoBehaviour, IPointerClickHandler
{
    public Image backingImage;

    private bool initialised = false;

    public PlayerBuildingManager.BuildType bType;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!initialised) return;
        if(storedController != null)
        {
            storedController.OnCategoryClick(this);
        }
    }

    private BuildController storedController;
    public void SetInitialised(bool value, BuildController controller)
    {
        initialised = value;
        if (initialised)
        {
            storedController = controller;
        }
    }
}
