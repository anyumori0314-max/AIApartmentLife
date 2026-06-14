using ApartmentLife.Data;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ApartmentLife.Runtime
{
    // 3D上の住人オブジェクトに付けるクリック受付用コンポーネントです。
    public class ResidentView : MonoBehaviour, IPointerClickHandler
    {
        private ApartmentLifeGame game;
        private ResidentData resident;
        private Color baseColor;
        private Renderer bodyRenderer;
        private TextMesh emotionIcon;
        private Vector3 normalPosition;
        private Quaternion normalRotation;
        private Vector3 normalScale;
        private Coroutine reactionCoroutine;
        private bool isReacting;

        public ResidentData Resident => resident;
        public Color BaseColor => baseColor;

        public void Initialize(ApartmentLifeGame owner, ResidentData data, Color residentColor)
        {
            game = owner;
            resident = data;
            baseColor = residentColor;
            bodyRenderer = GetComponent<Renderer>();
            normalPosition = transform.position;
            normalRotation = transform.rotation;
            normalScale = transform.localScale;
            gameObject.name = $"Resident_{data.name}";
            CreateEmotionIcon();
            SetEmotionVisual(EmotionState.Neutral);
        }

        public void SetMoodColor(int mood)
        {
            if (isReacting || bodyRenderer == null)
            {
                return;
            }

            // 普段は住人ごとの色を保ち、気分が高いほど少し明るく見せます。
            float moodPercent = Mathf.InverseLerp(0, 100, mood);
            bodyRenderer.material.color = Color.Lerp(baseColor * 0.78f, Color.Lerp(baseColor, Color.white, 0.22f), moodPercent);
        }

        public void PlayReaction(EmotionState emotion, ResidentView target)
        {
            if (reactionCoroutine != null)
            {
                StopCoroutine(reactionCoroutine);
                reactionCoroutine = null;
                ResetVisualState();
            }

            reactionCoroutine = StartCoroutine(PlayReactionRoutine(emotion, target));
        }

        private void OnMouseDown()
        {
            // コライダー付きのオブジェクトをクリックするとUnityがこの関数を呼びます。
            SelectThisResident();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Input SystemのUI入力経由でもクリックできるようにします。
            SelectThisResident();
        }

        private void SelectThisResident()
        {
            if (game != null && resident != null)
            {
                game.SelectResident(resident);
            }
        }

        private IEnumerator PlayReactionRoutine(EmotionState emotion, ResidentView target)
        {
            isReacting = true;
            normalPosition = transform.position;
            normalRotation = transform.rotation;
            normalScale = transform.localScale;
            SetEmotionVisual(emotion);

            Vector3 startPosition = normalPosition;
            Vector3 targetPosition = GetReactionPosition(emotion, target);
            Quaternion startRotation = normalRotation;
            Quaternion targetRotation = GetReactionRotation(emotion, target);
            Vector3 startScale = normalScale;
            float duration = 2.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float settle = Mathf.SmoothStep(0f, 1f, progress);
                float pulse = Mathf.Sin(progress * Mathf.PI * 6f);

                transform.position = Vector3.Lerp(startPosition, targetPosition, settle);
                transform.rotation = Quaternion.Slerp(startRotation, targetRotation, settle);
                transform.localScale = GetReactionScale(emotion, startScale, pulse);

                if (emotion == EmotionState.Angry)
                {
                    // 怒っているときは左右に細かく震わせます。
                    transform.position += transform.right * Mathf.Sin(elapsed * 45f) * 0.035f;
                }

                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            ResetVisualState();
            reactionCoroutine = null;
        }

        private Vector3 GetReactionPosition(EmotionState emotion, ResidentView target)
        {
            if (target == null)
            {
                return normalPosition;
            }

            Vector3 toTarget = target.transform.position - normalPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                return normalPosition;
            }

            Vector3 direction = toTarget.normalized;
            if (emotion == EmotionState.Friendly || emotion == EmotionState.Happy)
            {
                return normalPosition + direction * 0.45f;
            }

            if (emotion == EmotionState.Awkward || emotion == EmotionState.Angry)
            {
                return normalPosition - direction * 0.35f;
            }

            return normalPosition;
        }

        private Quaternion GetReactionRotation(EmotionState emotion, ResidentView target)
        {
            if (target == null)
            {
                return normalRotation;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f)
            {
                return normalRotation;
            }

            if (emotion == EmotionState.Awkward)
            {
                // 気まずいときは少しそっぽを向かせます。
                return Quaternion.LookRotation(-toTarget.normalized, Vector3.up);
            }

            return Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        }

        private Vector3 GetReactionScale(EmotionState emotion, Vector3 startScale, float pulse)
        {
            if (emotion == EmotionState.Happy || emotion == EmotionState.Friendly)
            {
                return startScale + Vector3.up * Mathf.Abs(pulse) * 0.08f;
            }

            if (emotion == EmotionState.Sad || emotion == EmotionState.Worried)
            {
                return startScale * 0.9f;
            }

            if (emotion == EmotionState.Surprised)
            {
                return startScale * (1f + Mathf.Abs(pulse) * 0.18f);
            }

            return startScale;
        }

        private void SetEmotionVisual(EmotionState emotion)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.material.color = GetEmotionColor(emotion);
            }

            if (emotionIcon != null)
            {
                emotionIcon.text = GetEmotionIcon(emotion);
                emotionIcon.color = GetIconColor(emotion);
                emotionIcon.gameObject.SetActive(emotion != EmotionState.Neutral);
            }
        }

        private Color GetEmotionColor(EmotionState emotion)
        {
            switch (emotion)
            {
                case EmotionState.Happy:
                case EmotionState.Friendly:
                    return Color.Lerp(baseColor, Color.white, 0.35f);
                case EmotionState.Angry:
                    return Color.Lerp(baseColor, new Color(1f, 0.12f, 0.08f), 0.72f);
                case EmotionState.Sad:
                    return Color.Lerp(baseColor, new Color(0.18f, 0.36f, 1f), 0.62f);
                case EmotionState.Worried:
                    return Color.Lerp(baseColor, new Color(0.18f, 0.18f, 0.22f), 0.55f);
                case EmotionState.Surprised:
                    return Color.Lerp(baseColor, Color.yellow, 0.5f);
                case EmotionState.Awkward:
                    return Color.Lerp(baseColor, Color.gray, 0.45f);
                default:
                    return baseColor;
            }
        }

        private string GetEmotionIcon(EmotionState emotion)
        {
            switch (emotion)
            {
                case EmotionState.Happy:
                    return "♪";
                case EmotionState.Angry:
                    return "!!";
                case EmotionState.Sad:
                    return "...";
                case EmotionState.Worried:
                    return "?";
                case EmotionState.Surprised:
                    return "!";
                case EmotionState.Friendly:
                    return "☆";
                case EmotionState.Awkward:
                    return "...";
                default:
                    return "";
            }
        }

        private Color GetIconColor(EmotionState emotion)
        {
            if (emotion == EmotionState.Angry)
            {
                return new Color(1f, 0.22f, 0.18f);
            }

            if (emotion == EmotionState.Worried || emotion == EmotionState.Sad || emotion == EmotionState.Awkward)
            {
                return new Color(0.25f, 0.25f, 0.3f);
            }

            return new Color(1f, 0.92f, 0.2f);
        }

        private void ResetVisualState()
        {
            transform.position = normalPosition;
            transform.rotation = normalRotation;
            transform.localScale = normalScale;
            isReacting = false;
            SetEmotionVisual(EmotionState.Neutral);
            SetMoodColor(resident != null ? resident.mood : 50);
        }

        private void CreateEmotionIcon()
        {
            GameObject iconObject = new GameObject("Emotion Icon");
            iconObject.transform.SetParent(transform);
            iconObject.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            iconObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            // TextMeshだけで頭上アイコンを作るので、外部アセットは不要です。
            emotionIcon = iconObject.AddComponent<TextMesh>();
            emotionIcon.anchor = TextAnchor.MiddleCenter;
            emotionIcon.alignment = TextAlignment.Center;
            emotionIcon.characterSize = 0.34f;
            emotionIcon.fontStyle = FontStyle.Bold;
            emotionIcon.gameObject.SetActive(false);
        }
    }
}
