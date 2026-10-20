using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Tool Setup Otomatis untuk Tantangan Hall 4: "The Colossus & The Rune Pressure Plates"
/// Menu: Tools → Setup Hall 4 Challenge (Rune Plates & Pumpkin Hulk)
/// </summary>
[InitializeOnLoad]
public class Hall4ChallengeSetup : EditorWindow
{
    const string KAYKIT_PATH = "Assets/Art/Kaykit_Dungeon/fbx(unity)";

    static Hall4ChallengeSetup()
    {
        EditorApplication.delayCall += AutoEnsureHall4Challenge;
    }

    static void AutoEnsureHall4Challenge()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null) return;

        Transform challengeRoot = arena.transform.Find("Hall4_Challenge");
        if (challengeRoot == null || challengeRoot.Find("Plate_Hall4_1") == null)
        {
            SetupHall4ChallengeInternal(silent: true);
        }
    }

    [MenuItem("Tools/Setup Hall 4 Challenge (Rune Plates & Pumpkin Hulk)")]
    public static void SetupHall4ChallengeMenu()
    {
        SetupHall4ChallengeInternal(silent: false);
    }

    public static void SetupHall4ChallengeInternal(bool silent)
    {
        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null)
        {
            if (!silent) EditorUtility.DisplayDialog("Error", "DungeonArena tidak ditemukan di scene!", "OK");
            return;
        }

        Transform existing = arena.transform.Find("Hall4_Challenge");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        GameObject root = new GameObject("Hall4_Challenge");
        root.transform.SetParent(arena.transform, false);

        Hall4ChallengeManager manager = root.AddComponent<Hall4ChallengeManager>();

        // 1. Buat 3 Pelat Tekanan Runa (Pressure Plates) di Hall 4
        // Plate 1: Barat Laut / Dekat Koridor 4A (6, 0, -8)
        GameObject p1 = CreatePressurePlate(root, "Plate_Hall4_1", 1, "Pelat Runa Alfa", new Vector3(6f, 0f, -8f));
        manager.pressurePlates.Add(p1.GetComponent<Hall4PressurePlate>());

        // Plate 2: Tengah Timur / Antara Dua Pilar (14, 0, -14)
        GameObject p2 = CreatePressurePlate(root, "Plate_Hall4_2", 2, "Pelat Runa Beta", new Vector3(14f, 0f, -14f));
        manager.pressurePlates.Add(p2.GetComponent<Hall4PressurePlate>());

        // Plate 3: Barat Daya / Dekat Koridor 4B (6, 0, -20)
        GameObject p3 = CreatePressurePlate(root, "Plate_Hall4_3", 3, "Pelat Runa Gamma", new Vector3(6f, 0f, -20f));
        manager.pressurePlates.Add(p3.GetComponent<Hall4PressurePlate>());

        // 2. Buat Penghalang Gerbang Keluar Menuju Koridor 4A & 4B (Akses Boss Arena)
        // Koridor 4A: X = 0, Z = -8
        GameObject bar4A = PlaceModel(root, "Barrier_Hall4_ExitA", "barrier", new Vector3(0f, 0f, -8f), 90f);
        SetupExitBarrier(bar4A, "Gerbang 4A Tersegel");
        manager.exitBarrier4A = bar4A;

        // Koridor 4B: X = 0, Z = -20
        GameObject bar4B = PlaceModel(root, "Barrier_Hall4_ExitB", "barrier", new Vector3(0f, 0f, -20f), 90f);
        SetupExitBarrier(bar4B, "Gerbang 4B Tersegel");
        manager.exitBarrier4B = bar4B;

        // 3. Hubungkan Pumpkin Hulk Enemy
        foreach (var ai in Object.FindObjectsByType<EnemyAI>())
        {
            if (ai != null && (ai.roomPrefix == "H4_" || ai.name.ToLower().Contains("pumpkin")))
            {
                manager.pumpkinHulkEnemy = ai;
                ai.maxHP = 160f;
                ai.currentHP = 160f;
                ai.chaseSpeed = 4.8f;
                ai.patrolSpeed = 2.4f;
                ai.attackDamage = 20f;
                ai.attackRadius = 2.8f;
                ai.hasArmor = true;
                EditorUtility.SetDirty(ai);
                break;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#00e5ff>[Hall4Challenge] ✅ Setup Tantangan Hall 4 (3 Pelat Runa, Barrier 4A/4B, Pumpkin Hulk AI) berhasil dibuat!</color>");
        if (!silent)
        {
            EditorUtility.DisplayDialog("Setup Tantangan Hall 4 Selesai!",
                "Tantangan Hall 4 Berhasil Dikonfigurasi:\n\n" +
                "• 3 Pelat Tekanan Runa (Pressure Plates) di Hall 4 (Alfa, Beta, Gamma)\n" +
                "• Injak dan tahan tiap pelat selama 2.5 detik untuk menguncinya\n" +
                "• Pumpkin Hulk mengejar dengan armor tebal dan skill Bull Charge\n" +
                "• Pancing serudukan Hulk menabrak pilar batu untuk membuatnya ter-STUN 3.5 detik!\n" +
                "• Mengunci 3 pelat akan membuka gerbang menuju Boss Arena dan memecahkan armor Hulk!",
                "Keren!");
        }
    }

    static GameObject CreatePressurePlate(GameObject parent, string name, int index, string plateName, Vector3 worldPos)
    {
        GameObject plateObj = new GameObject(name);
        plateObj.transform.SetParent(parent.transform, false);
        plateObj.transform.position = worldPos;

        // 1. Base pedestal menggunakan ornamen lantai batu
        GameObject baseModel = PlaceModel(plateObj, "StoneBase", "floor_tile_small_decorated", Vector3.zero, 0f);
        baseModel.transform.localScale = new Vector3(1f, 0.45f, 1f);

        // 2. Sinking Pad (Pelat tengah yang turun saat diinjak)
        GameObject padObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        padObj.name = "SinkingPad";
        padObj.transform.SetParent(plateObj.transform, false);
        padObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
        padObj.transform.localScale = new Vector3(1.5f, 0.04f, 1.5f);

        // Hapus collider bawaan silinder agar tidak mengganggu trigger
        Collider padCol = padObj.GetComponent<Collider>();
        if (padCol != null) DestroyImmediate(padCol);

        // Material batu pad
        Renderer padRend = padObj.GetComponent<Renderer>();
        if (padRend != null)
        {
            Material padMat = new Material(Shader.Find("Standard"));
            padMat.color = new Color(0.25f, 0.28f, 0.35f);
            padMat.SetFloat("_Metallic", 0.3f);
            padMat.SetFloat("_Glossiness", 0.5f);
            padRend.material = padMat;
        }

        // 3. Trigger BoxCollider utama
        BoxCollider col = plateObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(2.2f, 2.0f, 2.2f);
        col.center = new Vector3(0f, 1.0f, 0f);

        Hall4PressurePlate plateComp = plateObj.AddComponent<Hall4PressurePlate>();
        plateComp.plateIndex = index;
        plateComp.plateName = plateName;
        plateComp.sinkingPad = padObj.transform;

        // 4. Point Light energi magis
        GameObject lightObj = new GameObject("PlateLight");
        lightObj.transform.SetParent(plateObj.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light pl = lightObj.AddComponent<Light>();
        pl.type = LightType.Point;
        pl.color = new Color(0.15f, 0.75f, 1f);
        pl.range = 2.8f;
        pl.intensity = 1.2f;
        pl.shadows = LightShadows.None;
        plateComp.plateLight = pl;

        // 5. 3D Floating Status Text
        GameObject labelObj = new GameObject("StatusLabel");
        labelObj.transform.SetParent(plateObj.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 2.2f, 0f);
        labelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = $"<color=#00e5ff><b>{plateName.ToUpper()}</b></color>\n<size=20><color=#dddddd>[Injak & Tahan 2.5 Detik]</color></size>";
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 30;
        tm.characterSize = 0.08f;
        tm.fontStyle = FontStyle.Bold;
        plateComp.statusTextMesh = tm;

        // 6. Lingkaran visual batas pelat di lantai (LineRenderer)
        GameObject circleObj = new GameObject("RuneCircle");
        circleObj.transform.SetParent(plateObj.transform, false);
        circleObj.transform.localPosition = new Vector3(0f, 0.12f, 0f);

        LineRenderer lr = circleObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 40;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;

        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = new Color(0.15f, 0.75f, 1f, 0.65f);
        lr.material = lineMat;
        lr.startColor = lineMat.color;
        lr.endColor = lineMat.color;

        float radius = 1.1f;
        for (int i = 0; i < 40; i++)
        {
            float angle = i * (Mathf.PI * 2f / 40);
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
        plateComp.runeCircle = lr;

        return plateObj;
    }

    static void SetupExitBarrier(GameObject barObj, string label)
    {
        barObj.transform.localScale = new Vector3(1.1f, 1.4f, 1.1f);

        BoxCollider col = barObj.GetComponent<BoxCollider>();
        if (col == null) col = barObj.AddComponent<BoxCollider>();
        col.size = new Vector3(2.8f, 2.5f, 0.8f);
        col.center = new Vector3(0f, 1.25f, 0f);

        GameObject labelObj = new GameObject("BarrierLabel");
        labelObj.transform.SetParent(barObj.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 2.3f, 0f);
        labelObj.transform.localRotation = Quaternion.identity;

        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = $"<color=#00e5ff>[TERKUNCI]\n{label}</color>\n<size=20><color=#ffdd00>Kunci 3 Pelat Runa!</color></size>";
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 28;
        tm.characterSize = 0.08f;
        tm.fontStyle = FontStyle.Bold;
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
            go.name = goName;
        }

        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        return go;
    }
}
