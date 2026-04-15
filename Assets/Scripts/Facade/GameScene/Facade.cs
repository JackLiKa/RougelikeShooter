using Core;
namespace GameScene
{
    public class Facade:AbstractFacade
    {

        private PlayerController m_playerController;

        protected override void OnInit()
        {
            base.OnInit();
            m_playerController=new PlayerController();
            GameMediator.Instance.RegisterController(m_playerController);
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            m_playerController.GameUpdate();
        }
    }
}
