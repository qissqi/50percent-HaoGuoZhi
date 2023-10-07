using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public RouteMark Mark;

    private SpriteRenderer Renderer;
    private Color originColor;
    private Color halfLightColor;
    private Color darkColor;

    // public Action<Route,bool,int> OnMouseEnterAction;
    // public Action<Route,bool,int> OnMouseExitAction;

    public delegate void RouteSet(Route r, bool l);

    public event RouteSet OnMouseEnter;
    public event RouteSet OnMouseExit;

    private void Start()
    {
        Renderer = GetComponent<SpriteRenderer>();
        originColor = Renderer.color;
        darkColor = originColor * 0.5f + new Color(0, 0, 0, 1);
        halfLightColor = originColor * 0.75f + new Color(0, 0, 0, 1);
        if (IsStartPoint)
        {
            GameDataManager.Instance.startPoints.Add(this);
        }
        GameDataManager.Instance.AllRoutes.Add(this);
    }

    public override void OnDestroy()
    {
        OnMouseEnter = null;
        OnMouseExit = null;
    }

    public void Light()
    {
        Renderer.color = originColor;
    }

    public void Dark()
    {
        Renderer.color = darkColor;
    }

    public void HalfLight()
    {
        Renderer.color = halfLightColor;
    }

    public void GetAllRoutePaths(List<List<Route>> paths, int steps,List<Route> tmp=null)
    {
        if (steps == 0)
        {
            paths.Add(tmp);
            return;
        }

        if (tmp == null)
        {
            tmp = new List<Route>();
        }
        else
        {
            tmp.Add(this);
            steps = steps - 1;
        }

        if (Next.Count == 1)
        {
            Next[0].GetAllRoutePaths(paths,steps,tmp);
        }
        else
        {
            var end = Next.Count - 1;
            for (var i = 0; i < end; i++)
            {
                var t2 = tmp.ToList();
                Next[i].GetAllRoutePaths(paths,steps,t2);
            }
            
            Next[end].GetAllRoutePaths(paths,steps,tmp);
        }
        

    }

    public void CallOnMouseEnter(Route r)
    {
        OnMouseEnter?.Invoke(r,true);
    }

    public void CallOnMouseExit(Route r)
    {
        OnMouseExit?.Invoke(r,false);
    }
    
}
