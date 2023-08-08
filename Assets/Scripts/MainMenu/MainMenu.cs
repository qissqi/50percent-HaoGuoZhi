using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject roomSlotPrefab;
    private ExampleNetworkDiscovery networkDiscovery;
    Dictionary<IPAddress, DiscoveryResponseData> discoveredServers = new Dictionary<IPAddress, DiscoveryResponseData>();
    private List<GameObject> roomsList=new List<GameObject>();
    //[SerializeField] private Lobby lobbyController;
    [SerializeField] private GameObject lobbyPanel;
    

    [Header("主菜单")] 
    [SerializeField] private TMP_InputField playerNameField;
    [SerializeField] private Transform roomContent;
    //[SerializeField] private GameObject currentSelectedRoom;
    [SerializeField] private string currentSelectedIp;
    [SerializeField] private string currentSelectedPsw;
    
    [Header("创建房间界面")]
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private Toggle psw_toggle;
    [SerializeField] private TMP_InputField roomNameField;
    [SerializeField] private TMP_InputField roomPswField;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            startPanel.SetActive(true);
            gameObject.SetActive(false);
        }
        
    }

    private void Start()
    {
        playerNameField.text = GameManager.Instance.playerName;
        networkDiscovery = NetworkManager.Singleton.GetComponent<ExampleNetworkDiscovery>();
        networkDiscovery.OnServerFound.RemoveListener(OnServerFound);
        networkDiscovery.OnServerFound.AddListener(OnServerFound);
        
        GameManager.Instance.CreatePlayerInfo();
        networkDiscovery.StartClient();
        networkDiscovery.ClientBroadcast(new DiscoveryBroadcastData());
    }


    private void OnServerFound(IPEndPoint arg0, DiscoveryResponseData arg1)
    {
        Debug.Log($"Found Server: {arg0.Address}");
        discoveredServers.Add(arg0.Address,arg1);
        AddRoom(arg0.Address,arg1);
    }

    public void RoomSelected(GameObject obj)
    {
        
    }

    public void EditNameEnd()
    {
        if (string.IsNullOrEmpty(playerNameField.text))
        {
            playerNameField.text = GameManager.Instance.playerName;
        }
        else
        {
            GameManager.Instance.playerName = playerNameField.text;
        }
    }
    
    public void CreateClick()
    {
        roomPswField.text = "";
        roomPswField.GetComponent<Image>().color=Color.gray;
        roomPswField.readOnly = true;
        roomNameField.text = "";
        psw_toggle.isOn = false;
        createRoomPanel.SetActive(true);
    }
    
    //主机创建房间
    public void OnCreateRoomConfirmClick()
    {
        if (string.IsNullOrEmpty(roomNameField.text))
        {
            Debug.Log("Room name empty");
            return;
        }

        if (psw_toggle.isOn && string.IsNullOrEmpty(roomPswField.text))
        {
            Debug.Log("Room password empty");
            return;
        }
        
        GameManager.Instance.CurrentGameState = GameState.Lobby;
        //TODO:启动Server，配置信息，进入大厅

        createRoomPanel.SetActive(false);
        networkDiscovery.ServerName = roomNameField.text;
        networkDiscovery.Password = roomPswField.text;
        networkDiscovery.StartServer();
        
        NetworkManager.Singleton.StartHost();
        Debug.Log("启动！");
        
        //lobbyPanel.SetActive(true);
        GotoLobby();
        
    }

    public void OnCreateRoomCancelClick()
    {
        createRoomPanel.SetActive(false);
    }
    

    public void JoinClick()
    {
        //NetworkManager.Singleton.StartClient();
    }


    public void RefreshClick()
    {
        NetworkShutDown();
        discoveredServers.Clear();
        networkDiscovery.StartClient();
        networkDiscovery.ClientBroadcast(new DiscoveryBroadcastData());
        RefreshRoomsUI();
    }

    private void RefreshRoomsUI()
    {
        for (int i = roomsList.Count-1; i >=0; i--)
        {
            Destroy(roomsList[i]);
        }
        roomsList.Clear();
        foreach (var server in discoveredServers)
        {
            AddRoom(server.Key,server.Value);
        }
    }

    private void AddRoom(IPAddress ipAddress,DiscoveryResponseData data)
    {
        string dataServerName = data.ServerName;
        string ip = ipAddress.ToString();
        GameObject room = Instantiate(roomSlotPrefab,roomContent);
        Debug.Log("Room Added"+room.name);
        Transform bg = room.transform.GetChild(0);
        GameObject lock_ = room.transform.GetChild(1).gameObject;
        TMP_Text nameField = bg.GetChild(0).GetComponent<TMP_Text>();
        TMP_Text ipField = bg.GetChild(1).GetComponent<TMP_Text>();

        bg.GetComponent<Button>().onClick.AddListener((() =>
        {
            RoomClick(ip,data);
        }));
        nameField.text = dataServerName;
        ipField.text = ip;
            
        //判断房间加密？
        lock_.SetActive(false);
            
        roomsList.Add(room);
    }

    public void RoomClick(string ip,DiscoveryResponseData data)
    {
        currentSelectedIp = ip;
        currentSelectedPsw = data.Password;
        JoinRoom(ip);
    }

    public void NetworkShutDown()
    {
        NetworkManager.Singleton.Shutdown();
    }
    
    
    public void JoinRoom(string IP)
    {
        //NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = IP;
        
        GameManager.Instance.CurrentGameState = GameState.Lobby;
        if (NetworkManager.Singleton.StartClient())
        {
            GotoLobby();
        }
        
        
    }

    public void GotoLobby(bool isWatcher =false)
    {
        //lobbyController.gameObject.SetActive(true);
        lobbyPanel.SetActive(true);
    }

    public void DebugIp()
    {
        NetworkTransport transport;
        
    }
    
    
    
    public void Toggle_SetPassword()
    {
        if (psw_toggle.isOn)
        {
            roomPswField.readOnly = false;
            roomPswField.GetComponent<Image>().color=Color.white;
            
        }
        else
        {
            roomPswField.text = "";
            roomPswField.GetComponent<Image>().color=Color.gray;
            roomPswField.readOnly = true;
        }
    }
    
}
