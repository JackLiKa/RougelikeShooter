using UnityEngine;

public class Player1IdleState:IPlayerState
{
    private float hor=0f;
    private float ver=0f;
    private float moveSpeed = 5f;
    private Vector3 moveDir;

    public Player1IdleState(PlayerStateMachine machine):base(machine)
    {
        
    }
    protected override void OnEnter()
    {
        base.OnEnter();
        animationBridge?.SetRunning(false);
        m_rb.velocity=Vector3.zero;
    }
    protected override void OnUpdate()
    {
        base.OnUpdate();
        Move();
    }
    public override void OnExit()
    {
        base.OnExit();
        animationBridge?.SetRunning(true);
    }

    void Move(){
        if (!CanReadPlayerInput())
        {
            return;
        }

        hor=Input.GetAxis("Horizontal");
        ver=Input.GetAxis("Vertical");
        moveDir.Set(hor,ver,0f);
        if(moveDir.magnitude>0)
        {
            m_Machine.SetState<Player1RunState>();
            return;
        }

    }
}
