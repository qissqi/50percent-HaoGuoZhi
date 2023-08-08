using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : Singleton<GameManager>
{
    
    [Header("Data")] 
    public CharacterList GameCharacterSpriteList;

    [Header("GameServerInfo")] 
    // public List<NetworkObject> AllPlayer = new List<NetworkObject>();
    public string playerName;
    public GameState CurrentGameState;

    public AllPlayersInfo playersInfo; 
    
    private void Start()
    {
        CurrentGameState = GameState.None;
        DontDestroyOnLoad(gameObject);
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCall;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCall;
        
    }

    private void OnClientDisconnectedCall(ulong obj)
    {
        Debug.Log($"Client Disconnect : {obj}");
    }

    private void OnClientConnectedCall(ulong obj)
    {
        Debug.Log($"Client Join : {obj}");
    }

    public void CreatePlayerInfo()
    {
        playersInfo = new AllPlayersInfo(4);
    }
    
    public void RemovePlayerByNetId(int netId)
    {
        playersInfo.playerCount -= 1;
        int playerid = Array.FindIndex(playersInfo.netId, x => x == netId);
        if (playerid == -1)
        {
            Debug.LogWarning("");
            return;
        }

        for (int i = playerid; i < playersInfo.netId.Length-1; i++)
        {
            playersInfo.netId[i] = playersInfo.netId[i + 1];
            playersInfo.PlayerNames[i] = playersInfo.PlayerNames[i + 1];
            playersInfo.ChosenCharacter[i] = playersInfo.ChosenCharacter[i + 1];
            playersInfo.Ready[i] = playersInfo.Ready[i + 1];
        }

        playersInfo.netId[playersInfo.netId.Length - 1] = -1;
        playersInfo.ChosenCharacter[^1] = -1;
        playersInfo.Ready[^1] = false;
        playersInfo.PlayerNames[^1] = "";

    }

    public int FindPlayerIdByClientId(int clientId)
    {
        return Array.FindIndex(playersInfo.netId, x => x == clientId);
    }
    
}

[Serializable]
public struct AllPlayersInfo
{
    public int max;
    public int playerCount;
    public bool[] Ready;
    public string[] PlayerNames;
    public int[] ChosenCharacter;
    public int[] netId;

    public AllPlayersInfo(int n)
    {
        playerCount = 0;
        max = n;
        PlayerNames = new string[n];
        ChosenCharacter = new int[n];
        Array.Fill(ChosenCharacter,-1);
        Ready = new bool[n];
        Array.Fill(Ready,false);
        netId = new int[n];
        Array.Fill(netId,-1);
    }

    // public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    // {
    //     serializer.SerializeValue(ref playerCount);
    //     for (int i = 0; i < playerCount; i++)
    //     {
    //         serializer.SerializeValue(ref PlayerNames[i]);
    //     }
    //     serializer.SerializeValue(ref ChosenCharacter);
    // }
}