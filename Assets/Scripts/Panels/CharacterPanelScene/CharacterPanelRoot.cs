using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CharacterPanelScene
{
    public class CharacterPanelRoot : IPanel
    {
        private const float PreviewAnimationFps = 6f;
        private const float VirtualWidth = 1920f;
        private const float VirtualHeight = 1080f;

        private static readonly Color ScreenTint = new Color(0.04f, 0.06f, 0.1f, 0.9f);
        private static readonly Color CardBackground = new Color(0.09f, 0.13f, 0.18f, 0.94f);
        private static readonly Color CardBorder = new Color(0.37f, 0.68f, 0.95f, 1f);
        private static readonly Color Accent = new Color(0.96f, 0.72f, 0.26f, 1f);
        private static readonly Color SoftText = new Color(0.82f, 0.88f, 0.94f, 1f);
        private static readonly Color ButtonBackground = new Color(0.18f, 0.27f, 0.4f, 1f);
        private static readonly Color ButtonText = Color.white;
        private static readonly Color TitleColor = new Color(1f, 0.83f, 0.28f, 1f);
        private static readonly Color DimOverlay = new Color(0f, 0f, 0f, 0.48f);

        private readonly Dictionary<PlayerType, Sprite[]> idlePreviewCache = new Dictionary<PlayerType, Sprite[]>();
        private readonly Dictionary<WeaponType, Texture2D> weaponPreviewCache = new Dictionary<WeaponType, Texture2D>();

        private bool showReturnConfirm;
        private string statusMessage = string.Empty;
        private float statusMessageUntil;
        private Matrix4x4 previousGuiMatrix;

        public CharacterPanelRoot() : base(null)
        {
        }

        protected override void OnInit()
        {
            gameObject = GameObject.Find("CharacterPanel");
            rectTransform = gameObject != null ? gameObject.GetComponent<RectTransform>() : null;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                showReturnConfirm = !showReturnConfirm;
            }
        }

        public void DrawGUI()
        {
            PlayerType currentPlayer = GameSelectionConfig.CurrentPlayerType;
            PlayerProfile profile = PlayerProfileRepository.GetProfile(currentPlayer);
            UserProgressData progress = UserProgressRepository.GetProgress();

            BeginVirtualCanvas();

            DrawFilledRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight), ScreenTint);
            DrawHeader(profile, progress);
            DrawHeroCard(new Rect(76f, 154f, 472f, 784f), currentPlayer, profile);
            DrawStatsCard(new Rect(580f, 154f, 472f, 784f), profile, progress);
            DrawProgressCard(new Rect(1084f, 154f, 760f, 784f), profile, progress);

            if (showReturnConfirm)
            {
                DrawFilledRect(new Rect(0f, 0f, VirtualWidth, VirtualHeight), DimOverlay);
                DrawReturnDialog();
            }

            EndVirtualCanvas();
        }

        private void DrawHeader(PlayerProfile profile, UserProgressData progress)
        {
            GUI.Label(new Rect(VirtualWidth * 0.5f - 280f, 44f, 560f, 34f), "\u89d2\u8272\u9762\u677f", CreateLabelStyle(30, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.Label(new Rect(VirtualWidth * 0.5f - 420f, 84f, 840f, 22f), $"{profile.DisplayName}  |  \u5f53\u524d\u6b66\u5668\uff1a{GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType)}  |  \u91d1\u5e01\uff1a{progress.Coins}", CreateLabelStyle(16, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText));
        }

        private void DrawHeroCard(Rect rect, PlayerType playerType, PlayerProfile profile)
        {
            DrawCard(rect, "\u89d2\u8272\u9884\u89c8");
            GUI.Label(new Rect(rect.x + 24f, rect.y + 46f, rect.width - 48f, 28f), profile.DisplayName, CreateLabelStyle(24, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
            GUI.Label(new Rect(rect.x + 24f, rect.y + 80f, rect.width - 48f, 18f), GameSelectionConfig.GetPlayerDisplayName(playerType), CreateLabelStyle(14, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));

            Rect previewRect = new Rect(rect.x + 34f, rect.y + 124f, rect.width - 68f, 418f);
            DrawPreview(previewRect, playerType);

            GUI.Label(new Rect(rect.x + 26f, rect.y + 564f, rect.width - 52f, 90f), "\u8fd9\u91cc\u4f1a\u663e\u793a\u5f53\u524d\u89d2\u8272\u548c\u624b\u6301\u6b66\u5668\u7684 Idle \u52a8\u753b\u3002\u8fdb\u5165\u6e38\u620f\u524d\uff0c\u53ef\u5148\u5728\u8fd9\u91cc\u6838\u5bf9\u89d2\u8272\u5f62\u8c61\u548c\u6b66\u5668\u9009\u62e9\u3002", CreateLabelStyle(16, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));

            if (DrawButton(new Rect(rect.x + 24f, rect.y + rect.height - 78f, 186f, 44f), "\u4e0a\u4e00\u89d2\u8272"))
            {
                GameSelectionConfig.CurrentPlayerType = GameSelectionConfig.PreviousPlayer(GameSelectionConfig.CurrentPlayerType);
            }

            if (DrawButton(new Rect(rect.x + rect.width - 210f, rect.y + rect.height - 78f, 186f, 44f), "\u4e0b\u4e00\u89d2\u8272"))
            {
                GameSelectionConfig.CurrentPlayerType = GameSelectionConfig.NextPlayer(GameSelectionConfig.CurrentPlayerType);
            }
        }

        private void DrawStatsCard(Rect rect, PlayerProfile profile, UserProgressData progress)
        {
            DrawCard(rect, "\u57fa\u7840\u5c5e\u6027");

            GUIStyle labelStyle = CreateLabelStyle(16, FontStyle.Normal, TextAnchor.UpperLeft, SoftText);
            GUIStyle valueStyle = CreateLabelStyle(22, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);

            DrawStatRow(rect.x + 28f, rect.y + 72f, rect.width - 56f, "\u751f\u547d\u503c", profile.MaxHp.ToString(), labelStyle, valueStyle);
            DrawStatRow(rect.x + 28f, rect.y + 156f, rect.width - 56f, "\u653b\u51fb\u529b", profile.Attack.ToString(), labelStyle, valueStyle);
            DrawStatRow(rect.x + 28f, rect.y + 240f, rect.width - 56f, "\u79fb\u52a8\u901f\u5ea6", profile.MoveSpeed.ToString("0.0"), labelStyle, valueStyle);
            DrawStatRow(rect.x + 28f, rect.y + 324f, rect.width - 56f, "\u5c04\u901f", profile.ShootSpeed.ToString("0.0") + " /\u79d2", labelStyle, valueStyle);
            DrawStatRow(rect.x + 28f, rect.y + 408f, rect.width - 56f, "\u5f53\u524d\u6b66\u5668", GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType), labelStyle, valueStyle);
            DrawStatRow(rect.x + 28f, rect.y + 492f, rect.width - 56f, "\u89d2\u8272\u5f3a\u5316\u7b49\u7ea7", UserProgressRepository.GetUpgradeLevel(GameSelectionConfig.CurrentPlayerType).ToString(), labelStyle, valueStyle);
            DrawStatRow(rect.x + 28f, rect.y + 576f, rect.width - 56f, "\u4e0b\u6b21\u5f3a\u5316\u82b1\u8d39", UserProgressRepository.GetNextUpgradeCost(GameSelectionConfig.CurrentPlayerType).ToString(), labelStyle, valueStyle);
            GUI.Label(new Rect(rect.x + 28f, rect.y + 668f, rect.width - 56f, 22f), $"\u7528\u6237\u7b49\u7ea7 {progress.Level}  |  \u7528\u6237\u7ecf\u9a8c {progress.CurrentExp}/{UserProgressRepository.GetRequiredExpForLevel(progress.Level)}", CreateLabelStyle(15, FontStyle.Bold, TextAnchor.UpperLeft, Accent));

            if (DrawButton(new Rect(rect.x + 28f, rect.y + rect.height - 142f, rect.width - 56f, 46f), "\u5f3a\u5316\u5f53\u524d\u89d2\u8272"))
            {
                if (UserProgressRepository.TryUpgradePlayer(GameSelectionConfig.CurrentPlayerType))
                {
                    statusMessage = "\u89d2\u8272\u5f3a\u5316\u6210\u529f\u3002";
                }
                else
                {
                    statusMessage = "\u91d1\u5e01\u4e0d\u8db3\uff0c\u65e0\u6cd5\u5f3a\u5316\u3002";
                }

                statusMessageUntil = Time.realtimeSinceStartup + 2.5f;
            }

            if (DrawButton(new Rect(rect.x + 28f, rect.y + rect.height - 78f, 186f, 44f), "\u4e0a\u4e00\u6b66\u5668"))
            {
                GameSelectionConfig.CurrentWeaponType = GameSelectionConfig.PreviousWeapon(GameSelectionConfig.CurrentWeaponType);
            }

            if (DrawButton(new Rect(rect.x + rect.width - 214f, rect.y + rect.height - 78f, 186f, 44f), "\u4e0b\u4e00\u6b66\u5668"))
            {
                GameSelectionConfig.CurrentWeaponType = GameSelectionConfig.NextWeapon(GameSelectionConfig.CurrentWeaponType);
            }
        }

        private void DrawProgressCard(Rect rect, PlayerProfile profile, UserProgressData progress)
        {
            DrawCard(rect, "\u5c40\u5916\u6210\u957f\u4e0e\u51fa\u51fb");

            GUI.Label(new Rect(rect.x + 28f, rect.y + 56f, rect.width - 56f, 28f), "\u5f53\u524d\u9009\u62e9", CreateLabelStyle(24, FontStyle.Bold, TextAnchor.UpperLeft, TitleColor));
            GUI.Label(new Rect(rect.x + 28f, rect.y + 96f, rect.width - 56f, 22f), $"\u89d2\u8272\uff1a{profile.DisplayName}", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
            GUI.Label(new Rect(rect.x + 28f, rect.y + 126f, rect.width - 56f, 22f), $"\u6b66\u5668\uff1a{GameSelectionConfig.GetWeaponDisplayName(GameSelectionConfig.CurrentWeaponType)}", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));

            Rect infoRect = new Rect(rect.x + 24f, rect.y + 176f, rect.width - 48f, 252f);
            DrawFilledRect(infoRect, new Color(0.05f, 0.07f, 0.1f, 0.96f));
            DrawBorder(infoRect, CardBorder, 2f);
            GUI.Label(new Rect(infoRect.x + 18f, infoRect.y + 18f, infoRect.width - 36f, 24f), "\u5c40\u5916\u6210\u957f\u6570\u636e", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, Accent));
            GUI.Label(new Rect(infoRect.x + 18f, infoRect.y + 56f, infoRect.width - 36f, 22f), $"\u7528\u6237\u7b49\u7ea7\uff1a{progress.Level}", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
            GUI.Label(new Rect(infoRect.x + 18f, infoRect.y + 88f, infoRect.width - 36f, 22f), $"\u91d1\u5e01\uff1a{progress.Coins}", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, Color.white));
            GUI.Label(new Rect(infoRect.x + 18f, infoRect.y + 120f, infoRect.width - 36f, 22f), $"\u7ecf\u9a8c\uff1a{progress.CurrentExp}/{UserProgressRepository.GetRequiredExpForLevel(progress.Level)}", CreateLabelStyle(16, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));
            GUI.Label(new Rect(infoRect.x + 18f, infoRect.y + 156f, infoRect.width - 36f, 72f), "\u5f00\u59cb\u6e38\u620f\u540e\uff0c\u51fb\u6740\u602a\u7269\u4f1a\u83b7\u5f97\u7ecf\u9a8c\uff0c\u63d0\u5347\u6ce2\u6b21\u8868\u73b0\u53ef\u83b7\u5f97\u91d1\u5e01\u4e0e\u7528\u6237\u7ecf\u9a8c\uff0c\u7528\u4e8e\u5c40\u5916\u6301\u7eed\u63d0\u5347 Player \u5c5e\u6027\u3002", CreateLabelStyle(16, FontStyle.Normal, TextAnchor.UpperLeft, SoftText));

            if (!string.IsNullOrWhiteSpace(statusMessage) && Time.realtimeSinceStartup <= statusMessageUntil)
            {
                GUI.Label(new Rect(rect.x + 28f, rect.y + 452f, rect.width - 56f, 24f), statusMessage, CreateLabelStyle(16, FontStyle.Bold, TextAnchor.UpperLeft, Accent));
            }

            Rect actionRect = new Rect(rect.x + 24f, rect.y + 520f, rect.width - 48f, 214f);
            DrawFilledRect(actionRect, new Color(0.05f, 0.07f, 0.1f, 0.96f));
            DrawBorder(actionRect, CardBorder, 2f);
            GUI.Label(new Rect(actionRect.x + 18f, actionRect.y + 18f, actionRect.width - 36f, 22f), "\u64cd\u4f5c", CreateLabelStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, Accent));

            if (DrawButton(new Rect(actionRect.x + 18f, actionRect.y + 60f, actionRect.width - 36f, 48f), "\u8fd4\u56de\u4e3b\u83dc\u5355"))
            {
                showReturnConfirm = true;
            }

            if (DrawButton(new Rect(actionRect.x + 18f, actionRect.y + 124f, actionRect.width - 36f, 56f), "\u5f00\u59cb\u65b0\u5bf9\u5c40"))
            {
                SessionSaveRepository.SelectSaveForLoad(null);
                SessionSaveRepository.ClearSavedSession();
                SessionSaveRepository.ClearSnapshots();
                SceneManager.LoadScene("GameScene");
            }
        }

        private void DrawPreview(Rect rect, PlayerType playerType)
        {
            DrawFilledRect(rect, new Color(0.06f, 0.09f, 0.13f, 1f));
            DrawBorder(rect, CardBorder, 2f);

            Sprite[] previewFrames = GetIdlePreviewFrames(playerType);
            if (previewFrames.Length > 0)
            {
                int frameIndex = Mathf.FloorToInt(Time.realtimeSinceStartup * PreviewAnimationFps) % previewFrames.Length;
                Rect characterRect = new Rect(rect.x + 12f, rect.y + 8f, rect.width - 24f, rect.height - 16f);
                DrawSprite(characterRect, previewFrames[frameIndex]);
            }

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
            Rect dialogRect = new Rect(VirtualWidth * 0.5f - 214f, VirtualHeight * 0.5f - 110f, 428f, 220f);
            DrawCard(dialogRect, "\u786e\u8ba4\u8fd4\u56de");
            GUI.Label(new Rect(dialogRect.x + 24f, dialogRect.y + 62f, dialogRect.width - 48f, 24f), "\u8fd4\u56de\u4e3b\u83dc\u5355\u5417\uff1f", CreateLabelStyle(20, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white));
            GUI.Label(new Rect(dialogRect.x + 24f, dialogRect.y + 92f, dialogRect.width - 48f, 20f), "\u5f53\u524d\u89d2\u8272\u4e0e\u6b66\u5668\u9009\u62e9\u4f1a\u88ab\u4fdd\u7559\u3002", CreateLabelStyle(14, FontStyle.Normal, TextAnchor.MiddleCenter, SoftText));

            if (DrawButton(new Rect(dialogRect.x + 34f, dialogRect.y + 146f, 150f, 40f), "\u53d6\u6d88"))
            {
                showReturnConfirm = false;
            }

            if (DrawButton(new Rect(dialogRect.x + dialogRect.width - 184f, dialogRect.y + 146f, 150f, 40f), "\u786e\u8ba4\u8fd4\u56de"))
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
            GUI.Label(new Rect(rect.x + 20f, rect.y + 14f, rect.width - 40f, 22f), title, CreateLabelStyle(18, FontStyle.Bold, TextAnchor.UpperLeft, TitleColor));
        }

        private void DrawStatRow(float x, float y, float width, string label, string value, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            GUI.Label(new Rect(x, y, width, 24f), label, labelStyle);
            GUI.Label(new Rect(x, y + 28f, width, 32f), value, valueStyle);
        }

        private bool DrawButton(Rect rect, string label)
        {
            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                font = CjkFontHelper.GetFont()
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
                alignment = anchor,
                wordWrap = true,
                font = CjkFontHelper.GetFont()
            };
            style.normal.textColor = color;
            return style;
        }

        private void BeginVirtualCanvas()
        {
            previousGuiMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / VirtualWidth, Screen.height / VirtualHeight);
            Vector2 offset = new Vector2(
                (Screen.width - (VirtualWidth * scale)) * 0.5f,
                (Screen.height - (VirtualHeight * scale)) * 0.5f);
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1f));
        }

        private void EndVirtualCanvas()
        {
            GUI.matrix = previousGuiMatrix;
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
