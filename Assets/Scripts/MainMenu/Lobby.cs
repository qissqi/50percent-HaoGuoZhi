using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

//View
public class Lobby : MonoBehaviour
{
    // public static Lobby Instance;

    [Header("引用")] 
    public List<PlayerLobbyInfo> PlayerLobbyInfos;
    public Transform LobbyPanel;
    public Button ReadyButton;
    public Button LeaveButton;
    public List<Button> CharactersButton = new List<Button>();
    public List<Image> SelectorBackgrounds = new List<Image>();

    [Header("数据")] 
    public bool Choosed=false;

    private void Start()
    {
        ReadyButton.onClick.AddListener(ReadyButtonClick);
        LeaveButton.onClick.AddListener(LeaveLobbyClick);
        for (int i = 0; i < CharactersButton.Count; i++)
        {
            int index = i;
            CharactersButton[i].onClick.AddListener(() =>
            {
                ChooseCharacterClick(index);
            });
        }
        
        LobbyManager.Instance.RegisterLobbyUi(this);
    }

    private void OnEnable()
    {
        InitLobby(-1,false);
    }
    
    private void OnDestroy()
    {
        // Instance = null;
        LeaveButton.onClick.RemoveAllListeners();
        ReadyButton.onClick.RemoveAllListeners();
        foreach (var btn in CharactersButton)
        {
            btn.onClick.RemoveAllListeners();
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

    //根据当前玩家标识重置大厅内容
    public void InitLobby(int id,bool server)
    {
        Choosed = false;
        //重置玩家卡和状态标
        for (int i = 0; i < GameManager.Instance.playersInfo.max; i++)
        {
            if (i == id)
            {
                PlayerLobbyInfos[i].BackgroundCard.color = new Color(0, 0.6f, 1);
            }
            else
            {
                PlayerLobbyInfos[i].BackgroundCard.color = Color.white;
            }
            PlayerLobbyInfos[i].ReadyMark.SetActive(false);
        }
        
        //重置准备状态标
        if (server)
        {
            ReadyButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "开始";
        }
        else
        {
            ReadyButton.transform.GetChild(0).GetComponent<TMP_Text>().text = "准备";
        }
    }
    
    //刷新玩家卡
    public void UpdatePlayerInLobby(int id,string _name,int character,bool ready)
    {
        PlayerLobbyInfos[id].PlayerName.text = _name;
        PlayerLobbyInfos[id].ReadyMark.SetActive(ready);
        if (character != -1)
        {
            PlayerLobbyInfos[id].CharacterShow.sprite =
                GameManager.Instance.GameCharacterSpriteList.characters[character];
        }
        else
        {
            PlayerLobbyInfos[id].CharacterShow.sprite =
                GameManager.Instance.GameCharacterSpriteList.Empty;
        }
        
    }

    //刷新角色选择区
    public void UpdateCharacterSelect(int index, bool canChoose)
    {
        if(index<0)
            return;
        // selects.GetChild(index).GetComponent<Image>().color =
        //     canChoose ? Color.white : Color.grey;
        SelectorBackgrounds[index].color =
            canChoose ? Color.white : Color.grey;
        
        // selects.GetChild(index).GetComponent<Button>().interactable = canChoose;
        CharactersButton[index].interactable = canChoose;
    }
    
    public void ChooseCharacterClick(int index)
    {
        Debug.Log("Choose "+index);
        int playerId = NetPlayer.OwnerInstance.GivenId;
        
        LobbyManager.Instance.ChooseCharacter(playerId,index);
        Choosed = true;
    }
    
    public void ReadyButtonClick()
    {
        //服务端：开始
        if (NetworkManager.Singleton.IsServer)
        {
            LobbyManager.Instance.StartCheck();
        }
        
        //客户端：检查选角，准备
        else if (NetworkManager.Singleton.IsClient)
        {
            if(!Choosed)
                return;
            LobbyManager.Instance.Ready(NetPlayer.OwnerInstance.GivenId);
            ReadyButton.interactable = false;
            ReadyButton.image.color = Color.gray;
        }
        
    }
    
    public void LeaveLobbyClick()
    {
        LobbyManager.Instance.LeaveLobby();
    }

}

[Serializable]
public struct PlayerLobbyInfo
{
    public Image BackgroundCard;
    public TMP_Text PlayerName;
    public Image CharacterShow;
    public GameObject ReadyMark;
}

