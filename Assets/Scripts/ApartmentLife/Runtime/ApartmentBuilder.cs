using System.Collections.Generic;
using ApartmentLife.Data;
using UnityEngine;

namespace ApartmentLife.Runtime
{
    // 部屋、家具の代わりの床や壁、住人の仮モデルをプリミティブで生成します。
    public class ApartmentBuilder
    {
        private readonly ApartmentLifeGame game;
        private readonly Dictionary<string, ResidentView> residentViews = new Dictionary<string, ResidentView>();
        private readonly Color[] residentColors =
        {
            new Color(0.22f, 0.50f, 0.95f),
            new Color(0.95f, 0.38f, 0.35f),
            new Color(0.28f, 0.72f, 0.48f),
            new Color(0.86f, 0.56f, 0.95f)
        };

        private readonly Color[] floorColors =
        {
            new Color(0.78f, 0.73f, 0.63f),
            new Color(0.70f, 0.78f, 0.69f),
            new Color(0.72f, 0.74f, 0.84f),
            new Color(0.82f, 0.72f, 0.70f)
        };

        private readonly Color[] wallColors =
        {
            new Color(0.92f, 0.88f, 0.78f),
            new Color(0.84f, 0.91f, 0.82f),
            new Color(0.84f, 0.86f, 0.94f),
            new Color(0.94f, 0.84f, 0.84f)
        };

        public IReadOnlyDictionary<string, ResidentView> ResidentViews => residentViews;

        public ApartmentBuilder(ApartmentLifeGame game)
        {
            this.game = game;
        }

        public void Build(IReadOnlyList<ResidentData> residents)
        {
            residentViews.Clear();
            GameObject root = new GameObject("Generated Apartment");

            float roomWidth = 5f;
            float roomDepth = 4f;
            Vector3[] roomCenters =
            {
                new Vector3(-2.7f, 0f, 2.2f),
                new Vector3(2.7f, 0f, 2.2f),
                new Vector3(-2.7f, 0f, -2.2f),
                new Vector3(2.7f, 0f, -2.2f)
            };

            for (int i = 0; i < roomCenters.Length; i++)
            {
                CreateRoom(root.transform, roomCenters[i], roomWidth, roomDepth, i + 1);

                if (i < residents.Count)
                {
                    CreateResident(root.transform, roomCenters[i], residents[i], i);
                }
            }

            CreateSharedHall(root.transform);
        }

        public void RefreshResidentLabels()
        {
            foreach (ResidentView view in residentViews.Values)
            {
                view.SetMoodColor(view.Resident.mood);
            }
        }

        public void PlayPairReaction(ResidentData actor, ResidentData partner, EmotionState actorEmotion, EmotionState partnerEmotion)
        {
            if (!residentViews.TryGetValue(actor.id, out ResidentView actorView))
            {
                return;
            }

            if (!residentViews.TryGetValue(partner.id, out ResidentView partnerView))
            {
                return;
            }

            // イベントに関わった2人だけを短く動かし、数秒後にResidentView側で元の場所へ戻します。
            actorView.PlayReaction(actorEmotion, partnerView);
            partnerView.PlayReaction(partnerEmotion, actorView);
        }

        private void CreateRoom(Transform parent, Vector3 center, float width, float depth, int roomNumber)
        {
            GameObject roomRoot = new GameObject($"Room {roomNumber}");
            roomRoot.transform.SetParent(parent);

            Color floorColor = floorColors[(roomNumber - 1) % floorColors.Length];
            Color wallColor = wallColors[(roomNumber - 1) % wallColors.Length];
            Color sideWallColor = Color.Lerp(wallColor, Color.gray, 0.12f);

            CreateCube(roomRoot.transform, "Floor", center + new Vector3(0f, -0.05f, 0f), new Vector3(width, 0.1f, depth), floorColor);
            CreateCube(roomRoot.transform, "Back Wall", center + new Vector3(0f, 1f, depth / 2f), new Vector3(width, 2f, 0.12f), wallColor);
            CreateCube(roomRoot.transform, "Left Wall", center + new Vector3(-width / 2f, 1f, 0f), new Vector3(0.12f, 2f, depth), sideWallColor);
            CreateCube(roomRoot.transform, "Right Wall", center + new Vector3(width / 2f, 1f, 0f), new Vector3(0.12f, 2f, depth), sideWallColor);

            // 小さな家具の代わりにベッドと机をキューブで置き、部屋らしさを出します。
            CreateCube(roomRoot.transform, "Simple Bed", center + new Vector3(-1.2f, 0.2f, -0.9f), new Vector3(1.5f, 0.35f, 0.9f), new Color(0.45f, 0.65f, 0.9f));
            CreateCube(roomRoot.transform, "Simple Desk", center + new Vector3(1.25f, 0.35f, 0.8f), new Vector3(1.1f, 0.3f, 0.55f), new Color(0.55f, 0.38f, 0.23f));
        }

        private void CreateSharedHall(Transform parent)
        {
            CreateCube(parent, "Shared Hall", new Vector3(0f, 0f, 0f), new Vector3(1.1f, 0.08f, 9f), new Color(0.66f, 0.66f, 0.62f));
            CreateCube(parent, "Notice Board", new Vector3(0f, 1.1f, 4.35f), new Vector3(1.4f, 0.8f, 0.08f), new Color(0.9f, 0.55f, 0.18f));
        }

        private void CreateResident(Transform parent, Vector3 roomCenter, ResidentData resident, int index)
        {
            PrimitiveType type = index % 2 == 0 ? PrimitiveType.Capsule : PrimitiveType.Sphere;
            GameObject body = GameObject.CreatePrimitive(type);
            body.transform.SetParent(parent);
            body.transform.position = roomCenter + new Vector3(0f, type == PrimitiveType.Capsule ? 0.9f : 0.6f, 0f);
            body.transform.localScale = type == PrimitiveType.Capsule ? new Vector3(0.7f, 0.9f, 0.7f) : new Vector3(0.95f, 0.95f, 0.95f);

            Renderer renderer = body.GetComponent<Renderer>();
            renderer.material = new Material(GetDefaultShader());
            Color residentColor = residentColors[index % residentColors.Length];
            renderer.material.color = residentColor;

            ResidentView view = body.AddComponent<ResidentView>();
            view.Initialize(game, resident, residentColor);
            residentViews[resident.id] = view;

            CreateNameLabel(body.transform, resident.name);
        }

        private void CreateNameLabel(Transform parent, string label)
        {
            GameObject textObject = new GameObject("Name Label");
            textObject.transform.SetParent(parent);
            textObject.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            textObject.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);

            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = label;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.22f;
            textMesh.color = Color.black;
        }

        private GameObject CreateCube(Transform parent, string name, Vector3 position, Vector3 scale, Color color)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent);
            cube.transform.position = position;
            cube.transform.localScale = scale;

            Renderer renderer = cube.GetComponent<Renderer>();
            renderer.material = new Material(GetDefaultShader());
            renderer.material.color = color;

            return cube;
        }

        private Shader GetDefaultShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            return shader;
        }
    }
}
