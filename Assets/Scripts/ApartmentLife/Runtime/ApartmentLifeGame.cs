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
            apartmentBuilder.Build(saveData.residents);

            ui = new ApartmentLifeUI();
            ui.Build(AdvanceTime, SaveGame, LoadGame);
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

            ApplyEvent(eventType, actor, partner);
            RefreshView();
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
            RefreshView();
        }

        private void ApplyLoadedData(GameSaveData loaded)
        {
            saveData.day = loaded.day;

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

        private void ApplyEvent(DailyEventType eventType, ResidentData actor, ResidentData partner)
        {
            int relationshipDelta = 0;
            int actorMoodDelta = 0;
            int partnerMoodDelta = 0;
            int actorEnergyDelta = 0;
            int partnerEnergyDelta = 0;
            string eventName = "";
            string detail = "";

            switch (eventType)
            {
                case DailyEventType.Chat:
                    eventName = "雑談";
                    relationshipDelta = 4;
                    actorMoodDelta = 3;
                    partnerMoodDelta = 3;
                    actorEnergyDelta = -2;
                    partnerEnergyDelta = -2;
                    detail = $"{actor.name}と{partner.name}が廊下で短く近況を話した。";
                    break;
                case DailyEventType.Argument:
                    eventName = "喧嘩";
                    relationshipDelta = -9;
                    actorMoodDelta = -8;
                    partnerMoodDelta = -8;
                    actorEnergyDelta = -6;
                    partnerEnergyDelta = -6;
                    detail = $"{actor.name}と{partner.name}が共有スペースの使い方で言い合いになった。";
                    break;
                case DailyEventType.Reconcile:
                    eventName = "仲直り";
                    relationshipDelta = 8;
                    actorMoodDelta = 6;
                    partnerMoodDelta = 6;
                    actorEnergyDelta = -3;
                    partnerEnergyDelta = -3;
                    detail = $"{actor.name}が{partner.name}に声をかけ、気まずさを少し解いた。";
                    break;
                case DailyEventType.ShareHobby:
                    eventName = "趣味の共有";
                    relationshipDelta = 7;
                    actorMoodDelta = 7;
                    partnerMoodDelta = 5;
                    actorEnergyDelta = -4;
                    partnerEnergyDelta = 2;
                    detail = $"{actor.name}が趣味の「{actor.hobby}」を{partner.name}に紹介した。";
                    break;
                case DailyEventType.Consult:
                    eventName = "相談";
                    relationshipDelta = 6;
                    actorMoodDelta = 4;
                    partnerMoodDelta = 2;
                    actorEnergyDelta = -5;
                    partnerEnergyDelta = -2;
                    detail = $"{actor.name}が{partner.name}に最近の悩みを相談した。";
                    break;
            }

            actor.mood = ClampStatus(actor.mood + actorMoodDelta);
            partner.mood = ClampStatus(partner.mood + partnerMoodDelta);
            actor.energy = ClampStatus(actor.energy + actorEnergyDelta);
            partner.energy = ClampStatus(partner.energy + partnerEnergyDelta);
            ChangeRelationship(actor.id, partner.id, relationshipDelta);

            AddLog($"Day {saveData.day} [{eventName}] {detail} 関係 {FormatDelta(relationshipDelta)} / 気分 {actor.name}{FormatDelta(actorMoodDelta)}, {partner.name}{FormatDelta(partnerMoodDelta)}");
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
