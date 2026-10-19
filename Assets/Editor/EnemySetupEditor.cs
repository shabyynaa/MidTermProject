using UnityEngine;
using UnityEngine.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Editor Tool Otomatis untuk Integrasi 4 Enemy Mixamo (Enemy01–Enemy04):
/// - Start Room: 0 enemy
/// - Hall 1: 0 enemy
/// - Hall 2: 1 regular enemy (Enemy03 - Goblin)
/// - Hall 3: 1 regular enemy (Enemy02 - Vampire A Lusth)
/// - Hall 4: 1 regular enemy (Enemy04 - Pumpkinhulk L Shaw)
/// - Boss Arena: 1 boss enemy (Enemy01 - Maw J Laygo)
///
/// Mengatur ekstraksi tekstur, material URP Lit, Animator Controller (Idle, Walk, Run, Flee, Attack, Death),
/// skala proporsional, penempatan kaki di lantai NavMesh, Collider, NavMeshAgent, dan EnemyAI.
/// </summary>
[InitializeOnLoad]
public class EnemySetupEditor : EditorWindow
{
    static double playModeStartTime = 0;

    static EnemySetupEditor()
    {
        EditorApplication.delayCall += AutoEnsureEnemiesSetup;
        EditorApplication.update += OnEditorUpdateVerifyPlayMode;
    }

    static void OnEditorUpdateVerifyPlayMode()
    {
        if (!EditorApplication.isPlaying) return;
        if (!SessionState.GetBool("EnemyScaleAutoPlayVerifyPending", false)) return;

        if (playModeStartTime == 0)
        {
            playModeStartTime = EditorApplication.timeSinceStartup;
            return;
        }

        if (EditorApplication.timeSinceStartup - playModeStartTime >= 1.5)
        {
            SessionState.SetBool("EnemyScaleAutoPlayVerifyPending", false);
            playModeStartTime = 0;

            GameObject player = GameObject.FindWithTag("Player");
            float pScale = player != null ? player.transform.lossyScale.y : 1.3f;
            Debug.Log($"<color=#00ff88>[PlayMode Verify]</color> Player lossyScale.y = {pScale:F2}");

            foreach (EnemyAI ai in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
            {
                Transform vis = ai.transform.Find("Visual_Model");
                float vScale = vis != null ? vis.localScale.y : ai.transform.localScale.y;
                NavMeshAgent ag = ai.GetComponent<NavMeshAgent>();
                bool onNav = ag != null && ag.isOnNavMesh;
                bool hasAnim = ai.animator != null && ai.animator.runtimeAnimatorController != null;
                Debug.Log($"<color=#00ff88>[PlayMode Verify]</color> {ai.name} | VisualScale={vScale:F2} ({(vScale / pScale):F2}x Player) | Pos={ai.transform.position} | OnNavMesh={onNav} | State={ai.currentState} | AnimatorOK={hasAnim}");
            }

            EditorApplication.isPlaying = false;
        }
    }

    static void AutoEnsureEnemiesSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null) return;

        GameObject eH2 = GameObject.Find("Enemy_Hall2_Goblin");
        GameObject eH3 = GameObject.Find("Enemy_Hall3_Vampire");
        GameObject eH4 = GameObject.Find("Enemy_Hall4_Pumpkinhulk");
        GameObject eBoss = GameObject.Find("Enemy_Boss_MawJLaygo");
        GameObject oldSkel = GameObject.Find("Skeleton_Warrior");
        GameObject oldRogue = GameObject.Find("Rogue");

        EnemyAI[] allEnemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        bool needsFullSetup = (eH2 == null || eH3 == null || eH4 == null || eBoss == null ||
                               oldSkel != null || oldRogue != null || allEnemies.Length != 4);

        if (needsFullSetup)
        {
            Debug.Log("<color=#00ff88>[EnemySetupEditor] Mengintegrasikan 4 Mixamo Enemy (Hall 2, Hall 3, Hall 4, Boss Arena)...</color>");
            SetupAllEnemiesInternal(silent: true);
            return;
        }

        // Cek apakah skala visual enemy masih ukuran kecil (< 1.6x) dibanding Player (1.3x)
        Transform vH2 = eH2.transform.Find("Visual_Model");
        if (vH2 != null && vH2.localScale.x < 1.6f)
        {
            Debug.Log("<color=#00ff88>[EnemySetupEditor] Memperbesar skala visual keempat Enemy agar lebih besar & gagah dari Player...</color>");
            AdjustExistingEnemyScales();
            SessionState.SetBool("EnemyScaleAutoPlayVerifyPending", true);
            playModeStartTime = 0;
            EditorApplication.isPlaying = true;
        }

