using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameViewController
{
    public void InitSceneData(NetworkObjectReference[] characters)
    {
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
}
