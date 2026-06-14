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
        private GameObject detailPanel;

        public void Build(Action onAdvanceTime, Action onSave, Action onLoad)
        {
            Canvas canvas = CreateCanvas();
            CreateEventSystemIfNeeded();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (defaultFont == null)
            {
                defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject topPanel = CreatePanel(canvas.transform, "Top Panel", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(500f, 58f), new Color(0.05f, 0.07f, 0.09f, 0.72f));
            dayText = CreateText(topPanel.transform, "Day Text", "Day 1", defaultFont, 20, TextAnchor.MiddleLeft, new Vector2(16f, -12f), new Vector2(100f, 34f));
            CreateButton(topPanel.transform, "Advance Time Button", "時間を進める", defaultFont, new Vector2(120f, -12f), new Vector2(150f, 34f), onAdvanceTime);
            CreateButton(topPanel.transform, "Save Button", "セーブ", defaultFont, new Vector2(282f, -12f), new Vector2(88f, 34f), onSave);
            CreateButton(topPanel.transform, "Load Button", "ロード", defaultFont, new Vector2(382f, -12f), new Vector2(88f, 34f), onLoad);

            GameObject logPanel = CreatePanel(canvas.transform, "Event Log Panel", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, -18f), new Vector2(360f, 430f), new Color(0.05f, 0.07f, 0.09f, 0.68f));
            CreateText(logPanel.transform, "Log Header", "イベントログ", defaultFont, 20, TextAnchor.MiddleLeft, new Vector2(16f, -14f), new Vector2(220f, 30f));
            logText = CreateText(logPanel.transform, "Log Text", "", defaultFont, 16, TextAnchor.UpperLeft, new Vector2(16f, -54f), new Vector2(328f, 352f));

            detailPanel = CreatePanel(canvas.transform, "Resident Detail Panel", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-14f, -18f), new Vector2(340f, 430f), new Color(0.05f, 0.07f, 0.09f, 0.68f));
            CreateText(detailPanel.transform, "Detail Header", "住人詳細", defaultFont, 20, TextAnchor.MiddleLeft, new Vector2(16f, -14f), new Vector2(220f, 30f));
            detailText = CreateText(detailPanel.transform, "Detail Text", "住人をクリックすると、ここに性格・趣味・関係性が表示されます。", defaultFont, 16, TextAnchor.UpperLeft, new Vector2(16f, -54f), new Vector2(308f, 352f));
        }

        public void SetDay(int day)
        {
            dayText.text = $"Day {day}";
        }

        public void SetLogs(IReadOnlyList<string> logs)
        {
            int start = Mathf.Max(0, logs.Count - 7);
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
    }
}
