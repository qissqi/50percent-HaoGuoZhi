using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class GameViewController
{
    private GameDataManager _dataManager;
    private List<List<Route>> paths = new List<List<Route>>();
    
    public void InitScene(NetworkObjectReference[] characters)
    {
        _dataManager = GameDataManager.Instance;
        for (int i = 0; i < characters.Length; i++)
        {
            if (characters[i].TryGet(out NetworkObject obj))
            {
                obj.GetComponent<SpriteRenderer>().sprite =
                    GameManager.Instance.GameCharacterSpriteList
                        .characters[GameManager.Instance.playersInfo.ChosenCharacter[i]];
            }
        }
    }

    //玩家选择移动投骰子后
    public void StateSetMove()
    {
        foreach (var route in _dataManager.AllRoutes)
        {
            route.Dark();
        }

        InitPaths();
        CheckMouse_SelectMove().Forget();
    }

    private void InitPaths()
    {
        //List<Route> nextRoutes = _dataManager.currentStand.Next;
        foreach (Route r in _dataManager.currentStand.Next)
        {
            r.Mark |= RouteMark.StepStart;
            r.OnMouseEnter += ChangeRouteColor;
            r.OnMouseExit += ChangeRouteColor;
            r.Light();
        }
        paths = new List<List<Route>>();
        _dataManager.currentStand.GetAllRoutePaths(paths,_dataManager.Steps);
        foreach (List<Route> path in paths)
        {
            var end = path[^1];
            end.Mark |= RouteMark.StepEnd;
            end.OnMouseEnter += ChangeRouteColor;
            end.OnMouseExit += ChangeRouteColor;
            end.HalfLight();
        }
        
    }

    private async UniTaskVoid CheckMouse_SelectMove(bool l)
    {
        List<Route> nextRoutes = _dataManager.currentStand.Next;
        List<List<Route>> finalPaths = new List<List<Route>>();
        Route mouseCurrentRoute = null;
        foreach (Route r in nextRoutes)
        {
            // r.OnMouseEnterAction += ChangeRouteColor;
            // r.OnMouseExitAction += ChangeRouteColor;
        }
        
        while (true)
        {
            await UniTask.NextFrame();
            foreach (var route in nextRoutes)
            {
                route.Light();
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // RaycastHit2D hit2D = Physics2D.Raycast(mousePos, Vector2.zero);
            Physics.Raycast(ray, out RaycastHit hit);
            
            Debug.DrawRay(mousePos, Vector3.forward * 20f,
                hit.transform ? Color.green : Color.red,
                0.05f);
            Debug.DrawRay(mousePos, Vector3.forward * 2f,
                Color.magenta,
                0.05f);
            if (!hit.transform || !hit.transform.TryGetComponent(out Route hitRoute))
            {
                //空选
                if (mouseCurrentRoute)
                {
                    // mouseCurrentRoute?.OnMouseExitAction?.Invoke(mouseCurrentRoute,false,_dataManager.Steps);
                    mouseCurrentRoute = null;
                }
                continue;
            }
            
            foreach (Route r in nextRoutes)
            {
                //鼠标选择路线
                if (r == hitRoute)
                {
                    if (mouseCurrentRoute != hitRoute)
                    {
                        // mouseCurrentRoute?.OnMouseExitAction?.Invoke(mouseCurrentRoute,false,_dataManager.Steps);
                        mouseCurrentRoute = hitRoute;
                        // mouseCurrentRoute.OnMouseEnterAction?.Invoke(mouseCurrentRoute,true,_dataManager.Steps);
                    }
                }
                //未选择在路线上
                else
                {
                    if (mouseCurrentRoute)
                    {
                        // mouseCurrentRoute?.OnMouseExitAction?.Invoke(mouseCurrentRoute,false,_dataManager.Steps);
                        mouseCurrentRoute = null;
                    }
                }
            }
        }
    }

    private async UniTaskVoid CheckMouse_SelectMove()
    {
        Route currenSelectRoute = null;
        
        //帧检测
        while (true)
        {
            await UniTask.NextFrame();
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // RaycastHit2D hit2D = Physics2D.Raycast(mousePos, Vector2.zero);
            Physics.Raycast(ray, out RaycastHit hit);
            
            Debug.DrawRay(mousePos, Vector3.forward * 20f,
                hit.transform ? Color.green : Color.red,
                0.05f);
            Debug.DrawRay(mousePos, Vector3.forward * 2f,
                Color.magenta,
                0.05f);
            
            //未指到地块
            if (!hit.transform || !hit.transform.TryGetComponent(out Route hitRoute))
            {
                currenSelectRoute?.CallOnMouseExit(currenSelectRoute);
                currenSelectRoute = null;
                continue;
            }
            //指向没变
            if (hitRoute == currenSelectRoute)
            {
                continue;
            }
            
            //指向改变
            currenSelectRoute?.CallOnMouseExit(currenSelectRoute);
            
            switch (hitRoute.Mark)
            {
                //非路径地块
                case RouteMark.None:
                    currenSelectRoute = null;
                    break;
                
                //起步地块
                case RouteMark.StepStart:
                //终点地块
                case RouteMark.StepEnd:
                    currenSelectRoute = hitRoute;
                    currenSelectRoute.CallOnMouseEnter(currenSelectRoute);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }
        
    }

    private void ChangeRouteColor(Route start,bool isLight, int steps)
    {
        if(steps == 0)
            return;

        foreach (Route r in start.Next)
        {
            if (isLight)
            {
                r.Light();
            }
            else
            {
                r.Dark();
            }
            ChangeRouteColor(r,isLight,steps-1);
        }
        
    }

    private void ChangeRouteColor(Route choose,bool isLight)
    {
        if (choose.Mark == RouteMark.None)
        {
            Debug.Log("WrongChooseRoute");
            return;
        }

        if (choose.Mark == RouteMark.StepStart)
        {
            foreach (var path in paths)
            {
                if (path[0]==choose)
                {
                    foreach (var route in path)
                    {
                        //亮起
                        if (isLight)
                        {
                            route.Light();
                        }
                        
                        //暗下
                        else if(route.Mark == RouteMark.None)
                        {
                            route.Dark();
                        }
                        else if((route.Mark & RouteMark.StepEnd) !=0)
                        {
                            route.HalfLight();
                        }
                    }
                }
            }
        }

        if (choose.Mark == RouteMark.StepEnd)
        {
            foreach (var path in paths)
            {
                if (path[^1] == choose)
                {
                    foreach (var route in path)
                    {
                        //亮起
                        if (isLight)
                        {
                            route.Light();
                        }
                        
                        //暗下
                        else if(route.Mark == RouteMark.None)
                        {
                            route.Dark();
                        }
                        else if((route.Mark & RouteMark.StepEnd)!=0)
                        {
                            route.HalfLight();
                        }
                    }
                    //最终路径选择，只需要一条
                    break;
                }
            }
        }
        
    }
    
    
}
