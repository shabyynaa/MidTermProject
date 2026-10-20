using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor Setup untuk memasang dan mengatur komponen AudioSource, AudioClip,
/// dan DungeonAudioManager pada scene 'uts.unity' dan 'MainMenu.unity' melalui Inspector
/// TANPA mengubah script gameplay yang sudah ada.
/// </summary>
[InitializeOnLoad]
public static class DungeonAudioSetup
{
    private static readonly string[] UsedAudioPaths = new string[]
    {
        "Assets/Audio/Audio01/click1.ogg",
        "Assets/Audio/Audio01/switch10.ogg",
        "Assets/Audio/Audio02/bookOpen.ogg",
        "Assets/Audio/Audio02/bookClose.ogg",
        "Assets/Audio/Audio02/chop.ogg",
        "Assets/Audio/Audio02/clothBelt.ogg",
        "Assets/Audio/Audio02/creak1.ogg",
        "Assets/Audio/Audio02/doorClose_3.ogg",
        "Assets/Audio/Audio02/doorOpen_1.ogg",
        "Assets/Audio/Audio02/doorOpen_2.ogg",
        "Assets/Audio/Audio02/footstep02.ogg",
        "Assets/Audio/Audio02/handleCoins.ogg",
        "Assets/Audio/Audio02/handleCoins2.ogg",
        "Assets/Audio/Audio02/knifeSlice.ogg",
        "Assets/Audio/Audio02/knifeSlice2.ogg",
        "Assets/Audio/Audio02/metalClick.ogg",
        "Assets/Audio/Audio02/metalLatch.ogg",
        "Assets/Audio/Audio03/footstep_carpet_000.ogg",
        "Assets/Audio/Audio03/footstep_concrete_000.ogg",
        "Assets/Audio/Audio03/footstep_concrete_001.ogg",
        "Assets/Audio/Audio03/footstep_concrete_002.ogg",
        "Assets/Audio/Audio03/footstep_concrete_003.ogg",
        "Assets/Audio/Audio03/footstep_concrete_004.ogg",
        "Assets/Audio/Audio03/impactBell_heavy_000.ogg",
        "Assets/Audio/Audio03/impactBell_heavy_001.ogg",
        "Assets/Audio/Audio03/impactBell_heavy_002.ogg",
        "Assets/Audio/Audio03/impactBell_heavy_003.ogg",
        "Assets/Audio/Audio03/impactBell_heavy_004.ogg",
        "Assets/Audio/Audio03/impactMetal_heavy_000.ogg",
        "Assets/Audio/Audio03/impactMetal_heavy_001.ogg",
        "Assets/Audio/Audio03/impactPunch_heavy_000.ogg",
        "Assets/Audio/Audio03/impactPunch_heavy_001.ogg",
        "Assets/Audio/Audio03/impactPunch_heavy_002.ogg",
        "Assets/Audio/Audio03/impactSoft_heavy_000.ogg"
    };

    static DungeonAudioSetup()
    {
        EditorApplication.delayCall += AutoEnsureAudioSetup;
    }

    private static void AutoEnsureAudioSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        try
        {
            EnsureAudioImportersPreloaded();

            Scene active = EditorSceneManager.GetActiveScene();
            if (active.IsValid() && active.isLoaded)
            {
                if (active.name == "uts")
                {
                    bool changed = FixMirroredBarrierLabels();
                    if (Object.FindAnyObjectByType<DungeonAudioManager>() == null)
                    {
                        SetupGameplaySceneAudio();
                        changed = true;
                    }
                    if (changed)
                    {
                        EditorSceneManager.SaveScene(active);
                    }
                }
                else if (active.name == "MainMenu" && Object.FindAnyObjectByType<DungeonAudioManager>() == null)
                {
                    SetupMainMenuSceneAudio();
                    EditorSceneManager.SaveScene(active);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[DungeonAudioSetup] {e.Message}");
        }
    }

    private static bool FixMirroredBarrierLabels()
    {
        bool anyChanged = false;
        foreach (TextMesh tm in Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tm == null) continue;
            bool isBarrierText = tm.name == "BarrierLabel" || tm.name == "LockedText" ||
                                 (tm.text != null && tm.text.Contains("[TERKUNCI]"));
            if (isBarrierText && tm.transform.localScale.x > 0f)
            {
                Vector3 s = tm.transform.localScale;
                s.x = -Mathf.Abs(s.x);
                tm.transform.localScale = s;
                EditorUtility.SetDirty(tm.transform);
                EditorUtility.SetDirty(tm.gameObject);
                anyChanged = true;
            }
        }
        return anyChanged;
    }

