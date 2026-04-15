using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public abstract class IPanel
{
    public GameObject gameObject{get;protected set;}
    public Transform transform=>gameObject.transform;
    public RectTransform rectTransform{get;protected set;}
    protected IPanel parent;
    protected List<IPanel>children;
    private bool isInit;
    private bool isEnter;
    
    private bool isSuspend;
    protected bool isShowAfterExit;


    public IPanel(IPanel panel)
    {
        parent=panel;
        children=new List<IPanel>();
    }
    public void GameUpdate()
    {
        if(!isInit)
        {
            isInit=true;
            OnInit();
        }
        foreach(IPanel panel in children)
        {
            panel.GameUpdate();
        }
        if(!isSuspend)
        {
            OnUpdate();
        }
    }
    protected  virtual void OnInit()
    {
        Suspend();
        if(gameObject==null)
        {
            gameObject=GameObject.Find(GetType().Name);
        }
            rectTransform=gameObject.GetComponent<RectTransform>();

    }
    protected virtual void OnEnter()
    {

    }
    protected virtual void OnUpdate()
    {
        if(!isEnter)
        {
            isEnter=true;
            OnEnter();
        }
    }
    protected virtual void OnExit()
    {
        if(isShowAfterExit)
        {
            gameObject.SetActive(false);

        }

        parent.isEnter=false;
        parent.Resume();
        Suspend();
    }
    public void EnterPanel<T>()where T:IPanel
    {
        IPanel panel=GetPanel<T>();
        panel.Resume();
        panel.isEnter=false;
        Suspend();

    }
    public T GetPanel<T>() where T:IPanel
    {
        return children.Where(x=>x is T).ToArray()[0] as T;
    }
    public void Suspend()
    {
        isSuspend=true;
    }
    public void Resume()
    {
        isSuspend=false;
    }
}