using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetPlayer : NetworkBehaviour
{
    public static NetPlayer OwnerInstance { private set; get; }
    public string PlayerName;
    public int GivenId;
    public int ChosenCharacter;

    //测试用
    private void Update()
    {
        if(!GameDataManager.Instance)
            return;
        
        if (Input.GetKeyDown(KeyCode.I))
        {
            TestSServerRpc(KeyCode.I);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            TestSServerRpc(KeyCode.I);
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            TestSServerRpc(KeyCode.I);
        }
    }

    [ServerRpc]
    private void TestSServerRpc(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.I:
                break;
            case KeyCode.O:
                break;
            case KeyCode.P:
                break;
        }
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            if (GameManager.Instance.CurrentGameState == GameState.Lobby)
            {
                PlayerName = GameManager.Instance.playerName;
                OwnerInstance = this;
                PlayerRegisterServerRpc(PlayerName,(int)OwnerClientId);
                
                if (IsServer)
                {
                    NetworkManager.Singleton.SceneManager.OnLoadComplete += OnGameSceneLoaded;
                }
            }
            
            Debug.Log("Spawn at"+GameManager.Instance.CurrentGameState);
            
        }
        //DontDestroyOnLoad(this);
    }

    #region 大厅登录
    
    [ServerRpc]
    private void PlayerRegisterServerRpc(string name,int netId)
    {
        ref AllPlayersInfo infos = ref GameManager.Instance.playersInfo;
        infos.PlayerNames[infos.playerCount] = name;
        infos.netId[infos.playerCount] = netId;
        var count = infos.playerCount;
        
        SetGivenIdClientRpc(infos.playerCount,
            new ClientRpcParams(){Send = new ClientRpcSendParams()
        {
            TargetClientIds = new ulong[]{(ulong)netId}
        }});
        InitLobbyClientRpc(infos.playerCount,new ClientRpcParams(){Send = new ClientRpcSendParams()
        {
            TargetClientIds = new ulong[]{(ulong)netId}
        }});
        
        for (int i = 0; i < count+1; i++)
        {
            PlayerRegisterClientRpc(i,infos.PlayerNames[i],infos.netId[i],
                infos.ChosenCharacter[i],infos.Ready[i]);
            // GameManager.Instance.playersInfo.playerCount = i + 1;
            // GameManager.Instance.playersInfo.PlayerNames[i] = infos.PlayerNames[i];
            // GameManager.Instance.playersInfo.netId[i] = infos.netId[i];
            // Lobby.Instance.RefreshPlayerInLobby(i,infos.PlayerNames[i],-1);
        }
        
    }
    
    [ClientRpc]
    private void PlayerRegisterClientRpc(int id,string _name,int netId,int character,bool ready)
    {
        GameManager.Instance.playersInfo.playerCount = id + 1;
        GameManager.Instance.playersInfo.PlayerNames[id] = _name;
        GameManager.Instance.playersInfo.netId[id] = netId;
        Lobby.Instance.RefreshPlayerInLobby(id,_name,character,ready);
    }

    [ClientRpc]
    private void InitLobbyClientRpc(int id,ClientRpcParams rpcParams)
    {
        Lobby.Instance.InitLobby(id,IsServer);
    }
    
    
    [ClientRpc]
    public void SetGivenIdClientRpc(int givenId,ClientRpcParams rpcParams)
    {
        GivenId = givenId;
    }
    
    #endregion
    
    //进入正式游戏场景，Server调用
    private void OnGameSceneLoaded(ulong clientid, string scenename, LoadSceneMode loadscenemode)
    {
        if(GameManager.Instance.CurrentGameState!=GameState.Playing)
            return;
        Debug.Log($"{GameManager.Instance.FindPlayerIdByClientId((int)clientid)}({clientid}) load scene complete");
        
        // var p = Instantiate(GameManager.Instance.TestObject).GetComponent<NetworkObject>();
        // p.SpawnWithOwnership(clientid);
        // p.DontDestroyWithOwner = true;
    }

}
