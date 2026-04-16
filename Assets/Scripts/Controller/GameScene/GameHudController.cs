namespace GameScene
{
    public class GameHudController : AbstractController
    {
        private readonly GameHudRoot root = new GameHudRoot();
        private readonly PlayerController playerController;

        public GameHudController(PlayerController playerController)
        {
            this.playerController = playerController;
        }

        public void DrawGUI()
        {
            root.DrawGUI(playerController?.MainPlayer);
        }
    }
}
