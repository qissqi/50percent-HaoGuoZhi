using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Lobby : NetworkBehaviour
{
    public static Lobby Instance;

    private void Awake()
    {
        Instance = this;
    }

    [Header("引用")] 
    public Transform allPlayerInfo;
    public Transform selects;
    public Transform LobbyPanel;
    public Button ReadyButton;

    [Header("数据")] 
    public bool Choosed=false;

    //测试用
    private void Update()
    {
        return;
        if(!IsSpawned)
            return;

        ServerRpcParams rpcParams = new ServerRpcParams()
        {
            Receive = new ServerRpcReceiveParams()
            {
                SenderClientId = NetPlayer.OwnerInstance.OwnerClientId
            }
        };
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            TestServerRpc(KeyCode.J, rpcParams);
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            TestServerRpc(KeyCode.K, rpcParams);
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            TestServerRpc(KeyCode.L, rpcParams);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TestServerRpc(KeyCode key, ServerRpcParams rpcParams)
    {
        switch (key)
        {
            case KeyCode.J:
                var go1 = Instantiate(GameManager.Instance.TestObject);
                //go.GetComponent<NetworkObject>().Spawn();
                break;
            case KeyCode.K:
                var go2 = Instantiate(GameManager.Instance.TestObject);
                go2.GetComponent<NetworkObject>().Spawn();
                break;
            case KeyCode.L:
                var go3 = Instantiate(GameManager.Instance.TestObject);
                go3.GetComponent<NetworkObject>().SpawnAsPlayerObject(rpcParams.Receive.SenderClientId);
                break;
        }
    }

    public void InitLobby(int id,bool server)
    {
        //重置角色选择
        // for (int i = 0; i < 6; i++)
        // {
        //     selects.GetChild(i).GetComponent<Image>().color=Color.white;
        //     selects.GetChild(i).GetComponent<Button>().interactable = true;
        // }

        Choosed = false;
        //重置玩家卡和状态标
        for (int i = 0; i < GameManager.Instance.playersInfo.max; i++)
        {
            if (i == id)
            {
                allPlayerInfo.GetChild(id).GetComponent<Image>().color = new Color(0, 0.6f, 1);
            }
            else
            {
                allPlayerInfo.GetChild(i).GetComponent<Image>().color = Color.white;
            }
            allPlayerInfo.GetChild(i).GetChild(2).gameObject.SetActive(false);
        }
        
        //重置准备状态标
        if (server)
        {
            ReadyButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "开始";
        }
    }
    
    public void RefreshPlayerInLobby(int id,string _name,int character,bool ready)
    {
        allPlayerInfo.GetChild(id).GetChild(0).GetComponent<TMP_Text>().text = _name;
        if (character != -1)
        {
            allPlayerInfo.GetChild(id).GetChild(1).GetComponent<Image>().sprite =
                GameManager.Instance.GameCharacterSpriteList.characters[character];
            
        }
        else
        {
            allPlayerInfo.GetChild(id).GetChild(1).GetComponent<Image>().sprite =
                GameManager.Instance.GameCharacterSpriteList.Empty;
        }
        
    }

    private void RefreshCharacterSelect(int index, bool canChoose)
    {
        if(index<0)
            return;
        selects.GetChild(index).GetComponent<Image>().color =
            canChoose ? Color.white : Color.grey;
        selects.GetChild(index).GetComponent<Button>().interactable = canChoose;
    }
    
    [ClientRpc]
    private void RefreshPlayerInLobbyClientRpc(int id,string _name,int character,bool ready)
    {
        RefreshPlayerInLobby(id, _name, character,ready);
    }

    [ClientRpc]
    private void RefreshCharacterSelectClientRpc(int index, bool canChoose)
    {
        RefreshCharacterSelect(index,canChoose);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnect;
        }
        else
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnHolderDisconnect;
        }
        
        LobbySpawnLoaServerRpc();
    }
    public override void OnNetworkDespawn()
    {
        Debug.Log("despawn");
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnHolderDisconnect;
        
    }

    private void OnDisable()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnect;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnHolderDisconnect;
    }

    [ServerRpc(RequireOwnership = false)]
    private void LobbySpawnLoaServerRpc()
    {
        //重置角色选择
        for (int i = 0; i < 6; i++)
        {
            selects.GetChild(i).GetComponent<Image>().color=Color.white;
            selects.GetChild(i).GetComponent<Button>().interactable = true;
        }

        for (int i = 0; i < GameManager.Instance.playersInfo.playerCount; i++)
        {
            RefreshCharacterSelectClientRpc(
                GameManager.Instance.playersInfo.ChosenCharacter[i],false);
        }
        
    }
    

    public void ChooseCharacterClick(int index)
    {
        Debug.Log("Choose "+index);
        int playerId = NetPlayer.OwnerInstance.GivenId;
        
        ChooseCharacterServerRpc(playerId,index);
        Choosed = true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChooseCharacterServerRpc(int id,int character)
    {
        int lastChooseCharacter = GameManager.Instance.playersInfo.ChosenCharacter[id];
        GameManager.Instance.playersInfo.ChosenCharacter[id] = character; 
        RefreshPlayerInLobbyClientRpc(id,
            GameManager.Instance.playersInfo.PlayerNames[id],character,false);

        RefreshCharacterSelectClientRpc(lastChooseCharacter, true);
        RefreshCharacterSelectClientRpc(character,false);

    }


    public void ReadyButtonClick()
    {
        //服务端：开始
        if (IsServer)
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
                StartGameClientRpc();
                NetworkManager.Singleton.SceneManager.LoadScene("Zone2", LoadSceneMode.Single);
            }
            else
            {
                GameManager.Instance.playersInfo.Ready[NetPlayer.OwnerInstance.GivenId] = false;
            }

        }
        
        //客户端：检查选角，准备
        else if (IsClient)
        {
            if(!Choosed)
                return;
            ReadyServerRpc(NetPlayer.OwnerInstance.GivenId);
            ReadyButton.interactable = false;
            ReadyButton.image.color = Color.gray;
        }
        
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReadyServerRpc(int id)
    {
        GameManager.Instance.playersInfo.Ready[id] = true;
        ReadyClientRpc(id);
    }
    
    [ClientRpc]
    private void ReadyClientRpc(int id)
    {
        allPlayerInfo.GetChild(id).GetChild(2).gameObject.SetActive(true);
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        GameManager.Instance.CurrentGameState = GameState.Playing;
        Debug.Log("YS，启动！");
    }
    
    
    public void LeaveLobbyClick()
    {
        if (IsServer)
        {
            GameManager.Instance.CreatePlayerInfo();
        }
        LobbyPanel.gameObject.SetActive(false);
        NetworkManager.Singleton.Shutdown();
        
    }
    
    //其他玩家离开大厅刷新,仅由Server调用
    private void OnPlayerDisconnect(ulong netId)
    {
        int id = GameManager.Instance.FindPlayerIdByClientId((int)netId);
        if(id==-1)
            return;
        
        RefreshCharacterSelectClientRpc(GameManager.Instance.playersInfo.ChosenCharacter[id],true);
        
        GameManager.Instance.RemovePlayerByNetId((int)netId);
        for (int i = 0; i < GameManager.Instance.playersInfo.max; i++)
        {
            RefreshPlayerInLobbyClientRpc(i,
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

    private void OnHolderDisconnect(ulong cid)
    {
        if (cid == 0)
        {
            Debug.Log("Server Stop");
            LobbyPanel.gameObject.SetActive(false);
            NetworkManager.Singleton.Shutdown();
        }
    }
    
    
}


    
