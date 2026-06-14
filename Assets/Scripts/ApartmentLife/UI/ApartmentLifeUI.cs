using System;
using System.Collections.Generic;
using ApartmentLife.Data;
using UnityEngine;
using UnityEngine.UI;

namespace ApartmentLife.UI
{
    // uGUIをコードで作るためのクラスです。SceneにCanvasを手で置かなくても動きます。
    public class ApartmentLifeUI
    {
        private Text dayText;
        private Text logText;
        private Text detailText;
        private Text playerProfileText;
        private GameObject detailPanel;
        private GameObject playerEditorPanel;
        private InputField playerNameInput;
        private InputField playerPersonalityInput;
        private InputField playerHobbyInput;
        private InputField playerCatchphraseInput;
        private Dropdown playerBodyColorDropdown;
        private Dropdown playerClothesColorDropdown;
        private Action<PlayerData> onPlayerChanged;

        private readonly string[] colorOptions = { "青", "赤", "緑", "黄", "紫", "白" };

        public void Build(Action onAdvanceTime, Action onSave, Action onLoad, Action<PlayerData> onPlayerChanged, PlayerData player)
        {
            this.onPlayerChanged = onPlayerChanged;
            Canvas canvas = CreateCanvas();
            CreateEventSystemIfNeeded();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject topPanel = CreatePanel(canvas.transform, "Top Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(640f, 58f), new Color(0.05f, 0.07f, 0.09f, 0.72f));
            dayText = CreateText(topPanel.transform, "Day Text", "Day 1", defaultFont, 20, TextAnchor.MiddleLeft, new Vector2(16f, -12f), new Vector2(100f, 34f));
            CreateButton(topPanel.transform, "Advance Time Button", "時間を進める", defaultFont, new Vector2(120f, -12f), new Vector2(150f, 34f), onAdvanceTime);
            CreateButton(topPanel.transform, "Save Button", "セーブ", defaultFont, new Vector2(282f, -12f), new Vector2(88f, 34f), onSave);
            CreateButton(topPanel.transform, "Load Button", "ロード", defaultFont, new Vector2(382f, -12f), new Vector2(88f, 34f), onLoad);
            CreateButton(topPanel.transform, "Player Edit Button", "主人公編集", defaultFont, new Vector2(482f, -12f), new Vector2(126f, 34f), () => OpenPlayerEditor(player));

            GameObject logPanel = CreatePanel(canvas.transform, "Event Log Panel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -18f), new Vector2(360f, 430f), new Color(0.05f, 0.07f, 0.09f, 0.68f));
            CreateText(logPanel.transform, "Log Header", "最近の出来事", defaultFont, 20, TextAnchor.MiddleLeft, new Vector2(16f, -14f), new Vector2(220f, 30f));
            logText = CreateText(logPanel.transform, "Log Text", "", defaultFont, 16, TextAnchor.UpperLeft, new Vector2(16f, -54f), new Vector2(328f, 352f));

            detailPanel = CreatePanel(canvas.transform, "Resident Detail Panel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, -18f), new Vector2(340f, 430f), new Color(0.05f, 0.07f, 0.09f, 0.68f));
            CreateText(detailPanel.transform, "Detail Header", "住人詳細", defaultFont, 20, TextAnchor.MiddleLeft, new Vector2(16f, -14f), new Vector2(220f, 30f));
            detailText = CreateText(detailPanel.transform, "Detail Text", "住人をクリックすると、ここに性格・趣味・関係性が表示されます。", defaultFont, 16, TextAnchor.UpperLeft, new Vector2(16f, -54f), new Vector2(308f, 352f));

            GameObject playerProfilePanel = CreatePanel(canvas.transform, "Player Profile Panel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(390f, 120f), new Color(0.05f, 0.07f, 0.09f, 0.66f));
            CreateText(playerProfilePanel.transform, "Player Profile Header", "あなたのプロフィール", defaultFont, 18, TextAnchor.MiddleLeft, new Vector2(16f, -10f), new Vector2(240f, 26f));
            playerProfileText = CreateText(playerProfilePanel.transform, "Player Profile Text", "", defaultFont, 15, TextAnchor.UpperLeft, new Vector2(16f, -42f), new Vector2(358f, 66f));

            CreatePlayerEditor(canvas.transform, defaultFont, player);
            SetPlayerProfile(player);
        }

        public void SetDay(int day)
        {
            dayText.text = $"Day {day}";
        }

        public void SetLogs(IReadOnlyList<string> logs)
        {
            int start = Mathf.Max(0, logs.Count - 4);
            List<string> visibleLogs = new List<string>();
            for (int i = start; i < logs.Count; i++)
            {
                visibleLogs.Add(logs[i]);
            }

            logText.text = string.Join(Environment.NewLine, visibleLogs);
        }

        public void ShowResident(ResidentData resident, IReadOnlyList<string> relationships)
        {
            detailPanel.SetActive(true);
            detailText.text =
                $"名前: {resident.name}\n" +
                $"ID: {resident.id}\n\n" +
                $"性格: {resident.personality}\n" +
                $"趣味: {resident.hobby}\n" +
                $"口ぐせ: {resident.catchphrase}\n\n" +
                $"気分: {resident.mood} / 100\n" +
                $"元気: {resident.energy} / 100\n\n" +
                "関係性\n" +
                string.Join("\n", relationships);
        }

        public void SetPlayerProfile(PlayerData player)
        {
            if (playerProfileText == null || player == null)
            {
                return;
            }

            playerProfileText.text =
                $"名前: {player.name} / 性格: {player.personality}\n" +
                $"趣味: {player.hobby} / 口ぐせ: {player.catchphrase}\n" +
                $"体: {player.bodyColor} / 服: {player.clothesColor}";
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Apartment Life UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private void CreateEventSystemIfNeeded()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        private void CreatePlayerEditor(Transform parent, Font font, PlayerData player)
        {
            playerEditorPanel = CreatePanel(parent, "Player Editor Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(430f, 430f), new Color(0.04f, 0.05f, 0.07f, 0.9f));
            CreateText(playerEditorPanel.transform, "Player Editor Header", "主人公キャラクリエイト", font, 20, TextAnchor.MiddleLeft, new Vector2(18f, -14f), new Vector2(280f, 30f));

            playerNameInput = CreateInputField(playerEditorPanel.transform, "Name Input", "名前", player.name, font, new Vector2(18f, -58f), new Vector2(190f, 34f));
            playerPersonalityInput = CreateInputField(playerEditorPanel.transform, "Personality Input", "性格", player.personality, font, new Vector2(222f, -58f), new Vector2(190f, 34f));
            playerHobbyInput = CreateInputField(playerEditorPanel.transform, "Hobby Input", "趣味", player.hobby, font, new Vector2(18f, -126f), new Vector2(190f, 34f));
            playerCatchphraseInput = CreateInputField(playerEditorPanel.transform, "Catchphrase Input", "口ぐせ", player.catchphrase, font, new Vector2(222f, -126f), new Vector2(190f, 34f));

            CreateText(playerEditorPanel.transform, "Body Color Label", "体の色", font, 15, TextAnchor.MiddleLeft, new Vector2(18f, -194f), new Vector2(100f, 24f));
            playerBodyColorDropdown = CreateDropdown(playerEditorPanel.transform, "Body Color Dropdown", font, new Vector2(18f, -222f), new Vector2(190f, 34f), player.bodyColor);
            CreateText(playerEditorPanel.transform, "Clothes Color Label", "服の色", font, 15, TextAnchor.MiddleLeft, new Vector2(222f, -194f), new Vector2(100f, 24f));
            playerClothesColorDropdown = CreateDropdown(playerEditorPanel.transform, "Clothes Color Dropdown", font, new Vector2(222f, -222f), new Vector2(190f, 34f), player.clothesColor);

            CreateText(playerEditorPanel.transform, "Player Editor Note", "ここで作った主人公は、住人イベントとは別の「あなた」として保存されます。", font, 14, TextAnchor.UpperLeft, new Vector2(18f, -280f), new Vector2(394f, 52f));
            CreateButton(playerEditorPanel.transform, "Apply Player Button", "反映する", font, new Vector2(110f, -356f), new Vector2(100f, 36f), () => ApplyPlayerEditor(player));
            CreateButton(playerEditorPanel.transform, "Close Player Button", "閉じる", font, new Vector2(222f, -356f), new Vector2(100f, 36f), () => playerEditorPanel.SetActive(false));

            playerEditorPanel.SetActive(false);
        }

        private void OpenPlayerEditor(PlayerData player)
        {
            if (playerEditorPanel == null || player == null)
            {
                return;
            }

            // 開くたびに現在の主人公データを入力欄へ戻します。
            playerNameInput.text = player.name;
            playerPersonalityInput.text = player.personality;
            playerHobbyInput.text = player.hobby;
            playerCatchphraseInput.text = player.catchphrase;
            playerBodyColorDropdown.value = GetColorIndex(player.bodyColor);
            playerClothesColorDropdown.value = GetColorIndex(player.clothesColor);
            playerEditorPanel.SetActive(true);
        }

        private void ApplyPlayerEditor(PlayerData player)
        {
            if (player == null)
            {
                return;
            }

            player.name = string.IsNullOrWhiteSpace(playerNameInput.text) ? "あなた" : playerNameInput.text;
            player.personality = string.IsNullOrWhiteSpace(playerPersonalityInput.text) ? "穏やか" : playerPersonalityInput.text;
            player.hobby = string.IsNullOrWhiteSpace(playerHobbyInput.text) ? "散歩" : playerHobbyInput.text;
            player.catchphrase = string.IsNullOrWhiteSpace(playerCatchphraseInput.text) ? "よろしく。" : playerCatchphraseInput.text;
            player.bodyColor = colorOptions[playerBodyColorDropdown.value];
            player.clothesColor = colorOptions[playerClothesColorDropdown.value];
            player.isPlayer = true;

            SetPlayerProfile(player);
            onPlayerChanged?.Invoke(player);
            playerEditorPanel.SetActive(false);
        }

        private GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = color;

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMin.x, anchorMax.y);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return panel;
        }

        private Text CreateText(Transform parent, string name, string text, Font font, int fontSize, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text uiText = textObject.AddComponent<Text>();
            uiText.text = text;
            uiText.font = font;
            uiText.fontSize = fontSize;
            uiText.lineSpacing = 1.18f;
            uiText.color = Color.white;
            uiText.alignment = alignment;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return uiText;
        }

        private InputField CreateInputField(Transform parent, string name, string label, string value, Font font, Vector2 anchoredPosition, Vector2 size)
        {
            CreateText(parent, $"{name} Label", label, font, 15, TextAnchor.MiddleLeft, anchoredPosition + new Vector2(0f, 24f), new Vector2(size.x, 22f));

            GameObject inputObject = CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size, new Color(1f, 1f, 1f, 0.92f));
            InputField input = inputObject.AddComponent<InputField>();

            Text text = CreateText(inputObject.transform, "Text", value, font, 15, TextAnchor.MiddleLeft, new Vector2(8f, -5f), new Vector2(size.x - 16f, size.y - 8f));
            text.color = Color.black;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            Text placeholder = CreateText(inputObject.transform, "Placeholder", label, font, 15, TextAnchor.MiddleLeft, new Vector2(8f, -5f), new Vector2(size.x - 16f, size.y - 8f));
            placeholder.color = new Color(0.35f, 0.35f, 0.35f, 0.7f);

            input.textComponent = text;
            input.placeholder = placeholder;
            input.text = value;
            return input;
        }

        private Dropdown CreateDropdown(Transform parent, string name, Font font, Vector2 anchoredPosition, Vector2 size, string selectedColor)
        {
            GameObject dropdownObject = CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size, new Color(1f, 1f, 1f, 0.92f));
            Dropdown dropdown = dropdownObject.AddComponent<Dropdown>();
            dropdown.options.Clear();
            foreach (string option in colorOptions)
            {
                dropdown.options.Add(new Dropdown.OptionData(option));
            }

            Text label = CreateText(dropdownObject.transform, "Label", selectedColor, font, 15, TextAnchor.MiddleLeft, new Vector2(8f, -5f), new Vector2(size.x - 16f, size.y - 8f));
            label.color = Color.black;
            dropdown.captionText = label;
            CreateDropdownTemplate(dropdownObject.transform, dropdown, font, size);
            dropdown.value = GetColorIndex(selectedColor);
            dropdown.RefreshShownValue();
            return dropdown;
        }

        private void CreateDropdownTemplate(Transform parent, Dropdown dropdown, Font font, Vector2 dropdownSize)
        {
            GameObject template = CreatePanel(parent, "Template", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -2f), new Vector2(0f, 150f), new Color(1f, 1f, 1f, 0.98f));
            RectTransform templateRect = template.GetComponent<RectTransform>();
            templateRect.pivot = new Vector2(0.5f, 1f);
            template.SetActive(false);

            ScrollRect scrollRect = template.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            GameObject viewport = CreatePanel(template.transform, "Viewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0f));
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, colorOptions.Length * 28f);

            GameObject item = CreatePanel(content.transform, "Item", new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, 28f), new Color(1f, 1f, 1f, 0.95f));
            Toggle toggle = item.AddComponent<Toggle>();
            Text itemText = CreateText(item.transform, "Item Label", "Option", font, 15, TextAnchor.MiddleLeft, new Vector2(8f, -4f), new Vector2(dropdownSize.x - 16f, 24f));
            itemText.color = Color.black;
            toggle.targetGraphic = item.GetComponent<Image>();

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            dropdown.template = templateRect;
            dropdown.itemText = itemText;
        }

        private void CreateButton(Transform parent, string name, string label, Font font, Vector2 anchoredPosition, Vector2 size, Action onClick)
        {
            GameObject buttonObject = CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size, new Color(0.18f, 0.34f, 0.52f, 0.96f));
            Button button = buttonObject.AddComponent<Button>();
            button.onClick.AddListener(() => onClick?.Invoke());

            Text text = CreateText(buttonObject.transform, "Label", label, font, 15, TextAnchor.MiddleCenter, Vector2.zero, size);
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private int GetColorIndex(string colorName)
        {
            for (int i = 0; i < colorOptions.Length; i++)
            {
                if (colorOptions[i] == colorName)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
