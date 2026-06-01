/*
 * ArchDandara documentation
 * Purpose: Implements the custom return-to-Great-Ruins menu action.
 * Why: The AP mod adds routing without using the vanilla death or respawn money-loss path.
 * Notes: This service should avoid death routines because AP players use it as travel, not as a penalty.
 */

using UnityEngine;
using UnityEngine.UI;

namespace ArchDandara.Game
{
    public static class GreatRuinsReturnService
    {
        private const string GreatRuinsRoomId = "A1_GreatRuins";
        private const string ButtonObjectName = "ArchDandara_ReturnToGreatRuins";
        private const string ButtonText = "Return to GreatRuins";
        private static bool LoggedMissingReturnButton;
        private static bool LoggedButtonDump;
        private static float NextRetryTime;
        private static int HookedButtonId;

        public static void EnsurePauseMenuButton()
        {
            Button existingButton = FindExistingButton();
            if (!object.ReferenceEquals(existingButton, null))
            {
                RemoveDuplicateButtons(existingButton);
                HookButton(existingButton);
                return;
            }

            if (Time.unscaledTime < NextRetryTime)
                return;

            NextRetryTime = Time.unscaledTime + 0.5f;
            Button returnToFlagButton = FindReturnToFlagButton();
            if (object.ReferenceEquals(returnToFlagButton, null))
            {
                if (!LoggedMissingReturnButton)
                {
                    LoggedMissingReturnButton = true;
                    MLLog.Warning("[GreatRuinsReturn] Could not find Return To Flag button to clone.");
                }

                DumpButtonCandidates();
                return;
            }

            Button button = Object.Instantiate(returnToFlagButton, returnToFlagButton.transform.parent);
            button.name = ButtonObjectName;
            button.transform.SetSiblingIndex(returnToFlagButton.transform.GetSiblingIndex() + 1);
            HookButton(button);

            button.gameObject.SetActive(true);
            MLLog.Msg("[GreatRuinsReturn] Added pause menu button under " + PathFor(returnToFlagButton.transform) + ".");
        }

        public static void ReturnToGreatRuins()
        {
            GameManager gameManager = PersistentSingleton<GameManager>.instance;
            PlayerController player = GameAccess.Player;
            if (object.ReferenceEquals(gameManager, null) || object.ReferenceEquals(player, null))
            {
                MLLog.Warning("[GreatRuinsReturn] Cannot return to Great Ruins, game/player missing.");
                return;
            }

            int money = player.GetCurrentMoney();
            if (gameManager.IsPaused())
                gameManager.Unpause();

            bool changed = gameManager.ChangeRoom(GreatRuinsRoomId, SpawnID.Camp,
                ScreenTransitionManager.TransitionStyle.GameOver, true, delegate
                {
                    PlayerController currentPlayer = GameAccess.Player;
                    if (!object.ReferenceEquals(currentPlayer, null))
                        currentPlayer.SetMoney(money);
                });

            if (changed)
                MLLog.Msg("[GreatRuinsReturn] Returning player to " + GreatRuinsRoomId + " at Camp spawn.");
            else
                MLLog.Warning("[GreatRuinsReturn] ChangeRoom failed for " + GreatRuinsRoomId + ".");
        }

        private static Button FindReturnToFlagButton()
        {
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            Button returnToFlagByText = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.gameObject, null))
                    continue;

                if (button.name == ButtonObjectName)
                    continue;

                string path = PathFor(button.transform).ToLowerInvariant();
                if (HasPersistentClickMethod(button, "ReturnToHoistedFlag") ||
                    HasPersistentClickMethod(button, "OnPlayerReturnToHoistedFlag"))
                    return button;

                Text text = button.GetComponentInChildren<Text>(true);
                string buttonText = object.ReferenceEquals(text, null) || string.IsNullOrEmpty(text.text)
                    ? ""
                    : text.text;

