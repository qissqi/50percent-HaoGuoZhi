using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    //private bool loadLobbyCoroutine;

    [Header("状态-弃用")]
    private bool canLoadLobby;
    private bool loadingLobby;
    private CancellationTokenSource clientConnectToken;

    [Header("主菜单")] 
    [SerializeField] private Button manualJointButton;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TMP_InputField playerNameField;
    [SerializeField] private Transform roomContent;
    [SerializeField] private string currentTargetIp;
    [SerializeField] private int currentTargetPort; 
    [SerializeField] private bool currentSelectedNeedPsw;
    [SerializeField] private GameObject loadingPanel;
    
    [Header("创建房间界面")]
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private Toggle create_psw_toggle;
    [SerializeField] private TMP_InputField create_RoomNameField;
    [SerializeField] private TMP_InputField create_RoomPswField;
    [SerializeField] private Button create_Confirm;
    [SerializeField] private Button create_Cancel;

    [Header("手动加入房间")] 
    [SerializeField] private GameObject joinRoomPanel;
    [SerializeField] private TMP_InputField join_RoomIpField;
    [SerializeField] private TMP_InputField join_RoomPortField;
    [SerializeField] private Button join_Confirm;
    [SerializeField] private Button join_Cancel;

    [Header("输入密码")] 
    [SerializeField] private GameObject passwordPanel;
    [SerializeField] private Button passwordConfirm;
    [SerializeField] private Button passwordCancel;
    [SerializeField] private TMP_InputField passwordArea;
    [SerializeField] private string inputPassword ="";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // startPanel.SetActive(true);
            // gameObject.SetActive(false);
        }
        
    }

    private void Start()
    {
        playerNameField.text = GameManager.Instance.playerName;
        networkDiscovery = NetworkManager.Singleton.GetComponent<ExampleNetworkDiscovery>();
        networkDiscovery.OnServerFound.RemoveListener(OnServerFound);
        networkDiscovery.OnServerFound.AddListener(OnServerFound);
        
        GameManager.Instance.InitPlayerInfo();
        networkDiscovery.StartClient();
        networkDiscovery.ClientBroadcast(new DiscoveryBroadcastData());

        //交互层注册
        create_psw_toggle.onValueChanged.AddListener(Toggle_SetPassword);
        manualJointButton.onClick.AddListener(ManualJoinClick);
        createRoomButton.onClick.AddListener(CreateClick);
        refreshButton.onClick.AddListener(RefreshClick);
        create_Confirm.onClick.AddListener(OnCreateRoomConfirmClick);
        create_Cancel.onClick.AddListener(OnCreateRoomCancelClick);
        join_Confirm.onClick.AddListener(ManualJoinConfirmClick);
        join_Cancel.onClick.AddListener(ManualJoinCancelClick);
        passwordConfirm.onClick.AddListener(InputPasswordConfirmClick);
        passwordCancel.onClick.AddListener(InputPasswordCancelClick);
        
    }

    private void OnEnable()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += ClientConnectFailCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= ClientConnectFailCallback;
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnServerFound(IPEndPoint arg0, DiscoveryResponseData arg1)
    {
        Debug.Log($"Found Server: {arg0.Address}: {arg1.Port}");
        discoveredServers.Add(arg0,arg1);
        AddRoom(arg0.Address,arg0.Port,arg1);
    }

    // 编辑名字的检测
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
        create_psw_toggle.isOn = false;
        createRoomPanel.SetActive(true);
    }
    
    // 主机创建房间确认按钮
    public void OnCreateRoomConfirmClick()
    {
        if (string.IsNullOrEmpty(create_RoomNameField.text))
        {
            Debug.Log("Room name empty");
            return;
        }

        if (create_psw_toggle.isOn && string.IsNullOrEmpty(create_RoomPswField.text))
        {
            Debug.Log("Room password empty");
            return;
        }

        GameManager.Instance.CurrentGameState = GameState.Lobby;
        //TODO:启动Server，配置信息，进入大厅

        createRoomPanel.SetActive(false);
        networkDiscovery.ServerName = create_RoomNameField.text;
        if (create_psw_toggle.isOn)
        {
            networkDiscovery.NeedPassword = true;
            LobbyManager.Instance.RoomPassword = create_RoomPswField.text;
            LobbyManager.Instance.NeedPwd = true;
        }
        else
        {
            networkDiscovery.NeedPassword = false;
            LobbyManager.Instance.NeedPwd = false;
        }
        //networkDiscovery.Password = create_RoomPswField.text;
        //networkDiscovery.NeedPassword = create_psw_toggle.isOn;
        networkDiscovery.StartServer();
        
        Debug.Log("启动！");

        //lobbyPanel.SetActive(true);
        //TryGotoLobby(true);
        NetworkManager.Singleton.StartHost();

    }

    public void OnCreateRoomCancelClick()
    {
        createRoomPanel.SetActive(false);
    }

    public void ManualJoinClick()
    {
        join_RoomIpField.text = "";
        join_RoomPortField.text = "";
        joinRoomPanel.SetActive(true);

    }

    public void ManualJoinConfirmClick()
    {
        if(string.IsNullOrEmpty(join_RoomIpField.text) || string.IsNullOrEmpty(join_RoomPortField.text))
            return;
        
        SetConnectionData(Array.Empty<byte>());
        currentTargetIp = join_RoomIpField.text;
        currentTargetPort = int.Parse(join_RoomPortField.text);
        JoinRoom(join_RoomIpField.text,int.Parse(join_RoomPortField.text));
        joinRoomPanel.SetActive(false);
        
    }

    private void ManualJoinCancelClick()
    {
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
            RoomClick(ip,data.Port,data);
        }));
        nameField.text = dataServerName;
        ipField.text = ip;
            
        //判断房间加密？
        lock_.SetActive(data.NeedPassword);
            
        roomsList.Add(room);
    }

    public void RoomClick(string ip,int port,DiscoveryResponseData data)
    {
        currentTargetIp = ip;
        currentTargetPort = port;
        currentSelectedNeedPsw = data.NeedPassword;
        SetConnectionData(Array.Empty<byte>());
        if (currentSelectedNeedPsw)
        {
            OpenPasswordPanel();
        }
        else
        {
            JoinRoom(currentTargetIp,currentTargetPort);
        }
    }
    
    //房间需要输入密码，打开输入密码的面板
    private void OpenPasswordPanel()
    {
        passwordArea.text = "";
        passwordPanel.SetActive(true);
    }
    
    //输入完密码确认
    private void InputPasswordConfirmClick()
    {
        inputPassword = passwordArea.text;
        byte[] psw;
        psw = Encoding.UTF8.GetBytes(inputPassword);
        SetConnectionData(psw);
        passwordPanel.SetActive(false);
        JoinRoom(currentTargetIp,currentTargetPort);
    }

    private void InputPasswordCancelClick()
    {
        passwordPanel.SetActive(false);
    }

    public void NetworkShutDown()
    {
        NetworkManager.Singleton.Shutdown();
    }
    
    private void SetConnectionData(byte[] data)
    {
        NetworkManager.Singleton.NetworkConfig.ConnectionData = data;
    }
    
    public void JoinRoom(string IP,int port)
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = IP;
        NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = (ushort)port;
        
        GameManager.Instance.CurrentGameState = GameState.Lobby;
        loadingPanel.SetActive(true);
        
        //TryGotoLobby(false);
        NetworkManager.Singleton.StartClient();
    }

    public void Toggle_SetPassword(bool toggle)
    {
        if (toggle)
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

    public void TryGotoLobby(bool isServer)
    {
        canLoadLobby = false;
        loadingLobby = true;
        
        if (isServer)
        {
            if (NetworkManager.Singleton.StartHost())
            {
                // if (loadLobbyCoroutine == false)
                // {
                //     //loadLobbyCoroutine = StartCoroutine(LoadLobbyScene());
                //     loadLobbyCoroutine = true;
                //     LoadLobbyScene().Forget();
                // }

                GotoLobby();
            }
        }
        //非房主，两步走：尝试加入+密码验证
        else
        {
            if (NetworkManager.Singleton.StartClient())
            {
                clientConnectToken?.Cancel();
                clientConnectToken?.Dispose();
                clientConnectToken = new CancellationTokenSource();
                // WaitForConnectSuccess(clientConnectToken.Token).Forget();
            }
        }
    }

    //连接成功
    private void OnClientConnected(ulong netId)
    {
        // if (loadLobbyCoroutine == false)
        // {
        //     //loadLobbyCoroutine = StartCoroutine(LoadLobbyScene());
        //     loadLobbyCoroutine = true;
        //     LoadLobbyScene().Forget();
        // }
        if (netId == 0)
        {
            loadingPanel.SetActive(false);
            GotoLobby();
        }
    }

    //掉线或拒绝连接
    private void ClientConnectFailCallback(ulong obj)
    {
        loadingPanel.SetActive(false);
        if (!NetworkManager.Singleton.IsServer)
        {
            string reason = NetworkManager.Singleton.DisconnectReason;
            //连接被拒绝
            if (!string.IsNullOrEmpty(reason))
            {
                Debug.Log("Connect fail:\n"+reason);
                switch (reason)
                {
                    case ApprovalDeclinedReason.NEEDPASSWORD:
                        OpenPasswordPanel();
                        break;
                    case ApprovalDeclinedReason.WRONGPASSWORD:
                        OpenPasswordPanel();
                        break;
                }
            }
        }
    }

    public void GotoLobby()
    {
        SceneManager.LoadScene("Lobby", LoadSceneMode.Additive);
    }

    private async UniTaskVoid LoadLobbyScene()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync("Lobby",LoadSceneMode.Additive);  //协程操作变量为operation
        operation.allowSceneActivation = false; //加载完成时先不要自动跳转，允许也可以
		
        while(!operation.isDone)   //加载未完成，改变进度条
        {
            float progress = operation.progress / 0.9f;  //异步加载进程值为 0~0.9，需要除0.9 获得实际进度
			
            if(operation.progress>=0.9f && canLoadLobby)
            {
                operation.allowSceneActivation = true;
                canLoadLobby = false;
                break;
            }
            //yield return null; //协程别忘了
            await UniTask.NextFrame();
        }

        await UniTask.NextFrame();
        SceneManager.SetActiveScene(SceneManager.GetSceneAt(1));
        loadingLobby = false;
    } 
    
}
