using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sistem Audio Dungeon Terpusat & Non-Intrusif untuk MathGate Tactics.
/// Mengelola Background Music (BGM) suasana dungeon serta seluruh Sound Effects (SFX)
/// untuk Player, Enemy (Goblin, Vampire, Pumpkin Hulk, Boss), Kunci & Rak Hall 2,
/// Brazier Hall 3, Pressure Plate Hall 4, MathGate, Victory/Defeat, dan Main Menu
/// TANPA mengubah script gameplay, AI, maupun UI yang sudah ada.
/// </summary>
public class DungeonAudioManager : MonoBehaviour
{
    public static DungeonAudioManager Instance { get; private set; }

    [Header("UI & Menu Audio Clips (Assets/Audio/Audio01 & Audio02)")]
    public AudioClip clipUiClick;
    public AudioClip clipBookOpen;
    public AudioClip clipBookClose;
    public AudioClip clipMathGateSwitch;

    [Header("Player Audio Clips (Assets/Audio/Audio02 & Audio03)")]
    public AudioClip[] clipsPlayerFootstep;
    public AudioClip clipPlayerAttack;
    public AudioClip clipPlayerHitEnemy;
    public AudioClip clipPlayerHurt;
    public AudioClip clipPlayerDefeat;

    [Header("Enemy Audio Clips (Assets/Audio/Audio02 & Audio03)")]
    public AudioClip clipGoblinStep;
    public AudioClip clipGoblinAttack;
    public AudioClip clipVampireStep;
    public AudioClip clipVampireAttack;
    public AudioClip clipHulkStep;
    public AudioClip clipHulkAttack;
    public AudioClip clipBossStep;
    public AudioClip clipBossAttack;
    public AudioClip clipEnemyCharge;
    public AudioClip clipEnemyStun;

    [Header("Dungeon Objectives & Gates (Assets/Audio/Audio02 & Audio03)")]
    public AudioClip clipKeyPickup;
    public AudioClip clipKeyShelfClick;
    public AudioClip clipKeyLatch;
    public AudioClip clipGateOpen1;
    public AudioClip clipGateOpen2;
    public AudioClip clipBrazierIgnite;
    public AudioClip clipBrazierBell;
    public AudioClip clipPlateTick;
    public AudioClip clipPlateLock;
    public AudioClip clipVictoryChest;
    public AudioClip clipVictoryBell;

    [Header("Dungeon Ambient BGM Layers (Assets/Audio/Audio02 & Audio03)")]
    public AudioClip[] clipsDungeonBells;
    public AudioClip clipDungeonPulse;
    public AudioClip clipDungeonCreak;

    // Runtime AudioSources untuk BGM & 2D SFX
    private AudioSource bgmPadSourceA;
    private AudioSource bgmPadSourceB;
    private AudioSource bgmPulseSource;
    private AudioSource bgmAccentSource;
    private AudioSource sfx2DSource;

    // State BGM Harmonik
    private bool usePadA = true;
    private float nextPadSwitchTime = 0f;
    private float padCycleDuration = 3.2f;
    private int bellProgressionIndex = 0;
    private readonly float[] darkMinorPitches = { 0.25f, 0.28f, 0.22f, 0.30f, 0.24f, 0.33f };
    private float nextPulseTime = 0f;
    private float nextAccentTime = 0f;

    // State Player Tracking
    private PlayerController playerController;
    private PlayerStats playerStats;
    private Vector3 lastPlayerPos;
    private bool hasPlayerPos = false;
    private float nextPlayerStepTime = 0f;
    private int playerStepIndex = 0;
    private float lastPlayerAttackAudioTime = -10f;
    private float lastPlayerHP = -1f;
    private int lastPlayerLives = -1;
    private bool wasGameOverOpen = false;

    // State Main Menu Tracking
    private bool wasCaraMainOpen = false;
    private bool wasMenuGameStarted = false;

    // State Objectives Tracking
    private int lastKeysCarried = 0;
    private int lastKeysOnShelf = 0;
    private bool wasHall2Completed = false;

    private int lastLitBraziers = 0;
    private bool wasHall3Completed = false;

    private int lastLockedPlates = 0;
    private bool wasHall4Completed = false;
    private float nextPlateTickTime = 0f;

    private bool wasBossDefeated = false;
    private bool wasBossEnraged = false;
    private bool wasVictoryChestOpened = false;
    private bool wasVictoryModalOpen = false;

    private readonly HashSet<MathGate> usedMathGates = new HashSet<MathGate>();

    private class EnemyAudioTracker
    {
        public EnemyAI enemy;
        public AudioSource stepSource;
        public AudioSource attackSource;
        public Vector3 lastPos;
        public float lastHP;
        public EnemyState lastState;
        public bool wasDead;
        public bool wasCharging;
        public bool wasStunned;
        public float nextStepTime;
        public float nextAttackSoundTime;
    }

