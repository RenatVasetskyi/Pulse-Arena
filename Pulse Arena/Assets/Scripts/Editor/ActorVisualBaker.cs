using Data;
using Game.Visuals;
using UnityEditor;
using UnityEngine;

namespace GameEditor
{
    /// <summary>
    /// Bakes the player/enemy primitive visuals — previously built at runtime by
    /// PlayerPrimitiveVisual/EnemyPrimitiveVisual — into the actor prefabs as real child
    /// GameObjects with material assets. Runs once automatically, and on demand via the menu.
    /// The runtime build stays as a fallback, so a failed bake never breaks the game.
    /// </summary>
    public static class ActorVisualBaker
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Game/player.prefab";
        private const string EnemyPrefabPath = "Assets/Prefabs/Game/enemy.prefab";
        private const string SettingsPath = "Assets/Game Settings.asset";
        private const string MaterialsFolder = "Assets/Materials/Actors";
        private const string BakedPrefKey = "PulseArena.ActorVisualsBaked.v1";

        [MenuItem("Tools/Pulse Arena/Bake Actor Visuals")]
        public static void BakeMenu()
        {
            BakeAll(force: true);
            EditorPrefs.SetBool(BakedPrefKey, true);
        }

        [InitializeOnLoadMethod]
        private static void AutoBakeOnce()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorPrefs.GetBool(BakedPrefKey, false))
                    return;

                if (BakeAll(force: false))
                    EditorPrefs.SetBool(BakedPrefKey, true);
            };
        }

        private static bool BakeAll(bool force)
        {
            GameSettings settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);

            if (settings == null)
            {
                Debug.LogError($"[ActorVisualBaker] GameSettings not found at {SettingsPath}. Skipping bake.");
                return false;
            }

            EnsureMaterialsFolder();

            bool player = BakePlayer(settings.PlayerVisuals, force);
            bool enemy = BakeEnemy(settings.EnemyVisuals, force);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return player && enemy;
        }

        private static bool BakePlayer(PlayerVisualData data, bool force)
        {
            if (data == null)
                return false;

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            try
            {
                if (!PrepareVisualRoot(root, "PlayerPrimitiveVisual", data.RootOffset, force, out Transform visual))
                    return true;

                visual.gameObject.AddComponent<PlayerPrimitiveVisual>();

                Material body = GetOrCreateMaterial("Player_Body", data.BodyColor);
                Material head = GetOrCreateMaterial("Player_Head", data.HeadColor);
                Material dark = GetOrCreateMaterial("Player_Dark", data.DarkColor);
                Material accent = GetOrCreateMaterial("Player_Accent", data.AccentColor);

                BakePart("Body", PrimitiveType.Capsule, visual,
                    new Vector3(0f, 0.62f, 0f), Vector3.zero, new Vector3(0.72f, 0.84f, 0.72f), body);
                BakePart("Head", PrimitiveType.Sphere, visual,
                    new Vector3(0f, 1.44f, 0f), Vector3.zero, new Vector3(0.58f, 0.58f, 0.58f), head);
                BakePart("LeftArm", PrimitiveType.Capsule, visual,
                    new Vector3(-0.48f, 0.86f, 0f), new Vector3(0f, 0f, -22f), new Vector3(0.22f, 0.48f, 0.22f), body);
                Transform rightArm = BakePart("RightArm", PrimitiveType.Capsule, visual,
                    new Vector3(0.48f, 0.88f, 0f), new Vector3(0f, 0f, 22f), new Vector3(0.22f, 0.52f, 0.22f), body);

                Transform hatRoot = BakeEmpty("HatRoot", visual, new Vector3(0f, 1.72f, 0f));
                BakePart("HatBrim", PrimitiveType.Cylinder, hatRoot,
                    Vector3.zero, Vector3.zero, new Vector3(0.78f, 0.08f, 0.78f), dark);
                BakePart("HatTop", PrimitiveType.Cylinder, hatRoot,
                    new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(0.43f, 0.28f, 0.43f), dark);
                BakePart("HatBand", PrimitiveType.Cylinder, hatRoot,
                    new Vector3(0f, 0.09f, 0f), Vector3.zero, new Vector3(0.46f, 0.05f, 0.46f), accent);

                BakeEmpty("LassoOrigin", rightArm, new Vector3(0f, -0.62f, 0.08f));

                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                Debug.Log("[ActorVisualBaker] Baked player.prefab visual.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool BakeEnemy(EnemyVisualData data, bool force)
        {
            if (data == null)
                return false;

            GameObject root = PrefabUtility.LoadPrefabContents(EnemyPrefabPath);

            try
            {
                if (!PrepareVisualRoot(root, "EnemyPrimitiveVisual", data.RootOffset, force, out Transform visual))
                    return true;

                visual.gameObject.AddComponent<EnemyPrimitiveVisual>();

                Material body = GetOrCreateMaterial("Enemy_Body", data.BodyColor);
                Material belly = GetOrCreateMaterial("Enemy_Belly", data.BellyColor);
                Material eye = GetOrCreateMaterial("Enemy_Eye", data.EyeColor);
                Material pupil = GetOrCreateMaterial("Enemy_Pupil", data.PupilColor);
                Material spike = GetOrCreateMaterial("Enemy_Spike", data.SpikeColor);

                BakePart("Body", PrimitiveType.Sphere, visual,
                    new Vector3(0f, 0.68f, 0f), Vector3.zero, new Vector3(0.9f, 1.05f, 0.78f), body);
                BakePart("Head", PrimitiveType.Sphere, visual,
                    new Vector3(0f, 1.34f, 0.08f), Vector3.zero, new Vector3(0.62f, 0.5f, 0.58f), body);
                BakePart("Belly", PrimitiveType.Sphere, visual,
                    new Vector3(0f, 0.66f, 0.33f), Vector3.zero, new Vector3(0.5f, 0.6f, 0.28f), belly);

                Transform faceRoot = BakeEmpty("FaceRoot", visual, new Vector3(0f, 1.34f, 0.08f));
                Transform leftEye = BakePart("LeftEye", PrimitiveType.Sphere, faceRoot,
                    new Vector3(-0.175f, 0.1f, 0.27f), Vector3.zero, new Vector3(0.21f, 0.21f, 0.12f), eye);
                Transform rightEye = BakePart("RightEye", PrimitiveType.Sphere, faceRoot,
                    new Vector3(0.175f, 0.1f, 0.27f), Vector3.zero, new Vector3(0.21f, 0.21f, 0.12f), eye);
                BakePart("LeftPupil", PrimitiveType.Sphere, leftEye,
                    new Vector3(0f, 0f, 0.6f), Vector3.zero, new Vector3(0.42f, 0.42f, 0.22f), pupil);
                BakePart("RightPupil", PrimitiveType.Sphere, rightEye,
                    new Vector3(0f, 0f, 0.6f), Vector3.zero, new Vector3(0.42f, 0.42f, 0.22f), pupil);

                Transform spikeRoot = BakeEmpty("SpikeRoot", visual, Vector3.zero);
                BakePart("Spike_Back_1", PrimitiveType.Cube, spikeRoot,
                    new Vector3(0f, 1.52f, -0.34f), new Vector3(42f, 0f, 0f), new Vector3(0.24f, 0.42f, 0.24f), spike);
                BakePart("Spike_Back_2", PrimitiveType.Cube, spikeRoot,
                    new Vector3(-0.28f, 1.16f, -0.34f), new Vector3(48f, -18f, 0f), new Vector3(0.18f, 0.34f, 0.18f), spike);
                BakePart("Spike_Back_3", PrimitiveType.Cube, spikeRoot,
                    new Vector3(0.28f, 1.16f, -0.34f), new Vector3(48f, 18f, 0f), new Vector3(0.18f, 0.34f, 0.18f), spike);

                PrefabUtility.SaveAsPrefabAsset(root, EnemyPrefabPath);
                Debug.Log("[ActorVisualBaker] Baked enemy.prefab visual.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool PrepareVisualRoot(GameObject root, string visualName, Vector3 rootOffset, bool force,
            out Transform visual)
        {
            Transform existing = root.transform.Find(visualName);

            if (existing != null)
            {
                if (!force)
                {
                    visual = null;
                    return false;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            GameObject visualGo = new(visualName);
            visualGo.transform.SetParent(root.transform, false);
            visualGo.transform.localPosition = rootOffset;
            visualGo.transform.localRotation = Quaternion.identity;
            visualGo.transform.localScale = Vector3.one;
            visual = visualGo.transform;
            return true;
        }

        private static Transform BakePart(string name, PrimitiveType primitiveType, Transform parent,
            Vector3 localPosition, Vector3 localRotation, Vector3 localScale, Material material)
        {
            GameObject part = GameObject.CreatePrimitive(primitiveType);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localRotation = Quaternion.Euler(localRotation);
            part.transform.localScale = localScale;

            Collider collider = part.GetComponent<Collider>();

            if (collider != null)
                Object.DestroyImmediate(collider);

            Renderer renderer = part.GetComponent<Renderer>();

            if (renderer != null)
                renderer.sharedMaterial = material;

            return part.transform;
        }

        private static Transform BakeEmpty(string name, Transform parent, Vector3 localPosition)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.transform;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                shader ??= Shader.Find("Standard");
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);

            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureMaterialsFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
                AssetDatabase.CreateFolder("Assets/Materials", "Actors");
        }
    }
}
