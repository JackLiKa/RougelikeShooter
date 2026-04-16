namespace CharacterPanelScene
{
    public class CharacterPanelController : AbstractController
    {
        private CharacterPanelRoot rootPanel;

        protected override void OnInit()
        {
            base.OnInit();
            rootPanel = new CharacterPanelRoot();
        }

        protected override void AlwaysUpdate()
        {
            base.AlwaysUpdate();
            rootPanel.GameUpdate();
        }

        public void DrawGUI()
        {
            rootPanel?.DrawGUI();
        }
    }
}
