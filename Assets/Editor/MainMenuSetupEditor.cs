using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Setup Otomatis Main Menu "MathGate Tactics" sesuai gambar referensi:
/// 1. Membuat scene Assets/Scenes/MainMenu.unity
/// 2. Membuat 3D Dungeon Backdrop (Dinding batu, pilar, lantai, 2 obor bernyala hangat, dan karakter Kachujin tampak belakang)
/// 3. Membuat Canvas UI responsif (Scale with Screen Size 1920x1080)
/// 4. Panel Card gelap transparan di kiri dengan hierarki judul (RISK & BATTLE, MATHGATE, TACTICS) & deskripsi
/// 5. Dua tombol: START (Merah) & CARA MAIN (Slate)
/// 6. Footer teks: "UTS Game Cerdas · v0.1" (kiri bawah) & "WASD gerak · F1 debug AI" (kanan bawah)
/// 7. Modal Panel "CARA MAIN" lengkap dengan instruksi kontrol & gameplay + tombol KEMBALI
/// 8. Konfigurasi EditorBuildSettings: Scene 0 = MainMenu, Scene 1 = uts (gameplay)
/// 
/// Menu: Tools → Build Main Menu Scene (MathGate Tactics)
/// </summary>
[InitializeOnLoad]
public class MainMenuSetupEditor : EditorWindow
{
    const string KAYKIT_PATH = "Assets/Art/Kaykit_Dungeon/fbx(unity)";
    const string SCENE_PATH = "Assets/Scenes/MainMenu.unity";
    const string GAMEPLAY_SCENE_PATH = "Assets/uts.unity";

    static MainMenuSetupEditor()
    {
        EditorApplication.delayCall += AutoEnsureMainMenu;
    }

    static void AutoEnsureMainMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        bool hasHDEnv = false;
        if (File.Exists(SCENE_PATH))
        {
            string sceneText = File.ReadAllText(SCENE_PATH);
            hasHDEnv = sceneText.Contains("MainMenu_Environment") && sceneText.Contains("1260") && sceneText.Contains("0.88");
        }