    private readonly List<EnemyAudioTracker> enemyTrackers = new List<EnemyAudioTracker>();
    private float nextSceneScanTime = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance == null)
        {
            DungeonAudioManager existing = Object.FindAnyObjectByType<DungeonAudioManager>();
            if (existing != null)
            {
                Instance = existing;
            }
            else
            {
                GameObject host = GameObject.Find("Dungeon_BGM");
                if (host == null) host = GameObject.Find("MainMenu_BGM");
                if (host == null) host = new GameObject("Dungeon_BGM");
                Instance = host.AddComponent<DungeonAudioManager>();
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioListener();
        LoadAndPreloadAllClips();
        SetupBgmAnd2DSources();
        ScanCurrentSceneObjects();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureAudioListener();
        ResetSceneTrackingState();
        ScanCurrentSceneObjects();
    }

    private void EnsureAudioListener()
    {
        AudioListener.volume = 1.0f;
        AudioListener.pause = false;

        AudioListener listener = Object.FindAnyObjectByType<AudioListener>();
        if (listener == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
            }
            else
            {
                gameObject.AddComponent<AudioListener>();
            }
        }
        else if (!listener.enabled)
        {
            listener.enabled = true;
        }
    }

    private void LoadAndPreloadAllClips()
    {
#if UNITY_EDITOR
        if (clipUiClick == null)        clipUiClick        = LoadClipEditor("Assets/Audio/Audio01/click1.ogg");
        if (clipBookOpen == null)       clipBookOpen       = LoadClipEditor("Assets/Audio/Audio02/bookOpen.ogg");
        if (clipBookClose == null)      clipBookClose      = LoadClipEditor("Assets/Audio/Audio02/bookClose.ogg");
        if (clipMathGateSwitch == null) clipMathGateSwitch = LoadClipEditor("Assets/Audio/Audio01/switch10.ogg");

        if (clipsPlayerFootstep == null || clipsPlayerFootstep.Length == 0)
        {
            clipsPlayerFootstep = new AudioClip[]
            {
                LoadClipEditor("Assets/Audio/Audio03/footstep_concrete_000.ogg"),
                LoadClipEditor("Assets/Audio/Audio03/footstep_concrete_001.ogg"),
                LoadClipEditor("Assets/Audio/Audio03/footstep_concrete_002.ogg"),
                LoadClipEditor("Assets/Audio/Audio03/footstep_concrete_003.ogg"),
                LoadClipEditor("Assets/Audio/Audio03/footstep_concrete_004.ogg")
            };
        }

        if (clipPlayerAttack == null)   clipPlayerAttack   = LoadClipEditor("Assets/Audio/Audio02/knifeSlice.ogg");
        if (clipPlayerHitEnemy == null) clipPlayerHitEnemy = LoadClipEditor("Assets/Audio/Audio03/impactPunch_heavy_000.ogg");
        if (clipPlayerHurt == null)     clipPlayerHurt     = LoadClipEditor("Assets/Audio/Audio03/impactPunch_heavy_002.ogg");
        if (clipPlayerDefeat == null)   clipPlayerDefeat   = LoadClipEditor("Assets/Audio/Audio02/doorClose_3.ogg");

        if (clipGoblinStep == null)     clipGoblinStep     = LoadClipEditor("Assets/Audio/Audio02/footstep02.ogg");
        if (clipGoblinAttack == null)   clipGoblinAttack   = LoadClipEditor("Assets/Audio/Audio02/knifeSlice2.ogg");
        if (clipVampireStep == null)    clipVampireStep    = LoadClipEditor("Assets/Audio/Audio03/footstep_carpet_000.ogg");
        if (clipVampireAttack == null)  clipVampireAttack  = LoadClipEditor("Assets/Audio/Audio02/chop.ogg");
        if (clipHulkStep == null)       clipHulkStep       = LoadClipEditor("Assets/Audio/Audio03/footstep_concrete_001.ogg");
        if (clipHulkAttack == null)     clipHulkAttack     = LoadClipEditor("Assets/Audio/Audio03/impactPunch_heavy_001.ogg");
        if (clipBossStep == null)       clipBossStep       = LoadClipEditor("Assets/Audio/Audio03/impactSoft_heavy_000.ogg");
        if (clipBossAttack == null)     clipBossAttack     = LoadClipEditor("Assets/Audio/Audio03/impactMetal_heavy_000.ogg");
        if (clipEnemyCharge == null)    clipEnemyCharge    = LoadClipEditor("Assets/Audio/Audio03/impactMetal_heavy_001.ogg");
        if (clipEnemyStun == null)      clipEnemyStun      = LoadClipEditor("Assets/Audio/Audio03/impactBell_heavy_002.ogg");

        if (clipKeyPickup == null)      clipKeyPickup      = LoadClipEditor("Assets/Audio/Audio02/handleCoins2.ogg");
        if (clipKeyShelfClick == null)  clipKeyShelfClick  = LoadClipEditor("Assets/Audio/Audio02/metalClick.ogg");
        if (clipKeyLatch == null)       clipKeyLatch       = LoadClipEditor("Assets/Audio/Audio02/metalLatch.ogg");
        if (clipGateOpen1 == null)      clipGateOpen1      = LoadClipEditor("Assets/Audio/Audio02/doorOpen_1.ogg");
        if (clipGateOpen2 == null)      clipGateOpen2      = LoadClipEditor("Assets/Audio/Audio02/doorOpen_2.ogg");
        if (clipBrazierIgnite == null)  clipBrazierIgnite  = LoadClipEditor("Assets/Audio/Audio02/clothBelt.ogg");
        if (clipBrazierBell == null)    clipBrazierBell    = LoadClipEditor("Assets/Audio/Audio03/impactBell_heavy_000.ogg");
        if (clipPlateTick == null)      clipPlateTick      = LoadClipEditor("Assets/Audio/Audio02/metalClick.ogg");
        if (clipPlateLock == null)      clipPlateLock      = LoadClipEditor("Assets/Audio/Audio03/impactBell_heavy_001.ogg");
        if (clipVictoryChest == null)   clipVictoryChest   = LoadClipEditor("Assets/Audio/Audio02/handleCoins.ogg");
        if (clipVictoryBell == null)    clipVictoryBell    = LoadClipEditor("Assets/Audio/Audio03/impactBell_heavy_001.ogg");

        if (clipsDungeonBells == null || clipsDungeonBells.Length == 0)
        {
            clipsDungeonBells = new AudioClip[]
            {
                LoadClipEditor("Assets/Audio/Audio03/impactBell_heavy_000.ogg"),
                LoadClipEditor("Assets/Audio/Audio03/impactBell_heavy_001.ogg"),
                LoadClipEditor("Assets/Audio/Audio03/impactBell_heavy_002.ogg"),
                LoadClipEditor("Assets/Audio/Audio03/impactBell_heavy_003.ogg"),
                LoadClipEditor("Assets/Audio/Audio03/impactBell_heavy_004.ogg")
            };
        }

        if (clipDungeonPulse == null)   clipDungeonPulse   = LoadClipEditor("Assets/Audio/Audio03/impactSoft_heavy_000.ogg");
        if (clipDungeonCreak == null)   clipDungeonCreak   = LoadClipEditor("Assets/Audio/Audio02/creak1.ogg");
#endif

        // Pastikan seluruh AudioClip yang memiliki preloadAudioData = false dimuat ke memori
        PreloadClip(clipUiClick);
        PreloadClip(clipBookOpen);
        PreloadClip(clipBookClose);
        PreloadClip(clipMathGateSwitch);
        if (clipsPlayerFootstep != null) foreach (var c in clipsPlayerFootstep) PreloadClip(c);
        PreloadClip(clipPlayerAttack);
        PreloadClip(clipPlayerHitEnemy);
        PreloadClip(clipPlayerHurt);
        PreloadClip(clipPlayerDefeat);
        PreloadClip(clipGoblinStep);
        PreloadClip(clipGoblinAttack);
        PreloadClip(clipVampireStep);
        PreloadClip(clipVampireAttack);
        PreloadClip(clipHulkStep);
        PreloadClip(clipHulkAttack);
        PreloadClip(clipBossStep);
        PreloadClip(clipBossAttack);
        PreloadClip(clipEnemyCharge);
        PreloadClip(clipEnemyStun);
        PreloadClip(clipKeyPickup);
        PreloadClip(clipKeyShelfClick);
        PreloadClip(clipKeyLatch);
        PreloadClip(clipGateOpen1);
        PreloadClip(clipGateOpen2);
        PreloadClip(clipBrazierIgnite);
        PreloadClip(clipBrazierBell);
        PreloadClip(clipPlateTick);
        PreloadClip(clipPlateLock);
        PreloadClip(clipVictoryChest);
        PreloadClip(clipVictoryBell);
        if (clipsDungeonBells != null) foreach (var c in clipsDungeonBells) PreloadClip(c);
        PreloadClip(clipDungeonPulse);
        PreloadClip(clipDungeonCreak);

        // Juga preload seluruh AudioSource yang sudah ada di scene
        foreach (AudioSource src in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (src != null && src.clip != null)
            {
                PreloadClip(src.clip);
            }
        }
    }

