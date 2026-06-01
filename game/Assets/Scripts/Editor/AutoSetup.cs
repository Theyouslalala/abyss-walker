#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.IO;

namespace AbyssWalker.Editor
{
    public class AutoSetup : MonoBehaviour
    {
        [MenuItem("Abyss Walker/一键配置场景", false, 1)]
        public static void SetupScene()
        {
            // 创建 Tile 资源
            CreateTiles();

            // 创建 Prefabs
            CreatePrefabs();

            // 创建场景对象
            CreateSceneObjects();

            Debug.Log("[Abyss Walker] 场景配置完成！点击 Play 即可运行。");
        }

        [MenuItem("Abyss Walker/创建 Tile 资源", false, 10)]
        public static void CreateTiles()
        {
            string tilePath = "Assets/Tiles";
            if (!AssetDatabase.IsValidFolder(tilePath))
            {
                AssetDatabase.CreateFolder("Assets", "Tiles");
            }

            // 创建地板 Tile
            CreateRuleTile(tilePath + "/FloorTile.asset", new Color(0.4f, 0.4f, 0.4f));

            // 创建墙壁 Tile
            CreateRuleTile(tilePath + "/WallTile.asset", new Color(0.2f, 0.2f, 0.2f));

            // 创建出口 Tile
            CreateRuleTile(tilePath + "/ExitTile.asset", new Color(1f, 0.84f, 0f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Abyss Walker] Tile 资源创建完成");
        }

        private static void CreateRuleTile(string path, Color color)
        {
            if (File.Exists(path)) return;

            // 创建一个简单的 Tile（不用 RuleTile 避免依赖问题）
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.color = color;

            // 创建默认 Sprite
            Texture2D tex = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            string spritePath = path.Replace(".asset", "_sprite.png");
            byte[] pngData = tex.EncodeToPNG();
            File.WriteAllBytes(Application.dataPath + "/../" + spritePath, pngData);
            AssetDatabase.Refresh();

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null)
            {
                TextureImporter importer = AssetImporter.GetAtPath(spritePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 32;
                    importer.filterMode = FilterMode.Point;
                    importer.SaveAndReimport();
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                }
            }
            tile.sprite = sprite;

            AssetDatabase.CreateAsset(tile, path);
        }

        [MenuItem("Abyss Walker/创建预制体", false, 11)]
        public static void CreatePrefabs()
        {
            string prefabPath = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            // 创建玩家预制体
            CreateEntityPrefab(prefabPath + "/Player.prefab", "Player", Color.blue, true);

            // 创建敌人预制体
            CreateEntityPrefab(prefabPath + "/Skeleton.prefab", "Skeleton", Color.red, false);
            CreateEntityPrefab(prefabPath + "/Goblin.prefab", "Goblin", Color.green, false);
            CreateEntityPrefab(prefabPath + "/ShadowMage.prefab", "ShadowMage", new Color(0.5f, 0f, 0.5f), false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Abyss Walker] 预制体创建完成");
        }

        private static void CreateEntityPrefab(string path, string name, Color color, bool isPlayer)
        {
            if (File.Exists(path)) return;

            GameObject go = new GameObject(name);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite(color);
            sr.sortingOrder = 10;

            go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

            if (isPlayer)
            {
                go.AddComponent<AbyssWalker.Entity.Player>();
                go.AddComponent<AbyssWalker.Combat.AutoAttack>();
                go.tag = "Player";
            }
            else
            {
                go.AddComponent<AbyssWalker.Entity.Enemy>();
            }

            PrefabUtility.SaveAsPrefabAsset(go, path);
            DestroyImmediate(go);
        }

        private static Sprite CreateSquareSprite(Color color)
        {
            Texture2D tex = new Texture2D(32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }

        [MenuItem("Abyss Walker/创建场景对象", false, 2)]
        public static void CreateSceneObjects()
        {
            // ========== Grid + Tilemap ==========
            GameObject gridObj = GameObject.Find("Grid");
            if (gridObj == null)
            {
                gridObj = new GameObject("Grid");
                gridObj.AddComponent<Grid>();
            }

            Tilemap tilemap = gridObj.GetComponentInChildren<Tilemap>();
            TilemapRenderer tilemapRenderer = null;
            if (tilemap == null)
            {
                GameObject tilemapObj = new GameObject("DungeonTilemap");
                tilemapObj.transform.SetParent(gridObj.transform);
                tilemap = tilemapObj.AddComponent<Tilemap>();
                tilemapRenderer = tilemapObj.AddComponent<TilemapRenderer>();
                tilemapRenderer.sortingOrder = 0;
            }

            // ========== 玩家 ==========
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = CreateEntityInScene("Player", Color.blue, new Vector3(0, 0, 0));
                if (!player.GetComponent<AbyssWalker.Entity.Player>())
                    player.AddComponent<AbyssWalker.Entity.Player>();
                if (!player.GetComponent<AbyssWalker.Combat.AutoAttack>())
                    player.AddComponent<AbyssWalker.Combat.AutoAttack>();
            }

            // ========== 游戏管理器 ==========
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj == null)
            {
                gmObj = new GameObject("GameManager");
            }

