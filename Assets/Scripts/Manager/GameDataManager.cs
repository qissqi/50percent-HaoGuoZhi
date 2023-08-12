using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
struct PlayerGameData
{
    public int playerCount;
    public int[] HP;
    public bool[] playerMoveContrary;
    
    public int currentControlPlayer;
    public int playerPhase;
    public Route[] PlayerAt;

    public PlayerGameData(int n)
    {
        playerCount = n;
        HP = new int[n];
        playerMoveContrary = new bool[n];
        PlayerAt = new Route[n];

        currentControlPlayer = 0;
        playerPhase = 0;
    }
    
}

public class GameDataManager : NetworkSingleton<GameDataManager>
{
    //public Action OnPlayerAllLoad;
    [SerializeField] 
    private GameObject CharacterPrefab;
    private bool allLoaded;
    private PlayerGameData gameData;
    
    public List<Route> startPoints;
    private GameObject[] characters;

    private Vector3 standOffset = new Vector3(0.3f, 0.3f);


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameData = new PlayerGameData(GameManager.Instance.playersInfo.playerCount);
            characters = new GameObject[gameData.playerCount];
            GameManager.Instance.playersInfo.Ready[0] = true;
            NetworkManager.SceneManager.OnLoadComplete += OnPlayerLoad;
        }
    }
    
    //仅server调用
    private void OnPlayerLoad(ulong clientid, string scenename, LoadSceneMode loadscenemode)
    {
        int id = GameManager.Instance.FindPlayerIdByClientId((int)clientid);
        GameManager.Instance.playersInfo.Ready[id] = true;
        if (CheckAllLoad())
        {
            InitPLayersData();
            
            //测试用
            if (IsServer)
            {
                Test(0);
            }
        }
    }
    
    //测试用
    private async UniTaskVoid Test(int id)
    {
        GameObject obj = characters[id];
        while (true)
        {
            Route next = gameData.PlayerAt[id].Next[0];
            await obj.transform.DOMove(next.transform.position+standOffset, 0.7f);
            gameData.PlayerAt[id] = next;
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
    private void InitPLayersData()
    {
        Array.Fill(gameData.HP,20);
        for (int i = 0; i < gameData.playerCount; i++)
        {
            int cid = GameManager.Instance.playersInfo.netId[i];
            var p = Instantiate(CharacterPrefab).GetComponent<NetworkObject>();
            p.SpawnWithOwnership((ulong)cid,false);
            p.DontDestroyWithOwner = true;
            p.transform.position = startPoints[i].transform.position+standOffset;
            gameData.PlayerAt[i] = startPoints[i];
            characters[i] = p.gameObject;
            
            int character = GameManager.Instance.playersInfo.ChosenCharacter[i];
            p.GetComponent<SpriteRenderer>().sprite =
                GameManager.Instance.GameCharacterSpriteList.characters[character];
        }
    }
    
    
    
}