    private static void EnsureAudioImportersPreloaded()
    {
        foreach (string path in UsedAudioPaths)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                if (!settings.preloadAudioData)
                {
                    settings.preloadAudioData = true;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }
        }
    }

    [MenuItem("Tools/Setup Dungeon Audio (All Scenes)")]
    public static void SetupAllScenesAudio()
    {
        EnsureAudioImportersPreloaded();

        string originalScenePath = EditorSceneManager.GetActiveScene().path;
        if (string.IsNullOrEmpty(originalScenePath))
        {
            originalScenePath = "Assets/uts.unity";
        }

        // 1. Setup MainMenu.unity
        if (System.IO.File.Exists("Assets/Scenes/MainMenu.unity"))
        {
            var menuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity", OpenSceneMode.Single);
            SetupMainMenuSceneAudio();
            EditorSceneManager.SaveScene(menuScene);
        }

        // 2. Setup uts.unity
        if (System.IO.File.Exists("Assets/uts.unity"))
        {
            var gameScene = EditorSceneManager.OpenScene("Assets/uts.unity", OpenSceneMode.Single);
            SetupGameplaySceneAudio();
            EditorSceneManager.SaveScene(gameScene);
        }

        if (originalScenePath != EditorSceneManager.GetActiveScene().path && System.IO.File.Exists(originalScenePath))
        {
            EditorSceneManager.OpenScene(originalScenePath, OpenSceneMode.Single);
        }

        Debug.Log("<color=#00ff88><b>[DungeonAudioSetup]</b> Seluruh AudioSource, AudioClip, & DungeonAudioManager berhasil dipasang!</color>");
    }

    public static void SetupMainMenuSceneAudio()
    {
        AudioClip clipClick = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio01/click1.ogg");
        AudioClip clipBookOpen = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/bookOpen.ogg");
        AudioClip clipBellDrone = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_000.ogg");

        // 1. Dedicated BGM & DungeonAudioManager di Main Menu
        GameObject bgmObj = GameObject.Find("MainMenu_BGM");
        if (bgmObj == null)
        {
            bgmObj = new GameObject("MainMenu_BGM");
        }
        ConfigureAudioSource(bgmObj, clipBellDrone, playOnAwake: true, loop: true, volume: 0.28f, pitch: 0.25f, spatialBlend: 0f);
        AssignAudioManagerClips(bgmObj);

        // 2. UI Click AudioSource pada MainMenu_Canvas + Persistent Button Listeners
        GameObject canvasObj = GameObject.Find("MainMenu_Canvas");
        if (canvasObj != null)
        {
            AudioSource uiAudio = ConfigureAudioSource(canvasObj, clipClick, playOnAwake: false, loop: false, volume: 0.65f, pitch: 1.0f, spatialBlend: 0f);

            WireButtonClickAudio(canvasObj.transform.Find("Panel_MainMenuCard/Btn_Start"), uiAudio, clipClick);
            WireButtonClickAudio(canvasObj.transform.Find("Panel_MainMenuCard/Btn_CaraMain"), uiAudio, clipClick);
            WireButtonClickAudio(canvasObj.transform.Find("Panel_CaraMainModal/CaraMain_Box/Btn_BackToMenu"), uiAudio, clipClick);

            // 3. Cara Main Modal Open Audio (PlayOnAwake saat modal diaktifkan)
            Transform modalTr = canvasObj.transform.Find("Panel_CaraMainModal");
            if (modalTr != null)
            {
                ConfigureAudioSource(modalTr.gameObject, clipBookOpen, playOnAwake: true, loop: false, volume: 0.65f, pitch: 1.0f, spatialBlend: 0f);
            }
        }
    }

    private static void WireButtonClickAudio(Transform btnTr, AudioSource targetSource, AudioClip clip)
    {
        if (btnTr == null || targetSource == null || clip == null) return;
        Button btn = btnTr.GetComponent<Button>();
        if (btn == null) return;

        int count = btn.onClick.GetPersistentEventCount();
        for (int i = count - 1; i >= 0; i--)
        {
            if (btn.onClick.GetPersistentTarget(i) == targetSource)
            {
                UnityEventTools.RemovePersistentListener(btn.onClick, i);
            }
        }

        UnityEventTools.AddObjectPersistentListener<AudioClip>(btn.onClick, targetSource.PlayOneShot, clip);
        EditorUtility.SetDirty(btn);
    }

    public static void SetupGameplaySceneAudio()
    {
        AudioClip clipPlayerFootstep = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_concrete_000.ogg");
        AudioClip clipPlayerAttack   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/knifeSlice.ogg");
        AudioClip clipPlayerHit      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactPunch_heavy_000.ogg");
        AudioClip clipPlayerDefeat   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/doorClose_3.ogg");

        AudioClip clipGoblinStep     = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/footstep02.ogg");
        AudioClip clipGoblinAttack   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/knifeSlice2.ogg");

        AudioClip clipVampireStep    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_carpet_000.ogg");
        AudioClip clipVampireAttack  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/chop.ogg");

        AudioClip clipHulkStep       = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_concrete_001.ogg");
        AudioClip clipHulkAttack     = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactPunch_heavy_001.ogg");

        AudioClip clipBossStep       = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactSoft_heavy_000.ogg");
        AudioClip clipBossAttack     = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactMetal_heavy_000.ogg");

        AudioClip clipKeyPickup      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/handleCoins2.ogg");
        AudioClip clipKeyShelfClick  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/metalClick.ogg");
        AudioClip clipKeyLatch       = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/metalLatch.ogg");
        AudioClip clipGateOpen1      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/doorOpen_1.ogg");
        AudioClip clipGateOpen2      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/doorOpen_2.ogg");

        AudioClip clipBrazierBell    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_000.ogg");
        AudioClip clipBrazierFire    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/clothBelt.ogg");

        AudioClip clipPlateLock      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_001.ogg");
        AudioClip clipMathGate       = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio01/switch10.ogg");

        AudioClip clipChestCoins     = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/handleCoins.ogg");
        AudioClip clipVictoryBell    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_001.ogg");

        // 1. Dedicated BGM GameObject + DungeonAudioManager
        GameObject bgmObj = GameObject.Find("Dungeon_BGM");
        if (bgmObj == null)
        {
            bgmObj = new GameObject("Dungeon_BGM");
        }
        ConfigureAudioSource(bgmObj, clipBrazierBell, playOnAwake: true, loop: true, volume: 0.28f, pitch: 0.25f, spatialBlend: 0f);
        AssignAudioManagerClips(bgmObj);

        // 2. Player Audio (Footstep, Attack, Hit Impact, Defeat)
        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
        if (pc != null)
        {
            ConfigureAudioSource(pc.gameObject, clipPlayerFootstep, playOnAwake: false, loop: false, volume: 0.45f, pitch: 1.0f, spatialBlend: 0.2f);
            ConfigureChildAudioSource(pc.transform, "Audio_PlayerAttack", clipPlayerAttack, playOnAwake: false, loop: false, volume: 0.65f, pitch: 1.0f, spatialBlend: 0.2f);
            ConfigureChildAudioSource(pc.transform, "Audio_PlayerHitImpact", clipPlayerHit, playOnAwake: false, loop: false, volume: 0.70f, pitch: 1.0f, spatialBlend: 0.2f);
            ConfigureChildAudioSource(pc.transform, "Audio_PlayerDefeat", clipPlayerDefeat, playOnAwake: false, loop: false, volume: 0.75f, pitch: 0.85f, spatialBlend: 0f);
        }

        // 3. Enemies Audio (Goblin, Vampire, Pumpkin Hulk, Boss Maw J Laygo)
        foreach (EnemyAI enemy in Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (enemy == null) continue;
            string n = enemy.name.ToLower();

            if (enemy.isBoss || n.Contains("boss") || n.Contains("maw"))
            {
                ConfigureAudioSource(enemy.gameObject, clipBossStep, playOnAwake: false, loop: false, volume: 0.60f, pitch: 0.80f, spatialBlend: 1.0f, minDist: 4f, maxDist: 28f);
                ConfigureChildAudioSource(enemy.transform, "Audio_EnemyAttack", clipBossAttack, playOnAwake: false, loop: false, volume: 0.75f, pitch: 0.90f, spatialBlend: 1.0f, minDist: 4f, maxDist: 28f);
            }
            else if (enemy.roomPrefix == "H4_" || n.Contains("pumpkin") || n.Contains("hulk"))
            {
                ConfigureAudioSource(enemy.gameObject, clipHulkStep, playOnAwake: false, loop: false, volume: 0.55f, pitch: 0.75f, spatialBlend: 1.0f, minDist: 3f, maxDist: 22f);
                ConfigureChildAudioSource(enemy.transform, "Audio_EnemyAttack", clipHulkAttack, playOnAwake: false, loop: false, volume: 0.70f, pitch: 0.85f, spatialBlend: 1.0f, minDist: 3f, maxDist: 22f);
            }
            else if (enemy.roomPrefix == "H3_" || n.Contains("vampire"))
            {
                ConfigureAudioSource(enemy.gameObject, clipVampireStep, playOnAwake: false, loop: false, volume: 0.38f, pitch: 1.05f, spatialBlend: 1.0f, minDist: 2f, maxDist: 18f);
                ConfigureChildAudioSource(enemy.transform, "Audio_EnemyAttack", clipVampireAttack, playOnAwake: false, loop: false, volume: 0.60f, pitch: 1.0f, spatialBlend: 1.0f, minDist: 2f, maxDist: 18f);
            }
            else
            {
                ConfigureAudioSource(enemy.gameObject, clipGoblinStep, playOnAwake: false, loop: false, volume: 0.40f, pitch: 1.10f, spatialBlend: 1.0f, minDist: 2f, maxDist: 18f);
                ConfigureChildAudioSource(enemy.transform, "Audio_EnemyAttack", clipGoblinAttack, playOnAwake: false, loop: false, volume: 0.60f, pitch: 1.0f, spatialBlend: 1.0f, minDist: 2f, maxDist: 18f);
            }
        }

        // 4. Hall 2 Keys & Shelf
        foreach (Hall2KeyItem key in Object.FindObjectsByType<Hall2KeyItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (key == null) continue;
            ConfigureAudioSource(key.gameObject, clipKeyPickup, playOnAwake: false, loop: false, volume: 0.70f, pitch: 1.0f, spatialBlend: 0.8f, minDist: 2f, maxDist: 15f);
        }

        Hall2Shelf shelf = Object.FindAnyObjectByType<Hall2Shelf>(FindObjectsInactive.Include);
        if (shelf != null)
        {
            ConfigureAudioSource(shelf.gameObject, clipKeyShelfClick, playOnAwake: false, loop: false, volume: 0.70f, pitch: 1.0f, spatialBlend: 0.7f, minDist: 2f, maxDist: 16f);

            if (shelf.placedKeyVisuals != null)
            {
                for (int i = 0; i < shelf.placedKeyVisuals.Length; i++)
                {
                    GameObject kv = shelf.placedKeyVisuals[i];
                    if (kv == null) continue;
                    AudioClip slotClip = (i == shelf.placedKeyVisuals.Length - 1) ? clipGateOpen1 : clipKeyLatch;
                    float vol = (i == shelf.placedKeyVisuals.Length - 1) ? 0.80f : 0.75f;
                    ConfigureAudioSource(kv, slotClip, playOnAwake: true, loop: false, volume: vol, pitch: 1.0f, spatialBlend: 0.5f, minDist: 3f, maxDist: 20f);
                }
            }
        }

        // 5. Hall 3 Braziers & Challenge Manager
        foreach (Hall3Brazier brazier in Object.FindObjectsByType<Hall3Brazier>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (brazier == null) continue;
            ConfigureAudioSource(brazier.gameObject, clipBrazierFire, playOnAwake: false, loop: false, volume: 0.65f, pitch: 1.0f, spatialBlend: 0.8f, minDist: 2f, maxDist: 16f);

            if (brazier.flameVisual != null)
            {
                ConfigureAudioSource(brazier.flameVisual, clipBrazierBell, playOnAwake: true, loop: false, volume: 0.70f, pitch: 1.0f, spatialBlend: 0.6f, minDist: 3f, maxDist: 22f);
            }
        }

        Hall3ChallengeManager h3Mgr = Object.FindAnyObjectByType<Hall3ChallengeManager>(FindObjectsInactive.Include);
        if (h3Mgr != null)
        {
            ConfigureAudioSource(h3Mgr.gameObject, clipGateOpen2, playOnAwake: false, loop: false, volume: 0.80f, pitch: 1.0f, spatialBlend: 0.3f);
        }

        // 6. Hall 4 Pressure Plates & Challenge Manager
        foreach (Hall4PressurePlate plate in Object.FindObjectsByType<Hall4PressurePlate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (plate == null) continue;
            ConfigureAudioSource(plate.gameObject, clipPlateLock, playOnAwake: false, loop: false, volume: 0.75f, pitch: 1.0f, spatialBlend: 0.6f, minDist: 3f, maxDist: 20f);
        }

        Hall4ChallengeManager h4Mgr = Object.FindAnyObjectByType<Hall4ChallengeManager>(FindObjectsInactive.Include);
        if (h4Mgr != null)
        {
            ConfigureAudioSource(h4Mgr.gameObject, clipGateOpen2, playOnAwake: false, loop: false, volume: 0.80f, pitch: 1.0f, spatialBlend: 0.3f);
        }

        // 7. Math Gates (1A..4B)
        foreach (MathGate gate in Object.FindObjectsByType<MathGate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (gate == null) continue;
            ConfigureAudioSource(gate.gameObject, clipMathGate, playOnAwake: false, loop: false, volume: 0.65f, pitch: 1.0f, spatialBlend: 0.5f, minDist: 2.5f, maxDist: 18f);
        }

        // 8. Boss Arena & Victory Chest
        VictoryChest chest = Object.FindAnyObjectByType<VictoryChest>(FindObjectsInactive.Include);
        if (chest != null)
        {
            ConfigureAudioSource(chest.gameObject, clipChestCoins, playOnAwake: false, loop: false, volume: 0.80f, pitch: 1.0f, spatialBlend: 0.4f, minDist: 3f, maxDist: 22f);
        }

        BossArenaManager bossMgr = Object.FindAnyObjectByType<BossArenaManager>(FindObjectsInactive.Include);
        if (bossMgr != null)
        {
            ConfigureAudioSource(bossMgr.gameObject, clipVictoryBell, playOnAwake: false, loop: false, volume: 0.80f, pitch: 1.0f, spatialBlend: 0f);
        }

        FixMirroredBarrierLabels();
    }

    private static void AssignAudioManagerClips(GameObject host)
    {
        DungeonAudioManager mgr = host.GetComponent<DungeonAudioManager>();
        if (mgr == null) mgr = host.AddComponent<DungeonAudioManager>();

        mgr.clipUiClick        = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio01/click1.ogg");
        mgr.clipBookOpen       = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/bookOpen.ogg");
        mgr.clipBookClose      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/bookClose.ogg");
        mgr.clipMathGateSwitch = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio01/switch10.ogg");

        mgr.clipsPlayerFootstep = new AudioClip[]
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_concrete_000.ogg"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_concrete_001.ogg"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_concrete_002.ogg"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_concrete_003.ogg"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_concrete_004.ogg")
        };

        mgr.clipPlayerAttack   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/knifeSlice.ogg");
        mgr.clipPlayerHitEnemy = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactPunch_heavy_000.ogg");
        mgr.clipPlayerHurt     = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactPunch_heavy_002.ogg");
        mgr.clipPlayerDefeat   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/doorClose_3.ogg");

        mgr.clipGoblinStep     = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/footstep02.ogg");
        mgr.clipGoblinAttack   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/knifeSlice2.ogg");
        mgr.clipVampireStep    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_carpet_000.ogg");
        mgr.clipVampireAttack  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/chop.ogg");
        mgr.clipHulkStep       = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/footstep_concrete_001.ogg");
        mgr.clipHulkAttack     = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactPunch_heavy_001.ogg");
        mgr.clipBossStep       = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactSoft_heavy_000.ogg");
        mgr.clipBossAttack     = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactMetal_heavy_000.ogg");
        mgr.clipEnemyCharge    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactMetal_heavy_001.ogg");
        mgr.clipEnemyStun      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_002.ogg");

        mgr.clipKeyPickup      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/handleCoins2.ogg");
        mgr.clipKeyShelfClick  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/metalClick.ogg");
        mgr.clipKeyLatch       = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/metalLatch.ogg");
        mgr.clipGateOpen1      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/doorOpen_1.ogg");
        mgr.clipGateOpen2      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/doorOpen_2.ogg");
        mgr.clipBrazierIgnite  = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/clothBelt.ogg");
        mgr.clipBrazierBell    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_000.ogg");
        mgr.clipPlateTick      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/metalClick.ogg");
        mgr.clipPlateLock      = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_001.ogg");
        mgr.clipVictoryChest   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/handleCoins.ogg");
        mgr.clipVictoryBell    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_001.ogg");

        mgr.clipsDungeonBells = new AudioClip[]
        {
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_000.ogg"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_001.ogg"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_002.ogg"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_003.ogg"),
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactBell_heavy_004.ogg")
        };

        mgr.clipDungeonPulse   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio03/impactSoft_heavy_000.ogg");
        mgr.clipDungeonCreak   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Audio02/creak1.ogg");

        EditorUtility.SetDirty(mgr);
    }

    private static AudioSource ConfigureChildAudioSource(Transform parent, string childName, AudioClip clip,
        bool playOnAwake, bool loop, float volume, float pitch, float spatialBlend, float minDist = 1f, float maxDist = 20f)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            GameObject childObj = new GameObject(childName);
            childObj.transform.SetParent(parent, false);
            childObj.transform.localPosition = Vector3.zero;
            child = childObj.transform;
        }
        return ConfigureAudioSource(child.gameObject, clip, playOnAwake, loop, volume, pitch, spatialBlend, minDist, maxDist);
    }

    private static AudioSource ConfigureAudioSource(GameObject go, AudioClip clip,
        bool playOnAwake, bool loop, float volume, float pitch, float spatialBlend, float minDist = 1f, float maxDist = 20f)
    {
        AudioSource src = go.GetComponent<AudioSource>();
        if (src == null)
        {
            src = go.AddComponent<AudioSource>();
        }

        src.clip = clip;
        src.playOnAwake = playOnAwake;
        src.loop = loop;
        src.volume = volume;
        src.pitch = pitch;
        src.spatialBlend = spatialBlend;
        src.minDistance = minDist;
        src.maxDistance = maxDist;
        src.rolloffMode = AudioRolloffMode.Linear;

        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(src);
        return src;
    }
}
