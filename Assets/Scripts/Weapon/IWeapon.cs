using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.UI;

public class IWeapon
{
    
    public GameObject gameObject{get;protected set;}
    public Transform transform=>gameObject.transform;
    public IWeapon(GameObject obj,IPlayer player)
    {
        gameObject=obj;
        player=player;
    }
    
}