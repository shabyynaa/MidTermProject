using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager & Controller untuk Main Menu "MathGate Tactics".
/// Sesuai referensi:
/// - Judul: "RISK & BATTLE", "MATHGATE", "TACTICS"
/// - Deskripsi: "Pilih gerbangmu, hitung risikonya, lalu taklukkan dungeon sampai boss di ujungnya."
/// - Hanya 2 Tombol: START (Merah) & CARA MAIN (Slate)
/// - Footer: "UTS Game Cerdas · v0.1" & "WASD gerak · F1 debug AI"
/// - Panel Panduan "CARA MAIN" lengkap dengan instruksi kontrol & dungeon.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager Instance { get; private set; }

    [Header("Scene Configuration")]
    [Tooltip("Nama scene gameplay yang akan dimuat saat tombol START ditekan")]
    public string gameplaySceneName = "uts";

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject caraMainPanel;

    [Header("Buttons")]
    public Button btnStart;
    public Button btnCaraMain;
    public Button btnBackFromCaraMain;

    [Header("Runtime State")]
    public bool isGameStarted = false;

    private RectTransform btnStartRect;
    private RectTransform btnCaraMainRect;
    private RectTransform btnBackRect;

    public bool IsMenuOpen()
    {
        if (isGameStarted) return false;
        return (mainMenuPanel != null && mainMenuPanel.activeInHierarchy) ||
               (caraMainPanel != null && caraMainPanel.activeInHierarchy);
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        // Pastikan kursor mouse bebas & terlihat saat di Main Menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Start()
    {
        EnsureEventSystem();

        // Jika panel belum ter-assign di Inspector, bangun UI secara runtime otomatis
        if (mainMenuPanel == null)
        {
            SetupMainMenuUI();
        }

        // Hubungkan event tombol
        WireButtons();

        if (caraMainPanel != null) caraMainPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    private void Update()
    {
        // 1. Selama Main Menu terbuka, pertahankan kebebasan kursor mouse
        if (IsMenuOpen())
        {
            if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Keyboard Shortcuts
            if (caraMainPanel != null && caraMainPanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                {
                    OnCloseCaraMainClicked();
                    return;
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    OnStartClicked();
                    return;
                }
                else if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.H))
                {
                    OnCaraMainClicked();
                    return;
                }
            }

            // Direct Click Fallback untuk keandalan maksimal
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;
                if (caraMainPanel != null && caraMainPanel.activeSelf)
                {
                    if (btnBackRect != null && RectTransformUtility.RectangleContainsScreenPoint(btnBackRect, mousePos))
                    {
                        OnCloseCaraMainClicked();
                        return;
                    }
                }
                else
                {
                    if (btnStartRect != null && RectTransformUtility.RectangleContainsScreenPoint(btnStartRect, mousePos))
                    {
                        OnStartClicked();
                        return;
                    }
                    if (btnCaraMainRect != null && RectTransformUtility.RectangleContainsScreenPoint(btnCaraMainRect, mousePos))
                    {
                        OnCaraMainClicked();
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Menghubungkan listener pada tombol START, CARA MAIN, dan KEMBALI
    /// </summary>
    public void WireButtons()
    {
        if (btnStart != null)
        {
            btnStart.onClick.RemoveAllListeners();
            btnStart.onClick.AddListener(OnStartClicked);
            btnStartRect = btnStart.GetComponent<RectTransform>();
        }

        if (btnCaraMain != null)
        {
            btnCaraMain.onClick.RemoveAllListeners();
            btnCaraMain.onClick.AddListener(OnCaraMainClicked);
            btnCaraMainRect = btnCaraMain.GetComponent<RectTransform>();
        }

        if (btnBackFromCaraMain != null)
        {
            btnBackFromCaraMain.onClick.RemoveAllListeners();
            btnBackFromCaraMain.onClick.AddListener(OnCloseCaraMainClicked);
            btnBackRect = btnBackFromCaraMain.GetComponent<RectTransform>();
        }
    }

    /// <summary>
    /// Tombol START: Memulai permainan dengan memuat scene gameplay 'uts'
    /// </summary>
    public void OnStartClicked()
    {
        isGameStarted = true;
        Debug.Log($"<color=#00ff88><b>[MainMenu]</b> Memulai Game! Memuat scene gameplay: '{gameplaySceneName}'...</color>");

        string currentScene = SceneManager.GetActiveScene().name;

        // Jika berada di scene terpisah (misal MainMenu), muat scene gameplay
        if (!string.Equals(currentScene, gameplaySceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            // Jika MainMenu berada di dalam scene gameplay, tutup menu dan kunci kursor ke FPS
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (caraMainPanel != null) caraMainPanel.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// Tombol CARA MAIN: Menampilkan modal petunjuk bermain
    /// </summary>
    public void OnCaraMainClicked()
    {
        if (caraMainPanel != null)
        {
            caraMainPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Menutup modal petunjuk dan kembali ke Main Menu
    /// </summary>
    public void OnCloseCaraMainClicked()
    {
        if (caraMainPanel != null)
        {
            caraMainPanel.SetActive(false);
        }
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            UnityEngine.EventSystems.EventSystem existing = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (existing == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
    }

    /// <summary>
    /// Membuat struktur UI Main Menu secara runtime jika belum ada
    /// </summary>
    public void SetupMainMenuUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MainMenu_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.dynamicPixelsPerUnit = 2.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.dynamicPixelsPerUnit = 2.5f;
        }

        Transform existingCard = canvas.transform.Find("Panel_MainMenuCard");
        if (existingCard != null)
        {
            mainMenuPanel = existingCard.gameObject;
            btnStart = mainMenuPanel.transform.Find("Btn_Start")?.GetComponent<Button>();
            btnCaraMain = mainMenuPanel.transform.Find("Btn_CaraMain")?.GetComponent<Button>();
            Transform existingModal = canvas.transform.Find("Panel_CaraMainModal");
            if (existingModal != null)
            {
                caraMainPanel = existingModal.gameObject;
                btnBackFromCaraMain = caraMainPanel.transform.Find("CaraMain_Box/Btn_BackToMenu")?.GetComponent<Button>();
            }
            WireButtons();
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Sprite roundedSprite = CreateRuntimeRoundedSprite();

        // 1. Panel Menu Kiri (Dark Slate Card)
        GameObject cardObj = new GameObject("Panel_MainMenuCard", typeof(RectTransform));
        cardObj.transform.SetParent(canvas.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0f, 0.5f);
        cardRt.anchorMax = new Vector2(0f, 0.5f);
        cardRt.pivot = new Vector2(0f, 0.5f);
        cardRt.anchoredPosition = new Vector2(80f, 20f);
        cardRt.sizeDelta = new Vector2(560f, 540f);

        Image cardImg = cardObj.AddComponent<Image>();
        cardImg.sprite = roundedSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.14f, 0.17f, 0.22f, 0.94f);
        cardImg.raycastTarget = true;
        mainMenuPanel = cardObj;

        // Sub-Header: "R I S K   &   B A T T L E"
        CreateText(cardObj, "Txt_SubHeader", "R I S K   &   B A T T L E", font, 18, FontStyle.Bold,
            new Color(0.96f, 0.62f, 0.15f), TextAnchor.UpperLeft, new Vector2(40f, -44f), new Vector2(-80f, 28f));

        // Title Line 1: "MATHGATE"
        CreateText(cardObj, "Txt_TitleMathGate", "MATHGATE", font, 56, FontStyle.Bold,
            Color.white, TextAnchor.UpperLeft, new Vector2(40f, -80f), new Vector2(-80f, 66f));

        // Title Line 2: "TACTICS"
        CreateText(cardObj, "Txt_TitleTactics", "TACTICS", font, 56, FontStyle.Bold,
            new Color(0.96f, 0.72f, 0.16f), TextAnchor.UpperLeft, new Vector2(40f, -148f), new Vector2(-80f, 66f));

        // Deskripsi Singkat
        GameObject descObj = CreateText(cardObj, "Txt_Description",
            "Pilih gerbangmu, hitung risikonya, lalu taklukkan dungeon sampai boss di ujungnya.",
            font, 20, FontStyle.Normal, new Color(0.72f, 0.78f, 0.86f), TextAnchor.UpperLeft,
            new Vector2(40f, -232f), new Vector2(-80f, 85f));
        descObj.GetComponent<Text>().lineSpacing = 1.25f;

        // Tombol 1: START (Merah)
        GameObject btnStartObj = CreateButton(cardObj, "Btn_Start", "S T A R T", font, 24,
            new Color(0.90f, 0.18f, 0.18f), new Color(1f, 0.30f, 0.30f), new Color(0.70f, 0.10f, 0.10f),
            roundedSprite, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -345f), new Vector2(-80f, 66f));
        btnStart = btnStartObj.GetComponent<Button>();

        // Tombol 2: CARA MAIN (Slate / Gelap)
        GameObject btnCaraMainObj = CreateButton(cardObj, "Btn_CaraMain", "C A R A   M A I N", font, 22,
            new Color(0.20f, 0.24f, 0.30f), new Color(0.28f, 0.34f, 0.42f), new Color(0.12f, 0.15f, 0.19f),
            roundedSprite, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -428f), new Vector2(-80f, 66f));
        btnCaraMain = btnCaraMainObj.GetComponent<Button>();

        // Footer Teks Kiri Bawah: "UTS Game Cerdas · v0.1" (Warna Silver Terang & Kontras Tinggi)
        CreateTextAbsolute(canvas.gameObject, "Txt_FooterLeft", "UTS Game Cerdas · v0.1", font, 18, FontStyle.Bold,
            new Color(0.88f, 0.92f, 0.96f, 0.95f), TextAnchor.LowerLeft,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(45f, 26f), new Vector2(400f, 36f));

        // Footer Teks Kanan Bawah: "WASD gerak · F1 debug AI" (Warna Silver Terang & Kontras Tinggi)
        CreateTextAbsolute(canvas.gameObject, "Txt_FooterRight", "WASD gerak · F1 debug AI", font, 18, FontStyle.Bold,
            new Color(0.88f, 0.92f, 0.96f, 0.95f), TextAnchor.LowerRight,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-45f, 26f), new Vector2(400f, 36f));

        // Modal Panel "CARA MAIN"
        SetupCaraMainModalRuntime(canvas.gameObject, roundedSprite, font);

        WireButtons();
    }

    private void SetupCaraMainModalRuntime(GameObject canvasObj, Sprite roundedSprite, Font font)
    {
        GameObject modalRoot = new GameObject("Panel_CaraMainModal", typeof(RectTransform));
        modalRoot.transform.SetParent(canvasObj.transform, false);
        RectTransform modalRt = modalRoot.GetComponent<RectTransform>();
        modalRt.anchorMin = Vector2.zero;
        modalRt.anchorMax = Vector2.one;
        modalRt.sizeDelta = Vector2.zero;

        Image modalBg = modalRoot.AddComponent<Image>();
        modalBg.color = new Color(0f, 0f, 0f, 0.85f);
        modalBg.raycastTarget = true;
        caraMainPanel = modalRoot;

        GameObject boxObj = new GameObject("CaraMain_Box", typeof(RectTransform));
        boxObj.transform.SetParent(modalRoot.transform, false);
        RectTransform boxRt = boxObj.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.anchoredPosition = Vector2.zero;
        boxRt.sizeDelta = new Vector2(1260f, 840f);

        Image boxImg = boxObj.AddComponent<Image>();
        boxImg.sprite = roundedSprite;
        boxImg.type = Image.Type.Sliced;
        boxImg.color = new Color(0.10f, 0.12f, 0.17f, 0.98f);

        CreateTextAbsolute(boxObj, "HeaderTitle", "★ PANDUAN CARA BERMAIN MATHGATE TACTICS ★", font, 32, FontStyle.Bold,
            new Color(0.96f, 0.72f, 0.16f), TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -28f), new Vector2(-60f, 48f));

        CreateTextAbsolute(boxObj, "HeaderSub", "Taklukkan setiap tantangan matematika & bertarunglah hingga lantai terdalam!", font, 18, FontStyle.Italic,
            new Color(0.70f, 0.78f, 0.90f), TextAnchor.MiddleCenter,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -74f), new Vector2(-60f, 30f));

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

        GameObject contentObj = new GameObject("GuideContent", typeof(RectTransform));
        contentObj.transform.SetParent(boxObj.transform, false);
        RectTransform cRt = contentObj.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0f, 0f);
        cRt.anchorMax = new Vector2(1f, 1f);
        cRt.offsetMin = new Vector2(50f, 95f);
        cRt.offsetMax = new Vector2(-50f, -115f);
        Text cTxt = contentObj.AddComponent<Text>();
        cTxt.text = guideText;
        cTxt.font = font;
        cTxt.fontSize = 18;
        cTxt.color = Color.white;
        cTxt.lineSpacing = 1.22f;
        cTxt.raycastTarget = false;

        Outline contentOutline = contentObj.AddComponent<Outline>();
        contentOutline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        contentOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject btnBackObj = CreateButton(boxObj, "Btn_BackToMenu", "K E M B A L I   K E   M E N U", font, 20,
            new Color(0.15f, 0.68f, 0.38f), new Color(0.18f, 0.82f, 0.45f), new Color(0.10f, 0.48f, 0.26f),
            roundedSprite, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 30f), new Vector2(360f, 54f));
        btnBackFromCaraMain = btnBackObj.GetComponent<Button>();

        modalRoot.SetActive(false);
    }

    private GameObject CreateText(GameObject parent, string name, string content, Font font, int fontSize,
        FontStyle style, Color color, TextAnchor alignment, Vector2 pos, Vector2 size)
    {
        return CreateTextAbsolute(parent, name, content, font, fontSize, style, color, alignment,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), pos, size);
    }

    private GameObject CreateTextAbsolute(GameObject parent, string name, string content, Font font, int fontSize,
        FontStyle style, Color color, TextAnchor alignment,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent.transform, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

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

    private GameObject CreateButton(GameObject parent, string name, string label, Font font, int fontSize,
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

    private Sprite CreateRuntimeRoundedSprite()
    {
        int size = 64;
        int radius = 16;
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

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }
}