        // Pastikan RoomBoundaries tersedia di DungeonArena dan terhubung ke masing-masing EnemyAI
        bool boundariesUpdated = false;
        if (arena.transform.Find("RoomBoundaries") == null)
        {
            BuildDungeonArena.BuildRoomBoundaries(arena);
            boundariesUpdated = true;
        }

        foreach (EnemyAI ai in allEnemies)
        {
            if (ai != null && ai.roomBoundaryCollider == null)
            {
                ai.FindSceneReferences();
                EditorUtility.SetDirty(ai);
                boundariesUpdated = true;
            }
        }

        if (boundariesUpdated)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("<color=#00ff88>[EnemySetupEditor] ✅ RoomBoundaries untuk Hall 2, Hall 3, Hall 4, dan Boss Arena telah dihubungkan ke keempat Enemy!</color>");
        }
    }

    [MenuItem("Tools/Adjust Enemy Scales (Larger Than Player)")]
    public static void AdjustExistingEnemyScales()
    {
        ApplyScaleToExistingEnemy("Enemy_Hall2_Goblin", 1.75f, 3.1f, 0.55f, 1.8f, 2.6f);
        ApplyScaleToExistingEnemy("Enemy_Hall3_Vampire", 1.85f, 3.3f, 0.55f, 1.8f, 2.6f);
        ApplyScaleToExistingEnemy("Enemy_Hall4_Pumpkinhulk", 1.95f, 3.5f, 0.65f, 2.0f, 2.8f);
        ApplyScaleToExistingEnemy("Enemy_Boss_MawJLaygo", 2.40f, 4.3f, 0.75f, 2.4f, 3.2f);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("<color=#00ff88>[EnemySetupEditor] ✅ Skala visual keempat Enemy berhasil diperbesar (Regular: 1.75x–1.95x, Boss: 2.40x)!</color>");
    }

    static void ApplyScaleToExistingEnemy(string enemyName, float visualScale, float height, float radius, float stoppingDist, float attackRad)
    {
        GameObject enemyRoot = GameObject.Find(enemyName);
        if (enemyRoot == null) return;

        enemyRoot.transform.localScale = Vector3.one;
        Vector3 pos = enemyRoot.transform.position;
        pos.y = 0f;
        enemyRoot.transform.position = pos;

        Transform visual = enemyRoot.transform.Find("Visual_Model");
        if (visual != null)
        {
            visual.localScale = new Vector3(visualScale, visualScale, visualScale);
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            EditorUtility.SetDirty(visual);
        }

        CapsuleCollider col = enemyRoot.GetComponent<CapsuleCollider>();
        if (col != null)
        {
            col.height = height;
            col.radius = radius;
            col.center = new Vector3(0f, height * 0.5f, 0f);
            EditorUtility.SetDirty(col);
        }

        NavMeshAgent agent = enemyRoot.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.height = height;
            agent.radius = radius;
            agent.stoppingDistance = stoppingDist;
            agent.baseOffset = 0f;
            EditorUtility.SetDirty(agent);
        }

        EnemyAI ai = enemyRoot.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.attackRadius = attackRad;
            EditorUtility.SetDirty(ai);
        }

        EditorUtility.SetDirty(enemyRoot);
    }

    [MenuItem("Tools/Setup All 4 Mixamo Enemies (Halls 2-4 + Boss)")]
    public static void SetupAllEnemiesMenu()
    {
        SetupAllEnemiesInternal(silent: false);
    }

    public static void SetupAllEnemiesInternal(bool silent)
    {
        // 1. Pastikan Waypoint & SafePoint untuk Hall 2, 3, 4, dan Boss Arena tersedia di DungeonArena/AI_Points
        EnsureAllRoomWaypoints();

        // 2. Siapkan Material URP Lit & Animator Controller untuk keempat folder Enemy01–Enemy04
        // Enemy01: Maw J Laygo (Boss)
        Material matEnemy01 = PrepareEnemyMaterial(
            "Assets/Art/Enemy01",
            "Maw J Laygo.fbx",
            new string[] { "MAW_diffuse.png", "MAW_glow.png", "maria_specular.png", "MAW_normal.png" },
            "MawJLaygo_MAT",
            "MAW_diffuse.png",
            "MAW_normal.png",
            "MAW_glow.png"
        );
        AnimatorController ctrlEnemy01 = CreateEnemyAnimatorController(
            "Assets/Art/Enemy01",
            "Enemy01_MawJLaygo_Controller.controller",
            "Maw J Laygo@Idle.fbx",
            "Maw J Laygo@Walking.fbx",
            "Maw J Laygo@Running.fbx",
            "Maw J Laygo@Standing Melee Combo Attack Ver. 3.fbx",
            "Maw J Laygo@Flying Back Death.fbx",
            attackSpeedMultiplier: 1.0f
        );

        // Enemy02: Vampire A Lusth (Hall 3)
        // Catatan: Folder Enemy02 tidak memiliki file FBX Attack terpisah, sehingga state Attack menggunakan clip terdekat (Running lunge).
        Material matEnemy02 = PrepareEnemyMaterial(
            "Assets/Art/Enemy02",
            "Vampire A Lusth.fbx",
            new string[] { "Vampire_diffuse.png", "Vampire_diffuse_transparent.png", "Vampire_emission.png", "Vampire_specular.png", "Vampire_normal.png" },
            "Vampire_MAT",
            "Vampire_diffuse.png",
            "Vampire_normal.png",
            "Vampire_emission.png"
        );
        AnimatorController ctrlEnemy02 = CreateEnemyAnimatorController(
            "Assets/Art/Enemy02",
            "Enemy02_Vampire_Controller.controller",
            "Vampire A Lusth@Idle.fbx",
            "Vampire A Lusth@Walking.fbx",
            "Vampire A Lusth@Running.fbx",
            "Vampire A Lusth@Running.fbx",
            "Vampire A Lusth@Dying.fbx",
            attackSpeedMultiplier: 1.35f
        );

        // Enemy03: Goblin D Shareyko (Hall 2)
        Material matEnemy03 = PrepareEnemyMaterial(
            "Assets/Art/Enemy03",
            "goblin_d_shareyko.fbx",
            new string[] { "goblin_diffuse.png", "goblin_normal.png", "goblin_specular.png" },
            "Goblin_MAT",
            "goblin_diffuse.png",
            "goblin_normal.png",
            null
        );
        AnimatorController ctrlEnemy03 = CreateEnemyAnimatorController(
            "Assets/Art/Enemy03",
            "Enemy03_Goblin_Controller.controller",
            "goblin_d_shareyko@Idle.fbx",
            "goblin_d_shareyko@Walking.fbx",
            "goblin_d_shareyko@Running.fbx",
            "goblin_d_shareyko@Standing Melee Attack Horizontal.fbx",
            "goblin_d_shareyko@Dying.fbx",
            attackSpeedMultiplier: 1.0f
        );

        // Enemy04: Pumpkinhulk L Shaw (Hall 4)
        Material matEnemy04 = PrepareEnemyMaterial(
            "Assets/Art/Enemy04",
            "Pumpkinhulk L Shaw.fbx",
            new string[] { "pumpkinHulk_diffuse.png", "pumpkinHulk_normal.png" },
            "Pumpkinhulk_MAT",
            "pumpkinHulk_diffuse.png",
            "pumpkinHulk_normal.png",
            null
        );
        AnimatorController ctrlEnemy04 = CreateEnemyAnimatorController(
            "Assets/Art/Enemy04",
            "Enemy04_Pumpkinhulk_Controller.controller",
            "Pumpkinhulk L Shaw@Idle.fbx",
            "Pumpkinhulk L Shaw@Walking.fbx",
            "Pumpkinhulk L Shaw@Running.fbx",
            "Pumpkinhulk L Shaw@Standing Melee Attack Downward.fbx",
            "Pumpkinhulk L Shaw@Walking To Dying.fbx",
            attackSpeedMultiplier: 1.0f
        );

        // 3. Bersihkan seluruh instance enemy lama / duplikat dari scene
        PurgeExistingEnemies();

        // 4. Spawn tepat 4 Enemy pada posisi lantai walkable NavMesh di masing-masing ruangan:
        // Player (Kachujin) memiliki localScale = 1.30x.
        // Regular enemies diatur 1.75x - 1.95x (~1.35x - 1.50x ukuran Player), dan Boss diatur 2.40x (~1.85x ukuran Player).
        // - Start Room: 0 enemy
        // - Hall 1: 0 enemy
        // - Hall 2: 1 regular enemy (Enemy03 - Goblin, Visual Scale 1.75x)
        SpawnConfiguredEnemy(
            enemyName: "Enemy_Hall2_Goblin",
            fbxPath: "Assets/Art/Enemy03/goblin_d_shareyko.fbx",
            material: matEnemy03,
            controller: ctrlEnemy03,
            spawnPos: new Vector3(41f, 0f, 13f),
            rotY: 270f,
            visualScale: 1.75f,
            colliderHeight: 3.1f,
            roomPrefix: "H2_",
            isBoss: false,
            maxHP: 60f,
            fleeHP: 15f,
            attackDamage: 10f,
            detectRadius: 11f,
            attackRadius: 2.6f,
            patrolSpeed: 2.8f,
            chaseSpeed: 5.0f
        );

        // - Hall 3: 1 regular enemy (Enemy02 - Vampire A Lusth, Visual Scale 1.85x)
        SpawnConfiguredEnemy(
            enemyName: "Enemy_Hall3_Vampire",
            fbxPath: "Assets/Art/Enemy02/Vampire A Lusth.fbx",
            material: matEnemy02,
            controller: ctrlEnemy02,
            spawnPos: new Vector3(40f, 0f, -14f),
            rotY: 0f,
            visualScale: 1.85f,
            colliderHeight: 3.3f,
            roomPrefix: "H3_",
            isBoss: false,
            maxHP: 75f,
            fleeHP: 18f,
            attackDamage: 12f,
            detectRadius: 11f,
            attackRadius: 2.6f,
            patrolSpeed: 3.0f,
            chaseSpeed: 5.5f
        );

        // - Hall 4: 1 regular enemy (Enemy04 - Pumpkinhulk L Shaw, Visual Scale 1.95x)
        SpawnConfiguredEnemy(
            enemyName: "Enemy_Hall4_Pumpkinhulk",
            fbxPath: "Assets/Art/Enemy04/Pumpkinhulk L Shaw.fbx",
            material: matEnemy04,
            controller: ctrlEnemy04,
            spawnPos: new Vector3(10f, 0f, -14f),
            rotY: 90f,
            visualScale: 1.95f,
            colliderHeight: 3.5f,
            roomPrefix: "H4_",
            isBoss: false,
            maxHP: 100f,
            fleeHP: 22f,
            attackDamage: 15f,
            detectRadius: 11f,
            attackRadius: 2.8f,
            patrolSpeed: 2.6f,
            chaseSpeed: 4.8f
        );

        // - Boss Arena: 1 boss enemy (Enemy01 - Maw J Laygo, Visual Scale 2.40x)
        SpawnConfiguredEnemy(
            enemyName: "Enemy_Boss_MawJLaygo",
            fbxPath: "Assets/Art/Enemy01/Maw J Laygo.fbx",
            material: matEnemy01,
            controller: ctrlEnemy01,
            spawnPos: new Vector3(-39f, 0f, -14f),
            rotY: 90f,
            visualScale: 2.40f,
            colliderHeight: 4.3f,
            roomPrefix: "",
            isBoss: true,
            maxHP: 180f,
            fleeHP: 30f,
            attackDamage: 22f,
            detectRadius: 16f,
            attackRadius: 3.2f,
            patrolSpeed: 3.0f,
            chaseSpeed: 5.6f
        );

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#00ff88>[EnemySetupEditor] ✅ Keempat Mixamo Enemy berhasil dikonfigurasi dan ditempatkan!</color>");
        if (!silent)
        {
            EditorUtility.DisplayDialog("Setup 4 Enemy Selesai!",
                "Keempat karakter musuh Mixamo telah diintegrasikan:\n\n" +
                "• Start Room & Hall 1: 0 Enemy\n" +
                "• Hall 2: Enemy_Hall2_Goblin (Scale 1.75x)\n" +
                "• Hall 3: Enemy_Hall3_Vampire (Scale 1.85x)\n" +
                "• Hall 4: Enemy_Hall4_Pumpkinhulk (Scale 1.95x)\n" +
                "• Boss Arena: Enemy_Boss_MawJLaygo (Boss Scale 2.40x)\n\n" +
                "Animator Controller, Material URP, Waypoint Patrol, SafePoint, Collider, dan NavMeshAgent sudah siap.",
                "Mantap!");
        }
    }

    static void EnsureAllRoomWaypoints()
    {
        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null) return;

        Transform aiRoot = arena.transform.Find("AI_Points");
        if (aiRoot == null)
        {
            GameObject go = new GameObject("AI_Points");
            go.transform.SetParent(arena.transform);
            go.transform.localPosition = Vector3.zero;
            aiRoot = go.transform;
        }

        EnsurePoint(aiRoot, "PlayerSpawn", new Vector3(-24f, 0f, 14f));

        // Hall 2 Waypoints
        EnsurePoint(aiRoot, "H2_Patrol_W1", new Vector3(36f, 0f, 14f));
        EnsurePoint(aiRoot, "H2_Patrol_W2", new Vector3(46f, 0f, 14f));
        EnsurePoint(aiRoot, "H2_Patrol_W3", new Vector3(46f, 0f, 8f));
        EnsurePoint(aiRoot, "H2_Patrol_W4", new Vector3(36f, 0f, 8f));
        EnsurePoint(aiRoot, "H2_SafePoint", new Vector3(49f, 0f, 21f));

        // Hall 3 Waypoints
        EnsurePoint(aiRoot, "H3_Patrol_W1", new Vector3(35f, 0f, -9f));
        EnsurePoint(aiRoot, "H3_Patrol_W2", new Vector3(43f, 0f, -9f));
        EnsurePoint(aiRoot, "H3_Patrol_W3", new Vector3(43f, 0f, -19f));
        EnsurePoint(aiRoot, "H3_Patrol_W4", new Vector3(35f, 0f, -19f));
        EnsurePoint(aiRoot, "H3_SafePoint", new Vector3(49f, 0f, -21f));

        // Hall 4 Waypoints
        EnsurePoint(aiRoot, "H4_Patrol_W1", new Vector3(5f, 0f, -9f));
        EnsurePoint(aiRoot, "H4_Patrol_W2", new Vector3(13f, 0f, -9f));
        EnsurePoint(aiRoot, "H4_Patrol_W3", new Vector3(13f, 0f, -19f));
        EnsurePoint(aiRoot, "H4_Patrol_W4", new Vector3(5f, 0f, -19f));
        EnsurePoint(aiRoot, "H4_SafePoint", new Vector3(3f, 0f, -21f));

        // Boss Arena Waypoints
        EnsurePoint(aiRoot, "EnemySpawn", new Vector3(-39f, 0f, -14f));
        EnsurePoint(aiRoot, "PatrolPoint_W1", new Vector3(-31f, 0f, -4f));
        EnsurePoint(aiRoot, "PatrolPoint_W2", new Vector3(-21f, 0f, -4f));
        EnsurePoint(aiRoot, "PatrolPoint_W3", new Vector3(-21f, 0f, -24f));
        EnsurePoint(aiRoot, "PatrolPoint_W4", new Vector3(-31f, 0f, -24f));
        EnsurePoint(aiRoot, "SafePoint", new Vector3(-40f, 0f, -24f));

        BuildDungeonArena.BuildRoomBoundaries(arena);
    }

    static void EnsurePoint(Transform parent, string name, Vector3 pos)
    {
        Transform t = parent.Find(name);
        if (t == null)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            t = go.transform;
        }
        t.localPosition = pos;
    }

    static void PurgeExistingEnemies()
    {
        var toDelete = new List<GameObject>();

        foreach (EnemyAI ai in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (ai != null && !toDelete.Contains(ai.gameObject))
            {
                toDelete.Add(ai.gameObject);
            }
        }

        string[] legacyNames = new string[]
        {
            "Skeleton_Warrior",
            "Rogue",
            "Enemy_Hall2_Goblin",
            "Enemy_Hall3_Vampire",
            "Enemy_Hall4_Pumpkinhulk",
            "Enemy_Boss_MawJLaygo"
        };

        foreach (string n in legacyNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null && !toDelete.Contains(go))
            {
                toDelete.Add(go);
            }
        }

        foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go != null && go.CompareTag("Enemy") && !toDelete.Contains(go))
            {
                toDelete.Add(go);
            }
        }

        foreach (GameObject go in toDelete)
        {
            if (go != null)
            {
                DestroyImmediate(go);
            }
        }
    }

    static Material PrepareEnemyMaterial(
        string folderPath,
        string fbxFileName,
        string[] orderedPngNames,
        string materialName,
        string diffuseTexName,
        string normalTexName,
        string emissionTexName)
    {
        string texFolder = $"{folderPath}/Textures";
        string matFolder = $"{folderPath}/Materials";
        if (!Directory.Exists(texFolder)) Directory.CreateDirectory(texFolder);
        if (!Directory.Exists(matFolder)) Directory.CreateDirectory(matFolder);

        string fbxPath = $"{folderPath}/{fbxFileName}";
        ExtractEmbeddedPngsFromFbx(fbxPath, texFolder, orderedPngNames);

        if (!string.IsNullOrEmpty(normalTexName))
        {
            string normPath = $"{texFolder}/{normalTexName}";
            TextureImporter ti = AssetImporter.GetAtPath(normPath) as TextureImporter;
            if (ti != null && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
        }

        string matPath = $"{matFolder}/{materialName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(urpShader);
            mat.name = materialName;
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = urpShader;
        }

        Texture2D diffuseTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texFolder}/{diffuseTexName}");
        if (diffuseTex != null)
        {
            mat.SetTexture("_BaseMap", diffuseTex);
            mat.SetTexture("_MainTex", diffuseTex);
            mat.SetColor("_BaseColor", Color.white);
            mat.color = Color.white;
        }

        if (!string.IsNullOrEmpty(normalTexName))
        {
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texFolder}/{normalTexName}");
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        if (!string.IsNullOrEmpty(emissionTexName))
        {
            Texture2D emissionTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texFolder}/{emissionTexName}");
            if (emissionTex != null)
            {
                mat.SetTexture("_EmissionMap", emissionTex);
                mat.SetColor("_EmissionColor", Color.white * 1.25f);
                mat.EnableKeyword("_EMISSION");
            }
        }

        mat.SetFloat("_Smoothness", 0.3f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ExtractEmbeddedPngsFromFbx(string fbxPath, string texFolder, string[] targetNames)
    {
        bool allExist = true;
        foreach (string name in targetNames)
        {
            if (!File.Exists(Path.Combine(texFolder, name)))
            {
                allExist = false;
                break;
            }
        }
        if (allExist) return;
        if (!File.Exists(fbxPath)) return;

        byte[] fbxData = File.ReadAllBytes(fbxPath);
        byte[] pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        byte[] iendBytes = new byte[] { 0x49, 0x45, 0x4E, 0x44 };

        int searchIdx = 0;
        int extractedCount = 0;

        while (extractedCount < targetNames.Length)
        {
            int start = FindBytePattern(fbxData, pngHeader, searchIdx);
            if (start == -1) break;

            int iendPos = FindBytePattern(fbxData, iendBytes, start);
            if (iendPos == -1) break;

            int end = iendPos + 8;
            int length = end - start;

            byte[] pngData = new byte[length];
            System.Array.Copy(fbxData, start, pngData, 0, length);

            string outPath = Path.Combine(texFolder, targetNames[extractedCount]);
            File.WriteAllBytes(outPath, pngData);

            searchIdx = end;
            extractedCount++;
        }

        AssetDatabase.Refresh();
    }

    static int FindBytePattern(byte[] source, byte[] pattern, int startIndex)
    {
        for (int i = startIndex; i <= source.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }

    static void ConfigureClipLoopSetting(string clipFbxPath, bool loopTime)
    {
        ModelImporter importer = AssetImporter.GetAtPath(clipFbxPath) as ModelImporter;
        if (importer == null) return;

        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        if (clips == null || clips.Length == 0)
        {
            clips = importer.defaultClipAnimations;
        }
        if (clips == null || clips.Length == 0) return;

        bool changed = false;
        foreach (var clip in clips)
        {
            if (clip.loopTime != loopTime)
            {
                clip.loopTime = loopTime;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    static AnimationClip LoadClipAtPath(string fbxPath)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        foreach (Object a in assets)
        {
            if (a is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }

    static AnimatorController CreateEnemyAnimatorController(
        string folderPath,
        string controllerFileName,
        string idleFbx,
        string walkFbx,
        string runFbx,
        string attackFbx,
        string deathFbx,
        float attackSpeedMultiplier)
    {
        string idlePath = $"{folderPath}/{idleFbx}";
        string walkPath = $"{folderPath}/{walkFbx}";
        string runPath = $"{folderPath}/{runFbx}";
        string attackPath = $"{folderPath}/{attackFbx}";
        string deathPath = $"{folderPath}/{deathFbx}";

        ConfigureClipLoopSetting(idlePath, true);
        ConfigureClipLoopSetting(walkPath, true);
        ConfigureClipLoopSetting(runPath, true);
        if (attackPath != runPath) ConfigureClipLoopSetting(attackPath, false);
        ConfigureClipLoopSetting(deathPath, false);

        string controllerPath = $"{folderPath}/{controllerFileName}";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
        {
            AssetDatabase.DeleteAsset(controllerPath);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsWalking", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsRunning", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsFleeing", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        AnimationClip idleClip = LoadClipAtPath(idlePath);
        AnimationClip walkClip = LoadClipAtPath(walkPath);
        AnimationClip runClip = LoadClipAtPath(runPath);
        AnimationClip attackClip = LoadClipAtPath(attackPath);
        AnimationClip deathClip = LoadClipAtPath(deathPath);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        AnimatorState idleState = sm.AddState("Idle", new Vector3(260f, 60f, 0f));
        AnimatorState walkState = sm.AddState("Walk", new Vector3(520f, 20f, 0f));
        AnimatorState runState = sm.AddState("Run", new Vector3(520f, 140f, 0f));
        AnimatorState fleeState = sm.AddState("Flee", new Vector3(520f, 250f, 0f));
        AnimatorState attackState = sm.AddState("Attack", new Vector3(260f, 220f, 0f));
        AnimatorState deathState = sm.AddState("Death", new Vector3(40f, 260f, 0f));

        idleState.motion = idleClip;
        walkState.motion = walkClip;
        runState.motion = runClip;
        fleeState.motion = runClip;
        fleeState.speed = 1.15f;
        attackState.motion = attackClip;
        attackState.speed = attackSpeedMultiplier;
        deathState.motion = deathClip;

        sm.defaultState = idleState;

        // Idle <-> Walk
        var tIdleWalk = idleState.AddTransition(walkState);
        tIdleWalk.hasExitTime = false;
        tIdleWalk.duration = 0.15f;
        tIdleWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");

        var tWalkIdle = walkState.AddTransition(idleState);
        tWalkIdle.hasExitTime = false;
        tWalkIdle.duration = 0.15f;
        tWalkIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
        tWalkIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

        // Idle -> Run
        var tIdleRun = idleState.AddTransition(runState);
        tIdleRun.hasExitTime = false;
        tIdleRun.duration = 0.15f;
        tIdleRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");

        // Walk <-> Run
        var tWalkRun = walkState.AddTransition(runState);
        tWalkRun.hasExitTime = false;
        tWalkRun.duration = 0.15f;
        tWalkRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");

        var tRunWalk = runState.AddTransition(walkState);
        tRunWalk.hasExitTime = false;
        tRunWalk.duration = 0.15f;
        tRunWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        tRunWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");

        // Run -> Idle
        var tRunIdle = runState.AddTransition(idleState);
        tRunIdle.hasExitTime = false;
        tRunIdle.duration = 0.15f;
        tRunIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");
        tRunIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");

        // -> Flee
        var tRunFlee = runState.AddTransition(fleeState);
        tRunFlee.hasExitTime = false;
        tRunFlee.duration = 0.15f;
        tRunFlee.AddCondition(AnimatorConditionMode.If, 0, "IsFleeing");

        var tWalkFlee = walkState.AddTransition(fleeState);
        tWalkFlee.hasExitTime = false;
        tWalkFlee.duration = 0.15f;
        tWalkFlee.AddCondition(AnimatorConditionMode.If, 0, "IsFleeing");

        var tIdleFlee = idleState.AddTransition(fleeState);
        tIdleFlee.hasExitTime = false;
        tIdleFlee.duration = 0.15f;
        tIdleFlee.AddCondition(AnimatorConditionMode.If, 0, "IsFleeing");

        // Flee -> Idle / Walk / Run
        var tFleeIdle = fleeState.AddTransition(idleState);
        tFleeIdle.hasExitTime = false;
        tFleeIdle.duration = 0.15f;
        tFleeIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFleeing");
        tFleeIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");
        tFleeIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

        var tFleeWalk = fleeState.AddTransition(walkState);
        tFleeWalk.hasExitTime = false;
        tFleeWalk.duration = 0.15f;
        tFleeWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFleeing");
        tFleeWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");

        var tFleeRun = fleeState.AddTransition(runState);
        tFleeRun.hasExitTime = false;
        tFleeRun.duration = 0.15f;
        tFleeRun.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFleeing");
        tFleeRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");

        // AnyState -> Attack
        var tAnyAttack = sm.AddAnyStateTransition(attackState);
        tAnyAttack.hasExitTime = false;
        tAnyAttack.duration = 0.1f;
        tAnyAttack.canTransitionToSelf = false;
        tAnyAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        tAnyAttack.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");

        var tAttackIdle = attackState.AddTransition(idleState);
        tAttackIdle.hasExitTime = true;
        tAttackIdle.exitTime = 0.85f;
        tAttackIdle.duration = 0.15f;

        // AnyState -> Death
        var tAnyDeath = sm.AddAnyStateTransition(deathState);
        tAnyDeath.hasExitTime = false;
        tAnyDeath.duration = 0.1f;
        tAnyDeath.canTransitionToSelf = false;
        tAnyDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void SpawnConfiguredEnemy(
        string enemyName,
        string fbxPath,
        Material material,
        AnimatorController controller,
        Vector3 spawnPos,
        float rotY,
        float visualScale,
        float colliderHeight,
        string roomPrefix,
        bool isBoss,
        float maxHP,
        float fleeHP,
        float attackDamage,
        float detectRadius,
        float attackRadius,
        float patrolSpeed,
        float chaseSpeed)
    {
        GameObject enemyRoot = new GameObject(enemyName);
        enemyRoot.tag = "Enemy";

        // Snap ke permukaan NavMesh terdekat agar pasti berdiri di walkable floor
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
        {
            spawnPos = navHit.position;
        }
        spawnPos.y = 0f;
        enemyRoot.transform.position = spawnPos;
        enemyRoot.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        enemyRoot.transform.localScale = Vector3.one;

        // Instansiasi visual FBX sebagai child
        GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        Animator anim = null;

        if (modelPrefab != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            visual.name = "Visual_Model";
            visual.transform.SetParent(enemyRoot.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(visualScale, visualScale, visualScale);

            // Terapkan material URP Lit pada seluruh Renderer
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer rend in renderers)
            {
                if (material != null)
                {
                    Material[] mats = new Material[rend.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = material;
                    rend.sharedMaterials = mats;
                }
                rend.enabled = true;
            }

            anim = visual.GetComponent<Animator>();
            if (anim == null) anim = visual.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        }

        float radius = isBoss ? 0.75f : (visualScale >= 1.9f ? 0.65f : 0.55f);

        // Pasang CapsuleCollider yang sesuai ukuran tubuh
        CapsuleCollider col = enemyRoot.AddComponent<CapsuleCollider>();
        col.height = colliderHeight;
        col.radius = radius;
        col.center = new Vector3(0f, colliderHeight * 0.5f, 0f);

        // Pasang NavMeshAgent
        NavMeshAgent agent = enemyRoot.AddComponent<NavMeshAgent>();
        agent.speed = patrolSpeed;
        agent.angularSpeed = 360f;
        agent.acceleration = 12f;
        agent.stoppingDistance = isBoss ? 2.4f : (visualScale >= 1.9f ? 2.0f : 1.8f);
        agent.radius = radius;
        agent.height = colliderHeight;
        agent.baseOffset = 0f;
        agent.updatePosition = true;
        agent.updateRotation = true;

        // Pasang EnemyAI & hubungkan referensi Waypoint / SafePoint / Player / Animator
        EnemyAI ai = enemyRoot.AddComponent<EnemyAI>();
        ai.roomPrefix = roomPrefix;
        ai.isBoss = isBoss;
        ai.maxHP = maxHP;
        ai.currentHP = maxHP;
        ai.fleeHPThreshold = fleeHP;
        ai.attackDamage = attackDamage;
        ai.attackCooldown = isBoss ? 1.3f : 1.5f;
        ai.detectRadius = detectRadius;
        ai.attackRadius = attackRadius;
        ai.patrolSpeed = patrolSpeed;
        ai.chaseSpeed = chaseSpeed;
        ai.fleeSpeed = chaseSpeed + 0.8f;
        ai.animator = anim;
        ai.FindSceneReferences();

        EditorUtility.SetDirty(enemyRoot);
    }
}
