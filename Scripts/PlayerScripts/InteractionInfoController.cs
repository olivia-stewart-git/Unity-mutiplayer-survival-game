using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FishNet.Object;

public class InteractionInfoController : NetworkBehaviour
{

    //ui
    private GameObject infoPanelHolder;
    private TextMeshProUGUI infoText;
    private TextMeshProUGUI numberText;

    private bool initialsed = false;

    public void InitialiseInteractUi(UiReference uiRef)
    {
        if (!base.IsOwner) return;

        infoPanelHolder = uiRef.infoPanelHolder;
        infoText = uiRef.infoText;
        numberText = uiRef.numberText;

        initialsed = true;
    }

    public void SetPanel(bool value)
    {
        if (value)
        {
            infoPanelHolder.SetActive(true);
        }
        else
        {
            infoPanelHolder.SetActive(false);
        }
    }

    public void SetMainText(string text)
    {
        infoText.text = text;
    }

    public void SetNumText(string text)
    {
        numberText.text = text;
    }

}
