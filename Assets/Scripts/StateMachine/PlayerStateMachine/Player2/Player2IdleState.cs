using UnityEngine;

public class Player2IdleState : IPlayerState
{
    private float hor = 0f;
    private float ver = 0f;
    private Vector3 moveDir;

    public Player2IdleState(PlayerStateMachine machine) : base(machine)
    {
    }

    protected override void OnEnter()
    {
        base.OnEnter();
        m_Animator.SetBool("isRun", false);
        m_rb.velocity = Vector3.zero;
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        Move();
    }

    public override void OnExit()
    {
        base.OnExit();
        m_Animator.SetBool("isRun", true);
    }

    private void Move()
    {
        hor = Input.GetAxis("Horizontal");
        ver = Input.GetAxis("Vertical");
        moveDir.Set(hor, ver, 0f);

        if (moveDir.magnitude > 0)
        {
            m_Machine.SetState<Player2RunState>();
        }
    }
}
