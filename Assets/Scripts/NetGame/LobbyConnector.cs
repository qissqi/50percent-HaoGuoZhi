using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyConnector : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        LobbyManager.Instance.RegisterLobbyConnector(this);
    }

    [ClientRpc]
    public void UpdatePlayerInLobbyClientRpc(int id, string _name, int character, bool ready)
    {
        if (!LobbyManager.LobbyUI)
            return;

        LobbyManager.LobbyUI.UpdatePlayerInLobby(id, _name, character, ready);
    }

    [ClientRpc]
    public void RefreshCharacterSelectClientRpc(int index, bool canChoose)
    {
        if (!LobbyManager.LobbyUI)
            return;
        LobbyManager.LobbyUI.UpdateCharacterSelect(index, canChoose);
    }
    
    //新玩家重置房间信息
    [ClientRpc]
    public void NewPlayerInitLobbyClientRpc(int id,ClientRpcParams rpcParams)
    {
        LobbyManager.LobbyUI.InitLobby(id,IsServer);
    }
    
    //加载新玩家信息
    [ClientRpc]
    public void LoadNewPlayerInfoClientRpc(int id, string _name)
    {
        LobbyManager.LobbyUI.UpdatePlayerInLobby(id,_name,-1,false);
    }
    
    //新玩家加载当前信息
    [ClientRpc] 
    public void LoadCurrentPlayerInfoClientRpc(int id, string _name, int character,bool ready,ClientRpcParams rpcParams)
    {
        LobbyManager.LobbyUI.UpdatePlayerInLobby(id,_name,character,ready);
        LobbyManager.LobbyUI.UpdateCharacterSelect(character,false);
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void ChooseCharacterServerRpc(int id, int character, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log("Server: Choose-" + character);
        int lastChooseCharacter = GameManager.Instance.playersInfo.ChosenCharacter[id];
        GameManager.Instance.playersInfo.ChosenCharacter[id] = character;
        UpdatePlayerInLobbyClientRpc(id,
            GameManager.Instance.playersInfo.PlayerNames[id], character, false);

        RefreshCharacterSelectClientRpc(lastChooseCharacter, true);
        RefreshCharacterSelectClientRpc(character, false);
    }


    [ServerRpc(RequireOwnership = false)]
    public void ReadyServerRpc(int id)
    {
        GameManager.Instance.playersInfo.Ready[id] = true;
        ReadyClientRpc(id);
    }

    [ClientRpc]
    private void ReadyClientRpc(int id)
    {
        LobbyManager.LobbyUI.PlayerLobbyInfos[id].ReadyMark.SetActive(true);
    }

    [ServerRpc(RequireOwnership = false)]
    public void CancelReadyServerRpc(int id)
    {
        GameManager.Instance.playersInfo.Ready[id] = true;
        CancelReadyClientRpc(id);
    }

    [ClientRpc]
    private void CancelReadyClientRpc(int id)
    {
        LobbyManager.LobbyUI.PlayerLobbyInfos[id].ReadyMark.SetActive(false);
    }

    [ClientRpc]
    public void StartGameClientRpc()
    {
        GameManager.Instance.CurrentGameState = GameState.Playing;
        NetworkManager.Singleton.NetworkConfig.EnableSceneManagement = true;
        
        // NetworkManager.Singleton.SceneManager.LoadScene("Zone2", LoadSceneMode.Single);
        // SceneManager.LoadScene("Zone2", LoadSceneMode.Single);
        Debug.Log("YS，启动！");
    }

}