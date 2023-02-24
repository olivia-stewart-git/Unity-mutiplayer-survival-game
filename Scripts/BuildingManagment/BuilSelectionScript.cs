using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BuilSelectionScript : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private BuildData associatedData;

    private BuildController buildController;

    [SerializeField] private Image iconBacking;
    [SerializeField] private Image iconColorBacking;
    [SerializeField] private Image icon;
    [Space]
    [SerializeField] private Transform dataHolder;
    [SerializeField] private GameObject dataPrefab;

    public void SetData(BuildData data)
    {
        associatedData = data;
    }

    public BuildData GetBuildData()
    {
        return associatedData;
    }
    public void Initialise(BuildController controller, BuildData setData, ItemReference iRef)
    {
        buildController = controller;
        associatedData = setData;

        //set the build details
        for (int i = 0; i < setData.inputs.Length; i++)
        {
            GameObject creatededObj = Instantiate(dataPrefab, dataHolder);
            BuildUiDataScript dataUiScript = creatededObj.GetComponent<BuildUiDataScript>();
            dataUiScript.amountText.text = setData.inputs[i].quantity.ToString();
            dataUiScript.icon.sprite = iRef.allItems[setData.inputs[i].id].iconSprite;
        }      
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(buildController != null)
        {
            buildController.OnOptionClick(this);
        }   
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buildController != null)
        {
            buildController.OnOptionEnter(this);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buildController != null)
        {
            buildController.OnOptionExit(this);
        }
    }

    public void SetIconImage(Sprite image)
    {
        icon.sprite = image;
    }

    public void SetBackingColor(Color color)
    {
        iconBacking.color = color;
    }

    public void SetColorBacking(Color color)
    {
        iconColorBacking.color = color;
    }

    
}
