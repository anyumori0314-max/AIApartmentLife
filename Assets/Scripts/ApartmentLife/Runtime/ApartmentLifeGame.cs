using System;
using System.Collections.Generic;
using System.IO;
using ApartmentLife.Data;
using ApartmentLife.UI;
using UnityEngine;

namespace ApartmentLife.Runtime
{
    // MVP全体の進行を管理します。イベント発生、住人状態、セーブ/ロードをここに集約します。
    public class ApartmentLifeGame : MonoBehaviour
    {
        private const string SaveFileName = "apartment-life-save.json";

        private readonly GameSaveData saveData = new GameSaveData();
        private readonly System.Random random = new System.Random();
        private ApartmentBuilder apartmentBuilder;
        private ApartmentLifeUI ui;

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        private void Start()
        {
            CreateDefaultData();
            SetupCameraAndLight();

            apartmentBuilder = new ApartmentBuilder(this);
            apartmentBuilder.Build(saveData.residents, saveData.player);

            ui = new ApartmentLifeUI();
            ui.Build(AdvanceTime, SaveGame, LoadGame, ApplyPlayerProfile, saveData.player);
            AddLog("小さなアパートの一日が始まりました。住人をクリックすると詳細を見られます。");
            RefreshView();
        }

        public void SelectResident(ResidentData resident)
        {
            ui.ShowResident(resident, BuildRelationshipLines(resident.id));
        }

        public void AdvanceTime()
        {
            saveData.day++;

            ResidentData actor = PickResident();
            ResidentData partner = PickDifferentResident(actor.id);
            DailyEventType eventType = (DailyEventType)random.Next(Enum.GetValues(typeof(DailyEventType)).Length);
            GetEventEmotions(eventType, out EmotionState actorEmotion, out EmotionState partnerEmotion);

            ApplyEvent(eventType, actor, partner, actorEmotion, partnerEmotion);
            RefreshView();
            PlayEventReaction(actor, partner, actorEmotion, partnerEmotion);
            SelectResident(actor);
        }

        public void SaveGame()
        {
            string json = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(SavePath, json);
            AddLog($"セーブしました: {SavePath}");
            RefreshView();
        }

        public void LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                AddLog("ロードできるセーブデータがまだありません。");
                RefreshView();
                return;
            }

            string json = File.ReadAllText(SavePath);
            GameSaveData loaded = JsonUtility.FromJson<GameSaveData>(json);
            if (loaded == null)
            {
                AddLog("セーブデータの形式が正しくありません。");
                RefreshView();
                return;
            }

            ApplyLoadedData(loaded);

