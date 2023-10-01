using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
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

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        WaitLobbyLoad().Forget();

        //DontDestroyOnLoad(this);
    }

    public async UniTaskVoid WaitLobbyLoad()
    {
        while (!LobbyManager.LobbyUI)
        {
            await UniTask.NextFrame();
        }
        await UniTask.NextFrame();
        await UniTask.NextFrame();
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
    }

    #region 大厅登录
    
    [ServerRpc]     //新玩家进入并注册，加载新玩家信息，新玩家加载其他已有信息
    private void PlayerRegisterServerRpc(string _name,int netId)
    {
        ref AllPlayersInfo infos = ref GameManager.Instance.playersInfo;
        infos.PlayerNames[infos.playerCount] = _name;
        infos.netId[infos.playerCount] = netId;
        var tmp_id = infos.playerCount;
        
        SetGivenIdClientRpc(infos.playerCount,
            new ClientRpcParams(){Send = new ClientRpcSendParams()
        {
            TargetClientIds = new ulong[]{(ulong)netId}
        }});
        PlayerRegisterClientRpc(tmp_id, _name, netId);
        
        LobbyManager.Instance.NewPlayerRegisterLobby(netId,tmp_id,_name,infos.playerCount);
        
    }
    
    [ClientRpc]     //加载新玩家信息
    private void PlayerRegisterClientRpc(int id,string _name,int netId)
    {
        GameManager.Instance.playersInfo.playerCount = id + 1;
        GameManager.Instance.playersInfo.PlayerNames[id] = _name;
        GameManager.Instance.playersInfo.netId[id] = netId;
        //LobbyManager.LobbyUI.ReloadLobbyState(id,_name,character,ready);
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
