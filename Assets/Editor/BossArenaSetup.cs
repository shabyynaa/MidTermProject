using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Tool Setup Otomatis untuk Boss Arena: "The Abyssal Apex"
/// Menu: Tools → Setup Boss Arena (Encounter, Boss Bar, & Victory Altar)
/// </summary>
[InitializeOnLoad]
public class BossArenaSetup : EditorWindow
{
    const string KAYKIT_PATH = "Assets/Art/Kaykit_Dungeon/fbx(unity)";

    static BossArenaSetup()
    {
        EditorApplication.delayCall += AutoEnsureBossArena;
    }

    static void AutoEnsureBossArena()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null) return;

        Transform challengeRoot = arena.transform.Find("BossArena_Challenge");
        if (challengeRoot == null || challengeRoot.Find("Barrier_Boss_EntranceA") == null)
        {
            SetupBossArenaInternal(silent: true);
        }
    }

    [MenuItem("Tools/Setup Boss Arena (Encounter, Boss Bar, & Victory Altar)")]
    public static void SetupBossArenaMenu()
    {
        SetupBossArenaInternal(silent: false);
    }

    public static void SetupBossArenaInternal(bool silent)
    {
        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null)
        {
            if (!silent) EditorUtility.DisplayDialog("Error", "DungeonArena tidak ditemukan di scene!", "OK");
            return;
        }

        Transform existing = arena.transform.Find("BossArena_Challenge");
        if (existing != null)
        {
            DestroyImmediate(existing.gameObject);
        }

        GameObject root = new GameObject("BossArena_Challenge");
        root.transform.SetParent(arena.transform, false);

        BossArenaManager manager = root.AddComponent<BossArenaManager>();

        // 1. Buat Barrier Lockdown Pintu Masuk Boss Arena (dari Koridor 4A & 4B)
        // Koridor 4A: X = -8.2, Z = -8
        GameObject bar4A = PlaceModel(root, "Barrier_Boss_EntranceA", "barrier", new Vector3(-8.2f, 0f, -8f), 90f);
        SetupLockdownBarrier(bar4A, "Pintu Arena 4A");
        bar4A.SetActive(false); // Default mati sampai pemain masuk
        manager.entranceBarrierA = bar4A;

        // Koridor 4B: X = -8.2, Z = -20
        GameObject bar4B = PlaceModel(root, "Barrier_Boss_EntranceB", "barrier", new Vector3(-8.2f, 0f, -20f), 90f);
        SetupLockdownBarrier(bar4B, "Pintu Arena 4B");
        bar4B.SetActive(false); // Default mati sampai pemain masuk
        manager.entranceBarrierB = bar4B;

        // 2. Setup Peti Kemenangan Emas di Altar Tengah (-26, 1.0, -14)
        GameObject chestObj = GameObject.Find("Altar_Chest");
        if (chestObj != null)
        {
            BoxCollider col = chestObj.GetComponent<BoxCollider>();
            if (col == null) col = chestObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2.2f, 1.8f, 2.2f);
            col.center = new Vector3(0f, 0.4f, 0f);

            VictoryChest vc = chestObj.GetComponent<VictoryChest>();
            if (vc == null) vc = chestObj.AddComponent<VictoryChest>();

            // Point Light emas berkilauan
            Transform existingLight = chestObj.transform.Find("GoldenGlowLight");
            Light gl;
            if (existingLight != null)
            {
                gl = existingLight.GetComponent<Light>();
            }
            else
            {
                GameObject lObj = new GameObject("GoldenGlowLight");
                lObj.transform.SetParent(chestObj.transform, false);
                lObj.transform.localPosition = new Vector3(0f, 0.6f, 0f);
                gl = lObj.AddComponent<Light>();
            }
            gl.type = LightType.Point;
            gl.color = new Color(1f, 0.85f, 0.2f);
            gl.range = 4.5f;
            gl.intensity = 2.5f;
            gl.enabled = false;
            vc.goldenGlowLight = gl;

            // 3D Floating Status Text
            Transform existingLabel = chestObj.transform.Find("ChestStatusLabel");
            TextMesh tm;
            if (existingLabel != null)
            {
                tm = existingLabel.GetComponent<TextMesh>();
            }
            else
            {
                GameObject labelObj = new GameObject("ChestStatusLabel");
                labelObj.transform.SetParent(chestObj.transform, false);
                labelObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                labelObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                tm = labelObj.AddComponent<TextMesh>();
            }
            tm.text = "<color=#888888>PETI EMAS ALTAR [TERSEGEL]</color>\n<size=20><color=#ff4444>Kalahkan Maw J Laygo!</color></size>";
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.fontSize = 28;
            tm.characterSize = 0.08f;
            tm.fontStyle = FontStyle.Bold;
            vc.statusTextMesh = tm;

            manager.victoryChest = vc;
            EditorUtility.SetDirty(chestObj);
        }

        // 3. Hubungkan Boss Enemy (Maw J Laygo)
        foreach (var ai in Object.FindObjectsByType<EnemyAI>())
        {
            if (ai != null && (ai.isBoss || ai.name.ToLower().Contains("maw") || ai.name.ToLower().Contains("boss")))
            {
                manager.bossEnemy = ai;
                ai.isBoss = true;
                ai.maxHP = 280f;
                ai.currentHP = 280f;
                ai.attackDamage = 14f;
                ai.attackRadius = 2.8f;
                ai.attackCooldown = 1.6f;
                ai.patrolSpeed = 3.0f;
                ai.chaseSpeed = 5.4f;
                ai.fleeHPThreshold = 0f; // Tidak pernah kabur!
                ai.detectRadius = 20f;
                EditorUtility.SetDirty(ai);
                break;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#ffd700>[BossArenaSetup] ✅ Setup Boss Arena (Encounter Lockdown, Grand Boss Bar, Altar Victory Chest, & Maw J Laygo) berhasil dibuat!</color>");
        if (!silent)
        {
            EditorUtility.DisplayDialog("Setup Boss Arena Selesai!",
                "Boss Arena Berhasil Dikonfigurasi:\n\n" +
                "• Gerbang masuk otomatis mengunci saat pemain melangkah masuk (Lockdown)\n" +
                "• Grand Boss Health Bar tampil di atas layar: Maw J Laygo (280 HP)\n" +
                "• Fase 1: Serangan cakar beruntun savage\n" +
                "• Fase 2 (HP <= 50%): Enraged Blood Moon (Kecepatan melonjak & ATK meningkat!)\n" +
                "• Saat Boss mati: Pintu terbuka, Peti Emas di Altar memancarkan cahaya kemenangan!\n" +
                "• Buka Peti Emas untuk menampilkan Layar Game Clear (Statistik & Tombol Restart)!",
                "Luar Biasa!");
        }
    }

    static void SetupLockdownBarrier(GameObject barObj, string label)
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
        tm.text = $"<color=#ff2244>[ARENA TERKUNCI]\n{label}</color>\n<size=20><color=#ffaa00>Kalahkan Maw J Laygo!</color></size>";
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