#if UNITY_EDITOR
    private static AudioClip LoadClipEditor(string path)
    {
        return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
#endif

    private static void PreloadClip(AudioClip clip)
    {
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
        {
            clip.LoadAudioData();
        }
    }

    private void SetupBgmAnd2DSources()
    {
        bgmPadSourceA = GetComponent<AudioSource>();
        if (bgmPadSourceA == null) bgmPadSourceA = gameObject.AddComponent<AudioSource>();
        Configure2DSource(bgmPadSourceA, 0.26f);

        bgmPadSourceB   = CreateChild2DSource("BGM_Pad_B", 0.26f);
        bgmPulseSource  = CreateChild2DSource("BGM_Pulse", 0.22f);
        bgmAccentSource = CreateChild2DSource("BGM_Accent", 0.14f);
        sfx2DSource     = CreateChild2DSource("SFX_2D_Master", 0.75f);

        // Mulai BGM seketika saat Play ditekan
        TriggerNextBgmPadLayer(Time.unscaledTime);
        nextPulseTime = Time.unscaledTime + 0.5f;
        nextAccentTime = Time.unscaledTime + 2.2f;
    }

    private AudioSource CreateChild2DSource(string childName, float volume)
    {
        Transform tr = transform.Find(childName);
        if (tr == null)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(transform, false);
            tr = go.transform;
        }
        AudioSource src = tr.GetComponent<AudioSource>();
        if (src == null) src = tr.gameObject.AddComponent<AudioSource>();
        Configure2DSource(src, volume);
        return src;
    }

    private static void Configure2DSource(AudioSource src, float volume)
    {
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.volume = volume;
    }

    private void ResetSceneTrackingState()
    {
        hasPlayerPos = false;
        lastKeysCarried = Hall2ChallengeManager.Instance != null ? Hall2ChallengeManager.Instance.keysCarried : 0;
        lastKeysOnShelf = Hall2ChallengeManager.Instance != null ? Hall2ChallengeManager.Instance.keysOnShelf : 0;
        wasHall2Completed = Hall2ChallengeManager.Instance != null && Hall2ChallengeManager.Instance.isChallengeCompleted;

        lastLitBraziers = Hall3ChallengeManager.Instance != null ? Hall3ChallengeManager.Instance.litBraziersCount : 0;
        wasHall3Completed = Hall3ChallengeManager.Instance != null && Hall3ChallengeManager.Instance.isChallengeCompleted;

        lastLockedPlates = Hall4ChallengeManager.Instance != null ? Hall4ChallengeManager.Instance.activatedPlatesCount : 0;
        wasHall4Completed = Hall4ChallengeManager.Instance != null && Hall4ChallengeManager.Instance.isChallengeCompleted;

        wasBossDefeated = BossArenaManager.Instance != null && BossArenaManager.Instance.isBossDefeated;
        wasBossEnraged = BossArenaManager.Instance != null && BossArenaManager.Instance.isBossEnraged;
        wasVictoryModalOpen = BossArenaManager.Instance != null && BossArenaManager.Instance.IsVictoryModalOpen();

        usedMathGates.Clear();
        enemyTrackers.Clear();
    }

    private void ScanCurrentSceneObjects()
    {
        nextSceneScanTime = Time.unscaledTime + 2.0f;

        if (playerController == null)
        {
            playerController = Object.FindAnyObjectByType<PlayerController>();
            if (playerController != null)
            {
                lastPlayerPos = playerController.transform.position;
                hasPlayerPos = true;
            }
        }

        if (playerStats == null)
        {
            playerStats = Object.FindAnyObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                lastPlayerHP = playerStats.currentHP;
                lastPlayerLives = playerStats.currentLives;
                wasGameOverOpen = playerStats.IsGameOverModalOpen();
            }
        }

        // Hubungkan tombol Main Menu bila berada di MainMenu
        if (MainMenuManager.Instance != null)
        {
            WireMainMenuButtons(MainMenuManager.Instance);
        }

        // Daftarkan seluruh EnemyAI untuk tracking langkah kaki & serangan 3D
        EnemyAI[] allEnemies = Object.FindObjectsByType<EnemyAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (EnemyAI enemy in allEnemies)
        {
            if (enemy == null) continue;
            bool alreadyTracked = false;
            for (int i = 0; i < enemyTrackers.Count; i++)
            {
                if (enemyTrackers[i].enemy == enemy)
                {
                    alreadyTracked = true;
                    break;
                }
            }
            if (alreadyTracked) continue;

            AudioSource stepSrc = enemy.GetComponent<AudioSource>();
            if (stepSrc == null) stepSrc = enemy.gameObject.AddComponent<AudioSource>();
            stepSrc.playOnAwake = false;
            stepSrc.loop = false;
            stepSrc.spatialBlend = 1.0f;
            stepSrc.rolloffMode = AudioRolloffMode.Linear;
            stepSrc.minDistance = 2.5f;
            stepSrc.maxDistance = enemy.isBoss ? 28f : 18f;

            Transform atkTr = enemy.transform.Find("Audio_EnemyAttack");
            if (atkTr == null)
            {
                GameObject atkObj = new GameObject("Audio_EnemyAttack");
                atkObj.transform.SetParent(enemy.transform, false);
                atkTr = atkObj.transform;
            }
            AudioSource atkSrc = atkTr.GetComponent<AudioSource>();
            if (atkSrc == null) atkSrc = atkTr.gameObject.AddComponent<AudioSource>();
            atkSrc.playOnAwake = false;
            atkSrc.loop = false;
            atkSrc.spatialBlend = 1.0f;
            atkSrc.rolloffMode = AudioRolloffMode.Linear;
            atkSrc.minDistance = 3.0f;
            atkSrc.maxDistance = enemy.isBoss ? 28f : 20f;

            string n = enemy.name.ToLower();
            if (enemy.isBoss || n.Contains("boss") || n.Contains("maw"))
            {
                if (stepSrc.clip == null) stepSrc.clip = clipBossStep;
                if (atkSrc.clip == null) atkSrc.clip = clipBossAttack;
                stepSrc.volume = 0.60f;
                stepSrc.pitch = 0.80f;
                atkSrc.volume = 0.75f;
                atkSrc.pitch = 0.90f;
            }
            else if (enemy.roomPrefix == "H4_" || n.Contains("pumpkin") || n.Contains("hulk"))
            {
                if (stepSrc.clip == null) stepSrc.clip = clipHulkStep;
                if (atkSrc.clip == null) atkSrc.clip = clipHulkAttack;
                stepSrc.volume = 0.55f;
                stepSrc.pitch = 0.75f;
                atkSrc.volume = 0.70f;
                atkSrc.pitch = 0.85f;
            }
            else if (enemy.roomPrefix == "H3_" || n.Contains("vampire"))
            {
                if (stepSrc.clip == null) stepSrc.clip = clipVampireStep;
                if (atkSrc.clip == null) atkSrc.clip = clipVampireAttack;
                stepSrc.volume = 0.40f;
                stepSrc.pitch = 1.05f;
                atkSrc.volume = 0.62f;
                atkSrc.pitch = 1.0f;
            }
            else
            {
                if (stepSrc.clip == null) stepSrc.clip = clipGoblinStep;
                if (atkSrc.clip == null) atkSrc.clip = clipGoblinAttack;
                stepSrc.volume = 0.42f;
                stepSrc.pitch = 1.10f;
                atkSrc.volume = 0.62f;
                atkSrc.pitch = 1.0f;
            }

            PreloadClip(stepSrc.clip);
            PreloadClip(atkSrc.clip);

            enemyTrackers.Add(new EnemyAudioTracker
            {
                enemy = enemy,
                stepSource = stepSrc,
                attackSource = atkSrc,
                lastPos = enemy.transform.position,
                lastHP = enemy.currentHP,
                lastState = enemy.currentState,
                wasDead = enemy.isDead,
                wasCharging = enemy.isCharging,
                wasStunned = enemy.isStunned,
                nextStepTime = Time.time + Random.Range(0.1f, 0.4f),
                nextAttackSoundTime = 0f
            });
        }
    }

    private TextMesh[] sceneTextMeshes;

    private void WireMainMenuButtons(MainMenuManager menu)
    {
        if (menu == null) return;
        wasCaraMainOpen = menu.caraMainPanel != null && menu.caraMainPanel.activeSelf;
        wasMenuGameStarted = menu.isGameStarted;
    }

    private void Update()
    {
        float nowUnscaled = Time.unscaledTime;

        if (nowUnscaled >= nextSceneScanTime)
        {
            ScanCurrentSceneObjects();
            sceneTextMeshes = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        // 1. Update Background Music (BGM) Suasana Dungeon secara kontinu
        UpdateDungeonBgm(nowUnscaled);

        // 2. Pantau Interaksi Main Menu
        UpdateMainMenuAudio();

        // 3. Pantau Pergerakan, Serangan, Hit, & Kekalahan Player
        UpdatePlayerAudio();

        // 4. Pantau Langkah Kaki & Serangan Enemy / Boss
        UpdateEnemiesAudio();

        // 5. Pantau Objektif Hall 2 (Kunci & Rak), Hall 3 (Brazier), Hall 4 (Pressure Plate), MathGate, & Boss Victory
        UpdateObjectivesAndGatesAudio();
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (sceneTextMeshes == null || sceneTextMeshes.Length == 0)
        {
            sceneTextMeshes = Object.FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        Vector3 camRight = cam.transform.right;
        for (int i = 0; i < sceneTextMeshes.Length; i++)
        {
            TextMesh tm = sceneTextMeshes[i];
            if (tm == null || !tm.gameObject.activeInHierarchy) continue;

            // Pastikan arah teks (+X lokal di dunia) searah dengan sisi kanan kamera agar tidak pernah terbalik (mirror)
            Vector3 worldTextRight = tm.transform.TransformVector(Vector3.right);
            if (Vector3.Dot(worldTextRight, camRight) < -0.02f)
            {
                Vector3 s = tm.transform.localScale;
                s.x = -s.x;
                tm.transform.localScale = s;
            }
        }
    }

    private void UpdateDungeonBgm(float now)
    {
        // Pastikan pad BGM yang sedang aktif langsung berbunyi begitu AudioClip selesai dimuat
        AudioSource currentPad = usePadA ? bgmPadSourceB : bgmPadSourceA;
        if (currentPad != null && !currentPad.isPlaying && currentPad.clip != null && currentPad.clip.loadState == AudioDataLoadState.Loaded)
        {
            currentPad.Play();
        }

        // Layer 1: Alternating Deep Resonant Dungeon Bell Drones (Tanpa jeda)
        if (now >= nextPadSwitchTime)
        {
            TriggerNextBgmPadLayer(now);
        }

        // Cek apakah ada musuh yang sedang mengejar (Chase/Attack) untuk menaikkan intensitas pulse
        bool inDanger = false;
        for (int i = 0; i < enemyTrackers.Count; i++)
        {
            EnemyAI e = enemyTrackers[i].enemy;
            if (e != null && !e.isDead && (e.currentState == EnemyState.Chase || e.currentState == EnemyState.Attack))
            {
                inDanger = true;
                break;
            }
        }

        // Layer 2: Deep Dungeon Heartbeat Pulse
        if (clipDungeonPulse != null && now >= nextPulseTime)
        {
            float pulseInterval = inDanger ? 1.25f : 2.40f;
            nextPulseTime = now + pulseInterval;
            bgmPulseSource.pitch = inDanger ? 0.58f : 0.48f;
            bgmPulseSource.volume = inDanger ? 0.25f : 0.18f;
            bgmPulseSource.PlayOneShot(clipDungeonPulse);
        }

        // Layer 3: Distant Eerie Dungeon Chime & Creak Accents
        if (now >= nextAccentTime)
        {
            nextAccentTime = now + Random.Range(4.0f, 6.5f);
            if (clipsDungeonBells != null && clipsDungeonBells.Length > 0)
            {
                AudioClip bell = clipsDungeonBells[Random.Range(0, clipsDungeonBells.Length)];
                if (bell != null)
                {
                    bgmAccentSource.pitch = Random.Range(0.45f, 0.65f);
                    bgmAccentSource.volume = 0.13f;
                    bgmAccentSource.PlayOneShot(bell);
                }
            }
            if (clipDungeonCreak != null && Random.value < 0.45f)
            {
                bgmAccentSource.PlayOneShot(clipDungeonCreak, 0.22f);
            }
        }
    }

    private void TriggerNextBgmPadLayer(float now)
    {
        nextPadSwitchTime = now + padCycleDuration;
        if (clipsDungeonBells == null || clipsDungeonBells.Length == 0) return;

        AudioClip bellClip = clipsDungeonBells[bellProgressionIndex % clipsDungeonBells.Length];
        float targetPitch = darkMinorPitches[bellProgressionIndex % darkMinorPitches.Length];
        bellProgressionIndex++;

        if (bellClip == null) return;

        AudioSource activePad = usePadA ? bgmPadSourceA : bgmPadSourceB;
        usePadA = !usePadA;

        if (activePad != null)
        {
            activePad.Stop();
            activePad.clip = bellClip;
            activePad.pitch = targetPitch;
            activePad.volume = 0.28f;
            activePad.Play();
        }
    }

    private void UpdateMainMenuAudio()
    {
        MainMenuManager menu = MainMenuManager.Instance;
        if (menu == null) return;

        bool isCaraMainOpen = menu.caraMainPanel != null && menu.caraMainPanel.activeSelf;
        if (isCaraMainOpen != wasCaraMainOpen)
        {
            wasCaraMainOpen = isCaraMainOpen;
            Play2D(clipUiClick, 0.70f, 1.0f);
            Play2D(isCaraMainOpen ? clipBookOpen : clipBookClose, 0.65f, 1.0f);
        }

        if (menu.isGameStarted && !wasMenuGameStarted)
        {
            wasMenuGameStarted = true;
            Play2D(clipUiClick, 0.75f, 1.0f);
            Play2D(clipGateOpen1, 0.70f, 1.0f);
        }
    }

    private void UpdatePlayerAudio()
    {
        if (playerController == null) return;

        // 1. Cek HP & Lives Player (Hurt & Defeat)
        if (playerStats != null)
        {
            if (lastPlayerHP > 0f && playerStats.currentHP < lastPlayerHP - 0.1f)
            {
                PlayOnPlayerChild("Audio_PlayerHitImpact", clipPlayerHurt, 0.72f, Random.Range(0.95f, 1.05f));
            }
            lastPlayerHP = playerStats.currentHP;

            bool isGameOver = playerStats.IsGameOverModalOpen();
            if ((lastPlayerLives > 0 && playerStats.currentLives < lastPlayerLives) || (isGameOver && !wasGameOverOpen))
            {
                PlayOnPlayerChild("Audio_PlayerDefeat", clipPlayerDefeat, 0.80f, 0.85f);
            }
            lastPlayerLives = playerStats.currentLives;
            wasGameOverOpen = isGameOver;
        }

        // Jangan bunyikan langkah/serangan jika modal terbuka
        if (BossArenaManager.Instance != null && BossArenaManager.Instance.IsVictoryModalOpen()) return;
        if (playerStats != null && playerStats.IsGameOverModalOpen()) return;
        if (MainMenuManager.Instance != null && MainMenuManager.Instance.IsMenuOpen()) return;

        // 2. Suara Langkah Kaki Player (WASD / Arrow)
        Vector3 currentPos = playerController.transform.position;
        if (!hasPlayerPos)
        {
            lastPlayerPos = currentPos;
            hasPlayerPos = true;
        }

        Vector3 horizDelta = new Vector3(currentPos.x - lastPlayerPos.x, 0f, currentPos.z - lastPlayerPos.z);
        float horizSpeed = horizDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPlayerPos = currentPos;

        bool inputMoving = Mathf.Abs(Input.GetAxisRaw("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) > 0.1f;
        if ((inputMoving || horizSpeed > 0.4f) && Time.time >= nextPlayerStepTime)
        {
            bool isSprinting = Input.GetKey(KeyCode.LeftShift);
            float stepInterval = isSprinting ? 0.28f : 0.40f;
            nextPlayerStepTime = Time.time + stepInterval;

            if (clipsPlayerFootstep != null && clipsPlayerFootstep.Length > 0)
            {
                AudioClip stepClip = clipsPlayerFootstep[playerStepIndex % clipsPlayerFootstep.Length];
                playerStepIndex++;
                AudioSource playerSrc = playerController.GetComponent<AudioSource>();
                if (playerSrc != null && stepClip != null)
                {
                    playerSrc.pitch = Random.Range(0.94f, 1.06f);
                    playerSrc.PlayOneShot(stepClip, isSprinting ? 0.55f : 0.45f);
                }
                else
                {
                    Play2D(stepClip, isSprinting ? 0.50f : 0.40f, Random.Range(0.94f, 1.06f));
                }
            }
        }

        // 3. Suara Serangan Player (Tombol Space)
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= lastPlayerAttackAudioTime + playerController.attackCooldown - 0.02f)
        {
            lastPlayerAttackAudioTime = Time.time;
            PlayOnPlayerChild("Audio_PlayerAttack", clipPlayerAttack, 0.68f, Random.Range(0.95f, 1.05f));
        }
    }

    private void PlayOnPlayerChild(string childName, AudioClip clip, float volume, float pitch)
    {
        if (clip == null) return;
        if (playerController != null)
        {
            Transform child = playerController.transform.Find(childName);
            if (child != null)
            {
                AudioSource src = child.GetComponent<AudioSource>();
                if (src != null)
                {
                    src.pitch = pitch;
                    src.PlayOneShot(clip, volume);
                    return;
                }
            }
        }
        Play2D(clip, volume, pitch);
    }

    private void UpdateEnemiesAudio()
    {
        Vector3 listenerPos = playerController != null ? playerController.transform.position : transform.position;

        for (int i = 0; i < enemyTrackers.Count; i++)
        {
            EnemyAudioTracker t = enemyTrackers[i];
            EnemyAI e = t.enemy;
            if (e == null || !e.gameObject.activeInHierarchy) continue;

            // 1. Cek jika musuh terkena serangan (HP berkurang)
            if (e.currentHP < t.lastHP - 0.1f)
            {
                PlayOnPlayerChild("Audio_PlayerHitImpact", clipPlayerHitEnemy, 0.75f, Random.Range(0.92f, 1.06f));
            }
            t.lastHP = e.currentHP;

            // 2. Cek jika musuh baru saja mati
            if (e.isDead && !t.wasDead)
            {
                t.wasDead = true;
                if (t.attackSource != null && clipPlayerDefeat != null)
                {
                    t.attackSource.pitch = 0.90f;
                    t.attackSource.PlayOneShot(clipPlayerDefeat, 0.75f);
                }
                continue;
            }
            t.wasDead = e.isDead;
            if (e.isDead) continue;

            // 3. Cek mekanik khusus: Bull Charge & Stun (Hall 4 Pumpkin Hulk & Boss)
            if (e.isCharging && !t.wasCharging && t.attackSource != null && clipEnemyCharge != null)
            {
                t.attackSource.pitch = 0.85f;
                t.attackSource.PlayOneShot(clipEnemyCharge, 0.75f);
            }
            t.wasCharging = e.isCharging;

            if (e.isStunned && !t.wasStunned && t.attackSource != null && clipEnemyStun != null)
            {
                t.attackSource.pitch = 0.95f;
                t.attackSource.PlayOneShot(clipEnemyStun, 0.80f);
            }
            t.wasStunned = e.isStunned;

            // 4. Cek Langkah Kaki 3D Enemy (Hanya jika dalam jarak dengar pemain)
            Vector3 pos = e.transform.position;
            float distToPlayer = Vector3.Distance(pos, listenerPos);
            float movedDist = Vector3.Distance(new Vector3(pos.x, 0f, pos.z), new Vector3(t.lastPos.x, 0f, t.lastPos.z));
            float speed = movedDist / Mathf.Max(Time.deltaTime, 0.0001f);
            t.lastPos = pos;

            float maxAudibleDist = e.isBoss ? 28f : 18f;
            if (distToPlayer <= maxAudibleDist && speed > 0.35f && Time.time >= t.nextStepTime)
            {
                bool fastMove = e.currentState == EnemyState.Chase || e.currentState == EnemyState.Flee || e.isCharging;
                t.nextStepTime = Time.time + (fastMove ? 0.32f : 0.50f);
                if (t.stepSource != null && t.stepSource.clip != null)
                {
                    t.stepSource.PlayOneShot(t.stepSource.clip, t.stepSource.volume);
                }
            }

            // 5. Cek Serangan Enemy
            if (e.currentState == EnemyState.Attack && Time.time >= t.nextAttackSoundTime)
            {
                t.nextAttackSoundTime = Time.time + Mathf.Max(0.8f, e.attackCooldown);
                if (t.attackSource != null && t.attackSource.clip != null)
                {
                    t.attackSource.PlayOneShot(t.attackSource.clip, t.attackSource.volume);
                }
            }

            t.lastState = e.currentState;
        }
    }

    private void UpdateObjectivesAndGatesAudio()
    {
        // 1. Hall 2: Pengambilan Kunci, Penempatan di Rak, & Pembukaan Gerbang Hall 2
        Hall2ChallengeManager h2 = Hall2ChallengeManager.Instance;
        if (h2 != null)
        {
            if (h2.keysCarried > lastKeysCarried)
            {
                Play2D(clipKeyPickup, 0.78f, 1.0f);
            }
            lastKeysCarried = h2.keysCarried;

            if (h2.keysOnShelf > lastKeysOnShelf)
            {
                Play2D(clipKeyShelfClick, 0.75f, 1.0f);
                Play2D(clipKeyLatch, 0.78f, 1.0f);
            }
            lastKeysOnShelf = h2.keysOnShelf;

            if (h2.isChallengeCompleted && !wasHall2Completed)
            {
                wasHall2Completed = true;
                Play2D(clipGateOpen1, 0.85f, 0.95f);
            }
        }

        // 2. Hall 3: Menyalakan Tungku Api (Brazier) & Pembukaan Gerbang Hall 3
        Hall3ChallengeManager h3 = Hall3ChallengeManager.Instance;
        if (h3 != null)
        {
            if (h3.litBraziersCount > lastLitBraziers)
            {
                Play2D(clipBrazierIgnite, 0.75f, 1.0f);
                Play2D(clipBrazierBell, 0.72f, 1.0f);
            }
            lastLitBraziers = h3.litBraziersCount;

            if (h3.isChallengeCompleted && !wasHall3Completed)
            {
                wasHall3Completed = true;
                Play2D(clipGateOpen2, 0.85f, 0.95f);
            }
        }

        // 3. Hall 4: Menginjak & Mengunci Pressure Plate + Pembukaan Gerbang Hall 4
        Hall4ChallengeManager h4 = Hall4ChallengeManager.Instance;
        if (h4 != null)
        {
            if (h4.pressurePlates != null)
            {
                for (int i = 0; i < h4.pressurePlates.Count; i++)
                {
                    Hall4PressurePlate plate = h4.pressurePlates[i];
                    if (plate != null && plate.isPlayerOnPlate && !plate.isActivated && Time.time >= nextPlateTickTime)
                    {
                        nextPlateTickTime = Time.time + 0.50f;
                        float tickPitch = Mathf.Lerp(0.90f, 1.35f, plate.activationProgress);
                        Play2D(clipPlateTick, 0.45f, tickPitch);
                    }
                }
            }

            if (h4.activatedPlatesCount > lastLockedPlates)
            {
                Play2D(clipPlateLock, 0.80f, 1.0f);
            }
            lastLockedPlates = h4.activatedPlatesCount;

            if (h4.isChallengeCompleted && !wasHall4Completed)
            {
                wasHall4Completed = true;
                Play2D(clipGateOpen2, 0.85f, 0.92f);
            }
        }

        // 4. Math Gates (1A..4B)
        MathGate[] gates = Object.FindObjectsByType<MathGate>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < gates.Length; i++)
        {
            MathGate gate = gates[i];
            if (gate != null && gate.isUsed && !usedMathGates.Contains(gate))
            {
                usedMathGates.Add(gate);
                if (gate.pairedGate != null) usedMathGates.Add(gate.pairedGate);

                AudioSource gateSrc = gate.GetComponent<AudioSource>();
                if (gateSrc != null && gateSrc.clip != null)
                {
                    gateSrc.PlayOneShot(gateSrc.clip, 0.75f);
                }
                else
                {
                    Play2D(clipMathGateSwitch, 0.75f, 1.0f);
                }
                Play2D(clipKeyLatch, 0.55f, 1.15f);
            }
        }

        // 5. Boss Arena & Victory Chest
        BossArenaManager bossMgr = BossArenaManager.Instance;
        if (bossMgr != null)
        {
            if (bossMgr.isBossEnraged && !wasBossEnraged)
            {
                wasBossEnraged = true;
                Play2D(clipEnemyCharge, 0.80f, 0.75f);
            }

            if (bossMgr.isBossDefeated && !wasBossDefeated)
            {
                wasBossDefeated = true;
                Play2D(clipVictoryBell, 0.85f, 1.0f);
            }

            if (bossMgr.victoryChest != null && bossMgr.victoryChest.isOpened && !wasVictoryChestOpened)
            {
                wasVictoryChestOpened = true;
                Play2D(clipVictoryChest, 0.85f, 1.0f);
            }

            bool isVictoryOpen = bossMgr.IsVictoryModalOpen();
            if (isVictoryOpen && !wasVictoryModalOpen)
            {
                wasVictoryModalOpen = true;
                Play2D(clipVictoryChest, 0.85f, 1.0f);
                Play2D(clipVictoryBell, 0.85f, 1.05f);
            }
        }
    }

    public void Play2D(AudioClip clip, float volume = 0.7f, float pitch = 1.0f)
    {
        if (clip == null || sfx2DSource == null) return;
        sfx2DSource.pitch = pitch;
        sfx2DSource.PlayOneShot(clip, volume);
    }
}
