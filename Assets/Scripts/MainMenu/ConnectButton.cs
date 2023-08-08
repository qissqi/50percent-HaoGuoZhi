using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;

public class ConnectButton : MonoBehaviour
{
    [SerializeField] private Button server;
    [SerializeField] private Button client;
    [SerializeField] private Button host;
    [SerializeField] private Button Shutdown;
    [SerializeField] private Button confirm;
    [SerializeField] private TMP_InputField IP;

    private void Start()
    {
        server.onClick.AddListener((() => NetworkManager.Singleton.StartServer()));
        client.onClick.AddListener((() => NetworkManager.Singleton.StartClient()));
        host.onClick.AddListener((() => NetworkManager.Singleton.StartHost()));
        Shutdown.onClick.AddListener((() => NetworkManager.Singleton.Shutdown()));
        confirm.onClick.AddListener(() =>
        {
            string targetIP = "";
            string[] text = IP.text.Split(':');
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            switch (text.Length)
            {
                case 1:
                    transport.ConnectionData.Address = text[0].Trim();
                    transport.ConnectionData.Port = 7778;
                    break;
                case 2:
                    transport.ConnectionData.Address = text[0].Trim();
                    if (ushort.TryParse(text[1].Trim(), out ushort port))
                    {
                        transport.ConnectionData.Port = port;
                    }
                    else
                    {
                        //Warning
                        Debug.LogWarning("Wrong Port");
                        transport.ConnectionData.Port = 7778;
                    }
                    break;
                default:
                    //warning
                    Debug.LogWarning("Wrong IP");
                    break;
            }
            
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = IP.text;
        });
    }
}
