using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "GameData/CharacterList")]
public class CharacterList : ScriptableObject
{
    public Sprite Empty;
    public List<Sprite> characters = new List<Sprite>();
    
}
