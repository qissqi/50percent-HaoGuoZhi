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
using UnityEngine.Serialization;

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
    private bool onMyTurn;
    
    //public Action OnPlayerAllLoad;
    [SerializeField] 
    private GameObject CharacterPrefab;
    private bool allLoaded;
    private PlayerGameData gameData;

    public List<Route> AllRoutes;
    public List<Route> startPoints;
    private NetworkObject[] characters;

    private Vector3 standOffset = new Vector3(0.3f, 0.3f);
    private GameViewController viewController = new GameViewController();

    //Events
    public Action OnTurnStartAction;
    public Action OnTurnEndAction;

    [Header("MyState")] 
    public NetworkObject myCharacter;
    public int Steps;
    public Route currentStand;

    public override void OnNetworkSpawn()
    {
        OnTurnStartAction += OnTurnStart;
        OnTurnEndAction += OnTurnEnd;
            
        if (IsServer)
        {
            gameData = new PlayerGameData(GameManager.Instance.playersInfo.playerCount);
            characters = new NetworkObject[gameData.playerCount];
            GameManager.Instance.playersInfo.Ready[0] = true;
            NetworkManager.SceneManager.OnLoadComplete += OnPlayerLoad;
        }
    }
    
    public override void OnNetworkDespawn()
    {
        OnTurnStartAction -= OnTurnStart;
        OnTurnEndAction -= OnTurnEnd;
        NetworkManager.SceneManager.OnLoadComplete -= OnPlayerLoad;
    }

    private void Update()
    {
        ControlTest();
        CheckMouse();
    }
    
    //不考虑UI操作，测试用
    private void ControlTest()
    {
        if(!onMyTurn)
            return;

        if (Input.GetKeyDown(KeyCode.M))
        {
            ClearRoutesMark();
            RollDice_Move_ServerRpc(NetPlayer.OwnerInstance.GivenId);
        }
    }

    private void ClearRoutesMark()
    {
        foreach (var route in AllRoutes)
        {
            route.Mark = RouteMark.None;
        }
    }

    private void CheckMouse()
    {
        return;
        if(!onMyTurn)
            return;
        
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        RaycastHit2D hit2D = Physics2D.Raycast(mousePos, Camera.main.transform.forward);
        // Ray ray = new Ray(mousePos, Camera.main.transform.forward);
        // RaycastHit[] hits = new RaycastHit[2];
        // Physics.RaycastNonAlloc(ray, hits);
        
        Debug.DrawRay(mousePos,Camera.main.transform.forward*20f,
            hit2D? Color.green: Color.red,
            0.05f);
        Debug.Log($"hit: {hit2D.transform.name}\n");
    }

    #region 初始化

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
            InitDataClientRpc(characters.Select(n=>(NetworkObjectReference)n).ToArray());
            
            TurnSetToServerRpc(0);
            
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
    
    
    [ClientRpc]
    private void InitDataClientRpc(NetworkObjectReference[] _characters)
    {
        viewController.InitScene(_characters);
        currentStand = startPoints[NetPlayer.OwnerInstance.GivenId];
        foreach (var c in _characters)
        {
            if (c.TryGet(out NetworkObject obj))
            {
                if (obj.OwnerClientId == NetworkManager.LocalClientId)
                {
                    myCharacter = obj;
                    break;
                }
            }
        }
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

    #endregion

    #region 回合管理

    [ServerRpc(RequireOwnership = false)]
    public void TurnEndServerRpc(int Gid)
    {
        TurnEndClientRpc(Gid);
        TurnSetToServerRpc(Gid+1);
    }

    [ClientRpc]
    private void TurnEndClientRpc(int Gid)
    {
        if (NetPlayer.OwnerInstance.GivenId == Gid)
        {
            OnTurnEndAction?.Invoke();
        }
    }

    //仅Server调用
    [ServerRpc]
    private void TurnSetToServerRpc(int _id)
    {
        int current = _id % gameData.playerCount;
        gameData.currentControlPlayer = current;
        var currentPlayer = gameData.players[current];
        if (currentPlayer.IsDead)
        {
            
        }
        else
        {
            currentPlayer.Action_Card = true;
            currentPlayer.Action_Move = true;
        }
        
        TurnSetToClientRpc(current,currentPlayer.IsDead);

    }

    [ClientRpc]
    private void TurnSetToClientRpc(int id,bool dead)
    {
        if (id == NetPlayer.OwnerInstance.GivenId)
        {
            Debug.Log("It's your turn:"+id);
            OnTurnStartAction?.Invoke();
        }
        else
        {
            onMyTurn = false;
        }
    }
    
    private void OnTurnStart()
    {
        onMyTurn = true;
    }

    private void OnTurnEnd()
    {
        onMyTurn = false;
    }

    
    #endregion
    
    [ServerRpc(RequireOwnership = false)]
    public void RollDice_Move_ServerRpc(int id)
    {
        if (gameData.currentControlPlayer != id)
        {
            Debug.LogWarning($"Turn Wrong!\ncurrent: {gameData.currentControlPlayer} , target: {id}");
            return;
        }

        int value = Random.Range(1, 7);
        // int value = Random.Range(50, 100);
        gameData.leftSteps = value;
        RollDice_Move_ClientRpc(id,value);
        
    }

    [ClientRpc]
    private void RollDice_Move_ClientRpc(int id,int steps)
    {
        if (id != NetPlayer.OwnerInstance.GivenId)
            return;
        
        Debug.Log("steps: "+steps);
        Steps = steps;
        viewController.StateSetMove();
        
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
