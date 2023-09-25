using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Route : MonoBehaviour
{
    public RouteType Route_Type;
    public List<Route> Next, Previous;
    public List<int> Standings;
    public bool IsStartPoint;
    public RouteState State;
    
    private void Start()
    {
        if (IsStartPoint)
        {
            GameDataManager.Instance.startPoints.Add(this);
        }
    }
}
