using System;
using UnityEngine;


public class PlayerBase
{

    private bool m_isLeft;
    private bool isInit;
    private bool isStart;
    private bool isShouldRemove;
    private bool isAlreadyRemove;
 

    public GameObject gameObject{get;protected set;}
    public Transform transform=>gameObject.transform;
    public PlayerBase(GameObject obj)
    {
        gameObject=obj;
    }
    public void GameUpdate()
    {
        if(!isInit)
        {
            isInit=true;
            OnInit();
        }

        OnPlayerUpdate();
    }
    protected virtual void OnInit()
    {
        isInit=true;
        // Debug.Log("现在执行的是PlayerBase.cs的OnInit方法");
    }
    protected virtual void OnPlayerStart()
    {
        
    }
    protected virtual void OnPlayerUpdate()
    {
        if(!isStart)
        {
            isStart=true;
            OnPlayerStart();
        }
    }

    protected virtual void OnPlayerDieStart()
    {
        
    }
    protected virtual void OnPlayerDieUpdate()
    {
        
    }
    public void Remove()
    {
        isShouldRemove=true;
    }
   public bool isLeft
    {
        get=>m_isLeft;
        set
        {
            m_isLeft=value;
        }
    }
}
