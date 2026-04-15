using UnityEngine;
using System.Collections.Generic;
namespace Core
{
    public abstract class AbstractFacade
    {
        private bool isInit;
        public void GameUpdate()
        {
            OnUpdate();
        }
        protected virtual void OnInit()
        {

        }
        protected virtual void OnUpdate()
        {
            if(!isInit)
            {
                isInit=true;
                OnInit();

            }
        }
    }
}