            AddLog("セーブデータをロードしました。");
            apartmentBuilder.UpdatePlayer(saveData.player);
            ui.SetPlayerProfile(saveData.player);
            RefreshView();
        }

        private void ApplyLoadedData(GameSaveData loaded)
        {
            saveData.day = loaded.day;
            if (loaded.player != null)
            {
                CopyPlayerData(loaded.player, saveData.player);
            }

            // 生成済みの住人Viewが参照しているResidentDataを保つため、既存オブジェクトへ値をコピーします。
            if (loaded.residents != null)
            {
                foreach (ResidentData loadedResident in loaded.residents)
                {
                    ResidentData currentResident = saveData.residents.Find(resident => resident.id == loadedResident.id);
                    if (currentResident == null)
                    {
                        saveData.residents.Add(loadedResident);
                        continue;
                    }

                    currentResident.name = loadedResident.name;
                    currentResident.personality = loadedResident.personality;
                    currentResident.hobby = loadedResident.hobby;
                    currentResident.catchphrase = loadedResident.catchphrase;
                    currentResident.mood = loadedResident.mood;
                    currentResident.energy = loadedResident.energy;
                }
            }

            saveData.relationships.Clear();
            if (loaded.relationships != null)
            {
                saveData.relationships.AddRange(loaded.relationships);
            }

            saveData.eventLogs.Clear();
            if (loaded.eventLogs != null)
            {
                saveData.eventLogs.AddRange(loaded.eventLogs);
            }
        }

        private void CreateDefaultData()
        {
            saveData.day = 1;
            saveData.player = CreateDefaultPlayer();
            saveData.residents.Clear();
            saveData.relationships.Clear();
            saveData.eventLogs.Clear();

            saveData.residents.Add(new ResidentData("r01", "アオイ", "几帳面", "朝の散歩", "今日も整えていこう。", 60, 75));
            saveData.residents.Add(new ResidentData("r02", "ミナト", "陽気", "料理", "まあ、なんとかなるよ。", 70, 65));
            saveData.residents.Add(new ResidentData("r03", "スミレ", "慎重", "読書", "少し考えさせて。", 55, 80));
            saveData.residents.Add(new ResidentData("r04", "レン", "好奇心旺盛", "工作", "試してみたいな。", 65, 70));

            for (int i = 0; i < saveData.residents.Count; i++)
            {
                for (int j = i + 1; j < saveData.residents.Count; j++)
                {
                    saveData.relationships.Add(new RelationshipData(saveData.residents[i].id, saveData.residents[j].id, 50));
                }
            }
        }

        private PlayerData CreateDefaultPlayer()
        {
            return new PlayerData("player", "あなた", "穏やか", "散歩", "よろしく。", 60, 70, "白", "青", true);
        }

        private void ApplyPlayerProfile(PlayerData player)
        {
            CopyPlayerData(player, saveData.player);
            apartmentBuilder.UpdatePlayer(saveData.player);
            ui.SetPlayerProfile(saveData.player);
            AddLog($"{saveData.player.name}のプロフィールを更新しました。");
            RefreshView();
        }

        private void CopyPlayerData(PlayerData source, PlayerData target)
        {
            if (source == null || target == null)
            {
                return;
            }

            // UIと3D表示が同じPlayerDataを参照し続けられるよう、中身だけをコピーします。
            target.id = string.IsNullOrEmpty(source.id) ? "player" : source.id;
            target.name = string.IsNullOrEmpty(source.name) ? "あなた" : source.name;
            target.personality = string.IsNullOrEmpty(source.personality) ? "穏やか" : source.personality;
            target.hobby = string.IsNullOrEmpty(source.hobby) ? "散歩" : source.hobby;
            target.catchphrase = string.IsNullOrEmpty(source.catchphrase) ? "よろしく。" : source.catchphrase;
            target.mood = ClampStatus(source.mood);
            target.energy = ClampStatus(source.energy);
            target.bodyColor = string.IsNullOrEmpty(source.bodyColor) ? "白" : source.bodyColor;
            target.clothesColor = string.IsNullOrEmpty(source.clothesColor) ? "青" : source.clothesColor;
            target.isPlayer = true;
        }

        private void SetupCameraAndLight()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.tag = "MainCamera";
            }

            if (camera.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>() == null)
            {
                camera.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            }

            // 4部屋全体と廊下が一画面に入りやすいよう、少し引いた斜め上から見下ろします。
            camera.transform.position = new Vector3(0f, 10.8f, -11.8f);
            camera.transform.rotation = Quaternion.Euler(58f, 0f, 0f);
            camera.fieldOfView = 46f;
            camera.clearFlags = CameraClearFlags.Skybox;

            GameObject lightObject = new GameObject("Apartment Key Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            GameObject fillLightObject = new GameObject("Apartment Fill Light");
            Light fillLight = fillLightObject.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.intensity = 9f;
            fillLight.range = 12f;
            fillLight.transform.position = new Vector3(0f, 5f, 0f);
        }

        private void ApplyEvent(DailyEventType eventType, ResidentData actor, ResidentData partner, EmotionState actorEmotion, EmotionState partnerEmotion)
        {
            int relationshipDelta = 0;
            int actorMoodDelta = 0;
            int partnerMoodDelta = 0;
            int actorEnergyDelta = 0;
            int partnerEnergyDelta = 0;
            string eventName = "";

            switch (eventType)
            {
                case DailyEventType.Chat:
                    eventName = "雑談";
                    relationshipDelta = 4;
                    actorMoodDelta = 3;
                    partnerMoodDelta = 3;
                    actorEnergyDelta = -2;
                    partnerEnergyDelta = -2;
                    break;
                case DailyEventType.Argument:
                    eventName = "喧嘩";
                    relationshipDelta = -9;
                    actorMoodDelta = -8;
                    partnerMoodDelta = -8;
                    actorEnergyDelta = -6;
                    partnerEnergyDelta = -6;
                    break;
                case DailyEventType.Reconcile:
                    eventName = "仲直り";
                    relationshipDelta = 8;
                    actorMoodDelta = 6;
                    partnerMoodDelta = 6;
                    actorEnergyDelta = -3;
                    partnerEnergyDelta = -3;
                    break;
                case DailyEventType.ShareHobby:
                    eventName = "趣味の共有";
                    relationshipDelta = 7;
                    actorMoodDelta = 7;
                    partnerMoodDelta = 5;
                    actorEnergyDelta = -4;
                    partnerEnergyDelta = 2;
                    break;
                case DailyEventType.Consult:
                    eventName = "相談";
                    relationshipDelta = 6;
                    actorMoodDelta = 4;
                    partnerMoodDelta = 2;
                    actorEnergyDelta = -5;
                    partnerEnergyDelta = -2;
                    break;
            }

            actor.mood = ClampStatus(actor.mood + actorMoodDelta);
            partner.mood = ClampStatus(partner.mood + partnerMoodDelta);
            actor.energy = ClampStatus(actor.energy + actorEnergyDelta);
            partner.energy = ClampStatus(partner.energy + partnerEnergyDelta);
            ChangeRelationship(actor.id, partner.id, relationshipDelta);

            RelationshipData relationship = FindRelationship(actor.id, partner.id);
            int relationshipValue = relationship != null ? relationship.value : 50;
            string detail = BuildEventDetail(eventType, actor, partner, actorEmotion, partnerEmotion, relationshipValue);
            string stateNote = BuildStateNote(actor, partner, relationshipDelta, actorMoodDelta, partnerMoodDelta, relationshipValue);

            AddLog($"Day {saveData.day} [{eventName}] {detail} {stateNote}");
        }

        private void GetEventEmotions(DailyEventType eventType, out EmotionState actorEmotion, out EmotionState partnerEmotion)
        {
            actorEmotion = EmotionState.Neutral;
            partnerEmotion = EmotionState.Neutral;

            switch (eventType)
            {
                case DailyEventType.Chat:
                    actorEmotion = PickEmotion(EmotionState.Friendly, EmotionState.Happy);
                    partnerEmotion = PickEmotion(EmotionState.Friendly, EmotionState.Happy);
                    break;
                case DailyEventType.Argument:
                    actorEmotion = PickEmotion(EmotionState.Angry, EmotionState.Awkward);
                    partnerEmotion = PickEmotion(EmotionState.Angry, EmotionState.Awkward);
                    break;
                case DailyEventType.Reconcile:
                    actorEmotion = PickEmotion(EmotionState.Happy, EmotionState.Friendly);
                    partnerEmotion = PickEmotion(EmotionState.Happy, EmotionState.Friendly);
                    break;
                case DailyEventType.ShareHobby:
                    actorEmotion = EmotionState.Happy;
                    partnerEmotion = EmotionState.Happy;
                    break;
                case DailyEventType.Consult:
                    actorEmotion = EmotionState.Worried;
                    partnerEmotion = EmotionState.Friendly;
                    break;
            }
        }

        private void PlayEventReaction(ResidentData actor, ResidentData partner, EmotionState actorEmotion, EmotionState partnerEmotion)
        {
            // 感情は見た目だけの一時演出です。イベント結果やセーブデータには影響させません。
            apartmentBuilder.PlayPairReaction(actor, partner, actorEmotion, partnerEmotion);
        }

        private EmotionState PickEmotion(EmotionState first, EmotionState second)
        {
            return random.Next(2) == 0 ? first : second;
        }

        private string BuildEventDetail(DailyEventType eventType, ResidentData actor, ResidentData partner, EmotionState actorEmotion, EmotionState partnerEmotion, int relationshipValue)
        {
            switch (eventType)
            {
                case DailyEventType.Chat:
                    return BuildChatDetail(actor, partner, relationshipValue);
                case DailyEventType.Argument:
                    return BuildArgumentDetail(actor, partner, actorEmotion, partnerEmotion, relationshipValue);
                case DailyEventType.Reconcile:
                    return BuildReconcileDetail(actor, partner, relationshipValue);
                case DailyEventType.ShareHobby:
                    return BuildShareHobbyDetail(actor, partner, relationshipValue);
                case DailyEventType.Consult:
                    return BuildConsultDetail(actor, partner, relationshipValue);
                default:
                    return $"{actor.name}と{partner.name}は、しばらく同じ部屋で過ごしていた。";
            }
        }

        private string BuildChatDetail(ResidentData actor, ResidentData partner, int relationshipValue)
        {
            string actorStyle = BuildPersonalityGesture(actor);
            string partnerStyle = BuildMoodGesture(partner);

            switch (random.Next(3))
            {
                case 0:
                    return $"{actor.name}と{partner.name}は共用スペースで何気ない話をしていた。{actor.name}は{actorStyle}、{partner.name}も{partnerStyle}うなずいていた。{BuildCatchphraseLine(actor)}";
                case 1:
                    return $"{partner.name}が通りかかった{actor.name}に声をかけた。短い会話だったが、{BuildRelationshipPhrase(relationshipValue)}ふたりにはちょうどよい距離感だった。{BuildCatchphraseLine(partner)}";
                default:
                    return $"{actor.name}は{partner.name}と今日の小さな出来事を話していた。話題はすぐにそれたが、空気はやわらかい。{BuildMoodLine(actor, partner)}";
            }
        }

        private string BuildArgumentDetail(ResidentData actor, ResidentData partner, EmotionState actorEmotion, EmotionState partnerEmotion, int relationshipValue)
        {
            string actorReaction = actorEmotion == EmotionState.Angry ? "少し声が強くなっていた" : "目をそらして黙り込んでいた";
            string partnerReaction = partnerEmotion == EmotionState.Angry ? "言い返すように身を乗り出した" : "距離を置くように立っていた";

            switch (random.Next(3))
            {
                case 0:
                    return $"{actor.name}と{partner.name}は些細なことで言い合いになった。{actor.name}は{actorReaction}し、{partner.name}は{partnerReaction}。しばらく気まずい空気が残っている。";
                case 1:
                    return $"共有スペースの使い方をめぐって、{actor.name}と{partner.name}の間に小さな衝突が起きた。{BuildRelationshipPhrase(relationshipValue)}だけに、すぐには言葉が戻らない。";
                default:
                    return $"{actor.name}は普段なら{BuildPersonalityGesture(actor)}話すが、今日は少し余裕がなかったらしい。{partner.name}との会話は途中で途切れ、ふたりは別々の方向を向いた。";
            }
        }

        private string BuildReconcileDetail(ResidentData actor, ResidentData partner, int relationshipValue)
        {
            switch (random.Next(3))
            {
                case 0:
                    return $"{actor.name}が{partner.name}にそっと声をかけた。ぎこちなさは残っているが、ふたりの表情は少し明るい。{BuildCatchphraseLine(actor)}";
                case 1:
                    return $"{partner.name}は{actor.name}の話を最後まで聞いていた。言葉は多くないが、{BuildRelationshipPhrase(relationshipValue)}ふたりの間に安心した空気が戻ってきた。";
                default:
                    return $"{actor.name}と{partner.name}は短く謝り合った。大げさな仲直りではないが、近くに立つ姿はさっきより自然に見える。";
            }
        }

        private string BuildShareHobbyDetail(ResidentData actor, ResidentData partner, int relationshipValue)
        {
            switch (random.Next(3))
            {
                case 0:
                    return $"{actor.name}は趣味の「{actor.hobby}」について{partner.name}に話していた。{partner.name}は思ったより興味を示し、ふたりとも楽しそうに見える。";
                case 1:
                    return $"{actor.name}が「{actor.hobby}」の面白さを身振りを交えて説明した。{BuildPersonalityGesture(actor)}話す姿につられて、{partner.name}も少し笑っていた。{BuildCatchphraseLine(actor)}";
                default:
                    return $"{partner.name}は{actor.name}の趣味の話を聞きながら、何度か質問していた。{BuildRelationshipPhrase(relationshipValue)}ふたりの距離が少し近づいたようだ。";
            }
        }

        private string BuildConsultDetail(ResidentData actor, ResidentData partner, int relationshipValue)
        {
            switch (random.Next(3))
            {
                case 0:
                    return $"{actor.name}は{partner.name}に最近の悩みを打ち明けた。{actor.name}の声は少し小さく、{partner.name}は静かに話を聞いていた。";
                case 1:
                    return $"{actor.name}はしばらく迷ってから、{partner.name}に相談を始めた。{BuildMoodState(actor)}に気づいたのか、{partner.name}は急かさずに待っている。";
                default:
                    return $"{partner.name}は{actor.name}の言葉を遮らずに受け止めていた。{BuildRelationshipPhrase(relationshipValue)}ふたりだからこそ、話せたことなのかもしれない。{BuildCatchphraseLine(actor)}";
            }
        }

        private string BuildStateNote(ResidentData actor, ResidentData partner, int relationshipDelta, int actorMoodDelta, int partnerMoodDelta, int relationshipValue)
        {
            string distanceLine = relationshipDelta > 0
                ? "ふたりの距離は少し近づいたように見える。"
                : relationshipDelta < 0
                    ? "ふたりの間には、まだ少し距離がある。"
                    : "ふたりの距離感は大きく変わっていない。";

            string moodLine = actorMoodDelta >= 0 && partnerMoodDelta >= 0
                ? $"{actor.name}と{partner.name}の表情は、さっきより少しやわらかい。"
                : $"{actor.name}と{partner.name}は、少し疲れた表情をしている。";

            return $"{distanceLine}{moodLine}今の関係は{BuildRelationshipPhrase(relationshipValue)}ように見える。";
        }

        private string BuildPersonalityGesture(ResidentData resident)
        {
            // 住人データの性格を、短い仕草として文章へ混ぜます。
            if (resident.personality.Contains("陽気"))
            {
                return "明るく身振りを交えて";
            }

            if (resident.personality.Contains("慎重"))
            {
                return "言葉を選びながら";
            }

            if (resident.personality.Contains("几帳面"))
            {
                return "きちんと順を追って";
            }

            if (resident.personality.Contains("好奇心"))
            {
                return "少し身を乗り出して";
            }

            return "落ち着いた様子で";
        }

        private string BuildMoodGesture(ResidentData resident)
        {
            if (resident.energy <= 35)
            {
                return "少し疲れた様子で";
            }

            if (resident.mood >= 70)
            {
                return "楽しそうに";
            }

            if (resident.mood <= 40)
            {
                return "控えめに";
            }

            return "穏やかに";
        }

        private string BuildMoodLine(ResidentData actor, ResidentData partner)
        {
            return $"{actor.name}は{BuildMoodGesture(actor)}話し、{partner.name}は{BuildMoodGesture(partner)}聞いていた。";
        }

        private string BuildMoodState(ResidentData resident)
        {
            if (resident.energy <= 35)
            {
                return "疲れた表情";
            }

            if (resident.mood >= 70)
            {
                return "いつもより明るい声";
            }

            if (resident.mood <= 40)
            {
                return "沈んだ表情";
            }

            return "迷いのある様子";
        }

        private string BuildRelationshipPhrase(int value)
        {
            if (value >= 75)
            {
                return "かなり打ち解けた";
            }

            if (value >= 55)
            {
                return "ほどよく近い";
            }

            if (value >= 35)
            {
                return "まだ探り合っている";
            }

            return "少しぎこちない";
        }

        private string BuildCatchphraseLine(ResidentData resident)
        {
            if (string.IsNullOrEmpty(resident.catchphrase) || random.Next(3) != 0)
            {
                return "";
            }

            return $"{resident.name}は最後に「{resident.catchphrase}」と小さくつぶやいた。";
        }

        private void ChangeRelationship(string idA, string idB, int delta)
        {
            RelationshipData relationship = FindRelationship(idA, idB);
            if (relationship == null)
            {
                relationship = new RelationshipData(idA, idB, 50);
                saveData.relationships.Add(relationship);
            }

            relationship.value = ClampStatus(relationship.value + delta);
        }

        private RelationshipData FindRelationship(string idA, string idB)
        {
            foreach (RelationshipData relationship in saveData.relationships)
            {
                bool sameOrder = relationship.residentAId == idA && relationship.residentBId == idB;
                bool reverseOrder = relationship.residentAId == idB && relationship.residentBId == idA;
                if (sameOrder || reverseOrder)
                {
                    return relationship;
                }
            }

            return null;
        }

        private ResidentData PickResident()
        {
            return saveData.residents[random.Next(saveData.residents.Count)];
        }

        private ResidentData PickDifferentResident(string actorId)
        {
            ResidentData result = PickResident();
            while (result.id == actorId)
            {
                result = PickResident();
            }

            return result;
        }

        private List<string> BuildRelationshipLines(string residentId)
        {
            List<string> lines = new List<string>();
            foreach (RelationshipData relationship in saveData.relationships)
            {
                string otherId = null;
                if (relationship.residentAId == residentId)
                {
                    otherId = relationship.residentBId;
                }
                else if (relationship.residentBId == residentId)
                {
                    otherId = relationship.residentAId;
                }

                if (otherId == null)
                {
                    continue;
                }

                ResidentData other = saveData.residents.Find(resident => resident.id == otherId);
                if (other != null)
                {
                    lines.Add($"{other.name}: {relationship.value}");
                }
            }

            return lines;
        }

        private void AddLog(string message)
        {
            saveData.eventLogs.Add(message);
            if (saveData.eventLogs.Count > 30)
            {
                saveData.eventLogs.RemoveAt(0);
            }
        }

        private void RefreshView()
        {
            ui.SetDay(saveData.day);
            ui.SetLogs(saveData.eventLogs);
            apartmentBuilder.RefreshResidentLabels();
        }

        private int ClampStatus(int value)
        {
            return Mathf.Clamp(value, 0, 100);
        }

        private string FormatDelta(int value)
        {
            return value >= 0 ? $"+{value}" : value.ToString();
        }

        private enum DailyEventType
        {
            Chat,
            Argument,
            Reconcile,
            ShareHobby,
            Consult
        }
    }
}
