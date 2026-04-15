using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;   
using System.Linq;
using System;
namespace GameScene
{
    public class PlayerController:AbstractController
    {
        public IPlayer MainPlayer{get;protected set;}
        public PlayerController(){}
        protected override void OnInit()
        {
            base.OnInit();
            MainPlayer=PlayerFactory.Instance.GetPlayer(PlayerType.Player1);
        }
        protected override void AlwaysUpdate()
        {
            base.AlwaysUpdate();
            MainPlayer.GameUpdate();
        }
    }   
}