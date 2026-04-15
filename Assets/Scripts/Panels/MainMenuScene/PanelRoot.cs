using System.Collections.Generic;
// using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;
using UnityEngine;



public class PanelRoot:IPanel
{
    
    public PanelRoot():base(null)
    {
    }

    protected override void OnInit()
    {
        base.OnInit();
        UnityTool.Instance.GetComponentFromChildren<Button>(gameObject,"ButtonStart").onClick.AddListener(()=>{
            // GameMediator.Instance.SendNotification(NotificationNames.StartGame);
            Debug.Log("游戏开始");
        });
    }
    protected override void OnEnter()
    {
        base.OnEnter();
    }
}