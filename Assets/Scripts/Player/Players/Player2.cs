using UnityEngine;

public class Player2 : IPlayer
{
    public Player2(GameObject obj) : base(obj) { }

    protected override void OnInit()
    {
        base.OnInit();
        m_StateMachine.SetState<Player2IdleState>();
        Debug.Log("现在执行的是Player2.cs的OnInit方法");
    }
}
