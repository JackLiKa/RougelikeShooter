using UnityEngine;

namespace GameScene
{
    public class GameHudRoot
    {
        private const float WaveDuration = 30f;

        private static readonly Color CardBackground = new Color(0.07f, 0.1f, 0.14f, 0.88f);
        private static readonly Color CardBorder = new Color(0.37f, 0.68f, 0.95f, 1f);
        private static readonly Color Accent = new Color(0.96f, 0.72f, 0.26f, 1f);
        private static readonly Color BarBackground = new Color(0.15f, 0.2f, 0.25f, 1f);
        private static readonly Color HpBarColor = new Color(0.91f, 0.28f, 0.34f, 1f);
        private static readonly Color SoftText = new Color(0.82f, 0.88f, 0.94f, 1f);

        private float startTime;
        private bool initialized;

        public void DrawGUI(IPlayer player)
        {
            if (!initialized)
            {
                initialized = true;
                startTime = Time.time;
            }

            PlayerRuntimeStats stats = player != null ? PlayerRuntimeStats.Get(player.gameObject) : null;
            float elapsed = Mathf.Max(0f, Time.time - startTime);
            int currentWave = Mathf.FloorToInt(elapsed / WaveDuration) + 1;
            float nextWaveIn = WaveDuration - (elapsed % WaveDuration);

            DrawPlayerCard(stats);
            DrawWaveCard(currentWave, nextWaveIn);
            DrawTimerCard(elapsed);
        }

        private void DrawPlayerCard(PlayerRuntimeStats stats)
        {
            Rect rect = new Rect(18f, 18f, 300f, 150f);
            DrawCard(rect);

            GUIStyle nameStyle = CreateLabelStyle(22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            GUIStyle statStyle = CreateLabelStyle(15, FontStyle.Normal, TextAnchor.UpperLeft, SoftText);
            GUIStyle valueStyle = CreateLabelStyle(17, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);

            string name = stats != null ? stats.DisplayName : GameSelectionConfig.GetPlayerDisplayName(GameSelectionConfig.CurrentPlayerType);
            int maxHp = stats != null ? stats.MaxHp : PlayerProfileRepository.GetProfile(GameSelectionConfig.CurrentPlayerType).MaxHp;
            int currentHp = stats != null ? stats.CurrentHp : maxHp;
            int attack = stats != null ? stats.Attack : PlayerProfileRepository.GetProfile(GameSelectionConfig.CurrentPlayerType).Attack;
            float moveSpeed = stats != null ? stats.MoveSpeed : PlayerProfileRepository.GetProfile(GameSelectionConfig.CurrentPlayerType).MoveSpeed;
            float shootSpeed = stats != null ? stats.ShootSpeed : PlayerProfileRepository.GetProfile(GameSelectionConfig.CurrentPlayerType).ShootSpeed;

            GUI.Label(new Rect(rect.x + 18f, rect.y + 14f, rect.width - 36f, 28f), name, nameStyle);

            Rect barRect = new Rect(rect.x + 18f, rect.y + 48f, rect.width - 36f, 18f);
            DrawFilledRect(barRect, BarBackground);
            DrawFilledRect(new Rect(barRect.x, barRect.y, barRect.width * Mathf.Clamp01(maxHp <= 0 ? 0f : (float)currentHp / maxHp), barRect.height), HpBarColor);
            DrawBorder(barRect, CardBorder, 1f);

            GUI.Label(new Rect(rect.x + 20f, rect.y + 74f, 120f, 22f), "HP", statStyle);
            GUI.Label(new Rect(rect.x + 56f, rect.y + 72f, 120f, 24f), $"{currentHp} / {maxHp}", valueStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 104f, 126f, 22f), $"ATK  {attack}", statStyle);
            GUI.Label(new Rect(rect.x + 150f, rect.y + 104f, 130f, 22f), $"MOVE  {moveSpeed:0.0}", statStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 128f, 160f, 22f), $"FIRE RATE  {shootSpeed:0.0}/s", statStyle);
            GUI.Label(new Rect(rect.x + 176f, rect.y + 128f, 106f, 22f), GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType), statStyle);
        }

        private void DrawWaveCard(int currentWave, float nextWaveIn)
        {
            float width = 230f;
            Rect rect = new Rect(Screen.width * 0.5f - width * 0.5f, 18f, width, 88f);
            DrawCard(rect);

            GUIStyle titleStyle = CreateLabelStyle(16, FontStyle.Bold, TextAnchor.MiddleCenter, Accent);
            GUIStyle waveStyle = CreateLabelStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            GUIStyle hintStyle = CreateLabelStyle(13, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText);

            GUI.Label(new Rect(rect.x, rect.y + 8f, rect.width, 20f), "CURRENT WAVE", titleStyle);
            GUI.Label(new Rect(rect.x, rect.y + 26f, rect.width, 32f), $"WAVE {currentWave}", waveStyle);
            GUI.Label(new Rect(rect.x, rect.y + 58f, rect.width, 18f), $"Next wave in {nextWaveIn:0}s", hintStyle);
        }

        private void DrawTimerCard(float elapsed)
        {
            Rect rect = new Rect(Screen.width - 178f, 18f, 160f, 72f);
            DrawCard(rect);

            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);

            GUIStyle titleStyle = CreateLabelStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter, Accent);
            GUIStyle timerStyle = CreateLabelStyle(26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            GUI.Label(new Rect(rect.x, rect.y + 10f, rect.width, 18f), "SURVIVAL TIME", titleStyle);
            GUI.Label(new Rect(rect.x, rect.y + 30f, rect.width, 28f), $"{minutes:00}:{seconds:00}", timerStyle);
        }

        private GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, TextAnchor anchor, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = anchor
            };
            style.normal.textColor = color;
            return style;
        }

        private void DrawCard(Rect rect)
        {
            DrawFilledRect(rect, CardBackground);
            DrawBorder(rect, CardBorder, 2f);
            DrawFilledRect(new Rect(rect.x, rect.y, rect.width, 4f), Accent);
        }

        private void DrawFilledRect(Rect rect, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawFilledRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            DrawFilledRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawFilledRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
    }
}
