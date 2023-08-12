using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(Route),true)]
public class RouteEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Route t = (Route)target;
        {
            // if (GUILayout.Button("Refresh Color"))
            // {
            //     SpriteRenderer sr;
            //     if (!t.TryGetComponent<SpriteRenderer>(out sr))
            //         return;
            //
            //     switch (t.Route_Type)
            //     {
            //         case RouteType.A:
            //             sr.color = new Color(0, 1, 1);
            //             break;
            //         case RouteType.B:
            //             sr.color = new Color(1, 0, 1);
            //             break;
            //         case RouteType.C:
            //             sr.color = new Color(1, 1, 0);
            //             break;
            //         case RouteType.D:
            //             sr.color = new Color(0, 1, 0);
            //             break;
            //     }
            //     
            // }
        }

        {
            //Debug.Log($"{serializedObject.FindProperty(nameof(t.Route_Type)).intValue}");
            if (GUILayout.Toggle(true, "AutoFresh"))
            {
                RouteType targetType = (RouteType)serializedObject.FindProperty(nameof(t.Route_Type)).intValue;
                Debug.Log(targetType);
            }
            
        }

        {
            if (GUILayout.Button("New Connect Route"))
            {
                Route newRoute = Instantiate(t.gameObject).GetComponent<Route>();
                t.Next.Add(newRoute);
                newRoute.Previous.Add(t);
            }
        }

        {
            Transform transform = t.GetComponent<Transform>();
            if (GUILayout.Button("上"))
            {
                transform.position = new Vector3(transform.position.x, transform.position.y + 1.5f);
            }
            if (GUILayout.Button("下"))
            {
                transform.position = new Vector3(transform.position.x, transform.position.y - 1.5f);
            }
            if (GUILayout.Button("左"))
            {
                transform.position = new Vector3(transform.position.x -1.5f, transform.position.y);
            }
            if (GUILayout.Button("右"))
            {
                transform.position = new Vector3(transform.position.x+1.5f, transform.position.y);
            }
            
        }
    }
}
