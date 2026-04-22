using UnityEngine;

namespace GameScene
{
    public class GameHudRoot
    {
        private const float BarSmoothSpeed = 10f;
        private const float BarSnapThreshold = 0.001f;
        private static readonly Color CardBackground = new Color(0.07f, 0.1f, 0.14f, 0.88f);
        private static readonly Color CardBorder = new Color(0.37f, 0.68f, 0.95f, 1f);
        private static readonly Color Accent = new Color(0.96f, 0.72f, 0.26f, 1f);
        private static readonly Color BarBackground = new Color(0.15f, 0.2f, 0.25f, 1f);
        private static readonly Color HpBarColor = new Color(0.91f, 0.28f, 0.34f, 1f);
        private static readonly Color ExpBarColor = new Color(0.3f, 0.83f, 0.48f, 1f);
        private static readonly Color SoftText = new Color(0.82f, 0.88f, 0.94f, 1f);
        private static readonly Color OverlayTint = new Color(0.02f, 0.03f, 0.05f, 0.84f);
        private float displayedPlayerHpRatio;
        private float displayedExpRatio;
        private bool playerHpBarInitialized;
        private bool expBarInitialized;

        public void DrawGUI(IPlayer player)
        {
            RoguelikeGameManager session = RoguelikeGameManager.Instance;
            PlayerRuntimeStats stats = session != null ? session.PlayerStats : (player != null ? PlayerRuntimeStats.Get(player.gameObject) : null);

            DrawPlayerCard(stats);
            DrawWaveCard(session);
            DrawTimerCard(session);
            DrawProgressCard(session);

            if (session == null)
            {
                return;
            }

            if (session.ShowPauseMenu)
            {
                DrawPauseMenu(session);
            }

            if (session.ShowUpgradeChoices)
            {
                DrawUpgradeChoices(session);
            }

            if (session.ShowSettlement)
            {
                DrawSettlement(session);
            }
        }

        private void DrawPlayerCard(PlayerRuntimeStats stats)
        {
            RoguelikeGameManager session = RoguelikeGameManager.Instance;
            Rect rect = new Rect(18f, 18f, 320f, 162f);
            DrawCard(rect);

            GUIStyle nameStyle = CreateLabelStyle(22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            GUIStyle statStyle = CreateLabelStyle(15, FontStyle.Normal, TextAnchor.UpperLeft, SoftText);
            GUIStyle valueStyle = CreateLabelStyle(17, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);

            string name = stats != null ? stats.DisplayName : GameSelectionConfig.GetPlayerDisplayName(GameSelectionConfig.CurrentPlayerType);
            int maxHp = stats != null ? stats.MaxHp : PlayerProfileRepository.GetProfile(GameSelectionConfig.CurrentPlayerType).MaxHp;
            int currentHp = stats != null ? stats.CurrentHp : maxHp;
            int attack = stats != null ? stats.Attack : PlayerProfileRepository.GetProfile(GameSelectionConfig.CurrentPlayerType).Attack;
            float moveSpeed = stats != null ? stats.MoveSpeed : PlayerProfileRepository.GetProfile(GameSelectionConfig.CurrentPlayerType).MoveSpeed;
            float shootSpeed = session != null ? session.CurrentFireRate : (stats != null ? stats.ShootSpeed : PlayerProfileRepository.GetProfile(GameSelectionConfig.CurrentPlayerType).ShootSpeed);

            GUI.Label(new Rect(rect.x + 18f, rect.y + 14f, rect.width - 36f, 28f), name, nameStyle);

            Rect barRect = new Rect(rect.x + 18f, rect.y + 48f, rect.width - 36f, 18f);
            float hpRatio = Mathf.Clamp01(maxHp <= 0 ? 0f : (float)currentHp / maxHp);
            displayedPlayerHpRatio = SmoothBarValue(displayedPlayerHpRatio, hpRatio, ref playerHpBarInitialized);
            DrawFilledRect(barRect, BarBackground);
            DrawFilledRect(new Rect(barRect.x, barRect.y, barRect.width * displayedPlayerHpRatio, barRect.height), HpBarColor);
            DrawBorder(barRect, CardBorder, 1f);

            GUI.Label(new Rect(rect.x + 20f, rect.y + 74f, 120f, 22f), "HP", statStyle);
            GUI.Label(new Rect(rect.x + 56f, rect.y + 72f, 120f, 24f), $"{currentHp} / {maxHp}", valueStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 104f, 126f, 22f), $"ATK  {attack}", statStyle);
            GUI.Label(new Rect(rect.x + 150f, rect.y + 104f, 130f, 22f), $"MOVE  {moveSpeed:0.0}", statStyle);
            GUI.Label(new Rect(rect.x + 20f, rect.y + 128f, 160f, 22f), $"FIRE  {shootSpeed:0.0}/s", statStyle);
            GUI.Label(new Rect(rect.x + 176f, rect.y + 128f, 120f, 22f), GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType), statStyle);
        }

