using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject roomSlotPrefab;
    private ExampleNetworkDiscovery networkDiscovery;
    Dictionary<IPEndPoint, DiscoveryResponseData> discoveredServers = new Dictionary<IPEndPoint, DiscoveryResponseData>();
    private List<GameObject> roomsList=new List<GameObject>();
    //[SerializeField] private Lobby lobbyController;
    [SerializeField] private GameObject lobbyPanel;
    

    [Header("主菜单")] 
    [SerializeField] private TMP_InputField playerNameField;
    [SerializeField] private Transform roomContent;
    //[SerializeField] private GameObject currentSelectedRoom;
    [SerializeField] private string currentSelectedIp;
    [SerializeField] private int currenSelectedPort; 
    [SerializeField] private string currentSelectedPsw;
    
    [Header("创建房间界面")]
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private Toggle psw_toggle;
    [SerializeField] private TMP_InputField create_RoomNameField;
    [SerializeField] private TMP_InputField create_RoomPswField;

    [Header("手动加入房间")] 
    [SerializeField] private GameObject joinRoomPanel;
    [SerializeField] private TMP_InputField join_RoomIpField;
    [SerializeField] private TMP_InputField join_RoomPortField;
    [SerializeField] private Button join_ConfirmButton;
    

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
        discoveredServers.Add(arg0,arg1);
        AddRoom(arg0.Address,arg0.Port,arg1);
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
        create_RoomPswField.text = "";
        create_RoomPswField.GetComponent<Image>().color=Color.gray;
        create_RoomPswField.readOnly = true;
        create_RoomNameField.text = GameManager.Instance.playerName;
        psw_toggle.isOn = false;
        createRoomPanel.SetActive(true);
    }
    
    //主机创建房间
    public void OnCreateRoomConfirmClick()
    {
        if (string.IsNullOrEmpty(create_RoomNameField.text))
        {
            Debug.Log("Room name empty");
            return;
        }

        if (psw_toggle.isOn && string.IsNullOrEmpty(create_RoomPswField.text))
        {
            Debug.Log("Room password empty");
            return;
        }
        
        GameManager.Instance.CurrentGameState = GameState.Lobby;
        //TODO:启动Server，配置信息，进入大厅

        createRoomPanel.SetActive(false);
        networkDiscovery.ServerName = create_RoomNameField.text;
        networkDiscovery.Password = create_RoomPswField.text;
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
        join_RoomIpField.text = "";
        join_RoomPortField.text = "";
        joinRoomPanel.SetActive(true);

    }

    public void JoinConfirmClick()
    {
        if(string.IsNullOrEmpty(join_RoomIpField.text) || string.IsNullOrEmpty(join_RoomPortField.text))
            return;
        
        JoinRoom(join_RoomIpField.text,int.Parse(join_RoomPortField.text));
        joinRoomPanel.SetActive(false);
        
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
            AddRoom(server.Key.Address,server.Key.Port,server.Value);
        }
    }

    private void AddRoom(IPAddress ipAddress,int port,DiscoveryResponseData data)
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
            RoomClick(ip,port,data);
        }));
        nameField.text = dataServerName;
        ipField.text = ip;
            
        //判断房间加密？
        lock_.SetActive(false);
            
        roomsList.Add(room);
    }

    public void RoomClick(string ip,int port,DiscoveryResponseData data)
    {
        currentSelectedIp = ip;
        currenSelectedPort = port;
        currentSelectedPsw = data.Password;
        JoinRoom(ip,port);
    }

    public void NetworkShutDown()
    {
        NetworkManager.Singleton.Shutdown();
    }
    
    
    public void JoinRoom(string IP,int port)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = IP;
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = (ushort)port;
        
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
    
    
    
    public void Toggle_SetPassword()
    {
        if (psw_toggle.isOn)
        {
            create_RoomPswField.readOnly = false;
            create_RoomPswField.GetComponent<Image>().color=Color.white;
            
        }
        else
        {
            create_RoomPswField.text = "";
            create_RoomPswField.GetComponent<Image>().color=Color.gray;
            create_RoomPswField.readOnly = true;
        }
    }
    
}
