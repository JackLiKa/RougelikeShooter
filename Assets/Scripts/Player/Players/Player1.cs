using UnityEngine;

public class Player1 : IPlayer
{
    public Player1(GameObject obj) : base(obj) { }
    protected override void OnInit()
    {
        base.OnInit();

        m_StateMachine.SetState<Player1IdleState>();
        Debug.Log("现在执行的是Player1.cs的OnInit方法");
    }

}