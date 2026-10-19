using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Tool Setup Otomatis untuk Tantangan Hall 2 (3 Kunci + Rak + Barrier Gerbang 2A/2B).
/// Menu: Tools → Setup Hall 2 Challenge (Keys & Shelves)
/// </summary>
[InitializeOnLoad]
public class Hall2ChallengeSetup : EditorWindow
{
    const string KAYKIT_PATH = "Assets/Art/Kaykit_Dungeon/fbx(unity)";

    static Hall2ChallengeSetup()
    {
        EditorApplication.delayCall += AutoEnsureHall2Challenge;
    }

    static void AutoEnsureHall2Challenge()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null) return;

        Transform challengeRoot = arena.transform.Find("Hall2_Challenge");
        if (challengeRoot == null || challengeRoot.Find("Hall2_Shelf") == null)
        {
            SetupHall2ChallengeInternal(silent: true);
        }
    }

    [MenuItem("Tools/Setup Hall 2 Challenge (Keys & Shelves)")]
    public static void SetupHall2ChallengeMenu()
    {
        SetupHall2ChallengeInternal(silent: false);
    }

    public static void SetupHall2ChallengeInternal(bool silent)
    {
        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null)
        {
            if (!silent) EditorUtility.DisplayDialog("Error", "DungeonArena tidak ditemukan di scene!", "OK");
            return;
        }

        Transform existing = arena.transform.Find("Hall2_Challenge");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        GameObject root = new GameObject("Hall2_Challenge");
        root.transform.SetParent(arena.transform, false);

        Hall2ChallengeManager manager = root.AddComponent<Hall2ChallengeManager>();

        // 1. Buat Rak (Shelves) di Dinding Utara Hall 2 (41, 0, 23.2)
        GameObject shelfObj = PlaceModel(root, "Hall2_Shelf", "shelves", new Vector3(41f, 0f, 23.2f), 180f);
        BoxCollider shelfCol = shelfObj.GetComponent<BoxCollider>();
        if (shelfCol == null) shelfCol = shelfObj.AddComponent<BoxCollider>();
        shelfCol.size = new Vector3(3f, 2.8f, 1.2f);
        shelfCol.center = new Vector3(0f, 1.4f, 0f);

        Hall2Shelf shelfComp = shelfObj.AddComponent<Hall2Shelf>();

        // Buat 3 slot visual kunci di atas rak yang nantinya akan diaktifkan
        GameObject vKey1 = PlaceModel(shelfObj, "PlacedKey_1", "key", new Vector3(-0.45f, 2.1f, 0.1f), 0f);
        vKey1.transform.localScale = Vector3.one * 0.7f;
        vKey1.SetActive(false);

        GameObject vKey2 = PlaceModel(shelfObj, "PlacedKey_2", "key", new Vector3(0.0f, 1.35f, 0.1f), 45f);
        vKey2.transform.localScale = Vector3.one * 0.7f;
        vKey2.SetActive(false);

        GameObject vKey3 = PlaceModel(shelfObj, "PlacedKey_3", "key", new Vector3(0.45f, 0.6f, 0.1f), 90f);
        vKey3.transform.localScale = Vector3.one * 0.7f;
        vKey3.SetActive(false);

        shelfComp.placedKeyVisuals = new GameObject[] { vKey1, vKey2, vKey3 };

        // Label teks di atas rak
        GameObject labelObj = new GameObject("ShelfLabel");
        labelObj.transform.SetParent(shelfObj.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 3.1f, 0f);
        labelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = "RAK KUNCI\n<color=#ffcc00>0 / 3 Terpasang</color>";
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 36;
        tm.characterSize = 0.08f;
        tm.fontStyle = FontStyle.Bold;
        shelfComp.shelfTextMesh = tm;

        manager.shelf = shelfComp;

        // 2. Buat 3 Kunci Kolektibel di Hall 2 (Di balik pilar & pojok ruangan)
        // Key 1: Di balik pilar barat laut (33.5, 0.4, 20.5)
        GameObject k1 = PlaceModel(root, "Key_Hall2_NorthWest", "key", new Vector3(33.5f, 0.45f, 20.5f), 0f);
        SetupKeyItem(k1, 1);
        manager.keyItems.Add(k1.GetComponent<Hall2KeyItem>());

        // Key 2: Di pojok tenggara (49.5, 0.4, 6.5)
        GameObject k2 = PlaceModel(root, "Key_Hall2_SouthEast", "key", new Vector3(49.5f, 0.45f, 6.5f), 45f);
        SetupKeyItem(k2, 2);
        manager.keyItems.Add(k2.GetComponent<Hall2KeyItem>());

        // Key 3: Di balik pilar timur laut (48.5, 0.4, 21.0)
        GameObject k3 = PlaceModel(root, "Key_Hall2_NorthEast", "key", new Vector3(48.5f, 0.45f, 21.0f), 90f);
        SetupKeyItem(k3, 3);
        manager.keyItems.Add(k3.GetComponent<Hall2KeyItem>());

        // 3. Buat Penghalang Gerbang Keluar Hall 2 (Menuju Gate 2A dan 2B)
        // Koridor 2A: X = 34, Z = 4.1
        GameObject barA = PlaceModel(root, "Barrier_Hall2_ExitA", "barrier", new Vector3(34f, 0f, 4.1f), 0f);
        SetupExitBarrier(barA, "Gerbang 2A Terkunci");
        manager.exitBarrierA = barA;

        // Koridor 2B: X = 48, Z = 4.1
        GameObject barB = PlaceModel(root, "Barrier_Hall2_ExitB", "barrier", new Vector3(48f, 0f, 4.1f), 0f);
        SetupExitBarrier(barB, "Gerbang 2B Terkunci");
        manager.exitBarrierB = barB;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#00ff88>[Hall2Challenge] ✅ Setup Tantangan Hall 2 (Rak, 3 Kunci, Penghalang Gerbang) berhasil dibuat!</color>");
        if (!silent)
        {
            EditorUtility.DisplayDialog("Setup Tantangan Hall 2 Selesai!",
                "Tantangan Hall 2 Berhasil Dikonfigurasi:\n\n" +
                "• Rak (Shelves) diletakkan di dinding utara Hall 2 (41, 0, 23.2)\n" +
                "• 3 Kunci diletakkan di balik pilar & pojok Hall 2\n" +
                "• Gerbang keluar menuju Gate 2A & 2B dikunci sampai 3 kunci diletakkan di rak\n" +
                "• Enemy Goblin dikonfigurasi bergerak lambat dengan FOV lebar 140°.",
                "Mantap!");
        }
    }

    static void SetupKeyItem(GameObject keyObj, int index)
    {
        keyObj.tag = "Item";
        keyObj.transform.localScale = Vector3.one * 1.35f;

        BoxCollider col = keyObj.GetComponent<BoxCollider>();
        if (col == null) col = keyObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1.2f, 1.2f, 1.2f);
        col.center = Vector3.zero;

        Hall2KeyItem keyComp = keyObj.AddComponent<Hall2KeyItem>();
        keyComp.keyIndex = index;
        keyComp.keyName = $"Kunci Hall 2 #{index}";
    }

    static void SetupExitBarrier(GameObject barrierObj, string label)
    {
        barrierObj.transform.localScale = new Vector3(1.8f, 1.8f, 1.2f);

        BoxCollider col = barrierObj.GetComponent<BoxCollider>();
        if (col == null) col = barrierObj.AddComponent<BoxCollider>();
        col.isTrigger = false; // Solid barrier sampai challenge selesai!

        // Floating locked rune text
        GameObject textObj = new GameObject("LockedText");
        textObj.transform.SetParent(barrierObj.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 1.8f, -0.2f);
        textObj.transform.localRotation = Quaternion.identity;

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = $"[TERKUNCI]\n{label}\nLetakkan 3 Kunci di Rak";
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 32;
        tm.characterSize = 0.08f;
        tm.fontStyle = FontStyle.Bold;
        tm.color = new Color(1f, 0.3f, 0.3f);
    }

    static GameObject PlaceModel(GameObject parent, string goName, string assetName, Vector3 pos, float rotY = 0f)
    {
        string path = $"{KAYKIT_PATH}/{assetName}.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = goName;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"{goName}_PLACEHOLDER";
        }
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        return go;
    }
}
