using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
public class PlayerStateMachine:IStateMachine
{
    public IPlayer m_Player{get;protected set;}
    public PlayerStateMachine(IPlayer player):base()
    {
        m_Player=player;
    }
    
}