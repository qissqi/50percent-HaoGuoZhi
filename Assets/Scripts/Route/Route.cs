using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Route : MonoBehaviour
{
    public RouteType Route_Type;
    public List<Route> Next, Previous;
    public bool IsStartPoint;
    
    private void Start()
    {
        if (IsStartPoint)
        {
            GameDataManager.Instance.startPoints.Add(this);
        }
    }
}
