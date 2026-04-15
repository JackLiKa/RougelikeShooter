using UnityEngine;

public class IPlayer:PlayerBase
{
    protected Animator m_Animator;
    protected PlayerStateMachine m_StateMachine;
    public IPlayer(GameObject obj):base(obj)
    {
        
    }
    protected override void OnInit()
    {
        base.OnInit();
        // Debug.Log("现在执行的是IPlayer.cs的OnInit方法");
        m_StateMachine=new PlayerStateMachine(this);
        m_Animator=transform.Find("Sprite").GetComponent<Animator>();

    }
    protected override void OnPlayerUpdate()
    {
        base.OnPlayerUpdate();
        m_StateMachine.GameUpdate();
    }
}