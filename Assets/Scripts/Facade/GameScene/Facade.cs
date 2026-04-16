using Core;
namespace GameScene
{
    public class Facade:AbstractFacade
    {

        private PlayerController m_playerController;
        private GameHudController m_gameHudController;

        protected override void OnInit()
        {
            base.OnInit();
            m_playerController=new PlayerController();
            m_gameHudController = new GameHudController(m_playerController);
            GameMediator.Instance.RegisterController(m_playerController);
            GameMediator.Instance.RegisterController(m_gameHudController);
        }
        protected override void OnUpdate()
        {
            base.OnUpdate();
            m_playerController.GameUpdate();
            m_gameHudController.GameUpdate();
        }

        public void DrawGUI()
        {
            m_gameHudController?.DrawGUI();
        }
    }
}
