using UnityEngine;

public class Player1RunState:IPlayerState
{
    private float hor=0f;
    private float ver=0f;

    private bool isLeft;
    private float playerSize = 10f;  // Player 尺寸
    private float moveSpeed = 10f;
    private Vector3 moveDir;
    public Player1RunState(PlayerStateMachine machine):base(machine)
    {
        
    }


    // private  Rigidbody2D myRigidbody;           // Start is called before the first frame update
    // private Animator myAnimator;


    protected override void OnEnter()
    {
        base.OnEnter();
        moveSpeed = PlayerRuntimeStats.GetMoveSpeed(gameObject, moveSpeed);
        // Debug.Log(this);
        // myRigidbody=transform.GetComponent<Rigidbody2D>();
        // myAnimator=transform.GetComponent<Animator>();
        // myAnimator.SetBool("isRun",true);
        animationBridge?.SetRunning(true);

    }


    protected override void OnUpdate()
    {
        base.OnUpdate();
        Move();
    }



    void Move(){    
        if (!CanReadPlayerInput())
        {
            m_Machine.SetState<Player1IdleState>();
            return;
        }

        moveSpeed = PlayerRuntimeStats.GetMoveSpeed(gameObject, moveSpeed);
        hor=Input.GetAxis("Horizontal");
        ver=Input.GetAxis("Vertical");
        moveDir.Set(hor,ver,0f);
        if(moveDir.magnitude>0)
        {
            m_rb.transform.position += (Vector3)moveDir.normalized*moveSpeed*Time.deltaTime;
        }
        if(moveDir.magnitude==0)
        {
            m_Machine.SetState<Player1IdleState>();
            return;
        }
        if(hor<0)
        {
            isLeft=true;
            this.transform.localScale = new Vector3(-playerSize, playerSize, 1f);
        }
        else
        {
            isLeft=false;
            this.transform.localScale = new Vector3(playerSize, playerSize, 1f);
        }
        
    }
    public override void OnExit()
    {
        base.OnExit();
        // ✅ 设置动画
        animationBridge?.SetRunning(false);
    }
}
