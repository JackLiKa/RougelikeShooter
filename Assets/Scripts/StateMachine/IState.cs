using UnityEngine;
public abstract class IState
{
    public IStateMachine m_Machine{get;protected set;}
    private bool isInit;
    private bool isEnter;
    public IState(IStateMachine machine)
    {
        m_Machine=machine;
    }

    protected virtual void OnInit(){}
    protected virtual void OnEnter(){}    

    public virtual void GameUpdate()
    {
        if(!isInit)
        {
            isInit=true;
            OnInit();
        }
        OnUpdate();
    }

    protected virtual void OnUpdate()
    {
        if(!isEnter)
        {
            isEnter=true;
            OnEnter();
        }
    }
    public virtual void OnExit()
    {
        // isInit=false;
        isEnter=false;
    }


}