using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class Route : NetworkBehaviour
{
    public RouteType Route_Type;
    public List<Route> Next, Previous;
    public List<int> Standings;
    public bool IsStartPoint;
    public RouteState State;

    public SpriteRenderer Renderer;
    private Color originColor;
    private Color darkColor;

    public Action<Route,bool,int> OnMouseEnterAction;
    public Action<Route,bool,int> OnMouseExitAction;

    private void Start()
    {
        Renderer = GetComponent<SpriteRenderer>();
        originColor = Renderer.color;
        darkColor = originColor * 0.5f + new Color(0, 0, 0, 1);
        if (IsStartPoint)
        {
            GameDataManager.Instance.startPoints.Add(this);
        }
        GameDataManager.Instance.AllRoutes.Add(this);
    }

    public void Light()
    {
        Renderer.color = originColor;
    }

    public void Dark()
    {
        Renderer.color = darkColor;
    }

}
