using Core;
namespace MainMenuScene
{
    public class Facade:AbstractFacade
    {
        private UIController m_uiController;

        protected override void OnInit()
        {
            base.OnInit();
            m_uiController=new UIController();
            GameMediator.Instance.RegisterController(m_uiController);
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            m_uiController.GameUpdate();
        }


    }

}
