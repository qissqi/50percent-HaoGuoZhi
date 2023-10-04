using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class TransportString : INetworkSerializable
{
    public string str;

    public TransportString()
    {
        str = "";
    }
    
    public TransportString(string str)
    {
        this.str = str ?? "";
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    { 
        if (serializer.IsWriter)
        {
            serializer.GetFastBufferWriter().WriteValueSafe(str);
        }
        else
        {
            serializer.GetFastBufferReader().ReadValueSafe(out str);
        }
    }
}
