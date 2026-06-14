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

            GameObject topPanel = CreatePanel(canvas.transform, "Top Panel", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(430f, 76f), new Color(0f, 0f, 0f, 0.55f));
            dayText = CreateText(topPanel.transform, "Day Text", "Day 1", defaultFont, 18, TextAnchor.MiddleLeft, new Vector2(12f, -10f), new Vector2(100f, 28f));
            CreateButton(topPanel.transform, "Advance Time Button", "時間を進める", defaultFont, new Vector2(115f, -12f), new Vector2(130f, 34f), onAdvanceTime);
            CreateButton(topPanel.transform, "Save Button", "セーブ", defaultFont, new Vector2(255f, -12f), new Vector2(72f, 34f), onSave);
            CreateButton(topPanel.transform, "Load Button", "ロード", defaultFont, new Vector2(335f, -12f), new Vector2(72f, 34f), onLoad);

            GameObject logPanel = CreatePanel(canvas.transform, "Event Log Panel", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(510f, 250f), new Color(0f, 0f, 0f, 0.55f));
            CreateText(logPanel.transform, "Log Header", "イベントログ", defaultFont, 18, TextAnchor.MiddleLeft, new Vector2(12f, -8f), new Vector2(220f, 28f));
            logText = CreateText(logPanel.transform, "Log Text", "", defaultFont, 15, TextAnchor.UpperLeft, new Vector2(12f, -40f), new Vector2(486f, 198f));

            detailPanel = CreatePanel(canvas.transform, "Resident Detail Panel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(320f, 245f), new Color(0f, 0f, 0f, 0.6f));
            CreateText(detailPanel.transform, "Detail Header", "住人詳細", defaultFont, 18, TextAnchor.MiddleLeft, new Vector2(12f, -8f), new Vector2(220f, 28f));
            detailText = CreateText(detailPanel.transform, "Detail Text", "住人をクリックしてください。", defaultFont, 15, TextAnchor.UpperLeft, new Vector2(12f, -40f), new Vector2(296f, 190f));
        }

        public void SetDay(int day)
        {
            dayText.text = $"Day {day}";
        }

        public void SetLogs(IReadOnlyList<string> logs)
        {
            int start = Mathf.Max(0, logs.Count - 8);
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
                $"ID: {resident.id}\n" +
                $"名前: {resident.name}\n" +
                $"性格: {resident.personality}\n" +
                $"趣味: {resident.hobby}\n" +
                $"口ぐせ: {resident.catchphrase}\n" +
                $"気分: {resident.mood}\n" +
                $"元気: {resident.energy}\n\n" +
                "関係性\n" +
                string.Join("\n", relationships);
        }

        private Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject("Apartment Life UI");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
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
            GameObject buttonObject = CreatePanel(parent, name, new Vector2(0f, 1f), new Vector2(0f, 1f), anchoredPosition, size, new Color(0.18f, 0.35f, 0.55f, 0.95f));
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
