using UnityEngine;

public class Player2RunState : IPlayerState
{
    private float hor = 0f;
    private float ver = 0f;
    private float playerSize = 10f;
    private float moveSpeed = 10f;
    private Vector3 moveDir;

    public Player2RunState(PlayerStateMachine machine) : base(machine)
    {
    }

    protected override void OnEnter()
    {
        base.OnEnter();
        moveSpeed = PlayerRuntimeStats.GetMoveSpeed(gameObject, moveSpeed);
        animationBridge?.SetRunning(true);
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        Move();
    }

    public override void OnExit()
    {
        base.OnExit();
        animationBridge?.SetRunning(false);
    }

    private void Move()
    {
        if (!CanReadPlayerInput())
        {
            m_Machine.SetState<Player2IdleState>();
            return;
        }

        moveSpeed = PlayerRuntimeStats.GetMoveSpeed(gameObject, moveSpeed);
        hor = Input.GetAxis("Horizontal");
        ver = Input.GetAxis("Vertical");
        moveDir.Set(hor, ver, 0f);

        if (moveDir.magnitude > 0)
        {
            m_rb.transform.position += (Vector3)moveDir.normalized * moveSpeed * Time.deltaTime;
        }

        if (moveDir.magnitude == 0)
        {
            m_Machine.SetState<Player2IdleState>();
            return;
        }

        if (hor < 0)
        {
            transform.localScale = new Vector3(-playerSize, playerSize, 1f);
        }
        else
        {
            transform.localScale = new Vector3(playerSize, playerSize, 1f);
        }
    }
}
