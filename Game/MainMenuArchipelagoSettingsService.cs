/*
 * ArchDandara documentation
 * Purpose: Builds the in-game Arch Settings connection editor.
 * Why: Players need to edit AP connection info without leaving the game, including keyboard shortcuts and save/connect actions.
 * Notes: Text input code mirrors desktop shortcuts because Unity legacy input fields do not provide all expected behavior here.
 */

using System;
using ArchDandara.Archipelago;
using ArchDandara.Config;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArchDandara.Game
{
    public static class MainMenuArchipelagoSettingsService
    {
        private const string ButtonName = "ArchDandara_ArchSettingButton";
        private const string PanelName = "ArchDandara_ArchSettingPanel";

        private static Button SettingsButton;
        private static GameObject Panel;
        private static InputField ServerField;
        private static InputField PortField;
        private static InputField SlotField;
        private static InputField PasswordField;
        private static Toggle AutoConnectToggle;
        private static Button SaveConnectButton;
        private static Button CloseButton;
        private static InputField ActiveInput;
        private static bool ActiveInputAllSelected;
        private static float NextBackspaceRepeatTime;
        private static float NextScanTime;
        private static float NextLayoutTime;
        private static bool DumpedMenuLayout;

        public static void Update()
        {
            EnsureButton();
            ArrangeTitleMenuButtonsPeriodically();
            DumpMenuLayoutOnce();
            HandlePanelMouseFocus();
            HandlePanelButtonClicks();
            HandlePasteShortcut();
            HandleManualTextInput();
        }

        private static void EnsureButton()
        {
            if (IsValid(SettingsButton))
                return;

            if (Time.time < NextScanTime)
                return;

            NextScanTime = Time.time + 0.5f;

            Button existing = FindExistingSettingsButton();
            if (IsValid(existing))
            {
                HookSettingsButton(existing);
                SettingsButton = existing;
                return;
            }

            Button source = FindTitleMenuButton();
            if (object.ReferenceEquals(source, null))
                return;

            Button button = UnityEngine.Object.Instantiate(source, source.transform.parent);
            button.name = ButtonName;
            button.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
            SetButtonText(button, "ArchSetting");

            HookSettingsButton(button);
            StripInheritedMenuBehaviours(button);
            button.gameObject.SetActive(true);
            SettingsButton = button;
            ArrangeTitleMenuButtons();
            MLLog.Msg("[MainMenuBranding] Added ArchSetting main menu button.");
        }

        private static void HookSettingsButton(Button button)
        {
            if (object.ReferenceEquals(button, null))
                return;

            button.name = ButtonName;
            SetButtonText(button, "ArchSetting");
            StripInheritedMenuBehaviours(button);
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(ShowPanel);
        }

        private static void ShowPanel()
        {
            EnsurePanel();
            if (!IsValid(Panel))
                return;

            LoadCurrentConfigIntoFields();
            Panel.SetActive(true);
            Panel.transform.SetAsLastSibling();
            EnsureEventSystem();
            SelectInput(ServerField);
        }

        private static void HidePanel()
        {
            if (IsValid(Panel))
                Panel.SetActive(false);
        }

        private static void SavePanel()
        {
            SavePanel(false);
        }

        private static void SaveAndConnectPanel()
        {
            SavePanel(true);
        }

        private static void SavePanel(bool connect)
        {
            int port;
            if (!int.TryParse(PortField.text.Trim(), out port))
                port = 38281;

            APConfig.SaveAndReload(ServerField.text.Trim(), port, SlotField.text.Trim(), PasswordField.text,
                AutoConnectToggle.isOn);
            MLLog.Msg("[MainMenuBranding] Saved AP settings from main menu to " + APConfig.FilePath + ".");
            HidePanel();

            if (connect)
                APClient.Connect();
        }

        private static void LoadCurrentConfigIntoFields()
        {
            ServerField.text = APConfig.ServerAddress;
            PortField.text = APConfig.ServerPort.ToString();
            SlotField.text = APConfig.SlotName;
            PasswordField.text = APConfig.Password ?? "";
            AutoConnectToggle.isOn = false;
        }

        private static void EnsurePanel()
        {
            if (IsValid(Panel))
                return;

            ClearPanelReferences();

            Canvas canvas = FindMainMenuCanvas();
            if (object.ReferenceEquals(canvas, null))
                return;

            Panel = new GameObject(PanelName);
            Panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = Panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(210.0f, 142.0f);
            rect.anchoredPosition = new Vector2(0, 8);

            Image background = Panel.AddComponent<Image>();
            background.color = new Color(0.02f, 0.02f, 0.02f, 0.92f);
            background.raycastTarget = true;

            if (object.ReferenceEquals(canvas.GetComponent<GraphicRaycaster>(), null))
                canvas.gameObject.AddComponent<GraphicRaycaster>();

            CreateText(Panel.transform, "Title", "Archipelago Settings", 10, new Vector2(0, 56), TextAnchor.MiddleCenter,
                new Vector2(190, 14));
            ServerField = CreateLabeledInput(Panel.transform, "Server", new Vector2(0, 36), false);
            PortField = CreateLabeledInput(Panel.transform, "Port", new Vector2(0, 16), false);
            SlotField = CreateLabeledInput(Panel.transform, "Slot", new Vector2(0, -4), false);
            PasswordField = CreateLabeledInput(Panel.transform, "Password", new Vector2(0, -24), true);
            AutoConnectToggle = CreateToggle(Panel.transform, "AutoConnect", new Vector2(-75, -44));

            SaveConnectButton = CreateButton(Panel.transform, "Save & Connect", new Vector2(-47, -61), SaveAndConnectPanel);
            CloseButton = CreateButton(Panel.transform, "Close", new Vector2(47, -61), HidePanel);
            CreateText(Panel.transform, "Hint", "Ctrl+A/C/X/V supported", 5, new Vector2(0, -70),
                TextAnchor.MiddleCenter);

            Panel.SetActive(false);
            MLLog.Msg("[MainMenuBranding] Created ArchSetting edit panel.");
        }

        private static InputField CreateLabeledInput(Transform parent, string label, Vector2 position, bool password)
        {
            CreateText(parent, label + "Label", label, 7, new Vector2(-62, position.y), TextAnchor.MiddleRight,
                new Vector2(58, 12));

            GameObject inputObject = new GameObject(label + "Input");
            inputObject.transform.SetParent(parent, false);
            RectTransform rect = inputObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(112.0f, 14.0f);
            rect.anchoredPosition = new Vector2(30, position.y);

            Image image = inputObject.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0.92f);

            InputField input = inputObject.AddComponent<InputField>();
            input.targetGraphic = image;
            input.textComponent = CreateText(inputObject.transform, "Text", "", 7, Vector2.zero,
                TextAnchor.MiddleLeft, new Vector2(112, 14));
            input.textComponent.color = Color.black;
            input.textComponent.raycastTarget = false;
            StretchTextRect(input.textComponent.rectTransform);
            input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
            input.lineType = InputField.LineType.SingleLine;
            input.interactable = true;
            input.readOnly = true;
            input.onEndEdit.AddListener(delegate { input.text = SanitizeSingleLine(input.text); });
            return input;
        }

        private static Toggle CreateToggle(Transform parent, string label, Vector2 position)
        {
            GameObject toggleObject = new GameObject("AutoConnectToggle");
            toggleObject.transform.SetParent(parent, false);
            RectTransform rect = toggleObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.0f, 0.5f);
            rect.sizeDelta = new Vector2(160.0f, 12.0f);
            rect.anchoredPosition = position;

            GameObject box = new GameObject("Box");
            box.transform.SetParent(toggleObject.transform, false);
            RectTransform boxRect = box.AddComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0, 0.5f);
            boxRect.anchorMax = new Vector2(0, 0.5f);
            boxRect.sizeDelta = new Vector2(8, 8);
            boxRect.anchoredPosition = new Vector2(4, 0);
            Image boxImage = box.AddComponent<Image>();
            boxImage.color = Color.white;

            GameObject check = new GameObject("Check");
            check.transform.SetParent(box.transform, false);
            RectTransform checkRect = check.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(2, 2);
            checkRect.offsetMax = new Vector2(-2, -2);
            Image checkImage = check.AddComponent<Image>();
            checkImage.color = new Color(0.1f, 0.45f, 1.0f, 1.0f);

            Text text = CreateText(toggleObject.transform, "Label", label, 6, new Vector2(50, 0),
                TextAnchor.MiddleLeft, new Vector2(90, 12));
            text.raycastTarget = false;

            Toggle toggle = toggleObject.AddComponent<Toggle>();
            toggle.targetGraphic = boxImage;
            toggle.graphic = checkImage;
            return toggle;
        }

        private static Button CreateButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(text + "Button");
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(82.0f, 14.0f);
            rect.anchoredPosition = position;
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.12f, 0.25f, 0.45f, 1.0f);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(action);
            Text label = CreateText(buttonObject.transform, "Text", text, 6, Vector2.zero, TextAnchor.MiddleCenter,
                new Vector2(82, 14));
            label.raycastTarget = false;
            return button;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Vector2 position,
            TextAnchor anchor)
        {
            return CreateText(parent, name, value, size, position, anchor, new Vector2(560.0f, 32.0f));
        }

        private static Text CreateText(Transform parent, string name, string value, int size, Vector2 position,
            TextAnchor anchor, Vector2 sizeDelta)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;

            Text text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = value;
            return text;
        }

        private static void HandlePasteShortcut()
        {
            if (!IsPanelOpen())
                return;

            if (!(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                return;

            InputField input = ActiveInput;
            if (object.ReferenceEquals(input, null) && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != null)
                input = EventSystem.current.currentSelectedGameObject.GetComponent<InputField>();

            if (object.ReferenceEquals(input, null))
                input = ServerField;

            if (Input.GetKeyDown(KeyCode.A))
            {
                SelectAllInput(input);
                return;
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                GUIUtility.systemCopyBuffer = SelectedTextOrAll(input);
                SelectInput(input);
                return;
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                GUIUtility.systemCopyBuffer = SelectedTextOrAll(input);
                if (HasAnySelection(input))
                    input.text = "";
                SelectInput(input);
                ActiveInputAllSelected = false;
                return;
            }

            if (!Input.GetKeyDown(KeyCode.V))
                return;

            string clipboard = SanitizeSingleLine(GUIUtility.systemCopyBuffer ?? "");
            bool replaceText = HasAnySelection(input);
            if (replaceText)
                input.text = clipboard;
            else
                input.text += clipboard;
            input.text = SanitizeSingleLine(input.text);
            input.MoveTextEnd(false);
            ActiveInputAllSelected = false;
            FocusInputWithoutClearingSelection(input);
        }

        private static void HandlePanelMouseFocus()
        {
            if (!IsPanelOpen())
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            InputField clicked = FieldAtMouse();
            if (!object.ReferenceEquals(clicked, null))
                SelectInput(clicked);
        }

        private static void HandlePanelButtonClicks()
        {
            if (!IsPanelOpen())
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            if (IsButtonAtMouse(SaveConnectButton))
            {
                SaveAndConnectPanel();
                return;
            }

            if (IsButtonAtMouse(CloseButton))
                HidePanel();
        }

        private static bool IsButtonAtMouse(Button button)
        {
            if (!IsValid(button))
                return false;

            RectTransform rect = button.GetComponent<RectTransform>();
            return !object.ReferenceEquals(rect, null) &&
                   RectTransformUtility.RectangleContainsScreenPoint(rect, Input.mousePosition, null);
        }

        private static void HandleManualTextInput()
        {
            if (!IsPanelOpen() || object.ReferenceEquals(ActiveInput, null))
                return;

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                SelectInput(NextInput(ActiveInput));
                return;
            }

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SaveAndConnectPanel();
                return;
            }

            if (ShouldDeleteBackspace() && ActiveInput.text.Length > 0)
            {
                if (HasAnySelection(ActiveInput))
                    ActiveInput.text = "";
                else
                    ActiveInput.text = ActiveInput.text.Substring(0, ActiveInput.text.Length - 1);
                ActiveInput.MoveTextEnd(false);
                ActiveInputAllSelected = false;
            }

            string typed = Input.inputString;
            if (string.IsNullOrEmpty(typed))
                return;

            for (int i = 0; i < typed.Length; i++)
            {
                char c = typed[i];
                if (c == '\b' || c == '\n' || c == '\r' || c == '\t')
                    continue;

                if (HasAnySelection(ActiveInput))
                {
                    ActiveInput.text = "";
                    ActiveInputAllSelected = false;
                }

                ActiveInput.text += c;
            }

            ActiveInput.text = SanitizeSingleLine(ActiveInput.text);
            ActiveInput.MoveTextEnd(false);
        }

        private static bool ShouldDeleteBackspace()
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                NextBackspaceRepeatTime = Time.realtimeSinceStartup + 0.35f;
                return true;
            }

            if (!Input.GetKey(KeyCode.Backspace))
                return false;

            if (Time.realtimeSinceStartup < NextBackspaceRepeatTime)
                return false;

            NextBackspaceRepeatTime = Time.realtimeSinceStartup + 0.045f;
            return true;
        }

        private static InputField FieldAtMouse()
        {
            Vector2 mouse = Input.mousePosition;
            InputField[] fields = {ServerField, PortField, SlotField, PasswordField};
            for (int i = 0; i < fields.Length; i++)
            {
                InputField field = fields[i];
                if (object.ReferenceEquals(field, null))
                    continue;

                RectTransform rect = field.GetComponent<RectTransform>();
                if (!object.ReferenceEquals(rect, null) &&
                    RectTransformUtility.RectangleContainsScreenPoint(rect, mouse, null))
                    return field;
            }

            return null;
        }

        private static void SelectInput(InputField input)
        {
            if (object.ReferenceEquals(input, null))
                return;

            ActiveInput = input;
            ActiveInputAllSelected = false;
            FocusInputWithoutClearingSelection(input);
        }

        private static void FocusInputWithoutClearingSelection(InputField input)
        {
            if (object.ReferenceEquals(input, null))
                return;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(input.gameObject);

            input.ActivateInputField();
            input.MoveTextEnd(false);
        }

        private static void SelectAllInput(InputField input)
        {
            if (object.ReferenceEquals(input, null))
                return;

            SelectInput(input);
            ActiveInputAllSelected = true;
            TrySetInputSelection(input, 0, (input.text ?? "").Length);
        }

        private static bool HasFullSelection(InputField input)
        {
            if (object.ReferenceEquals(input, null) || ActiveInput != input)
                return false;

            if (ActiveInputAllSelected)
                return true;

            int length = (input.text ?? "").Length;
            if (length == 0)
                return false;

            return SelectionStart(input) == 0 && SelectionEnd(input) == length;
        }

        private static bool HasAnySelection(InputField input)
        {
            if (HasFullSelection(input))
                return true;

            if (object.ReferenceEquals(input, null) || ActiveInput != input)
                return false;

            return SelectionStart(input) != SelectionEnd(input);
        }

        private static string SelectedTextOrAll(InputField input)
        {
            if (object.ReferenceEquals(input, null))
                return "";

            string text = input.text ?? "";
            int start = SelectionStart(input);
            int end = SelectionEnd(input);
            if (end > start && start >= 0 && end <= text.Length)
                return text.Substring(start, end - start);

            return text;
        }

        private static int SelectionStart(InputField input)
        {
            try
            {
                return Math.Min(input.selectionAnchorPosition, input.selectionFocusPosition);
            }
            catch
            {
                return 0;
            }
        }

        private static int SelectionEnd(InputField input)
        {
            try
            {
                return Math.Max(input.selectionAnchorPosition, input.selectionFocusPosition);
            }
            catch
            {
                return 0;
            }
        }

        private static void TrySetInputSelection(InputField input, int start, int end)
        {
            try
            {
                input.selectionAnchorPosition = start;
                input.selectionFocusPosition = end;
            }
            catch
            {
                // Older Unity builds can ignore explicit selection positions; the mod tracks the selection state too.
            }
        }

        private static InputField NextInput(InputField current)
        {
            if (current == ServerField)
                return PortField;
            if (current == PortField)
                return SlotField;
            if (current == SlotField)
                return PasswordField;
            return ServerField;
        }

        private static string SanitizeSingleLine(string value)
        {
            return (value ?? "").Replace("\r", "").Replace("\n", "").Replace("\t", "");
        }

        private static void StretchTextRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(8, 0);
            rect.offsetMax = new Vector2(-8, 0);
        }

        private static void EnsureEventSystem()
        {
            if (!object.ReferenceEquals(EventSystem.current, null))
                return;

            GameObject eventSystemObject = new GameObject("ArchDandara_EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        private static void DumpMenuLayoutOnce()
        {
            if (DumpedMenuLayout || !IsValid(SettingsButton))
                return;

            DumpedMenuLayout = true;
            DumpMenuLayout();
        }

        private static void ArrangeTitleMenuButtonsPeriodically()
        {
            if (Time.time < NextLayoutTime)
                return;

            NextLayoutTime = Time.time + 0.5f;
            ArrangeTitleMenuButtons();
        }

        private static void ArrangeTitleMenuButtons()
        {
            Button start = null;
            Button credits = null;
            Button option = null;
            Button quit = null;
            Button arch = IsValid(SettingsButton) ? SettingsButton : FindExistingSettingsButton();
            Transform buttonsParent = null;

            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.transform, null))
                    continue;

                string path = PathFor(button.transform);
                if (path.IndexOf("MainMenuManager_TrialsOfFear", StringComparison.OrdinalIgnoreCase) < 0 ||
                    path.IndexOf("Title Menu", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                string text = GetButtonText(button);
                if (button.name == ButtonName || text.IndexOf("archsetting", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    arch = button;
                    buttonsParent = button.transform.parent;
                }
                else if (text.IndexOf("startgame", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    start = button;
                    buttonsParent = button.transform.parent;
                }
                else if (text.IndexOf("credit", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    credits = button;
                }
                else if (text.IndexOf("option", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    option = button;
                    buttonsParent = button.transform.parent;
                }
                else if (text.IndexOf("quit", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         text.IndexOf("exit", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    quit = button;
                }
            }

            if (object.ReferenceEquals(buttonsParent, null))
                buttonsParent = FindTitleButtonsParent();

            if (object.ReferenceEquals(buttonsParent, null))
                return;

            RectTransform parentRect = buttonsParent.GetComponent<RectTransform>();
            if (!object.ReferenceEquals(parentRect, null))
                parentRect.sizeDelta = new Vector2(202.0f, 68.0f);

            MoveToButtonsParent(start, buttonsParent);
            MoveToButtonsParent(credits, buttonsParent);
            MoveToButtonsParent(arch, buttonsParent);
            MoveToButtonsParent(quit, buttonsParent);
            MoveToButtonsParent(option, buttonsParent);

            PositionTitleButton(start, 0, 0);
            PositionTitleButton(credits, 1, 0);
            PositionTitleButton(arch, 0, 1);
            PositionTitleButton(quit, 1, 1);
            PositionTitleButton(option, 0, 2);

            if (IsValid(arch))
            {
                HookSettingsButton(arch);
                StripInheritedMenuBehaviours(arch);
            }

            if (IsValid(option))
                SetButtonText(option, "Option");
            SetSiblingOrder(start, credits, arch, quit, option);
        }

        private static void StripInheritedMenuBehaviours(Button button)
        {
            if (!IsValid(button))
                return;

            MonoBehaviour[] behaviours = button.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (object.ReferenceEquals(behaviour, null) || behaviour == button)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (typeName.IndexOf("ChangeMenuButton", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("ButtonProxy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    typeName.IndexOf("ButtonAttribute", StringComparison.OrdinalIgnoreCase) >= 0)
                    UnityEngine.Object.Destroy(behaviour);
            }
        }

        private static void MoveToButtonsParent(Button button, Transform buttonsParent)
        {
            if (!IsValid(button) || object.ReferenceEquals(buttonsParent, null))
                return;

            if (button.transform.parent != buttonsParent)
                button.transform.SetParent(buttonsParent, false);
        }

        private static void PositionTitleButton(Button button, int column, int row)
        {
            if (!IsValid(button))
                return;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (object.ReferenceEquals(rect, null))
                return;

            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(1, 0);
            rect.sizeDelta = new Vector2(92.0f, 17.0f);
            rect.anchoredPosition = new Vector2(column == 0 ? 92.0f : 202.0f, -21.0f - row * 18.0f);
            rect.localScale = Vector3.one;
        }

        private static void SetSiblingOrder(params Button[] buttons)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (IsValid(button))
                    button.transform.SetSiblingIndex(i);
            }
        }

        private static void DumpMenuLayout()
        {
            MLLog.Msg("[MainMenuBranding][MenuLayout] Begin Trials of Fear menu button RectTransform dump.");
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.transform, null))
                    continue;

                string path = PathFor(button.transform);
                if (path.IndexOf("MainMenuManager_TrialsOfFear", StringComparison.OrdinalIgnoreCase) < 0 ||
                    path.IndexOf("Title Menu", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                RectTransform rect = button.GetComponent<RectTransform>();
                RectTransform parentRect = button.transform.parent == null
                    ? null
                    : button.transform.parent.GetComponent<RectTransform>();

                MLLog.Msg("[MainMenuBranding][MenuLayout] Button text=\"" + GetRawButtonText(button) +
                          "\" name=\"" + button.name +
                          "\" path=\"" + path +
                          "\" parent=\"" + PathFor(button.transform.parent) +
                          "\" siblingIndex=" + button.transform.GetSiblingIndex() +
                          "\" activeSelf=" + button.gameObject.activeSelf +
                          "\" activeInHierarchy=" + button.gameObject.activeInHierarchy +
                          "\" rect=" + FormatRect(rect) +
                          "\" parentRect=" + FormatRect(parentRect) +
                          "\" localPosition=" + FormatVector3(button.transform.localPosition) +
                          "\" localScale=" + FormatVector3(button.transform.localScale) + "\"");
            }

            MLLog.Msg("[MainMenuBranding][MenuLayout] End Trials of Fear menu button RectTransform dump.");
        }

        private static Button FindExistingSettingsButton()
        {
            Button found = null;
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.transform, null))
                    continue;

                if (button.name == ButtonName)
                {
                    if (object.ReferenceEquals(found, null))
                        found = button;
                    else
                        UnityEngine.Object.Destroy(button.gameObject);
                }
            }

            return found;
        }

        private static Button FindTitleMenuButton()
        {
            Button fallback = null;
            Button option = null;
            Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (object.ReferenceEquals(button, null) || object.ReferenceEquals(button.transform, null))
                    continue;

                string path = PathFor(button.transform);
                if (path.IndexOf("MainMenuManager_TrialsOfFear", StringComparison.OrdinalIgnoreCase) < 0 ||
                    path.IndexOf("Title Menu", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                fallback = button;
                Text text = button.GetComponentInChildren<Text>(true);
                if (object.ReferenceEquals(text, null))
                    continue;

                string normalizedText = text.text.Replace(" ", "").Trim();
                if (normalizedText.IndexOf("StartGame", StringComparison.OrdinalIgnoreCase) >= 0)
                    return button;

                if (normalizedText.IndexOf("Options", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    normalizedText.IndexOf("Option", StringComparison.OrdinalIgnoreCase) >= 0)
                    option = button;
            }

            return !object.ReferenceEquals(option, null) ? option : fallback;
        }

        private static Transform FindTitleButtonsParent()
        {
            Button source = FindTitleMenuButton();
            return object.ReferenceEquals(source, null) ? null : source.transform.parent;
        }

        private static string GetButtonText(Button button)
        {
            return GetRawButtonText(button).Replace(" ", "").Trim();
        }

        private static string GetRawButtonText(Button button)
        {
            Text text = button.GetComponentInChildren<Text>(true);
            return object.ReferenceEquals(text, null) || string.IsNullOrEmpty(text.text) ? "" : text.text.Trim();
        }

        private static Canvas FindMainMenuCanvas()
        {
            Canvas[] canvases = Resources.FindObjectsOfTypeAll<Canvas>();
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (object.ReferenceEquals(canvas, null) || object.ReferenceEquals(canvas.transform, null))
                    continue;

                string path = PathFor(canvas.transform);
                if (path.IndexOf("MainMenuManager_TrialsOfFear", StringComparison.OrdinalIgnoreCase) >= 0)
                    return canvas;
            }

            return object.ReferenceEquals(SettingsButton, null) ? null : SettingsButton.GetComponentInParent<Canvas>();
        }

        private static void SetButtonText(Button button, string value)
        {
            Text[] texts = button.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
                texts[i].text = value;
        }

        private static bool IsValid(Button button)
        {
            try
            {
                return !object.ReferenceEquals(button, null) && button != null &&
                       !object.ReferenceEquals(button.gameObject, null) && button.gameObject != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValid(GameObject gameObject)
        {
            try
            {
                return !object.ReferenceEquals(gameObject, null) && gameObject != null;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsPanelOpen()
        {
            try
            {
                if (!IsValid(Panel))
                {
                    ClearPanelReferences();
                    return false;
                }

                return Panel.activeSelf;
            }
            catch
            {
                ClearPanelReferences();
                return false;
            }
        }

        private static void ClearPanelReferences()
        {
            Panel = null;
            ServerField = null;
            PortField = null;
            SlotField = null;
            PasswordField = null;
            AutoConnectToggle = null;
            SaveConnectButton = null;
            CloseButton = null;
            ActiveInput = null;
            ActiveInputAllSelected = false;
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

        private static string FormatRect(RectTransform rect)
        {
            if (object.ReferenceEquals(rect, null))
                return "<null>";

            return "anchorMin=" + FormatVector2(rect.anchorMin) +
                   " anchorMax=" + FormatVector2(rect.anchorMax) +
                   " pivot=" + FormatVector2(rect.pivot) +
                   " anchoredPosition=" + FormatVector2(rect.anchoredPosition) +
                   " sizeDelta=" + FormatVector2(rect.sizeDelta);
        }

        private static string FormatVector2(Vector2 value)
        {
            return "(" + value.x.ToString("0.###") + ", " + value.y.ToString("0.###") + ")";
        }

        private static string FormatVector3(Vector3 value)
        {
            return "(" + value.x.ToString("0.###") + ", " + value.y.ToString("0.###") + ", " +
                   value.z.ToString("0.###") + ")";
        }
    }
}