                string normalized = buttonText.ToLowerInvariant();
                if (normalized.IndexOf("return") >= 0 && normalized.IndexOf("flag") >= 0)
                {
                    if (path.IndexOf("gameplay") >= 0)
                        return button;

                    if (object.ReferenceEquals(returnToFlagByText, null))
                        returnToFlagByText = button;
                }
            }

            return returnToFlagByText;
        }

        private static Button FindExistingButton()
        {
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            Button fallback = null;
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.gameObject, null))
                    continue;

                if (button.name != ButtonObjectName)
                    continue;

                if (!button.gameObject.activeInHierarchy)
                    return button;

                if (object.ReferenceEquals(fallback, null))
                    fallback = button;
            }

            return fallback;
        }

        private static void RemoveDuplicateButtons(Button keep)
        {
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.gameObject, null))
                    continue;

                if (button.name != ButtonObjectName)
                    continue;

                if (object.ReferenceEquals(button, keep))
                    continue;

                MLLog.Msg("[GreatRuinsReturn] Removed duplicate button at " + PathFor(button.transform) + ".");
                Object.Destroy(button.gameObject);
            }
        }

        private static void HookButton(Button button)
        {
            if (object.ReferenceEquals(button, null))
                return;

            if (HookedButtonId == button.GetInstanceID() && IsButtonTextSet(button))
                return;

            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(ReturnToGreatRuins);
            SetButtonText(button);
            HookedButtonId = button.GetInstanceID();
        }

        private static void SetButtonText(Button button)
        {
            Text[] texts = button.GetComponentsInChildren<Text>(true);
            int changed = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (object.ReferenceEquals(text, null))
                    continue;

                text.text = ButtonText;
                changed++;
                MLLog.Msg("[GreatRuinsReturn] Set button text at " + PathFor(text.transform) + ".");
            }

            if (changed == 0)
                MLLog.Warning("[GreatRuinsReturn] No UnityEngine.UI.Text found under " + PathFor(button.transform) + ".");
        }

        private static bool IsButtonTextSet(Button button)
        {
            Text[] texts = button.GetComponentsInChildren<Text>(true);
            if (texts.Length == 0)
                return false;

            for (int i = 0; i < texts.Length; i++)
            {
                Text text = texts[i];
                if (object.ReferenceEquals(text, null))
                    continue;

                if (text.text != ButtonText)
                    return false;
            }

            return true;
        }

        private static bool HasPersistentClickMethod(Button button, string methodName)
        {
            if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.onClick, null))
                return false;

            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                string persistentMethod = button.onClick.GetPersistentMethodName(i);
                if (persistentMethod == methodName)
                    return true;
            }

            return false;
        }

        private static void DumpButtonCandidates()
        {
            if (LoggedButtonDump)
                return;

            LoggedButtonDump = true;
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            int logged = 0;
            for (int i = 0; i < buttons.Length && logged < 80; i++)
            {
                Button button = buttons[i];
                if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.gameObject, null))
                    continue;

                Text text = button.GetComponentInChildren<Text>(true);
                string buttonText = object.ReferenceEquals(text, null) ? "" : text.text;
                if (string.IsNullOrEmpty(buttonText) && button.onClick.GetPersistentEventCount() == 0)
                    continue;

                MLLog.Msg("[GreatRuinsReturn][ButtonCandidate] path=" + PathFor(button.transform) +
                                " | active=" + button.gameObject.activeInHierarchy +
                                " | text=" + buttonText +
                                " | methods=" + PersistentMethods(button));
                logged++;
            }
        }

        private static string PersistentMethods(Button button)
        {
            if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.onClick, null))
                return "";

            string result = "";
            int count = button.onClick.GetPersistentEventCount();
            for (int i = 0; i < count; i++)
            {
                if (result.Length > 0)
                    result += ",";
                result += button.onClick.GetPersistentMethodName(i);
            }

            return result;
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
    }
}
