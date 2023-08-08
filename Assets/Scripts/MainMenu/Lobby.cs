using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Lobby : NetworkBehaviour
{
    public static Lobby Instance;
    public Action OnPlayerConnectLobby;

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
    public bool isReady=false;
    
    public void InitLobby(int id,bool server)
    {
        //重置角色选择
        // for (int i = 0; i < 6; i++)
        // {
        //     selects.GetChild(i).GetComponent<Image>().color=Color.white;
        //     selects.GetChild(i).GetComponent<Button>().interactable = true;
        // }
        
        //重置玩家卡
        allPlayerInfo.GetChild(id).GetComponent<Image>().color = new Color(0, 0.6f, 1);
        if (server)
        {
            ReadyButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "开始";
        }
    }
    
    public void RefreshPlayerInLobby(int id,string _name,int character)
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
    private void RefreshPlayerInLobbyClientRpc(int id,string _name,int character)
    {
        RefreshPlayerInLobby(id, _name, character);
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

        LobbySpawnLoaServerRpc();
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
    
    public override void OnNetworkDespawn()
    {
        Debug.Log("despawn");
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnect;
    }

    public void ChooseCharacterClick(int index)
    {
        Debug.Log("Choose "+index);
        int playerId = NetPlayer.OwnerInstance.GivenId;
        
        ChooseCharacterServerRpc(playerId,index);
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChooseCharacterServerRpc(int id,int character)
    {
        int lastChooseCharacter = GameManager.Instance.playersInfo.ChosenCharacter[id];
        GameManager.Instance.playersInfo.ChosenCharacter[id] = character; 
        RefreshPlayerInLobbyClientRpc(id,
            GameManager.Instance.playersInfo.PlayerNames[id],character);

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
            
            GameManager.Instance.playersInfo.Ready[NetPlayer.OwnerInstance.GivenId] = true;
            
            int canStart = Array.FindIndex(GameManager.Instance.playersInfo.Ready,
                0,GameManager.Instance.playersInfo.playerCount, x => !x);
            // -1为可开始
            if (canStart == -1)
            {
                StartGameClientRpc();
            }

        }
        
        //客户端：准备
        else if (IsClient)
        {
            ReadyServerRpc(NetPlayer.OwnerInstance.GivenId);
            ReadyButton.interactable = false;
            ReadyButton.image.color = Color.gray;
        }
        
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void ReadyServerRpc(int id)
    {
        GameManager.Instance.playersInfo.Ready[id] = true;
    }
    
    [ClientRpc]
    private void ReadyClientRpc(int id)
    {
        
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("YS，启动！");
    }
    
    
    public void LeaveLobbyClick()
    {
        LobbyPanel.gameObject.SetActive(false);
        NetworkManager.Singleton.Shutdown();
        
    }
    
    //其他玩家离开大厅刷新,仅由Server调用
    private void OnPlayerDisconnect(ulong netId)
    {
        int id = GameManager.Instance.FindPlayerIdByClientId((int)netId);
        RefreshCharacterSelectClientRpc(GameManager.Instance.playersInfo.ChosenCharacter[id],true);
        
        GameManager.Instance.RemovePlayerByNetId((int)netId);
        for (int i = 0; i < GameManager.Instance.playersInfo.max; i++)
        {
            RefreshPlayerInLobbyClientRpc(i,
                GameManager.Instance.playersInfo.PlayerNames[i],
                GameManager.Instance.playersInfo.ChosenCharacter[i]);
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

}


    
