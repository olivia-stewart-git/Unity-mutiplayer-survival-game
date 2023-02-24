using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using UnityEngine.UI;

public class CrosshairManager : NetworkBehaviour
{
    private GameObject crosshairImage;

    private RectTransform crosshairRect;

    private bool initialsed = false;
    private bool crosshairOn = true;

    [SerializeField] private float returnTime = 8f;
    [SerializeField] private float smooth = 3f;

    private Vector2 target = new Vector2(2f, 2f);
    private Vector2 damper;

    //hitmarker
    private GameObject hitMarker;
    private float lastHitmarker;

    public void Initialise(UiReference uiref)
    {
        crosshairImage = uiref.crosshairImage;
        crosshairRect = uiref.crosshairImage.GetComponent<RectTransform>();
        damper = target;
        hitMarker = uiref.hitmarker;
        initialsed = true;
    }

    public void SetCrosshairOn(bool value)
    {
        if (!base.IsOwner) return;
        crosshairImage.SetActive(value);
        crosshairOn = value;
    }

    public void Update()
    {
        if (!base.IsOwner || !initialsed) return;

            damper = Vector2.Lerp(damper, target, smooth * Time.deltaTime);
            crosshairRect.sizeDelta = Vector2.Lerp(crosshairRect.sizeDelta, damper, returnTime * Time.deltaTime);
       
        //hitmarker
        if(Time.time > lastHitmarker)
        {
            hitMarker.SetActive(false);
        }
        else
        {
            hitMarker.SetActive(true);
        }
    }

    public void ExpandCrosshair(float amount, float scalar)
    {
        damper = new Vector2(damper.x + (amount * scalar * 75f), damper.y + (amount * scalar * 75f));
    }

    public void SetCrosshairTarget(Vector2 useTarget)
    {
        target = useTarget;
    }

    public void DoHitmarker()
    {
        lastHitmarker = Time.time + 0.1f;
    }
}
