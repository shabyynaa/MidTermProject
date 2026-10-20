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
        if (challengeRoot == null || challengeRoot.Find("BossArena_Manager") == null)
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

        Debug.Log("<color=#ffd700>[BossArenaSetup] ✅ Setup Boss Arena (Grand Boss Bar, Altar Victory Chest, & Maw J Laygo) berhasil dibuat!</color>");
        if (!silent)
        {
            EditorUtility.DisplayDialog("Setup Boss Arena Selesai!",
                "Boss Arena Berhasil Dikonfigurasi:\n\n" +
                "• Grand Boss Health Bar tampil di atas layar: Maw J Laygo (280 HP)\n" +
                "• Fase 1: Serangan cakar beruntun savage\n" +
                "• Fase 2 (HP <= 50%): Enraged Blood Moon (Kecepatan melonjak & ATK meningkat!)\n" +
                "• Saat Boss mati: Peti Emas di Altar memancarkan cahaya kemenangan!\n" +
                "• Buka Peti Emas untuk menampilkan Layar Game Clear (Statistik & Tombol Restart)!",
                "Luar Biasa!");
        }
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