using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Steamworks;
using FishNet.Object;
using HeathenEngineering.SteamworksIntegration;
using TMPro;
using FishNet.Managing;

public class LoadManager : MonoBehaviour
{
    public static LoadManager Instance;

    [SerializeField] private GameObject _loaderCanvas;
    [SerializeField] private TextMeshProUGUI loadText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private GameObject lookCamera;
    [SerializeField] private NetworkManager netManager;
    [SerializeField] private ItemReference itemReference;

    [Header("starting game settings")]
    [SerializeField] private GameObject startGameCanvas;

    private bool startGameMode = false;

    [Header("multiplayer stuff")]
    [SerializeField] private LobbyManager lobbyManager;

    private const string HostAddressKey = "HostAddress";

    [Header("lobby querying")]
    [SerializeField] private GameObject lobbyUiRepresentor;

    [Header("Maps")]
    public GameMap[] loadedMaps;
    public Dictionary<string, GameMap> gameMaps;

    public LobbyManager GetLobbyManager()
    {
        return lobbyManager;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        //initialise maps
        gameMaps = new Dictionary<string, GameMap>();
        foreach (GameMap map in loadedMaps)
        {
            gameMaps.Add(map.name, map);
        }
    }

    private void Start()
    {
        startGameMode = true;
        startGameCanvas.SetActive(true);
        _loaderCanvas.SetActive(false);
    }

    private void Update()
    {
        if (startGameMode == true)
        {
            if (Input.anyKeyDown)
            {
                Debug.Log("start game");
                LoadScene("MainMenu");

                startGameMode = false;
                startGameCanvas.SetActive(false);
            }
        }

        if (SceneManager.sceneCount == 1)
        {
            lookCamera.SetActive(true);
        }
    }

    public void LoadScene(string sceneName)
    {
        var scene = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        scene.allowSceneActivation = false;

        loadText.text = "Loading";

        _loaderCanvas.SetActive(true);

        do
        {
            progressBar.value = scene.progress;
        } while (scene.progress < 0.9f);

        if (scene.isDone)
        {
            lookCamera.SetActive(false);
        }

        scene.allowSceneActivation = true;
        _loaderCanvas.SetActive(false);

    }

    public void OnLobbyCreated()
    {

    }

    public void UnloadScene(string sceneName)
    {
        //check if scene is loaded
        var scene = SceneManager.GetSceneByName(sceneName);
        if (scene.isLoaded)
        {
            var sceneAsync = SceneManager.UnloadSceneAsync(sceneName);
        }
    }

    private bool isLoading = false;
    private bool inGame = false;
    private string curMap;

    #region hosting
    public void StartLobbyHost()
    {
        StartCoroutine(HostLobby());
    }

    public IEnumerator HostLobby()
    {
        inGame = true;
        _loaderCanvas.SetActive(true);

        isLoading = true;
        //unloadMainMenu
        UnloadScene("MainMenu");

        //create
        lobbyManager.Create();

        //load scene
        var scene = SceneManager.LoadSceneAsync(gameMaps[lobbyManager.createArguments.metadata[0].value].sceneName, LoadSceneMode.Additive);
        curMap = gameMaps[lobbyManager.createArguments.metadata[0].value].sceneName;
        while (!scene.isDone)
        {
            loadText.text = "Loading_World";
            progressBar.value = scene.progress;
            yield return null;
        }
        Debug.Log("WorldLoaded");
        //load  player
        var p_scene = SceneManager.LoadSceneAsync("PlayerScene", LoadSceneMode.Additive);
        while (!p_scene.isDone)
        {
            loadText.text = "Loading_Player";
            yield return null;
        }
        lookCamera.SetActive(false);

        if (!netManager.ClientManager.Connection.IsActive && Application.platform != RuntimePlatform.WebGLPlayer)
        {
            // Server + Client
            netManager.ServerManager.StartConnection();
            Debug.Log("started server");
            netManager.ClientManager.StartConnection();
            Debug.Log("startedClient");
        }

        lobbyManager.SetLobbyData(HostAddressKey, SteamUser.GetSteamID().ToString());

        _loaderCanvas.SetActive(false);

        isLoading = false;
    }

    #endregion

    #region joining
    private bool joining = false;
    //get a request to join a queried lobby item
    private CSteamID tojoin;
    public void LobbyJoinRequest(CSteamID lobbyId)
    {
        //if (ServerManager.active || joining) return;
        if (joining) return;
        tojoin = lobbyId;
        joining = true;
        _loaderCanvas.SetActive(true);
        lobbyManager.Join(lobbyId);
    }
    public void OnLobbyJoin()
    {
        StartCoroutine(CommenceJoin());
    }

    IEnumerator CommenceJoin()
    {
        inGame = true;

        isLoading = true;
        //unloadMainMenu
        UnloadScene("MainMenu");

        //load scene
        var scene = SceneManager.LoadSceneAsync(lobbyManager.Lobby.GetMetadata()["map"], LoadSceneMode.Additive);
        while (!scene.isDone)
        {
            loadText.text = "Loading_World";
            progressBar.value = scene.progress;
            yield return null;
        }
        Debug.Log("WorldLoaded");
        //load  player
        var p_scene = SceneManager.LoadSceneAsync("PlayerScene", LoadSceneMode.Additive);
        while (!p_scene.isDone)
        {
            loadText.text = "Loading_Player";
            yield return null;
        }
        lookCamera.SetActive(false);

        if (!netManager.ClientManager.Connection.IsActive && Application.platform != RuntimePlatform.WebGLPlayer)
        {
            netManager.ClientManager.StartConnection(SteamMatchmaking.GetLobbyData(lobbyManager.Lobby.id, HostAddressKey));
        }

        lobbyManager.SetLobbyData(HostAddressKey, SteamUser.GetSteamID().ToString());

        //finishing up
        _loaderCanvas.SetActive(false);

        isLoading = false;
    }
    public void OnLobbyJoinFail()
    {
        Debug.Log("failed to join lobby");
        joining = false;
        _loaderCanvas.SetActive(false);
    }
    #endregion

