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
            /*
             * [AI_COMMENTED_OUT]
             * 原逻辑固定读取 Player1，无法响应主菜单/角色面板的角色切换结果。
             * 这里保留旧实现作为对照，下面改为读取运行时选择配置并应用角色、武器。
             */
            // MainPlayer=PlayerFactory.Instance.GetPlayer(PlayerType.Player1);
            GameSceneSelectionApplier.Apply(GameSelectionConfig.CurrentPlayerType, GameSelectionConfig.CurrentWeaponType);
            MainPlayer=PlayerFactory.Instance.GetPlayer(GameSelectionConfig.CurrentPlayerType);
        }
        protected override void AlwaysUpdate()
        {
            base.AlwaysUpdate();
            MainPlayer?.GameUpdate();
        }
    }   
}
