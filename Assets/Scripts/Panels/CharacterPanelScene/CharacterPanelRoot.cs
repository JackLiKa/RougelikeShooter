using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CharacterPanelScene
{
    public class CharacterPanelRoot : IPanel
    {
        private const float PreviewAnimationFps = 6f;

        private static readonly Color ScreenTint = new Color(0.04f, 0.06f, 0.1f, 0.9f);
        private static readonly Color CardBackground = new Color(0.09f, 0.13f, 0.18f, 0.94f);
        private static readonly Color CardBorder = new Color(0.37f, 0.68f, 0.95f, 1f);
        private static readonly Color Accent = new Color(0.96f, 0.72f, 0.26f, 1f);
        private static readonly Color SoftText = new Color(0.82f, 0.88f, 0.94f, 1f);
        private static readonly Color ButtonBackground = new Color(0.34f, 0.37f, 0.42f, 1f);
        private static readonly Color ButtonText = Color.white;
        private static readonly Color TitleColor = new Color(1f, 0.83f, 0.28f, 1f);

        private readonly Dictionary<PlayerType, Sprite[]> idlePreviewCache = new Dictionary<PlayerType, Sprite[]>();
        private readonly Dictionary<WeaponType, Texture2D> weaponPreviewCache = new Dictionary<WeaponType, Texture2D>();

        private bool showReturnConfirm;
        private Rect dialogRect;

        public CharacterPanelRoot() : base(null)
        {
        }

        protected override void OnInit()
        {
            gameObject = GameObject.Find("CharacterPanel");
            rectTransform = gameObject != null ? gameObject.GetComponent<RectTransform>() : null;
            dialogRect = new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f - 95f, 340f, 190f);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                showReturnConfirm = true;
            }
        }

        public void DrawGUI()
        {
            PlayerType currentPlayer = GameSelectionConfig.CurrentPlayerType;
            PlayerProfile profile = PlayerProfileRepository.GetProfile(currentPlayer);

            DrawFilledRect(new Rect(0f, 0f, Screen.width, Screen.height), ScreenTint);
            DrawHeader(profile);
            DrawHeroCard(new Rect(68f, 118f, 360f, 392f), currentPlayer, profile);
            DrawStatsCard(new Rect(456f, 118f, 360f, 392f), profile);
            DrawBottomBar(profile);

            if (showReturnConfirm)
            {
                DrawReturnDialog();
            }
        }

        private void DrawHeader(PlayerProfile profile)
        {
            GUI.Label(
                new Rect(Screen.width * 0.5f - 220f, 28f, 440f, 30f),
                "CHARACTER PANEL",
                CreateLabelStyle(24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.Label(
                new Rect(Screen.width * 0.5f - 220f, 60f, 440f, 20f),
                profile.DisplayName,
                CreateLabelStyle(16, FontStyle.Bold, TextAnchor.MiddleCenter, TitleColor));
        }

        private void DrawHeroCard(Rect rect, PlayerType playerType, PlayerProfile profile)
        {
            DrawCard(rect, "CHARACTER");
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 46f, rect.width - 44f, 26f),
                profile.DisplayName,
                CreateLabelStyle(20, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
            GUI.Label(
                new Rect(rect.x + 22f, rect.y + 76f, rect.width - 44f, 18f),
                GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType),
                CreateLabelStyle(13, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));

            Rect previewRect = new Rect(rect.x + 50f, rect.y + 112f, rect.width - 100f, 160f);
            DrawPreview(previewRect, playerType);

            if (DrawButton(new Rect(rect.x + 24f, rect.y + rect.height - 72f, 132f, 40f), "PREV HERO"))
            {
                GameSelectionConfig.CurrentPlayerType = GameSelectionConfig.PreviousPlayer(GameSelectionConfig.CurrentPlayerType);
            }

            if (DrawButton(new Rect(rect.x + rect.width - 156f, rect.y + rect.height - 72f, 132f, 40f), "NEXT HERO"))
            {
                GameSelectionConfig.CurrentPlayerType = GameSelectionConfig.NextPlayer(GameSelectionConfig.CurrentPlayerType);
            }
        }

        private void DrawStatsCard(Rect rect, PlayerProfile profile)
        {
            DrawCard(rect, "STATS");

            GUIStyle labelStyle = CreateLabelStyle(14, FontStyle.Normal, TextAnchor.UpperLeft, SoftText);
            GUIStyle valueStyle = CreateLabelStyle(17, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);

            DrawStatRow(rect.x + 24f, rect.y + 72f, "HP", profile.MaxHp.ToString(), labelStyle, valueStyle);
            DrawStatRow(rect.x + 24f, rect.y + 118f, "ATK", profile.Attack.ToString(), labelStyle, valueStyle);
            DrawStatRow(rect.x + 24f, rect.y + 164f, "MOVE SPD", profile.MoveSpeed.ToString("0.0"), labelStyle, valueStyle);
            DrawStatRow(rect.x + 24f, rect.y + 210f, "FIRE RATE", profile.ShootSpeed.ToString("0.0") + "/s", labelStyle, valueStyle);
            DrawStatRow(
                rect.x + 24f,
                rect.y + 256f,
                "WEAPON",
                GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType),
                labelStyle,
                valueStyle);

            if (DrawButton(new Rect(rect.x + 24f, rect.y + rect.height - 72f, 132f, 40f), "PREV GUN"))
            {
                GameSelectionConfig.CurrentWeaponType = GameSelectionConfig.PreviousWeapon(GameSelectionConfig.CurrentWeaponType);
            }

            if (DrawButton(new Rect(rect.x + rect.width - 156f, rect.y + rect.height - 72f, 132f, 40f), "NEXT GUN"))
            {
                GameSelectionConfig.CurrentWeaponType = GameSelectionConfig.NextWeapon(GameSelectionConfig.CurrentWeaponType);
            }
        }

        private void DrawBottomBar(PlayerProfile profile)
        {
            Rect barRect = new Rect(68f, 540f, 748f, 92f);
            DrawCard(barRect, "READY");
            GUI.Label(
                new Rect(barRect.x + 24f, barRect.y + 40f, 430f, 22f),
                $"{profile.DisplayName}  |  HP {profile.MaxHp}  |  ATK {profile.Attack}",
                CreateLabelStyle(14, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));

            if (DrawButton(new Rect(barRect.x + 472f, barRect.y + 28f, 120f, 40f), "MAIN MENU"))
            {
                showReturnConfirm = true;
            }

            if (DrawButton(new Rect(barRect.x + 608f, barRect.y + 28f, 116f, 40f), "START"))
            {
                SceneManager.LoadScene("GameScene");
            }
        }

        private void DrawPreview(Rect rect, PlayerType playerType)
        {
            DrawFilledRect(rect, new Color(0.06f, 0.09f, 0.13f, 1f));
            DrawBorder(rect, CardBorder, 2f);

            Sprite[] previewFrames = GetIdlePreviewFrames(playerType);
            if (previewFrames.Length == 0)
            {
                DrawWeaponPreview(rect);
                return;
            }

            int frameIndex = Mathf.FloorToInt(Time.realtimeSinceStartup * PreviewAnimationFps) % previewFrames.Length;
            Rect characterRect = new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, rect.height - 16f);
            DrawSprite(characterRect, previewFrames[frameIndex]);
            DrawWeaponPreview(rect);
        }

        private Sprite[] GetIdlePreviewFrames(PlayerType playerType)
        {
            if (idlePreviewCache.TryGetValue(playerType, out Sprite[] cachedSprites))
            {
                return cachedSprites;
            }

            string previewFolder = playerType == PlayerType.Player2 ? "Player/player2" : "Player/player1";
            string[] candidatePaths =
            {
                previewFolder + "/Idle-Sheet",
                previewFolder + "/Idle_Sheet"
            };

            foreach (string resourcePath in candidatePaths)
            {
                Sprite[] loadedSprites = Resources.LoadAll<Sprite>(resourcePath);
                if (loadedSprites != null && loadedSprites.Length > 0)
                {
                    idlePreviewCache[playerType] = loadedSprites;
                    return loadedSprites;
                }
            }

            idlePreviewCache[playerType] = Array.Empty<Sprite>();
            return idlePreviewCache[playerType];
        }

        private void DrawSprite(Rect rect, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            Texture texture = sprite.texture;
            Rect spriteRect = sprite.rect;
            Rect uvRect = new Rect(
                spriteRect.x / texture.width,
                spriteRect.y / texture.height,
                spriteRect.width / texture.width,
                spriteRect.height / texture.height);
            float spriteAspect = spriteRect.width / spriteRect.height;
            float rectAspect = rect.width / rect.height;
            Rect drawRect = rect;

            if (spriteAspect > rectAspect)
            {
                float height = rect.width / spriteAspect;
                drawRect.y += (rect.height - height) * 0.5f;
                drawRect.height = height;
            }
            else
            {
                float width = rect.height * spriteAspect;
                drawRect.x += (rect.width - width) * 0.5f;
                drawRect.width = width;
            }

            GUI.DrawTextureWithTexCoords(drawRect, texture, uvRect, true);
        }

        private void DrawReturnDialog()
        {
            DrawCard(dialogRect, "CONFIRM");
            GUI.Label(
                new Rect(dialogRect.x + 22f, dialogRect.y + 58f, dialogRect.width - 44f, 24f),
                "Return to main menu?",
                CreateLabelStyle(16, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.Label(
                new Rect(dialogRect.x + 22f, dialogRect.y + 88f, dialogRect.width - 44f, 20f),
                "Current selection will be kept.",
                CreateLabelStyle(13, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText));

            if (DrawButton(new Rect(dialogRect.x + 34f, dialogRect.y + 128f, 112f, 38f), "CANCEL"))
            {
                showReturnConfirm = false;
            }

            if (DrawButton(new Rect(dialogRect.x + dialogRect.width - 146f, dialogRect.y + 128f, 112f, 38f), "CONFIRM"))
            {
                showReturnConfirm = false;
                SceneManager.LoadScene("MainMenuScene");
            }
        }

        private void DrawCard(Rect rect, string title)
        {
            DrawFilledRect(rect, CardBackground);
            DrawBorder(rect, CardBorder, 2f);
            DrawFilledRect(new Rect(rect.x, rect.y, rect.width, 5f), Accent);
            GUI.Label(
                new Rect(rect.x + 20f, rect.y + 14f, rect.width - 40f, 20f),
                title,
                CreateLabelStyle(15, FontStyle.Bold, TextAnchor.UpperLeft, TitleColor));
        }

        private void DrawStatRow(float x, float y, string label, string value, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            GUI.Label(new Rect(x, y, 150f, 20f), label, labelStyle);
            GUI.Label(new Rect(x, y + 18f, 220f, 22f), value, valueStyle);
        }

        private bool DrawButton(Rect rect, string label)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            buttonStyle.normal.textColor = ButtonText;
            buttonStyle.hover.textColor = ButtonText;
            buttonStyle.active.textColor = ButtonText;

            Color previousColor = GUI.backgroundColor;
            GUI.backgroundColor = ButtonBackground;
            bool clicked = GUI.Button(rect, label, buttonStyle);
            GUI.backgroundColor = previousColor;
            return clicked;
        }

        private void DrawWeaponPreview(Rect previewRect)
        {
            Texture2D weaponTexture = GetWeaponPreviewTexture(GameSelectionConfig.CurrentWeaponType);
            if (weaponTexture == null)
            {
                return;
            }

            float angle = Mathf.Sin(Time.realtimeSinceStartup * 2f) * 12f - 16f;
            Vector2 pivot = new Vector2(previewRect.x + previewRect.width * 0.62f, previewRect.y + previewRect.height * 0.56f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, pivot);

            Rect weaponRect = new Rect(
                previewRect.x + previewRect.width * 0.38f,
                previewRect.y + previewRect.height * 0.42f,
                previewRect.width * 0.44f,
                previewRect.height * 0.2f);
            GUI.DrawTexture(weaponRect, weaponTexture, ScaleMode.ScaleToFit, true);
            GUI.matrix = previousMatrix;
        }

        private Texture2D GetWeaponPreviewTexture(WeaponType weaponType)
        {
            if (weaponPreviewCache.TryGetValue(weaponType, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            Texture2D texture = Resources.Load<Texture2D>("Images/Weapon/" + GameSelectionConfig.GetWeaponObjectName(weaponType));
            weaponPreviewCache[weaponType] = texture;
            return texture;
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
