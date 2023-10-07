using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// public class Enums : MonoBehaviour
// {
//     
// }

public enum GameState
{
    None,Lobby,Playing
}

public enum RouteType
{
    A,B,C,D
}

[System.Flags]
public enum RouteState
{
    None=0,
    EndWay=1<<0,
    PlayerToAttack=1<<1,
    MultiWay=1<<2
}

[System.Flags]
public enum RouteMark
{
    None = 0,
    StepEnd = 1<< 0,
    StepStart = 1<< 1
}


public struct ApprovalDeclinedReason
{
    public const string NEEDPASSWORD = "Need Password";
    public const string WRONGPASSWORD = "Wrong Password";
}

