using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/RouteData")]
public class RouteData : ScriptableObject
{
    public List<GameObject> target;
}