        private void DrawWaveCard(RoguelikeGameManager session)
        {
            float width = 250f;
            Rect rect = new Rect(Screen.width * 0.5f - width * 0.5f, 18f, width, 88f);
            DrawCard(rect);

            GUIStyle titleStyle = CreateLabelStyle(16, FontStyle.Bold, TextAnchor.MiddleCenter, Accent);
            GUIStyle waveStyle = CreateLabelStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            GUIStyle hintStyle = CreateLabelStyle(13, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText);

            int currentWave = session != null ? session.CurrentWave : 1;
            float nextWaveIn = session != null ? session.NextWaveIn : 30f;

            GUI.Label(new Rect(rect.x, rect.y + 8f, rect.width, 20f), "CURRENT WAVE", titleStyle);
            GUI.Label(new Rect(rect.x, rect.y + 26f, rect.width, 32f), $"WAVE {currentWave}", waveStyle);
            GUI.Label(new Rect(rect.x, rect.y + 58f, rect.width, 18f), $"Next wave in {nextWaveIn:0}s", hintStyle);
        }

        private void DrawTimerCard(RoguelikeGameManager session)
        {
            Rect rect = new Rect(Screen.width - 178f, 18f, 160f, 72f);
            DrawCard(rect);

            float elapsed = session != null ? session.ElapsedTime : 0f;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);

