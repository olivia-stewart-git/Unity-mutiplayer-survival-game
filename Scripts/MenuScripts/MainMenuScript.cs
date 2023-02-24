using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HeathenEngineering.SteamworksIntegration;
using TMPro;
using Steamworks;
using FishNet.Object;

public class MainMenuScript : MonoBehaviour
{
    [Header("Profile Settings")]
    public Image steamIcon;
    public TextMeshProUGUI steamNameText;

    LoadManager loadManager;

    protected Callback<LobbyCreated_t> lobbyCreated;

    [Header("Hosting and joining settings")]
    [SerializeField] private Transform lobbyView;
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Slider playerCountInput;
    [SerializeField] private TextMeshProUGUI playercountMaxText;

    [SerializeField] private Toggle privateInput;
    [SerializeField] private TMP_Dropdown mapSelect;

    // Start is called before the first frame update
    void Awake()
    {
        loadManager = LoadManager.Instance;

        //initialise map select
        Dictionary<string, GameMap> maps = loadManager.QueryMaps();
        mapSelect.ClearOptions();

        foreach (KeyValuePair<string, GameMap> map in maps)
        {
            mapSelect.options.Add(new TMP_Dropdown.OptionData() { text = maps[map.Key].name, image = maps[map.Key].mapIcon });
        }
    }

    private void Start()
    {
        PCSliderValueChanged();
    }

    public void StartNewHost()
    {
        if(loadManager!= null)
        {
            LobbyManager lManager = loadManager.GetLobbyManager();

            lManager.createArguments.name = nameInput.text;
            int playerCountMax = (int)playerCountInput.value;
            lManager.createArguments.slots = playerCountMax;
            if (privateInput.isOn)
            {
                lManager.createArguments.type = ELobbyType.k_ELobbyTypePrivate;
            }
            else
            {
                lManager.createArguments.type = ELobbyType.k_ELobbyTypePublic;
            }

            lManager.createArguments.metadata[0] = new LobbyManager.MetadataTempalate() { key = "Map", value = mapSelect.options[mapSelect.value].text };
            //mapSelect.options[mapSelect.value].text;

            loadManager.StartLobbyHost();
        }     
    }

    public void RefreshLobbyList()
    {
        loadManager.RefreshLobbyList(lobbyView);
    }

    public void PCSliderValueChanged()
    {
        playercountMaxText.text = playerCountInput.value.ToString();
    }

    public void CreateExampleLobby()
    {
        LobbyManager lManager = loadManager.GetLobbyManager();

        lManager.createArguments.name = nameInput.text;
        int playerCountMax = (int)playerCountInput.value;
        lManager.createArguments.slots = playerCountMax;
        if (privateInput.isOn)
        {
            lManager.createArguments.type = ELobbyType.k_ELobbyTypePrivate;
        }
        else
        {
            lManager.createArguments.type = ELobbyType.k_ELobbyTypePublic;
        }

        lManager.createArguments.metadata[0] = new LobbyManager.MetadataTempalate() {key = "map", value = mapSelect.options[mapSelect.value].text};
            //mapSelect.options[mapSelect.value].text;

        lManager.Create();
    }

    public void CreateSinglePlayerGame()
    {
        loadManager.NewSinglePlayerGame("Map_Test");
    }
}


