using System;
using Unity.Netcode;

public class NetworkSingleton<T> : NetworkBehaviour where T : NetworkSingleton<T>
{
    public static T Instance { private set; get; }

    private void Awake()
    {
        if (Instance != null)
            Destroy(gameObject);
        else
            Instance = (T)this;
    }


    public override void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}