            AddIfMissing<AbyssWalker.Core.GameManager>(gmObj);
            AddIfMissing<AbyssWalker.Core.TurnManager>(gmObj);
            AddIfMissing<AbyssWalker.Core.EventManager>(gmObj);
            AddIfMissing<AbyssWalker.Core.GridManager>(gmObj);
            AddIfMissing<AbyssWalker.Entity.EntityManager>(gmObj);
            AddIfMissing<AbyssWalker.Combat.CombatManager>(gmObj);
            AddIfMissing<AbyssWalker.Events.EventPlacer>(gmObj);
            AddIfMissing<AbyssWalker.Meta.ProgressManager>(gmObj);
            AddIfMissing<AbyssWalker.Meta.RunManager>(gmObj);

            // ========== 网络管理器 ==========
            GameObject netObj = GameObject.Find("NetworkManager");
            if (netObj == null)
            {
                netObj = new GameObject("NetworkManager");
            }
            AddIfMissing<AbyssWalker.Network.SocketClient>(netObj);
            // GameStateSerializer is a plain class, not a MonoBehaviour — accessed via SocketClient

            // ========== DungeonRenderer ==========
            GameObject dungeonRendererObj = GameObject.Find("DungeonRenderer");
            if (dungeonRendererObj == null)
            {
                dungeonRendererObj = new GameObject("DungeonRenderer");
                dungeonRendererObj.AddComponent<AbyssWalker.Map.DungeonRenderer>();
            }

            // ========== UI ==========
            CreateUI();

            // ========== 移动端输入 ==========
            if (GameObject.Find("MobileInput") == null)
            {
                GameObject mobileInput = new GameObject("MobileInput");
                mobileInput.AddComponent<AbyssWalker.UI.MobileInputHandler>();
            }

            // 标记场景已修改
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[Abyss Walker] 场景对象创建完成");
        }

        private static GameObject CreateEntityInScene(string name, Color color, Vector3 position)
        {
            GameObject go = new GameObject(name);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateSquareSprite(color);
            sr.sortingOrder = 10;
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            return go;
        }

        private static void AddIfMissing<T>(GameObject go) where T : Component
        {
            if (go.GetComponent<T>() == null)
                go.AddComponent<T>();
        }

        private static void CreateUI()
        {
            // 查找或创建 Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                // 创建 EventSystem
                if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventSystem = new GameObject("EventSystem");
                    eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // PlayerHPBar
            if (canvas.transform.Find("PlayerHPBar") == null)
            {
                GameObject hpBarObj = new GameObject("PlayerHPBar");
                hpBarObj.transform.SetParent(canvas.transform, false);
                Slider slider = hpBarObj.AddComponent<Slider>();
                RectTransform rt = hpBarObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0.3f, 1);
                rt.pivot = new Vector2(0.5f, 1);
                rt.anchoredPosition = new Vector2(0, -20);
                rt.sizeDelta = new Vector2(0, 30);

                // Background
                GameObject bg = new GameObject("Background");
                bg.transform.SetParent(hpBarObj.transform, false);
                Image bgImg = bg.AddComponent<Image>();
                bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                RectTransform bgRt = bg.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.sizeDelta = Vector2.zero;

                // Fill Area
                GameObject fillArea = new GameObject("Fill Area");
                fillArea.transform.SetParent(hpBarObj.transform, false);
                RectTransform fillAreaRt = fillArea.AddComponent<RectTransform>();
                fillAreaRt.anchorMin = Vector2.zero;
                fillAreaRt.anchorMax = Vector2.one;
                fillAreaRt.sizeDelta = Vector2.zero;

                GameObject fill = new GameObject("Fill");
                fill.transform.SetParent(fillArea.transform, false);
                Image fillImg = fill.AddComponent<Image>();
                fillImg.color = Color.green;
                RectTransform fillRt = fill.GetComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.sizeDelta = Vector2.zero;

                slider.fillRect = fillRt;
            }

            // FloorText
            if (canvas.transform.Find("FloorText") == null)
            {
                GameObject floorTextObj = new GameObject("FloorText");
                floorTextObj.transform.SetParent(canvas.transform, false);
                Text text = floorTextObj.AddComponent<Text>();
                text.text = "Floor 1";
                text.fontSize = 24;
                text.color = Color.white;
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (text.font == null) text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                RectTransform rt = floorTextObj.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-20, -20);
                rt.sizeDelta = new Vector2(200, 40);
            }

            // PopupPanel (隐藏)
            if (canvas.transform.Find("PopupPanel") == null)
            {
                GameObject popup = new GameObject("PopupPanel");
                popup.transform.SetParent(canvas.transform, false);
                Image bgImg = popup.AddComponent<Image>();
                bgImg.color = new Color(0, 0, 0, 0.7f);
                popup.AddComponent<AbyssWalker.UI.PopupUI>();
                RectTransform rt = popup.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.sizeDelta = Vector2.zero;
                popup.SetActive(false);
            }

            // 添加 HUDController 到 Canvas
            if (canvas.GetComponent<AbyssWalker.UI.HUDController>() == null)
            {
                canvas.gameObject.AddComponent<AbyssWalker.UI.HUDController>();
            }
        }

        [MenuItem("Abyss Walker/运行测试地图生成", false, 20)]
        public static void TestMapGeneration()
        {
            Debug.Log("请在 Python 端运行: python -c \"from ai.mapgen.dungeon_generator import DungeonGenerator; from ai.utils.visualization import print_dungeon; d=DungeonGenerator(seed=42).generate(); print(print_dungeon(d))\"");
        }
    }
}
#endif
