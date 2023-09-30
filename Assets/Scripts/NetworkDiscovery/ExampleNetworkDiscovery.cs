using System;
using System.Net;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkManager))]
public class ExampleNetworkDiscovery : NetworkDiscovery<DiscoveryBroadcastData, DiscoveryResponseData>
{
    [Serializable]
    public class ServerFoundEvent : UnityEvent<IPEndPoint, DiscoveryResponseData>
    {
    };

    NetworkManager m_NetworkManager;
    
    [SerializeField]
    [Tooltip("If true NetworkDiscovery will make the server visible and answer to client broadcasts as soon as netcode starts running as server.")]
    bool m_StartWithServer = true;

    public string ServerName = "EnterName";
    //public string Password = "";
    public bool NeedPassword;

    public ServerFoundEvent OnServerFound;
    
    private bool m_HasStartedWithServer = false;

    public void Awake()
    {
        m_NetworkManager = GetComponent<NetworkManager>();
        OnServerFound.AddListener(ServerFound);
    }

    private void ServerFound(IPEndPoint arg0, DiscoveryResponseData arg1)
    {
        Debug.Log("Found!!!\n" +
                  $"{arg0.Address} : {arg0.Port} , {arg0.AddressFamily}\n" +
                  $"{arg1.ServerName} - {arg1.Port}");
    }

    public void Update()
    {
        if (m_StartWithServer && m_HasStartedWithServer == false && IsRunning == false)
        {
            if (m_NetworkManager.IsServer)
            {
                StartServer();
                m_HasStartedWithServer = true;
            }
        }
    }
    
    //服务端的检索回应
    protected override bool ProcessBroadcast(IPEndPoint sender, DiscoveryBroadcastData broadCast, out DiscoveryResponseData response)
    {
        response = new DiscoveryResponseData()
        {
            ServerName = this.ServerName,
            //Password = this.Password,
            NeedPassword = this.NeedPassword,
            Port = ((UnityTransport) m_NetworkManager.NetworkConfig.NetworkTransport).ConnectionData.Port,
        };
        return true;
    }

    protected override void ResponseReceived(IPEndPoint sender, DiscoveryResponseData response)
    {
        OnServerFound.Invoke(sender, response);
    }
}