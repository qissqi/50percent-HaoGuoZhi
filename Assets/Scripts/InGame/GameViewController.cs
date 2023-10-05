using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class GameViewController
{
    private GameDataManager _dataManager;
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

        CheckMouse_SelectMove().Forget();
    }

    private async UniTaskVoid CheckMouse_SelectMove()
    {
        List<Route> nextRoutes = _dataManager.currentStand.Next;
        Route mouseCurrentRoute = null;
        foreach (Route r in nextRoutes)
        {
            r.OnMouseEnterAction += ChangeRouteColor;
            r.OnMouseExitAction += ChangeRouteColor;
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
                    mouseCurrentRoute?.OnMouseExitAction?.Invoke(mouseCurrentRoute,false,_dataManager.Steps);
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
                        mouseCurrentRoute?.OnMouseExitAction?.Invoke(mouseCurrentRoute,false,_dataManager.Steps);
                        mouseCurrentRoute = hitRoute;
                        mouseCurrentRoute.OnMouseEnterAction?.Invoke(mouseCurrentRoute,true,_dataManager.Steps);
                    }
                }
                //未选择在路线上
                else
                {
                    if (mouseCurrentRoute)
                    {
                        mouseCurrentRoute?.OnMouseExitAction?.Invoke(mouseCurrentRoute,false,_dataManager.Steps);
                        mouseCurrentRoute = null;
                    }
                }
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
    
}
