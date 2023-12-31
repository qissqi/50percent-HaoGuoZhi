﻿using Unity.Netcode;
using UnityEngine;

public struct DiscoveryResponseData: INetworkSerializable
{
    public ushort Port;

    public bool NeedPassword;
    //public string Password;
    public string ServerName;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Port);
        serializer.SerializeValue(ref ServerName);
        serializer.SerializeValue(ref NeedPassword);
    }
}
