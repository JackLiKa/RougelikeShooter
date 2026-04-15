using UnityEngine;

public abstract class AbstractController
{
    private bool isRun;

    private bool isInit;
    private bool isBeforeRunStart;
    private bool isAfterRunStart;
    public void TurnOnController()
    {
        isRun=true;
    }
    public void TurnOffController()
    {
        isRun=false;
    }
    protected virtual void OnInit(){}
    protected virtual void OnBeforeRunStart()
    {

    }
    protected virtual void OnBeforeRunUpdate()
    {
        if(!isBeforeRunStart)
        {
            isBeforeRunStart=true;
            OnBeforeRunStart();
        }
    }
    protected virtual void OnAfterRunStart()
    {
 
    }
    protected virtual void OnAfterRunUpdate()
    {
        if(!isAfterRunStart)
        {
            isAfterRunStart=true;
            OnAfterRunStart();
        }
    }
    protected virtual void AlwaysUpdate(){}

    public void GameUpdate()
    {
        if(!isInit)
        {
            isInit=true;
            OnInit();
        }
        if(!isRun)
        {
            OnBeforeRunUpdate();
        }else
        {
            OnAfterRunUpdate();
        }
        AlwaysUpdate();
    }
    protected virtual void OnRun(){}
}