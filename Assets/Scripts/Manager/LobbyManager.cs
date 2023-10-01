using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

//仅作为房主时使用
public class LobbyManager : Singleton<LobbyManager>
{
    public static Lobby LobbyUI;
    public string RoomPassword ="";
    public bool NeedPwd;
    private LobbyConnector lobbyConnector;
    [SerializeField] private GameObject lobbyConnectorPrefab;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }

    //大厅UI加载出时注册
    public void RegisterLobbyUi(Lobby lobby)
    {
        LobbyUI = lobby;
        if (NetworkManager.Singleton.IsServer)
        {
            GameObject connector = Instantiate(lobbyConnectorPrefab);
            connector.GetComponent<NetworkObject>().Spawn();
        }
    }

    public void RegisterLobbyConnector(LobbyConnector connector)
    {
        lobbyConnector = connector;
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnect;
        }
        else
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnHolderDisconnect;
        }
    }
    
    //UI界面离开时
    public void LeaveLobby()
    {
        GameManager.Instance.InitPlayerInfo();
        //LobbyPanel.gameObject.SetActive(false);
        NetworkManager.Singleton.Shutdown();
        UnloadLobby();
        
        LobbyUI = null;
        lobbyConnector = null;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnHolderDisconnect;
        NetworkManager.Singleton.GetComponent<ExampleNetworkDiscovery>().StopDiscovery();
    }

    public void ChooseCharacter(int playerId,int index)
    {
        lobbyConnector.ChooseCharacterServerRpc(playerId,index);
    }

    public void Ready(int ownerInstanceGivenId)
    {
        lobbyConnector.ReadyServerRpc(ownerInstanceGivenId);
    }
    
    public void StartCheck()
    {
        if(GameManager.Instance.playersInfo.playerCount<2)
            return;
        if(GameManager.Instance.playersInfo.ChosenCharacter[NetPlayer.OwnerInstance.GivenId]<0)
            return;
            
        GameManager.Instance.playersInfo.Ready[NetPlayer.OwnerInstance.GivenId] = true;
            
        int canStart = Array.FindIndex(GameManager.Instance.playersInfo.Ready,
            0,GameManager.Instance.playersInfo.playerCount, x => !x);
        // -1为可开始
        if (canStart == -1)
        {
                
            Array.Fill(GameManager.Instance.playersInfo.Ready,false);
            lobbyConnector.StartGameClientRpc();
        }
        else
        {
            GameManager.Instance.playersInfo.Ready[NetPlayer.OwnerInstance.GivenId] = false;
        }
    }


    //检查客户端连接
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest arg1,
        NetworkManager.ConnectionApprovalResponse arg2)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        byte[] byteData = arg1.Payload;
        ulong clientId = arg1.ClientNetworkId;
        if (clientId==0)
        {
            arg2.Approved = true;
            arg2.CreatePlayerObject = true;
            return;
        }
        string message = Encoding.UTF8.GetString(byteData);
        Debug.Log($"{clientId}: {message}");

        if (NeedPwd)
        {
            //空密码
            if (string.IsNullOrEmpty(message))
            {
                arg2.Approved = false;
                arg2.Reason = ApprovalDeclinedReason.NEEDPASSWORD;
            }
            else
            {
                //密码正确
                if (message == RoomPassword)
                {
                    arg2.Approved = true;
                    arg2.CreatePlayerObject = true;
                }
                //密码错误
                else
                {
                    arg2.Approved = false;
                    arg2.Reason = ApprovalDeclinedReason.WRONGPASSWORD;
                }
            }
        }
        //不需要密码
        else
        {
            arg2.Approved = true;
            arg2.CreatePlayerObject = true;
        }

    }
    
    
    //其他玩家离开大厅刷新,仅由Server生效
    private void OnPlayerDisconnect(ulong netId)
    {
        int id = GameManager.Instance.FindPlayerIdByClientId((int)netId);
        if(id==-1)
            return;
        
        lobbyConnector.RefreshCharacterSelectClientRpc(GameManager.Instance.playersInfo.ChosenCharacter[id],true);
        lobbyConnector.CancelReadyServerRpc(id);
        
        GameManager.Instance.RemovePlayerByNetId((int)netId);
        for (int i = 0; i < GameManager.Instance.playersInfo.max; i++)
        {
            lobbyConnector.UpdatePlayerInLobbyClientRpc(i,
                GameManager.Instance.playersInfo.PlayerNames[i],
                GameManager.Instance.playersInfo.ChosenCharacter[i],false);
        }
        
        for (int i = 0; i < GameManager.Instance.playersInfo.playerCount; i++)
        {
            NetPlayer.OwnerInstance.SetGivenIdClientRpc(i,
                new ClientRpcParams(){Send = new ClientRpcSendParams()
                {
                    TargetClientIds = new ulong[]{(ulong)GameManager.Instance.playersInfo.netId[i]}
                }});
        }
    }

    //房主离开游戏
    private void OnHolderDisconnect(ulong cid)
    {
        if (cid == 0)
        {
            Debug.Log("Server Stop");
            LobbyUI.LobbyPanel.gameObject.SetActive(false);
            NetworkManager.Singleton.Shutdown();
        }
    }
    
    private async UniTaskVoid UnloadLobby()
    {
        AsyncOperation operation = SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());
        while (!operation.isDone)
        {
            if (operation.progress >= 0.9f)
            {
                operation.allowSceneActivation = true;
                break;
            }

            await UniTask.NextFrame();
        }

        //SceneManager.SetActiveScene(SceneManager.GetSceneAt(0));
    }

    //Server
    public void NewPlayerRegisterLobby(int netId,int id, string _name,int playerCount)
    {
        ref AllPlayersInfo infos = ref GameManager.Instance.playersInfo;
        
        lobbyConnector.NewPlayerInitLobbyClientRpc(id,new ClientRpcParams(){Send = new ClientRpcSendParams()
        {
            TargetClientIds = new ulong[]{(ulong)netId}
        }});

        lobbyConnector.LoadNewPlayerInfoClientRpc(id, _name);
        
        //新玩家加载当前已有信息
        for (int i = 0; i < playerCount; i++)
        {
            lobbyConnector.LoadCurrentPlayerInfoClientRpc(i,infos.PlayerNames[i],
                infos.ChosenCharacter[i],infos.Ready[i],
                new ClientRpcParams(){Send = new ClientRpcSendParams() {
                        TargetClientIds = new ulong[]{(ulong)netId} 
                }});
        }

    }
    
    
}
