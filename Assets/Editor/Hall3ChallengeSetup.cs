using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Tool Setup Otomatis untuk Tantangan Hall 3: "The Torchbearer & Shadow Hunter"
/// Menu: Tools → Setup Hall 3 Challenge (Braziers & Vampire Light)
/// </summary>
[InitializeOnLoad]
public class Hall3ChallengeSetup : EditorWindow
{
    const string KAYKIT_PATH = "Assets/Art/Kaykit_Dungeon/fbx(unity)";

    static Hall3ChallengeSetup()
    {
        EditorApplication.delayCall += AutoEnsureHall3Challenge;
    }

    static void AutoEnsureHall3Challenge()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null) return;

        Transform challengeRoot = arena.transform.Find("Hall3_Challenge");
        if (challengeRoot == null || challengeRoot.Find("Brazier_Hall3_1") == null)
        {
            SetupHall3ChallengeInternal(silent: true);
        }
    }

    [MenuItem("Tools/Setup Hall 3 Challenge (Braziers & Vampire Light)")]
    public static void SetupHall3ChallengeMenu()
    {
        SetupHall3ChallengeInternal(silent: false);
    }

    public static void SetupHall3ChallengeInternal(bool silent)
    {
        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null)
        {
            if (!silent) EditorUtility.DisplayDialog("Error", "DungeonArena tidak ditemukan di scene!", "OK");
            return;
        }

        Transform existing = arena.transform.Find("Hall3_Challenge");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        GameObject root = new GameObject("Hall3_Challenge");
        root.transform.SetParent(arena.transform, false);

        Hall3ChallengeManager manager = root.AddComponent<Hall3ChallengeManager>();

        // 1. Buat 3 Tungku Api / Braziers di Hall 3
        // Brazier 1: Sisi Timur Laut / Dekat Pilar Atas (45, 0, -7)
        GameObject b1 = CreateBrazier(root, "Brazier_Hall3_1", 1, "Tungku Api Timur", new Vector3(45f, 0f, -7f));
        manager.braziers.Add(b1.GetComponent<Hall3Brazier>());

        // Brazier 2: Pusat / Tengah Hall 3 (39, 0, -14)
        GameObject b2 = CreateBrazier(root, "Brazier_Hall3_2", 2, "Tungku Api Pusat", new Vector3(39f, 0f, -14f));
        manager.braziers.Add(b2.GetComponent<Hall3Brazier>());

        // Brazier 3: Sisi Barat Daya / Dekat Akses Gerbang Keluar (35, 0, -21)
        GameObject b3 = CreateBrazier(root, "Brazier_Hall3_3", 3, "Tungku Api Barat", new Vector3(35f, 0f, -21f));
        manager.braziers.Add(b3.GetComponent<Hall3Brazier>());

        // 2. Buat Penghalang Gerbang Keluar Hall 3 (Menuju Gate 3A & 3B)
        // Koridor 3A: X = 29.8, Z = -8
        GameObject bar3A = PlaceModel(root, "Barrier_Hall3_ExitA", "barrier", new Vector3(29.8f, 0f, -8f), 90f);
        SetupExitBarrier(bar3A, "Gerbang 3A Terkunci");
        manager.exitBarrier3A = bar3A;

        // Koridor 3B: X = 29.8, Z = -20
        GameObject bar3B = PlaceModel(root, "Barrier_Hall3_ExitB", "barrier", new Vector3(29.8f, 0f, -20f), 90f);
        SetupExitBarrier(bar3B, "Gerbang 3B Terkunci");
        manager.exitBarrier3B = bar3B;

        // 3. Hubungkan Vampire Enemy
        foreach (var ai in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (ai != null && (ai.roomPrefix == "H3_" || ai.name.ToLower().Contains("vampire")))
            {
                manager.vampireEnemy = ai;
                ai.maxHP = 100f;
                ai.currentHP = 100f;
                ai.chaseSpeed = 6.2f;
                ai.patrolSpeed = 3.0f;
                ai.attackDamage = 12f;
                EditorUtility.SetDirty(ai);
                break;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#ff66aa>[Hall3Challenge] ✅ Setup Tantangan Hall 3 (3 Tungku Api, Barrier Gerbang 3A/3B, Vampire AI) berhasil dibuat!</color>");
        if (!silent)
        {
            EditorUtility.DisplayDialog("Setup Tantangan Hall 3 Selesai!",
                "Tantangan Hall 3 Berhasil Dikonfigurasi:\n\n" +
                "• 3 Tungku Api (Braziers) diletakkan di Hall 3 dengan radius Safe Zone 2.0m\n" +
                "• Vampire agresif di kegelapan dengan Life Steal, tetapi takut mendekati api\n" +
                "• Gerbang keluar menuju Gate 3A & 3B terkunci sampai 3 tungku dinyalakan\n" +
                "• Ketika 3 tungku menyala, ruangan terang dan gerbang terbuka!",
                "Mantap!");
        }
    }

    static GameObject CreateBrazier(GameObject parent, string name, int index, string brazierName, Vector3 worldPos)
    {
        GameObject brazierObj = new GameObject(name);
        brazierObj.transform.SetParent(parent.transform, false);
        brazierObj.transform.position = worldPos;

        // Pedestal tiang batu menggunakan model column
        GameObject pedestal = PlaceModel(brazierObj, "Pedestal", "column", Vector3.zero, 0f);
        pedestal.transform.localScale = new Vector3(1f, 1f, 1f);

        // Obor di atas tiang
        GameObject torch = PlaceModel(brazierObj, "TorchTop", "torch_lit", new Vector3(0f, 1.25f, 0f), 0f);
        torch.transform.localScale = Vector3.one * 1.3f;

        // Collider interaksi
        BoxCollider col = brazierObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(1.8f, 2.5f, 1.8f);
        col.center = new Vector3(0f, 1.25f, 0f);

        Hall3Brazier brazierComp = brazierObj.AddComponent<Hall3Brazier>();
        brazierComp.brazierIndex = index;
        brazierComp.brazierName = brazierName;
        brazierComp.safeRadius = 2.0f;
        brazierComp.isLit = false;

        // Point Light api obor
        GameObject lightObj = new GameObject("FlameLight");
        lightObj.transform.SetParent(brazierObj.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        Light flameLight = lightObj.AddComponent<Light>();
        flameLight.type = LightType.Point;
        flameLight.color = new Color(1f, 0.62f, 0.18f);
        flameLight.range = 3.5f;
        flameLight.intensity = 0f;
        flameLight.enabled = false;
        flameLight.shadows = LightShadows.None;
        brazierComp.flameLight = flameLight;

        // Visual api diaktifkan saat nyala
        brazierComp.flameVisual = torch;

        // Label 3D Text
        GameObject labelObj = new GameObject("StatusLabel");
        labelObj.transform.SetParent(brazierObj.transform, false);
        labelObj.transform.localPosition = new Vector3(0f, 2.5f, 0f);
        labelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        TextMesh tm = labelObj.AddComponent<TextMesh>();
        tm.text = "<color=#888888>TUNGKU API [PADAM]</color>\n<size=22><color=#ffcc00>[Klik Kiri / E] Nyalakan</color></size>";
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 32;
        tm.characterSize = 0.08f;
        tm.fontStyle = FontStyle.Bold;
        brazierComp.statusTextMesh = tm;

        // Lingkaran visual batas Safe Zone di lantai menggunakan LineRenderer
        GameObject circleObj = new GameObject("SafeZoneCircle");
        circleObj.transform.SetParent(brazierObj.transform, false);
        circleObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);

        LineRenderer lr = circleObj.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.positionCount = 40;
        lr.startWidth = 0.09f;
        lr.endWidth = 0.09f;

        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = new Color(1f, 0.65f, 0.15f, 0.65f);
        lr.material = lineMat;
        lr.startColor = new Color(1f, 0.65f, 0.15f, 0.65f);
        lr.endColor = new Color(1f, 0.65f, 0.15f, 0.65f);

        float radius = brazierComp.safeRadius;
        for (int i = 0; i < 40; i++)
        {
            float angle = i * (Mathf.PI * 2f / 40);
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
        lr.enabled = false;
        brazierComp.safeZoneCircle = lr;

        return brazierObj;
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
        tm.text = $"<color=#ff2244>[TERKUNCI]\n{label}</color>\n<size=20><color=#ffaa00>Nyalakan 3 Tungku Api!</color></size>";
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
