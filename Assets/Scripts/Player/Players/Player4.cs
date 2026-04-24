using UnityEngine;

public class Player4 : IPlayer
{
    public Player4(GameObject obj) : base(obj) { }

    protected override void OnInit()
    {
        base.OnInit();
        m_StateMachine.SetState<Player2IdleState>();
        Debug.Log("现在执行的是Player4.cs的OnInit方法");
    }
}
