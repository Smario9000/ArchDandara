/*
 * ArchDandara documentation
 * Purpose: Adds optional Archipelago branding and layout changes to the Trials of Fear main menu.
 * Why: Branding edits fragile runtime UI, so it is isolated in one service.
 * Notes: Menu edits cache Unity objects carefully because scene transitions can destroy them while static fields remain.
 */

using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ArchDandara.Game
{
    public static class MainMenuArchipelagoBrandingService
    {
        private const string DlcTitlePath =
            "MainMenuManager_TrialsOfFear/Image Title/MENU_DLCTitle_WithTimer/MENU_DLCTitle";
        private const string MiniCreditsPath =
            "MainMenuManager_TrialsOfFear/Title Menu/Content/Margin/Mini credits/Text";
        private const string TitleImageName = "TrialsOfArchipelagoTitle.png";
        private const string CreditsLineOne = "Mod Made by: Smores9000";
        private const string CreditsLineTwo = "AP Helped By: SW_CreeperKing";
        private const string DiscordUrl = "https://discord.gg/archipelago";

        private static bool CreditsApplied;
        private static bool LoggedTitleMissing;
        private static bool LoggedCreditsMissing;
        private static bool LoggedTitleLoadFailure;
        private static bool LoggedTitleApplied;
        private static bool LoggedFrameInfo;
        private static Sprite TitleSprite;
        private static Image CachedTitleImage;
        private static float NextTitleScanTime;
        private static float NextScanTime;

        public static void Update()
        {
            ApplyTitleImage();

            if (!CreditsApplied && Time.time >= NextScanTime)
            {
                NextScanTime = Time.time + 0.5f;
                ApplyCredits();
            }
        }

        private static void ApplyTitleImage()
        {
            try
            {
                Image image = CachedTitleImage;
                if (!IsValidTitleImage(image))
                {
                    CachedTitleImage = null;
                    image = null;
                }

                if (object.ReferenceEquals(image, null))
                {
                    if (Time.time < NextTitleScanTime)
                        return;

                    NextTitleScanTime = Time.time + 0.5f;
                    image = FindDlcTitleImage();
                    if (!object.ReferenceEquals(image, null))
                        CachedTitleImage = image;
                }

                if (object.ReferenceEquals(image, null))
                {
                    LogMissingOnce(ref LoggedTitleMissing, "[MainMenuBranding] DLC title image not found yet.");
                    return;
                }

                Sprite sprite = GetTitleSprite(image.sprite);
                if (object.ReferenceEquals(sprite, null))
                    return;

                LogOriginalFrameInfo(image.sprite);
                image.sprite = sprite;
                image.overrideSprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;

                if (!LoggedTitleApplied)
                {
                    LoggedTitleApplied = true;
                    MLLog.Msg("[MainMenuBranding] Replaced DLC title image with " + GetTitleImagePath() + ".");
                }
            }
            catch (NullReferenceException)
            {
                CachedTitleImage = null;
                NextTitleScanTime = Time.time + 1.0f;
            }
            catch (MissingReferenceException)
            {
                CachedTitleImage = null;
                NextTitleScanTime = Time.time + 1.0f;
            }
        }

        private static void ApplyCredits()
        {
            Text text = FindMiniCreditsText();
            if (object.ReferenceEquals(text, null))
            {
                LogMissingOnce(ref LoggedCreditsMissing, "[MainMenuBranding] Mini credits text not found yet.");
                return;
            }

            string baseText = StripExistingArchDandaraCredits(text.text);
            if (!string.IsNullOrEmpty(baseText) && !baseText.EndsWith("\n"))
                baseText += "\n";

            text.text = baseText + CreditsLineOne + "\n" + CreditsLineTwo + "\n" + DiscordUrl;
            text.raycastTarget = true;
            EnsureClickableLink(text.gameObject);

            CreditsApplied = true;
            MLLog.Msg("[MainMenuBranding] Added Archipelago mini credits and Discord link.");
        }

        private static string StripExistingArchDandaraCredits(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            text = text.Replace("\r\n", "\n");
            text = text.Replace("\n" + CreditsLineOne, "");
            text = text.Replace("\n" + CreditsLineTwo, "");
            text = text.Replace("\n" + DiscordUrl, "");
            text = text.Replace(CreditsLineOne, "");
            text = text.Replace(CreditsLineTwo, "");
            text = text.Replace(DiscordUrl, "");
            return text.TrimEnd('\n', '\r', ' ');
        }

        private static void EnsureClickableLink(GameObject gameObject)
        {
            Button button = gameObject.GetComponent<Button>();
            if (object.ReferenceEquals(button, null))
                button = gameObject.AddComponent<Button>();

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(OpenDiscord);
            button.transition = Selectable.Transition.None;
        }

        private static void OpenDiscord()
        {
            Application.OpenURL(DiscordUrl);
        }

        private static Image FindDlcTitleImage()
        {
            Image exactPath = FindImageByPath(DlcTitlePath);
            if (IsValidTitleImage(exactPath))
                return exactPath;

            Image fallback = null;
            Image[] images = Resources.FindObjectsOfTypeAll<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (!IsValidTitleImage(image))
                    continue;

                string path = NormalizePath(PathFor(image.transform));
                if (path.IndexOf("MENU_DLCTitle_WithTimer", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    image.name.IndexOf("MENU_DLCTitle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return image;

                if (image.name.IndexOf("MENU_DLCTitle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    fallback = image;
            }

            return fallback;
        }

        private static bool IsValidTitleImage(Image image)
        {
            try
            {
                if (object.ReferenceEquals(image, null) || image == null)
                    return false;

                if (object.ReferenceEquals(image.gameObject, null) || image.gameObject == null ||
                    object.ReferenceEquals(image.transform, null) || image.transform == null)
                    return false;

                string path = NormalizePath(PathFor(image.transform));
                return path.IndexOf("MainMenuManager_TrialsOfFear", StringComparison.OrdinalIgnoreCase) >= 0 &&
                       path.IndexOf("MENU_DLCTitle", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private static Text FindMiniCreditsText()
        {
            Text exactPath = FindTextByPath(MiniCreditsPath);
            if (!object.ReferenceEquals(exactPath, null))
                return exactPath;

            Text fallback = null;
            Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (object.ReferenceEquals(text, null) || object.ReferenceEquals(text.transform, null))
                    continue;

                string path = NormalizePath(PathFor(text.transform));
                if (path.IndexOf("Mini credits", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return text;

                if (!string.IsNullOrEmpty(text.text) &&
                    text.text.IndexOf("Long Hat House", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    fallback = text;
            }

            return fallback;
        }

        private static Image FindImageByPath(string targetPath)
        {
            Image[] images = Resources.FindObjectsOfTypeAll<Image>();
            for (int i = 0; i < images.Length; i++)
            {
                Image image = images[i];
                if (object.ReferenceEquals(image, null) || object.ReferenceEquals(image.transform, null))
                    continue;

                string path = NormalizePath(PathFor(image.transform));
                if (path == targetPath)
                    return image;
            }

            return null;
        }

        private static Text FindTextByPath(string targetPath)
        {
            Text[] texts = Resources.FindObjectsOfTypeAll<Text>();
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (object.ReferenceEquals(text, null) || object.ReferenceEquals(text.transform, null))
                    continue;

                string path = NormalizePath(PathFor(text.transform));
                if (path == targetPath)
                    return text;
            }

            return null;
        }

        private static Sprite GetTitleSprite(Sprite sourceSprite)
        {
            if (!object.ReferenceEquals(TitleSprite, null))
                return TitleSprite;

            string path = GetTitleImagePath();
            if (!File.Exists(path))
            {
                if (!LoggedTitleLoadFailure)
                {
                    LoggedTitleLoadFailure = true;
                    MLLog.Warning("[MainMenuBranding] Missing DLC title replacement image: " + path);
                }

                return null;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.ARGB32, false);
                if (!ImageConversion.LoadImage(texture, bytes))
                {
                    MLLog.Warning("[MainMenuBranding] Unity could not load DLC title replacement image: " + path);
                    return null;
                }

                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                float pixelsPerUnit = object.ReferenceEquals(sourceSprite, null) ? 100.0f : sourceSprite.pixelsPerUnit;
                TitleSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f), pixelsPerUnit);
                return TitleSprite;
            }
            catch (Exception ex)
            {
                if (!LoggedTitleLoadFailure)
                {
                    LoggedTitleLoadFailure = true;
                    MLLog.Warning("[MainMenuBranding] Failed to load DLC title replacement image: " +
                                        ex.GetType().Name + ": " + ex.Message);
                }

                return null;
            }
        }

        private static void LogOriginalFrameInfo(Sprite sprite)
        {
            if (LoggedFrameInfo || object.ReferenceEquals(sprite, null))
                return;

            LoggedFrameInfo = true;
            Rect rect = sprite.rect;
            MLLog.Msg("[MainMenuBranding] Original DLC title frame: " + sprite.name +
                            " rect=" + rect.x + "," + rect.y + "," + rect.width + "," + rect.height +
                            " ppu=" + sprite.pixelsPerUnit);
        }

        private static string GetTitleImagePath()
        {
            string gameDirectory = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(Path.Combine(Path.Combine(gameDirectory, "UserData"), "ArchDandaraData"),
                Path.Combine("Images", TitleImageName));
        }

        private static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? "" : path.Replace("(Clone)", "");
        }

        private static string PathFor(Transform transform)
        {
            if (object.ReferenceEquals(transform, null))
                return "";

            string path = transform.name;
            Transform parent = transform.parent;
            while (!object.ReferenceEquals(parent, null))
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static void LogMissingOnce(ref bool logged, string message)
        {
            if (logged)
                return;

            logged = true;
            MLLog.Msg(message);
        }
    }
}