        if (!hasHDEnv)
        {
            BuildMainMenuSceneInternal(silent: true);
        }
        else
        {
            ConfigureBuildSettings();
        }
    }

    [MenuItem("Tools/Build Main Menu Scene (MathGate Tactics)")]
    public static void BuildMainMenuScene()
    {
        BuildMainMenuSceneInternal(silent: false);
    }

    [MenuItem("Tools/Open Main Menu Scene")]
    public static void OpenMainMenuScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SCENE_PATH);
        }
    }

    [MenuItem("Tools/Open Gameplay Scene (uts)")]
    public static void OpenGameplayScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(GAMEPLAY_SCENE_PATH);
        }
    }

    public static void BuildMainMenuSceneInternal(bool silent)
    {
        Debug.Log("<color=cyan>[MainMenuSetup]</color> Memulai pembuatan Scene Main Menu MathGate Tactics...");

        // 1. Simpan scene yang sedang terbuka jika perlu
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            EditorSceneManager.SaveOpenScenes();
        }

        // 2. Pastikan folder Assets/Scenes & Assets/Art/UI ada
        EnsureDirectories();

        // 3. Buat Sprite 9-slice Rounded Rectangle & Shadow Circle
        Sprite roundedRectSprite = GetOrCreateRoundedRectSprite();
        Sprite shadowSprite = GetOrCreateShadowSprite();

        // 4. Buat Scene Baru MainMenu
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 5. Setup Lighting & RenderSettings
        SetupRenderSettings();

        // 6. Buat Main Camera & Directional Light
        Camera cam = SetupCamera();
        SetupDirectionalLight();

        // 7. Buat 3D Dungeon Environment Backdrop
        Setup3DEnvironment(shadowSprite);

        // 8. Buat Canvas UI & EventSystem
        SetupUI(roundedRectSprite);

        // 9. Simpan Scene ke Assets/Scenes/MainMenu.unity
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Debug.Log($"<color=#00ff88>[MainMenuSetup] Scene berhasil disimpan di '{SCENE_PATH}'!</color>");

        // 10. Konfigurasi Build Settings
        ConfigureBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!silent)
        {
            EditorUtility.DisplayDialog("Sukses!",
                "Main Menu MathGate Tactics Berhasil Dibangun!\n\n" +
                "• Scene: Assets/Scenes/MainMenu.unity\n" +
                "• Gameplay Scene terhubung: Assets/uts.unity\n" +
                "• Tombol START: Memuat scene gameplay 'uts'\n" +
                "• Tombol CARA MAIN: Menampilkan panduan kontrol & dungeon\n" +
                "• Build Settings: Scene 0 = MainMenu, Scene 1 = uts\n\n" +
                "Tekan Play untuk langsung mencobanya!", "Siap!");
        }
    }

    private static void EnsureDirectories()
    {
        if (!Directory.Exists("Assets/Scenes")) Directory.CreateDirectory("Assets/Scenes");
        if (!Directory.Exists("Assets/Art/UI")) Directory.CreateDirectory("Assets/Art/UI");
    }

    private static void SetupRenderSettings()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f, 0.28f, 0.35f, 1f);
        RenderSettings.fog = false;
    }

    private static Camera SetupCamera()
    {
        GameObject camObj = new GameObject("Main Camera");
        camObj.tag = "MainCamera";
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.13f, 0.15f, 0.18f, 1f);
        cam.fieldOfView = 42f;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        camObj.transform.position = new Vector3(0f, 1.85f, -4.5f);
        camObj.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        camObj.AddComponent<AudioListener>();
        return camObj.GetComponent<Camera>();
    }

    private static void SetupDirectionalLight()
    {
        GameObject lightObj = new GameObject("Directional Light");
        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.38f, 0.44f, 0.52f);
        light.intensity = 0.5f;
        lightObj.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
    }

    private static void Setup3DEnvironment(Sprite shadowSprite)
    {
        GameObject envRoot = new GameObject("MainMenu_Environment");

        // 1. Lantai Batu (KayKit floor_tile_large)
        GameObject groundRoot = new GameObject("Ground");
        groundRoot.transform.SetParent(envRoot.transform);
        for (float z = -4f; z <= 2f; z += 2f)
        {
            for (float x = -6f; x <= 6f; x += 2f)
            {
                PlaceModel(groundRoot, $"Floor_{x}_{z}", "floor_tile_large", new Vector3(x, 0f, z), 0f);
            }
        }

        // 2. Dinding Batu di Belakang (Z = 2.0m)
        GameObject wallRoot = new GameObject("Walls");
        wallRoot.transform.SetParent(envRoot.transform);

        float[] xWalls = { -4f, -2f, 2f, 4f };
        float[] yHeights = { 0f, 2.05f, 4.10f };

        foreach (float y in yHeights)
        {
            foreach (float x in xWalls)
            {
                PlaceModel(wallRoot, $"Wall_{x}_{y}", "wall", new Vector3(x, y, 2.0f), 180f);
            }
        }

        // 3. Bagian Tengah (Pintu Gerbang / Pilar Gelap seperti di referensi)
        PlaceModel(wallRoot, "Wall_Center_Door", "wall_arched", new Vector3(0f, 0f, 2.0f), 180f);
        PlaceModel(wallRoot, "Wall_Center_Pillar_L", "wall_pillar", new Vector3(-0.95f, 0f, 1.95f), 180f);
        PlaceModel(wallRoot, "Wall_Center_Pillar_R", "wall_pillar", new Vector3(0.95f, 0f, 1.95f), 180f);
        PlaceModel(wallRoot, "Wall_Center_Top1", "wall", new Vector3(0f, 2.05f, 2.0f), 180f);
        PlaceModel(wallRoot, "Wall_Center_Top2", "wall", new Vector3(0f, 4.10f, 2.0f), 180f);

        // 4. Obor Kiri (Warm Amber Glow)
        GameObject torchL = PlaceModel(envRoot, "Torch_Mounted_Left", "torch_mounted", new Vector3(-1.15f, 1.95f, 1.95f), 180f);
        GameObject lightLObj = new GameObject("Light_Torch_Left");
        lightLObj.transform.SetParent(torchL.transform);
        lightLObj.transform.localPosition = new Vector3(0f, 0.25f, -0.2f);
        Light lightL = lightLObj.AddComponent<Light>();
        lightL.type = LightType.Point;
        lightL.color = new Color(1f, 0.72f, 0.22f); // Warna api hangat
        lightL.intensity = 4.5f;
        lightL.range = 5.5f;

        // 5. Obor Kanan (Warm Amber Glow)
        GameObject torchR = PlaceModel(envRoot, "Torch_Mounted_Right", "torch_mounted", new Vector3(2.45f, 1.95f, 1.95f), 180f);
        GameObject lightRObj = new GameObject("Light_Torch_Right");
        lightRObj.transform.SetParent(torchR.transform);
        lightRObj.transform.localPosition = new Vector3(0f, 0.25f, -0.2f);
        Light lightR = lightRObj.AddComponent<Light>();
        lightR.type = LightType.Point;
        lightR.color = new Color(1f, 0.72f, 0.22f); // Warna api hangat
        lightR.intensity = 4.5f;
        lightR.range = 5.5f;

        // 6. Karakter Player (Kachujin G Rosales) di sisi kanan tampak belakang
        string playerFbxPath = "Assets/Art/Player/Kachujin G Rosales.fbx";
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerFbxPath);
        if (playerPrefab != null)
        {
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Kachujin_PlayerModel";
            player.transform.SetParent(envRoot.transform);
            player.transform.localPosition = new Vector3(1.85f, 0f, 0.45f);
            player.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            player.transform.localScale = Vector3.one * 1.3f;

            // Pasang Animator Controller agar animasi Idle bernapas aktif
            Animator anim = player.GetComponent<Animator>();
            if (anim == null) anim = player.AddComponent<Animator>();
            RuntimeAnimatorController rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Player/Kachujin_AnimatorController.controller");
            if (rac != null) anim.runtimeAnimatorController = rac;
        }

        // 7. Bayangan Lembut di Bawah Kaki Karakter
        GameObject shadowObj = new GameObject("Kachujin_Shadow");
        shadowObj.transform.SetParent(envRoot.transform);
        shadowObj.transform.localPosition = new Vector3(1.85f, 0.015f, 0.45f);
        shadowObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shadowObj.transform.localScale = new Vector3(1.1f, 0.75f, 1f);

        SpriteRenderer sr = shadowObj.AddComponent<SpriteRenderer>();
        sr.sprite = shadowSprite;
        sr.color = new Color(0f, 0f, 0f, 0.65f);
        Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (spriteShader == null) spriteShader = Shader.Find("Sprites/Default");
        if (spriteShader != null) sr.material = new Material(spriteShader);
    }

    private static void SetupUI(Sprite roundedRectSprite)
    {
        // 1. Canvas
        GameObject canvasObj = new GameObject("MainMenu_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 2.5f; // Resolusi teks HD ultra tajam

        canvasObj.AddComponent<GraphicRaycaster>();

        // 2. EventSystem
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 3. MainMenuManager
        MainMenuManager manager = canvasObj.AddComponent<MainMenuManager>();
        manager.gameplaySceneName = "uts";

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 4. Panel Menu Kiri (Dark Slate Card)
        GameObject cardObj = new GameObject("Panel_MainMenuCard", typeof(RectTransform));
        cardObj.transform.SetParent(canvasObj.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0f, 0.5f);
        cardRt.anchorMax = new Vector2(0f, 0.5f);
        cardRt.pivot = new Vector2(0f, 0.5f);
        cardRt.anchoredPosition = new Vector2(80f, 20f);
        cardRt.sizeDelta = new Vector2(560f, 540f);

        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.sprite = roundedRectSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.14f, 0.17f, 0.22f, 0.94f);
        cardImg.raycastTarget = true;

        manager.mainMenuPanel = cardObj;

        // Sub-Header: "R I S K   &   B A T T L E"
        GameObject subHeadObj = CreateText(cardObj, "Txt_SubHeader", "R I S K   &   B A T T L E", font, 18, FontStyle.Bold,
            new Color(0.96f, 0.62f, 0.15f), TextAnchor.UpperLeft);
        RectTransform shRt = subHeadObj.GetComponent<RectTransform>();
        shRt.anchorMin = new Vector2(0f, 1f);
        shRt.anchorMax = new Vector2(1f, 1f);
        shRt.pivot = new Vector2(0f, 1f);
        shRt.anchoredPosition = new Vector2(40f, -44f);
        shRt.sizeDelta = new Vector2(-80f, 28f);

        // Title Line 1: "MATHGATE"
        GameObject t1Obj = CreateText(cardObj, "Txt_TitleMathGate", "MATHGATE", font, 56, FontStyle.Bold,
            Color.white, TextAnchor.UpperLeft);
        RectTransform t1Rt = t1Obj.GetComponent<RectTransform>();
        t1Rt.anchorMin = new Vector2(0f, 1f);
        t1Rt.anchorMax = new Vector2(1f, 1f);
        t1Rt.pivot = new Vector2(0f, 1f);
        t1Rt.anchoredPosition = new Vector2(40f, -80f);
        t1Rt.sizeDelta = new Vector2(-80f, 66f);

        // Title Line 2: "TACTICS"
        GameObject t2Obj = CreateText(cardObj, "Txt_TitleTactics", "TACTICS", font, 56, FontStyle.Bold,
            new Color(0.96f, 0.72f, 0.16f), TextAnchor.UpperLeft);
        RectTransform t2Rt = t2Obj.GetComponent<RectTransform>();
        t2Rt.anchorMin = new Vector2(0f, 1f);
        t2Rt.anchorMax = new Vector2(1f, 1f);
        t2Rt.pivot = new Vector2(0f, 1f);
        t2Rt.anchoredPosition = new Vector2(40f, -148f);
        t2Rt.sizeDelta = new Vector2(-80f, 66f);

        // Deskripsi Singkat
        GameObject descObj = CreateText(cardObj, "Txt_Description",
            "Pilih gerbangmu, hitung risikonya, lalu taklukkan dungeon sampai boss di ujungnya.",
            font, 20, FontStyle.Normal, new Color(0.72f, 0.78f, 0.86f), TextAnchor.UpperLeft);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 1f);
        descRt.anchorMax = new Vector2(1f, 1f);
        descRt.pivot = new Vector2(0f, 1f);
        descRt.anchoredPosition = new Vector2(40f, -232f);
        descRt.sizeDelta = new Vector2(-80f, 85f);
        Text descText = descObj.GetComponent<Text>();
        descText.lineSpacing = 1.25f;

        // Tombol 1: START (Merah)
        GameObject btnStartObj = CreateButton(cardObj, "Btn_Start", "S T A R T", font, 24,
            new Color(0.90f, 0.18f, 0.18f),
            new Color(1f, 0.30f, 0.30f),
            new Color(0.70f, 0.10f, 0.10f),
            roundedRectSprite,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -345f), new Vector2(-80f, 66f));
        manager.btnStart = btnStartObj.GetComponent<Button>();

        // Tombol 2: CARA MAIN (Slate / Gelap)
        GameObject btnCaraMainObj = CreateButton(cardObj, "Btn_CaraMain", "C A R A   M A I N", font, 22,
            new Color(0.20f, 0.24f, 0.30f),
            new Color(0.28f, 0.34f, 0.42f),
            new Color(0.12f, 0.15f, 0.19f),
            roundedRectSprite,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -428f), new Vector2(-80f, 66f));
        manager.btnCaraMain = btnCaraMainObj.GetComponent<Button>();

        // 5. Footer Teks Kiri Bawah: "UTS Game Cerdas · v0.1" (Warna Silver Terang & Kontras Tinggi)
        GameObject fLeftObj = CreateText(canvasObj, "Txt_FooterLeft", "UTS Game Cerdas · v0.1", font, 18, FontStyle.Bold,
            new Color(0.88f, 0.92f, 0.96f, 0.95f), TextAnchor.LowerLeft);
        RectTransform fLeftRt = fLeftObj.GetComponent<RectTransform>();
        fLeftRt.anchorMin = new Vector2(0f, 0f);
        fLeftRt.anchorMax = new Vector2(0f, 0f);
        fLeftRt.pivot = new Vector2(0f, 0f);
        fLeftRt.anchoredPosition = new Vector2(45f, 26f);
        fLeftRt.sizeDelta = new Vector2(400f, 36f);

        // 6. Footer Teks Kanan Bawah: "WASD gerak · F1 debug AI" (Warna Silver Terang & Kontras Tinggi)
        GameObject fRightObj = CreateText(canvasObj, "Txt_FooterRight", "WASD gerak · F1 debug AI", font, 18, FontStyle.Bold,
            new Color(0.88f, 0.92f, 0.96f, 0.95f), TextAnchor.LowerRight);
        RectTransform fRightRt = fRightObj.GetComponent<RectTransform>();
        fRightRt.anchorMin = new Vector2(1f, 0f);
        fRightRt.anchorMax = new Vector2(1f, 0f);
        fRightRt.pivot = new Vector2(1f, 0f);
        fRightRt.anchoredPosition = new Vector2(-45f, 26f);
        fRightRt.sizeDelta = new Vector2(400f, 36f);

        // 7. Modal Panel "CARA MAIN" (Petunjuk Bermain)
        SetupCaraMainModal(canvasObj, manager, roundedRectSprite, font);
    }

    private static void SetupCaraMainModal(GameObject canvasObj, MainMenuManager manager, Sprite roundedRectSprite, Font font)
    {
        // Backdrop Overlay Redup (Full Screen)
        GameObject modalRoot = new GameObject("Panel_CaraMainModal", typeof(RectTransform));
        modalRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform modalRt = modalRoot.GetComponent<RectTransform>();
        modalRt.anchorMin = Vector2.zero;
        modalRt.anchorMax = Vector2.one;
        modalRt.sizeDelta = Vector2.zero;

        Image modalBg = modalRoot.AddComponent<Image>();
        modalBg.color = new Color(0f, 0f, 0f, 0.85f);
        modalBg.raycastTarget = true;

        manager.caraMainPanel = modalRoot;

        // Kotak Konten Petunjuk (Card Tengah Besar)
        GameObject boxObj = new GameObject("CaraMain_Box", typeof(RectTransform));
        boxObj.transform.SetParent(modalRoot.transform, false);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(1260f, 840f);

        Image boxImg = boxObj.AddComponent<Image>();
        boxImg.sprite = roundedRectSprite;
        boxImg.type = Image.Type.Sliced;
        boxImg.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);

        // Header Modal
        GameObject headerObj = CreateText(boxObj, "HeaderTitle", "★ PANDUAN CARA BERMAIN MATHGATE TACTICS ★",
            font, 32, FontStyle.Bold, new Color(0.96f, 0.72f, 0.16f), TextAnchor.MiddleCenter);
        RectTransform hRt = headerObj.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0f, -28f);
        hRt.sizeDelta = new Vector2(-60f, 48f);

        // Subheader
        GameObject subHeaderObj = CreateText(boxObj, "HeaderSub", "Taklukkan setiap tantangan matematika & bertarunglah hingga lantai terdalam!",
            font, 18, FontStyle.Italic, new Color(0.70f, 0.78f, 0.90f), TextAnchor.MiddleCenter);
        RectTransform subRt = subHeaderObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -74f);
        subRt.sizeDelta = new Vector2(-60f, 30f);

        // Konten Lengkap Petunjuk Gameplay
        string guideText =
            "<b><size=22><color=#F5B82A>1. TUJUAN PERMAINAN</color></size></b>\n" +
            "Jelajahi 4 Hall Dungeon, pecahkan logika gerbang matematika, bertahan dari patroli monster, dan kalahkan Boss <b>Maw J Laygo</b> di Boss Arena untuk membuka Peti Kemenangan Emas di Altar.\n\n" +

            "<b><size=22><color=#00E5FF>2. KONTROL KARAKTER</color></size></b>\n" +
            "• <b>WASD / Tombol Panah</b> : Berjalan\n" +
            "• <b>Tahan Left Shift</b> : Berlari Cepat (Sprint)\n" +
            "• <b>Gerakan Mouse</b> : Mengarahkan pandangan & kamera\n" +
            "• <b>Tombol V</b> : Ganti sudut pandang Third-Person (Kachujin) ⇄ First-Person (POV Mata)\n" +
            "• <b>Scroll Mouse</b> : Zoom in / out jarak pandang kamera\n" +
            "• <b>Tombol Spasi</b> : Serangan jarak dekat (Melee Attack) saat berada di dekat musuh\n" +
            "• <b>Klik Kiri / Tombol E</b> : Ambil kunci (Hall 2), nyalakan obor (Hall 3), & buka peti emas altar\n" +
            "• <b>Tombol F1</b> : Tampilkan info status & debug AI musuh\n\n" +

            "<b><size=22><color=#FFD700>3. GERBANG MATEMATIKA & PERHITUNGAN RISIKO</color></size></b>\n" +
            "Di setiap Hall terdapat 2 lorong gerbang (Gate A vs Gate B) dengan kartu hitungan matematika.\n" +
            "• Hitung risikonya: Pilih gerbang dengan kalkulasi terbaik untuk memperkuat HP, ATK, atau Speed.\n" +
            "• Gerbang yang salah memberi penalti stat atau mengarahkan ke musuh dengan level lebih tinggi!\n\n" +

            "<b><size=22><color=#FF6B6B>4. TANTANGAN 4 HALL & BOSS ARENA</color></size></b>\n" +
            "• <b>Hall 1</b> : Hindari patroli Pumpkin Hulk & lewati gerbang matematika pembuka.\n" +
            "• <b>Hall 2 (Stealth)</b> : Kumpulkan kunci tersembunyi tanpa tertangkap Vampire, taruh di rak kunci.\n" +
            "• <b>Hall 3 (Light & Dark)</b> : Selalu berlindung di dalam lingkaran cahaya tungku api agar selamat dari sergapan Goblin.\n" +
            "• <b>Hall 4 (Pelat Tekanan)</b> : Injak kombinasi pressure plate yang tepat untuk membuka gerbang arena.\n" +
            "• <b>Boss Arena (The Abyssal Apex)</b> : Taklukkan Maw J Laygo (Fase 1 Savage Hunter & Fase 2 Blood Moon), lalu ambil harta di Peti Emas Altar!";

        GameObject contentObj = CreateText(boxObj, "GuideContent", guideText, font, 18, FontStyle.Normal, Color.white, TextAnchor.UpperLeft);
        RectTransform cRt = contentObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 0f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.offsetMin = new Vector2(50f, 95f);
        cRt.offsetMax = new Vector2(-50f, -115f);
        Text cTxt = contentObj.GetComponent<Text>();
        cTxt.lineSpacing = 1.22f;

        // Tombol Kembali
        GameObject btnBackObj = CreateButton(boxObj, "Btn_BackToMenu", "K E M B A L I   K E   M E N U", font, 20,
            new Color(0.15f, 0.68f, 0.38f),
            new Color(0.18f, 0.82f, 0.45f),
            new Color(0.10f, 0.48f, 0.26f),
            roundedRectSprite,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 30f), new Vector2(360f, 54f));
        manager.btnBackFromCaraMain = btnBackObj.GetComponent<Button>();

        modalRoot.SetActive(false);
    }

    private static GameObject CreateText(GameObject parent, string name, string content, Font font, int fontSize,
        FontStyle style, Color color, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent.transform, false);
        Text text = obj.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;

        // Outline agar teks tajam dan ber-kontras tinggi di semua resolusi
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return obj;
    }

    private static GameObject CreateButton(GameObject parent, string name, string label, Font font, int fontSize,
        Color normalColor, Color hoverColor, Color pressedColor, Sprite sprite,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform));
        btnObj.transform.SetParent(parent.transform, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = normalColor;
        img.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor = pressedColor;
        cb.selectedColor = hoverColor;
        btn.colors = cb;

        GameObject txtObj = new GameObject("Text", typeof(RectTransform));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform tRt = txtObj.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.sizeDelta = Vector2.zero;

        Text text = txtObj.AddComponent<Text>();
        text.text = label;
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;

        return btnObj;
    }

    private static GameObject PlaceModel(GameObject parent, string goName, string assetName, Vector3 pos, float rotY = 0f)
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
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        return go;
    }

    public static Sprite GetOrCreateRoundedRectSprite()
    {
        string path = "Assets/Art/UI/UI_RoundedRect.png";
        if (!File.Exists(path))
        {
            int size = 128;
            int radius = 28;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = x < radius ? radius : (x >= size - radius ? size - radius - 1 : x);
                    float cy = y < radius ? radius : (y >= size - radius ? size - radius - 1 : y);
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));

                    float alpha = 1f;
                    if (dist > radius - 1f)
                    {
                        alpha = Mathf.Clamp01(radius - dist);
                    }
                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spriteBorder = new Vector4(30, 30, 30, 30);
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static Sprite GetOrCreateShadowSprite()
    {
        string path = "Assets/Art/UI/UI_ShadowCircle.png";
        if (!File.Exists(path))
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float t = Mathf.Clamp01(d / maxR);
                    float a = Mathf.SmoothStep(0.75f, 0f, t);
                    colors[y * size + x] = new Color(0f, 0f, 0f, a);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static void ConfigureBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
        scenes.Add(new EditorBuildSettingsScene(SCENE_PATH, true));
        scenes.Add(new EditorBuildSettingsScene(GAMEPLAY_SCENE_PATH, true));

        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("<color=green>[BuildSettings] Scenes dikonfigurasi: 0. MainMenu, 1. uts (gameplay)</color>");
    }
}
