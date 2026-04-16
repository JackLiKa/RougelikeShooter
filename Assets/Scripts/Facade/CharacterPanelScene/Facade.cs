using Core;

namespace CharacterPanelScene
{
    public class Facade : AbstractFacade
    {
        private CharacterPanelController m_characterPanelController;

        protected override void OnInit()
        {
            base.OnInit();
            m_characterPanelController = new CharacterPanelController();
            GameMediator.Instance.RegisterController(m_characterPanelController);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            m_characterPanelController.GameUpdate();
        }

        public void DrawGUI()
        {
            m_characterPanelController?.DrawGUI();
        }
    }
}