            GUIStyle titleStyle = CreateLabelStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter, Accent);
            GUIStyle timerStyle = CreateLabelStyle(26, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);

            GUI.Label(new Rect(rect.x, rect.y + 10f, rect.width, 18f), "SURVIVAL TIME", titleStyle);
            GUI.Label(new Rect(rect.x, rect.y + 30f, rect.width, 28f), $"{minutes:00}:{seconds:00}", timerStyle);
        }

        private void DrawProgressCard(RoguelikeGameManager session)
        {
            if (session == null)
            {
                return;
            }

            Rect rect = new Rect(18f, Screen.height - 118f, 430f, 96f);
            DrawCard(rect);

            GUI.Label(new Rect(rect.x + 18f, rect.y + 12f, 120f, 18f), $"LEVEL {session.PlayerLevel}", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, Accent));
            GUI.Label(new Rect(rect.x + 150f, rect.y + 12f, 120f, 18f), $"AMMO {session.CurrentAmmo}/{session.MaxAmmo}", CreateLabelStyle(15, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
            GUI.Label(new Rect(rect.x + 300f, rect.y + 12f, 110f, 18f), $"REWIND {session.RewindUsesRemaining}", CreateLabelStyle(15, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));

            Rect expRect = new Rect(rect.x + 18f, rect.y + 42f, rect.width - 36f, 18f);
            displayedExpRatio = SmoothBarValue(displayedExpRatio, session.ExpRatio, ref expBarInitialized);
            DrawFilledRect(expRect, BarBackground);
            DrawFilledRect(new Rect(expRect.x, expRect.y, expRect.width * displayedExpRatio, expRect.height), ExpBarColor);
            DrawBorder(expRect, CardBorder, 1f);

            GUI.Label(new Rect(rect.x + 18f, rect.y + 64f, rect.width - 36f, 18f), $"EXP {session.CurrentExp:0}/{session.ExpToNextLevel:0}    Press R to rewind to the latest snapshot", CreateLabelStyle(13, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
            if (session.IsReloading)
            {
                GUI.Label(new Rect(rect.x + 18f, rect.y + 80f, rect.width - 36f, 16f), $"Reloading {session.ReloadRemaining:0.0}s", CreateLabelStyle(12, FontStyle.Bold, TextAnchor.UpperLeft, Accent));
            }
        }

        private void DrawPauseMenu(RoguelikeGameManager session)
        {
            DrawOverlay();

            Rect rect = new Rect(Screen.width * 0.5f - 210f, Screen.height * 0.5f - 180f, 420f, 360f);
            DrawCard(rect);

            GUI.Label(new Rect(rect.x, rect.y + 20f, rect.width, 28f), "PAUSED", CreateLabelStyle(28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.Label(new Rect(rect.x + 28f, rect.y + 62f, rect.width - 56f, 18f), "Save, settle, return to menu, or quit the game from here.", CreateLabelStyle(13, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText));

            if (DrawButton(new Rect(rect.x + 110f, rect.y + 104f, 200f, 40f), "SAVE RUN"))
            {
                session.SaveSession();
            }

            if (DrawButton(new Rect(rect.x + 110f, rect.y + 154f, 200f, 40f), "SETTLE RUN"))
            {
                session.FinalizeRun();
            }

            if (DrawButton(new Rect(rect.x + 110f, rect.y + 204f, 200f, 40f), "RETURN TO MENU"))
            {
                session.ReturnToMainMenu();
            }

            if (DrawButton(new Rect(rect.x + 110f, rect.y + 254f, 200f, 40f), "QUIT GAME"))
            {
                session.QuitGameWithSave();
            }

            if (DrawButton(new Rect(rect.x + 110f, rect.y + 304f, 200f, 34f), "RESUME"))
            {
                session.TogglePauseMenu();
            }
        }

        private void DrawUpgradeChoices(RoguelikeGameManager session)
        {
            DrawOverlay();

            Rect titleRect = new Rect(Screen.width * 0.5f - 250f, 84f, 500f, 44f);
            GUI.Label(titleRect, "LEVEL UP  |  Pick one card", CreateLabelStyle(30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));

            float cardWidth = 260f;
            float spacing = 36f;
            float totalWidth = cardWidth * session.CurrentCardChoices.Count + spacing * Mathf.Max(0, session.CurrentCardChoices.Count - 1);
            float startX = Screen.width * 0.5f - totalWidth * 0.5f;

            for (int index = 0; index < session.CurrentCardChoices.Count; index++)
            {
                PowerCardData card = session.CurrentCardChoices[index];
                Rect rect = new Rect(startX + index * (cardWidth + spacing), Screen.height * 0.5f - 150f, cardWidth, 300f);
                DrawCard(rect);

                GUI.Label(new Rect(rect.x + 18f, rect.y + 24f, rect.width - 36f, 28f), card.Title, CreateLabelStyle(22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
                GUI.Label(new Rect(rect.x + 18f, rect.y + 60f, rect.width - 36f, 48f), card.Description, CreateLabelStyle(14, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
                GUI.Label(new Rect(rect.x + 18f, rect.y + 132f, rect.width - 36f, 20f), $"Current stacks: {session.GetCardStack(card.CardKey)} / {card.MaxStacks}", CreateLabelStyle(13, FontStyle.Bold, TextAnchor.UpperLeft, Accent));
                GUI.Label(new Rect(rect.x + 18f, rect.y + 164f, rect.width - 36f, 82f), BuildCardSummary(card), CreateLabelStyle(13, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));

                if (DrawButton(new Rect(rect.x + 30f, rect.y + rect.height - 58f, rect.width - 60f, 38f), "CHOOSE"))
                {
                    session.ChooseUpgradeCard(index);
                }
            }
        }

        private void DrawSettlement(RoguelikeGameManager session)
        {
            DrawOverlay();

            Rect rect = new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f - 170f, 440f, 340f);
            DrawCard(rect);

            GUI.Label(new Rect(rect.x, rect.y + 22f, rect.width, 32f), "RUN COMPLETE", CreateLabelStyle(30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.Label(new Rect(rect.x, rect.y + 62f, rect.width, 20f), $"Reached wave {session.CurrentWave}", CreateLabelStyle(16, FontStyle.Bold, TextAnchor.MiddleCenter, Accent));

            GUI.Label(new Rect(rect.x + 42f, rect.y + 112f, rect.width - 84f, 24f), $"Gold earned: {session.EarnedGold}", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.Label(new Rect(rect.x + 42f, rect.y + 144f, rect.width - 84f, 24f), $"User EXP earned: {session.EarnedUserExp}", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.Label(new Rect(rect.x + 42f, rect.y + 184f, rect.width - 84f, 20f), $"Current meta level: {UserProgressRepository.GetProgress().Level}", CreateLabelStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText));
            GUI.Label(new Rect(rect.x + 42f, rect.y + 208f, rect.width - 84f, 20f), $"Current gold: {UserProgressRepository.GetProgress().Coins}", CreateLabelStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText));

            if (DrawButton(new Rect(rect.x + 120f, rect.y + 252f, 200f, 40f), "RETURN TO MENU"))
            {
                session.ReturnToMainMenu();
            }
        }

        private string BuildCardSummary(PowerCardData card)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            AppendBonus(builder, card.BonusHp, "HP");
            AppendBonus(builder, card.BonusAttack, "ATK");
            AppendBonus(builder, card.BonusMoveSpeed, "Move");
            AppendBonus(builder, card.BonusShootRate, "Fire Rate");
            AppendBonus(builder, card.BonusBulletSpeed, "Bullet Speed");
            AppendBonus(builder, card.BonusProjectileCount, "Projectile");
            AppendBonus(builder, card.BonusPierce, "Pierce");
            AppendBonus(builder, card.BonusPickupRadius, "Pickup Radius");
            AppendBonus(builder, card.BonusHealOnPickup, "Heal on Pickup");
            return builder.Length == 0 ? "No active effect" : builder.ToString();
        }

        private void AppendBonus(System.Text.StringBuilder builder, int value, string label)
        {
            if (value == 0)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(label).Append(" +").Append(value);
        }

        private void AppendBonus(System.Text.StringBuilder builder, float value, string label)
        {
            if (Mathf.Abs(value) <= 0.001f)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append('\n');
            }

            builder.Append(label).Append(" +").Append(value.ToString("0.0"));
        }

        private void DrawOverlay()
        {
            DrawFilledRect(new Rect(0f, 0f, Screen.width, Screen.height), OverlayTint);
        }

        private bool DrawButton(Rect rect, string label)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = Color.white;

            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.18f, 0.27f, 0.4f, 1f);
            bool clicked = GUI.Button(rect, label, buttonStyle);
            GUI.backgroundColor = previous;
            return clicked;
        }

        private GUIStyle CreateLabelStyle(int fontSize, FontStyle fontStyle, TextAnchor anchor, Color color)
        {
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                fontStyle = fontStyle,
                alignment = anchor,
                wordWrap = true
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

        private float SmoothBarValue(float currentValue, float targetValue, ref bool initialized)
        {
            float clampedTarget = Mathf.Clamp01(targetValue);
            if (!initialized)
            {
                initialized = true;
                return clampedTarget;
            }

            if (Mathf.Abs(currentValue - clampedTarget) <= BarSnapThreshold)
            {
                return clampedTarget;
            }

            float interpolation = 1f - Mathf.Exp(-BarSmoothSpeed * Time.unscaledDeltaTime);
            return Mathf.Lerp(currentValue, clampedTarget, interpolation);
        }
    }
}
