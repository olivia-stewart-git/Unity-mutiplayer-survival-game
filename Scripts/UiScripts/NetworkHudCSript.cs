using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FishNet.Object;
using FishNet.Managing;

public class NetworkHudCSript : NetworkBehaviour
{
    public TMP_InputField conncetionField;
    public NetworkManager netManager;

    public void JoinClicked()
    {
     
        gameObject.SetActive(false);
    }

    public void StartAsServer()
    {
     
    }

    public void Host()
    {
     
    }
}
