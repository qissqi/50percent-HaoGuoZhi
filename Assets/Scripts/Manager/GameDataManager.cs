using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using System.Linq;

//玩家的数据信息
public struct PlayerData
{
    public int HP;
    public bool IsMoveContrary;
    public Route RouteAt;
    public bool IsDead;
    public bool Action_Move;
    public bool Action_Card;
    
}

//当前游戏的信息
public class PlayerGameData
{
    public PlayerData[] players;
    public int playerCount;
    public int currentControlPlayer;
    public int playerPhase;
    public int leftSteps;

    public PlayerGameData(int n)
    {
        playerCount = n;
        players = new PlayerData[n];
        
        currentControlPlayer = 0;
        playerPhase = 0;
    }
    
}

//当前游戏的逻辑模块
public class GameDataManager : NetworkSingleton<GameDataManager>
{
    //public Action OnPlayerAllLoad;
    [SerializeField] 
    private GameObject CharacterPrefab;
    private bool allLoaded;
    private PlayerGameData gameData;
    
    public List<Route> startPoints;
    private NetworkObject[] characters;

    private Vector3 standOffset = new Vector3(0.3f, 0.3f);
    private GameViewController viewController = new GameViewController();


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameData = new PlayerGameData(GameManager.Instance.playersInfo.playerCount);
            characters = new NetworkObject[gameData.playerCount];
            GameManager.Instance.playersInfo.Ready[0] = true;
            NetworkManager.SceneManager.OnLoadComplete += OnPlayerLoad;
        }
    }
    
    //仅server调用
    private void OnPlayerLoad(ulong clientid, string scenename, LoadSceneMode loadscenemode)
    {
        int id = GameManager.Instance.FindPlayerIdByClientId((int)clientid);
        GameManager.Instance.playersInfo.Ready[id] = true;
        Debug.Log($"Player {id} has loaded");
        if (CheckAllLoad())
        {
            InitPlayersData();
            AllPlayersInfo info = GameManager.Instance.playersInfo;
            TransportString[] transportStrings = new TransportString[info.max];
            for (int i = 0; i < info.max; i++)
            {
                transportStrings[i] = new TransportString(info.PlayerNames[i]);
            }
            
            SyncPlayerDataClientRpc(info,transportStrings);
            InitSceneDataClientRpc(characters.Select(n=>(NetworkObjectReference)n).ToArray());
            //测试用
            // if (IsServer)
            // {
            //     Test(0);
            // }
            TurnSetToServerRpc(0);
            
        }
    }

    [ClientRpc]
    private void InitSceneDataClientRpc(NetworkObjectReference[] characters)
    {
        viewController.InitSceneData(characters);
    }


    [ClientRpc]
    private void SyncPlayerDataClientRpc(AllPlayersInfo info,TransportString[] playerNames)
    {
        info.PlayerNames = new string[info.max];
        for (int i = 0; i < info.max; i++)
        {
            Debug.Log(i);
            info.PlayerNames[i] = playerNames[i].str;
        }
        
        GameManager.Instance.playersInfo = info;
    }
    
    

    //测试用
    private async UniTaskVoid Test(int id)
    {
        NetworkObject obj = characters[id];
        while (true)
        {
            // Route next = gameData.PlayerAt[id].Next[0];
            Route next = gameData.players[id].RouteAt.Next[0];
            await obj.transform.DOMove(next.transform.position+standOffset, 0.7f);
            // gameData.PlayerAt[id] = next;
            gameData.players[id].RouteAt = next;
            await UniTask.Delay(TimeSpan.FromSeconds(0.1));
        }
        
    }

    private bool CheckAllLoad()
    {
        if (allLoaded)
            return true;

        int ck= Array.FindIndex(GameManager.Instance.playersInfo.Ready, 0, GameManager.Instance.playersInfo.playerCount,
            x => !x);

        if (ck == -1)
        {
            allLoaded = true;
            return true;
        }

        return false;

    }

    //仅server调用
    private void InitPlayersData()
    {
        //Array.Fill(gameData.HP,20);
        for (int i = 0; i < gameData.playerCount; i++)
        {
            gameData.players[i].HP = 20;
            int cid = GameManager.Instance.playersInfo.netId[i];
            var p = Instantiate(CharacterPrefab).GetComponent<NetworkObject>();
            
            p.DontDestroyWithOwner = true;
            p.transform.position = startPoints[i].transform.position+standOffset;
            //gameData.PlayerAt[i] = startPoints[i];
            gameData.players[i].RouteAt = startPoints[i];
            gameData.players[i].IsMoveContrary = false;
            gameData.players[i].IsDead = false;
            gameData.players[i].Action_Move = false;
            gameData.players[i].Action_Card = false;
            characters[i] = p;
            
            startPoints[i].Standings.Add(i);
            int character = GameManager.Instance.playersInfo.ChosenCharacter[i];
            // p.GetComponent<SpriteRenderer>().sprite =
            //     GameManager.Instance.GameCharacterSpriteList.characters[character];
            
            p.SpawnWithOwnership((ulong)cid,false);
        }
    }

    //仅Server调用
    [ServerRpc]
    private void TurnSetToServerRpc(int _id)
    {
        int current = _id % gameData.playerCount;
        gameData.currentControlPlayer = current;
        var currentplayer = gameData.players[current];
        if (currentplayer.IsDead)
        {
            
        }
        else
        {
            currentplayer.Action_Card = true;
            currentplayer.Action_Move = true;
        }
        
        TurnSetToClientRpc(current,currentplayer.IsDead);

    }

    [ClientRpc]
    private void TurnSetToClientRpc(int id,bool dead)
    {
        if (id == NetPlayer.OwnerInstance.GivenId)
        {
            Debug.Log("It's your turn:"+id);
        }
        else
        {
            
        }
    }

    
    [ServerRpc(RequireOwnership = false)]
    public void RollDice_Move_ServerRpc(int id)
    {
        if (gameData.currentControlPlayer != id)
        {
            Debug.LogWarning("Nor Your Turn!");
            return;
        }

        int value = Random.Range(0, 7);
        gameData.leftSteps = value;
        RollDice_Move_ClientRpc(id);
        Debug.Log("Roll: "+value);
        //CharacterMoveServerRpc(value);
        
    }

    [ClientRpc]
    private void RollDice_Move_ClientRpc(int id)
    {
        //动画表现

        if (id == NetPlayer.OwnerInstance.GivenId)
        {
            CharacterMoveServerRpc();
        }
    }
    
    
    //会使用存储在GameData类内的leftSteps
    //将会计算到所有特殊路径（需要选择的路径）
    [ServerRpc(RequireOwnership = false)]
    private void CharacterMoveServerRpc()
    {
        Route route = gameData.players[gameData.currentControlPlayer].RouteAt;
        //按步数遍历格子标记(最后一格停下，所以不用检查)
        //也许需要NetworkVariable<>同步变量?
        List<Route> endRoutes = new List<Route>();
        CheckRoute(route,gameData.leftSteps,endRoutes);
        
    }

    
    //[ServerRpc(RequireOwnership = false)]
    private void CheckRoute(Route route,int count,List<Route> endRoutes)
    {
        route.State = RouteState.None;
        if (count == 0)
        {
            //endRoutes.Add(route);
            route.State |= RouteState.EndWay;
            return;
        }
        
        //其他检查
        //分支路
        if (route.Next.Count > 1)
        {
            route.State |= RouteState.MultiWay;
        }
        
        int c = count - 1;
        foreach (var r in route.Next)
        {
            CheckRoute(r,c,endRoutes);
        }
    }

    [ClientRpc]
    private void CharacterMoveClientRpc(int step)
    {
        
    }
    
    
    
    
    
    
}