    #region leaving
    public void OnGameLeaveRequest()
    {
        if (!inGame) return;
        _loaderCanvas.SetActive(true);
        loadText.text = "loading...";

        inGame = false;
        //leaving lobby
         lobbyManager.Lobby.Leave();

        if (netManager.ServerManager.Started && netManager.ClientManager.Connection.IsActive)
        {
            netManager.ServerManager.StopConnection(false);
        }
        // stop client if client-only
        else if (netManager.ClientManager.Connection.IsActive)
        {
            netManager.ClientManager.StopConnection();
        }
        // stop server if server-only
        else if (netManager.IsServerOnly && netManager.ServerManager.Started)
        {
            netManager.ServerManager.StopConnection(false);
        }

        //unload
        lookCamera.SetActive(true);

        UnloadScene("PlayerScene");
        UnloadScene(curMap);

        LoadScene("MainMenu");

        _loaderCanvas.SetActive(false);

        inGame = false;
    }

    #endregion

    //connect player to these settings for leaving the game
    public void OnPlayerLoaded(GameObject player)
    {
      //  player.GetComponent<PlayerMenuManager>().p_Menu.disconnectButton.onClick.AddListener(OnGameLeaveRequest);
        PlayerScript p_Script = player.GetComponent<PlayerScript>();

        p_Script.InitialisePlayer(itemReference);
        _loaderCanvas.SetActive(false);
        //finishing u    }


    }

    #region serverBrowser

    private List<GameObject> createdLobbyObjects = new List<GameObject>();

    bool refreshingList = false;
    Transform _createUnder;
    //can only be called from the main menu scene
    public void RefreshLobbyList(Transform createUnder)
    {
        if (refreshingList) return;
        refreshingList = true;
        _createUnder = createUnder;

        //remove old entries
        if (createdLobbyObjects != null)
        {
            if (createdLobbyObjects.Count > 0)
            {
                foreach (GameObject g in createdLobbyObjects)
                {
                    Destroy(g);
                }
            }
        }

        createdLobbyObjects = new List<GameObject>();

        //search for lobbies
        lobbyManager.Search(50);
    }

    private Lobby[] knownLobbies;
    public void ReportSearchResults(Lobby[] results)
    {
        knownLobbies = results;
        if (refreshingList)
        {
            if (knownLobbies.Length > 0)
            {
                foreach (Lobby lob in knownLobbies)
                {
                    GameObject createdInstance = Instantiate(lobbyUiRepresentor, _createUnder);
                    createdLobbyObjects.Add(createdInstance);

                    LobbyUiOption representLobby = createdInstance.GetComponent<LobbyUiOption>();
                    representLobby.SetLoadManager(this);
                    representLobby.SetLobbyId(lob.id);

                    representLobby.SetNameText(lob.Name);
                    representLobby.SetPlayerCountText(lob.Members.Length, lob.MaxMembers);
                    if (lob.Type == ELobbyType.k_ELobbyTypePrivate)
                    {
                        representLobby.SetLockOn(true);
                    }
                    else
                    {
                        representLobby.SetLockOn(false);
                    }
                }
            }
            else
            {
                Debug.Log("no lobbies found");
            }

            refreshingList = false;
        }
    }

    #endregion

    #region map management
    public Dictionary<string, GameMap> QueryMaps()
    {
        return gameMaps;
    }
    #endregion

    public void NewSinglePlayerGame(string map)
    {
        StartCoroutine(NewGameCreation(map));
    }

    public IEnumerator NewGameCreation(string map)
    {
        inGame = true;
        _loaderCanvas.SetActive(true);

        isLoading = true;
        //unloadMainMenu
        UnloadScene("MainMenu");

        //load scene
        var scene = SceneManager.LoadSceneAsync(map, LoadSceneMode.Additive);
        curMap = map;
        while (!scene.isDone)
        {
            loadText.text = "Loading_World";
            progressBar.value = scene.progress;
            yield return null;
        }
        Debug.Log("WorldLoaded");
        //load  player
        var p_scene = SceneManager.LoadSceneAsync("PlayerScene", LoadSceneMode.Additive);
        while (!p_scene.isDone)
        {
            loadText.text = "Loading_Player";
            yield return null;
        }
        lookCamera.SetActive(false);
        Debug.Log("player scene loaded");

        if (netManager == null) {
            Debug.LogError("Null network manager");
            yield break;
        }
        if (!netManager.ClientManager.Connection.IsActive && Application.platform != RuntimePlatform.WebGLPlayer)
        {
            // Server + Client
            netManager.ServerManager.StartConnection();
            Debug.Log("startedServer");
            netManager.ClientManager.StartConnection();
            Debug.Log("started client");
        }

        lobbyManager.SetLobbyData(HostAddressKey, SteamUser.GetSteamID().ToString());

        isLoading = false;
    }
}

[System.Serializable]
public struct GameMap
{
    public string name;
    public string sceneName;
    public Sprite mapIcon;
}
