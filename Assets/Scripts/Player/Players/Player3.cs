using UnityEngine;

public class Player3 : IPlayer
{
    public Player3(GameObject obj) : base(obj) { }

    protected override void OnInit()
    {
        base.OnInit();
        m_StateMachine.SetState<Player2IdleState>();
        Debug.Log("现在执行的是Player3.cs的OnInit方法");
    }
}
