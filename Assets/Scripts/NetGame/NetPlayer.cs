using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetPlayer : NetworkBehaviour
{
    public static NetPlayer OwnerInstance { private set; get; }
    public string PlayerName;
    public int GivenId;
    public int ChosenCharacter;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            PlayerName = GameManager.Instance.playerName;
            OwnerInstance = this;
            PlayerRegisterServerRpc(PlayerName,(int)OwnerClientId);
        }
        DontDestroyOnLoad(this);
    }
    
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
            PlayerRegisterClientRpc(i,infos.PlayerNames[i],infos.netId[i],infos.ChosenCharacter[i]);
            // GameManager.Instance.playersInfo.playerCount = i + 1;
            // GameManager.Instance.playersInfo.PlayerNames[i] = infos.PlayerNames[i];
            // GameManager.Instance.playersInfo.netId[i] = infos.netId[i];
            // Lobby.Instance.RefreshPlayerInLobby(i,infos.PlayerNames[i],-1);
        }
        
    }
    
    [ClientRpc]
    private void PlayerRegisterClientRpc(int id,string _name,int netId,int character)
    {
        GameManager.Instance.playersInfo.playerCount = id + 1;
        GameManager.Instance.playersInfo.PlayerNames[id] = _name;
        GameManager.Instance.playersInfo.netId[id] = netId;
        Lobby.Instance.RefreshPlayerInLobby(id,_name,character);
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

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }

}
