using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteracter : NetworkBehaviour
{
    private PlayerAudioManager p_Audio;

    private bool initialsed = false;

    [SerializeField] private InteractionInfoController interactInfo;
    [SerializeField] private PlayerMenuManager p_Menu;

    [Header("Interaction settings")]
    [SerializeField] private Transform lookFromTransform;
    [SerializeField] private LayerMask interactionMask;
    [SerializeField] private float interactionDistance;

    [Header("Visual settings")]
    [SerializeField] private Color outlineColor;
    [SerializeField] private float outlineThickness;

    [Header("audio settings")]
    public AudioClip[] pickupSounds;

    private GameObject lastLooked;

    public void Initialise()
    {
        initialsed = true;
    }

    private void Start()
    {
        //establish audio
        p_Audio = GetComponent<PlayerAudioManager>();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (!base.IsOwner || !initialsed) return;

        //raycasting
        RaycastHit hit;
        if (Physics.Raycast(lookFromTransform.position, lookFromTransform.forward, out hit, interactionDistance, interactionMask))
        {            
            if(lastLooked != null && lastLooked != hit.transform.gameObject) lastLooked.GetComponent<Outline>().enabled = false; 

            lastLooked = hit.transform.gameObject;

            Outline hitOutline = lastLooked.GetComponent<Outline>();
            hitOutline.enabled = true;
            hitOutline.OutlineColor = outlineColor;
            hitOutline.OutlineWidth = outlineThickness;

            interactInfo.SetPanel(true);
            if (hit.transform.CompareTag("Pickup"))
            {
                ItemPickupScript p_Script = hit.transform.gameObject.GetComponent<ItemPickupScript>();
                interactInfo.SetMainText(p_Script.GetName());
                interactInfo.SetNumText(p_Script.ReturnStack().ToString());
            }else if(hit.transform.CompareTag("Storage"))
            {
                StorageHoldObject sHeld = hit.transform.gameObject.GetComponent<StorageHoldObject>();

                interactInfo.SetMainText(sHeld.storagename);
                interactInfo.SetNumText("");
            }
        }
        else 
        {
            interactInfo.SetPanel(false);

            if (lastLooked != null)
            {
                lastLooked.GetComponent<Outline>().enabled = false;
                lastLooked = null;
            }
        }
    }

    public void Interact(InputAction.CallbackContext context)
    {
        if (context.performed && lastLooked != null && p_Menu.paused == false)
        {
            lastLooked.GetComponent<I_Interactable>().Interacted(gameObject);

            switch (lastLooked.tag)
            {
                case "Pickup":
                    if(p_Audio != null)
                    {
                        p_Audio.PlayLocalAudioClip(pickupSounds[Random.Range(0, pickupSounds.Length)]);
                    }
                    break;
            }
        }
    }
}
