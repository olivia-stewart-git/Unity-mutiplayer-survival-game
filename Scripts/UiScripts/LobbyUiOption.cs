using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using HeathenEngineering.SteamworksIntegration;

public class LobbyUiOption : MonoBehaviour
{
    private LoadManager loadManager;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private TextMeshProUGUI countText;

    private CSteamID lobbyId;

    public void SetLoadManager(LoadManager _loadManager)
    {
        loadManager = _loadManager;
    }

    public void JoinPressed()
    {
        if(loadManager != null)
        {
            loadManager.LobbyJoinRequest(lobbyId);
        }
    }

    public void SetLobbyId(CSteamID id)
    {
        lobbyId = id;
    }

    public void SetPlayerCountText(int cur, int max)
    {
        countText.text = cur.ToString() + "/" + max.ToString();
    }


    public void SetNameText(string text)
    {
        nameText.text = text;
    }

    public CSteamID GetLobbyId()
    {
        return lobbyId;
    }

    public void SetLockOn(bool input)
    {
        if (input)
        {
            lockIcon.SetActive(true);
        }
        else
        {
            lockIcon.SetActive(false);
        }
    }
}
