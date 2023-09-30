using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

//仅作为房主时使用
public class LobbyManager : Singleton<LobbyManager>
{
    public string RoomPassword ="";
    public bool NeedPwd;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
    }

    private void OnEnable()
    {
    }

    //检查客户端连接
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest arg1,
        NetworkManager.ConnectionApprovalResponse arg2)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        byte[] byteData = arg1.Payload;
        ulong clientId = arg1.ClientNetworkId;
        if (clientId==0)
        {
            arg2.Approved = true;
            arg2.CreatePlayerObject = true;
            return;
        }
        string message = Encoding.UTF8.GetString(byteData);
        Debug.Log($"{clientId}: {message}");

        if (NeedPwd)
        {
            //空密码
            if (string.IsNullOrEmpty(message))
            {
                arg2.Approved = false;
                arg2.Reason = ApprovalDeclinedReason.NEEDPASSWORD;
            }
            else
            {
                //密码正确
                if (message == RoomPassword)
                {
                    arg2.Approved = true;
                    arg2.CreatePlayerObject = true;
                }
                //密码错误
                else
                {
                    arg2.Approved = false;
                    arg2.Reason = ApprovalDeclinedReason.WRONGPASSWORD;
                }
            }
        }
        //不需要密码
        else
        {
            arg2.Approved = true;
            arg2.CreatePlayerObject = true;
        }

    }
    
}
