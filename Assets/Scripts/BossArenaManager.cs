using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manager Pertarungan Boss Arena: "The Abyssal Apex"
/// 1. Mengunci gerbang masuk saat pemain melangkah ke dalam Boss Arena.
/// 2. Menampilkan Grand Boss Health Bar di atas layar (Sinkron dengan HP Maw J Laygo).
/// 3. Mengelola Transisi Fase 1 (Savage Hunter) -> Fase 2 (Enraged Blood Moon).
/// 4. Membuka Peti Emas Kemenangan di Altar saat Boss mati.
/// 5. Menampilkan Layar Kemenangan Akhir (Victory / Game Clear Modal).
/// </summary>
public class BossArenaManager : MonoBehaviour
{
    public static BossArenaManager Instance { get; private set; }

    [Header("Encounter Status")]
    public bool encounterStarted = false;
    public bool isBossEnraged = false;
    public bool isBossDefeated = false;

    [Header("Scene References")]
    public EnemyAI bossEnemy;
    public VictoryChest victoryChest;
    public GameObject entranceBarrierA;
    public GameObject entranceBarrierB;

    [Header("Boss Bar UI")]
    public GameObject bossBarPanel;
    public Slider bossHPSlider;
    public Image bossHPFillImage;
    public Text bossTitleText;
    public Text bossSubtext;

    [Header("Victory Screen UI")]
    public GameObject victoryModalPanel;
    public Text victoryStatsText;
    private RectTransform btnRestartRect;
    private RectTransform btnCloseRect;

    private Transform playerTransform;

    public bool IsVictoryModalOpen()
    {
        return victoryModalPanel != null && victoryModalPanel.activeInHierarchy;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        EnsureReferences();
    }

    private void Start()
    {
        FindPlayer();
        SetupBossBarUI();
        SetupVictoryModalUI();

        // Barrier pintu masuk default tidak aktif sampai pemain melangkah masuk
        if (entranceBarrierA != null) entranceBarrierA.SetActive(false);
        if (entranceBarrierB != null) entranceBarrierB.SetActive(false);
        if (bossBarPanel != null) bossBarPanel.SetActive(false);
        if (victoryModalPanel != null) victoryModalPanel.SetActive(false);
    }

    private void Update()
    {
        if (playerTransform == null) FindPlayer();

        // 1. Deteksi masuk arena untuk memicu Lockdown Encounter
        if (!encounterStarted && playerTransform != null)
        {
            Vector3 pos = playerTransform.position;
            // Area Boss Arena: X [-44..-8], Z [-28..0]. Masuk jika X < -8.5f
            if (pos.x <= -8.5f && pos.x >= -44f && pos.z >= -27.5f && pos.z <= -0.5f)
            {
                StartBossEncounter();
            }
        }

        // 2. Sinkronisasi HP Boss dengan Grand Health Bar UI
        if (encounterStarted && !isBossDefeated && bossEnemy != null)
        {
            UpdateBossBar(bossEnemy.currentHP, bossEnemy.maxHP);
        }

        // 3. Jika Layar Kemenangan sedang terbuka, pastikan kursor aktif & tangani shortcut / fallback click
        if (IsVictoryModalOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Keyboard Shortcuts
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))
            {
                RestartGame();
                return;
            }
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            {
                CloseVictoryModal();
                return;
            }

            // Direct Screen-Point Click Fallback (100% menjamin tombol bisa diklik)
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;
                if (btnRestartRect != null && RectTransformUtility.RectangleContainsScreenPoint(btnRestartRect, mousePos))
                {
                    RestartGame();
                    return;
                }
                if (btnCloseRect != null && RectTransformUtility.RectangleContainsScreenPoint(btnCloseRect, mousePos))
                {
                    CloseVictoryModal();
                    return;
                }
            }
        }
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) p = GameObject.Find("Player");
        if (p != null) playerTransform = p.transform;
    }

    public void StartBossEncounter()
    {
        if (encounterStarted) return;
        encounterStarted = true;

        Debug.Log("<color=red><b>[BOSS ENCOUNTER]</b> Maw J Laygo bangkit! Arena terkunci!</color>");

        // 1. Kunci pintu masuk
        if (entranceBarrierA != null) entranceBarrierA.SetActive(true);
        if (entranceBarrierB != null) entranceBarrierB.SetActive(true);

        // 2. Tampilkan Grand Boss Bar
        if (bossBarPanel != null)
        {
            bossBarPanel.SetActive(true);
            if (bossEnemy != null)
            {
                bossHPSlider.maxValue = bossEnemy.maxHP;
                bossHPSlider.value = bossEnemy.currentHP;
            }
        }
    }

    public void UpdateBossBar(float currentHP, float maxHP)
    {
        if (bossHPSlider != null)
        {
            bossHPSlider.maxValue = maxHP;
            bossHPSlider.value = currentHP;
        }

        if (bossSubtext != null)
        {
            int hpInt = Mathf.CeilToInt(Mathf.Max(0f, currentHP));
            int maxInt = Mathf.CeilToInt(maxHP);

            if (isBossEnraged)
            {
                bossSubtext.text = $"<color=#ff4444><b>{hpInt} / {maxInt} HP  •  [FASE 2: ENRAGED BLOOD MOON]</b></color>";
            }
            else
            {
                bossSubtext.text = $"<color=#dddddd>{hpInt} / {maxInt} HP  •  [FASE 1: THE SAVAGE HUNTER]</color>";
            }
        }
    }

    public void OnBossEnraged()
    {
        if (isBossEnraged) return;
        isBossEnraged = true;

        Debug.Log("<color=red><b>[BOSS FASE 2]</b> Maw J Laygo memasuki FASE AMARAH (Enraged Berserk)!</color>");

        if (bossHPFillImage != null)
        {
            bossHPFillImage.color = new Color(1f, 0.15f, 0.05f); // Burning crimson
        }
    }

    public void OnBossDefeated()
    {
        if (isBossDefeated) return;
        isBossDefeated = true;

        Debug.Log("<color=#00ff88><b>[BOSS DEFEATED]</b> Maw J Laygo telah dikalahkan! Peti Emas di Altar Terbuka!</color>");

        // 1. Buka kembali pintu masuk
        if (entranceBarrierA != null) entranceBarrierA.SetActive(false);
        if (entranceBarrierB != null) entranceBarrierB.SetActive(false);

        // 2. Ubah teks Boss Bar menjadi kemenangan
        if (bossSubtext != null)
        {
            bossSubtext.text = "<color=#00ff88><b>★ MAW J LAYGO TELAH DIKALAHKAN! AMBIL HARTA DI ALTAR! ★</b></color>";
        }

        // 3. Buka segel Peti Emas di Altar
        if (victoryChest != null)
        {
            victoryChest.UnlockChest();
        }
    }

    public void ShowVictoryScreen()
    {
        if (victoryModalPanel == null) SetupVictoryModalUI();

        if (victoryModalPanel != null)
        {
            victoryModalPanel.SetActive(true);

            // Buka kunci kursor mouse agar pemain bisa mengklik tombol
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Isi statistik akhir pemain
            if (victoryStatsText != null && playerTransform != null)
            {
                PlayerStats ps = playerTransform.GetComponent<PlayerStats>();
                float curHP = ps != null ? ps.currentHP : 100f;
                float maxHP = ps != null ? ps.maxHP : 100f;
                float atk = ps != null ? ps.attackPower : 10f;
                float spd = ps != null ? ps.moveSpeedMultiplier * 100f : 100f;

                victoryStatsText.text =
                    $"<b><size=18><color=#ffd700>STATISTIK PENAKLUK DUNGEON:</color></size></b>\n\n" +
                    $"• Sisa HP: <b><color=#00ff88>{Mathf.RoundToInt(curHP)} / {Mathf.RoundToInt(maxHP)}</color></b>\n" +
                    $"• Kekuatan Serang (ATK): <b><color=#ffcc00>{Mathf.RoundToInt(atk)}</color></b>\n" +
                    $"• Kecepatan Gerak: <b><color=#00e5ff>{Mathf.RoundToInt(spd)}%</color></b>\n" +
                    $"• Status Kemenangan: <b><color=#aaff00>DUNGEON CHAMPION</color></b>\n\n" +
                    $"<i>Semua rintangan Hall 1 - 4 dan Boss Maw J Laygo telah berhasil ditaklukkan!</i>";
            }
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void CloseVictoryModal()
    {
        if (victoryModalPanel != null)
        {
            victoryModalPanel.SetActive(false);
        }

        // Kunci kembali kursor ke gameplay
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetupBossBarUI()
    {
        GameObject canvasObj = GameObject.Find("Player_UI_Canvas");
        if (canvasObj == null)
        {
            Canvas existing = Object.FindAnyObjectByType<Canvas>();
            if (existing != null) canvasObj = existing.gameObject;
        }
        if (canvasObj == null) return;

        Transform existingBar = canvasObj.transform.Find("Boss_HealthBarPanel");
        if (existingBar != null)
        {
            bossBarPanel = existingBar.gameObject;
            bossHPSlider = bossBarPanel.GetComponentInChildren<Slider>();
            if (bossHPSlider != null && bossHPSlider.fillRect != null)
            {
                bossHPFillImage = bossHPSlider.fillRect.GetComponent<Image>();
            }
            Text[] txts = bossBarPanel.GetComponentsInChildren<Text>();
            if (txts.Length > 0) bossTitleText = txts[0];
            if (txts.Length > 1) bossSubtext = txts[1];
            return;
        }

        // Buat panel Grand Boss Bar
        bossBarPanel = new GameObject("Boss_HealthBarPanel", typeof(RectTransform));
        bossBarPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform rt = bossBarPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -20f);
        rt.sizeDelta = new Vector2(520f, 65f);

        Image bg = bossBarPanel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.05f, 0.08f, 0.92f);

        // Judul Boss
        GameObject titleObj = new GameObject("BossTitle", typeof(RectTransform));
        titleObj.transform.SetParent(bossBarPanel.transform, false);
        RectTransform titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -6f);
        titleRt.sizeDelta = new Vector2(500f, 22f);

        bossTitleText = titleObj.AddComponent<Text>();
        bossTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bossTitleText.fontSize = 14;
        bossTitleText.fontStyle = FontStyle.Bold;
        bossTitleText.color = new Color(1f, 0.85f, 0.2f);
        bossTitleText.alignment = TextAnchor.MiddleCenter;
        bossTitleText.text = "★ BOSS: MAW J LAYGO — THE ABYSSAL OVERLORD ★";

        // Slider Bar HP
        GameObject sliderObj = new GameObject("BossSlider", typeof(RectTransform));
        sliderObj.transform.SetParent(bossBarPanel.transform, false);
        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRt.pivot = new Vector2(0.5f, 0.5f);
        sliderRt.anchoredPosition = new Vector2(0f, -6f);
        sliderRt.sizeDelta = new Vector2(490f, 16f);

        bossHPSlider = sliderObj.AddComponent<Slider>();

        GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform));
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;

        bossHPFillImage = fillObj.AddComponent<Image>();
        bossHPFillImage.color = new Color(0.9f, 0.2f, 0.2f); // Fiery red
        bossHPSlider.fillRect = fillRt;

        // Subtitle Status
        GameObject subObj = new GameObject("BossSubtext", typeof(RectTransform));
        subObj.transform.SetParent(bossBarPanel.transform, false);
        RectTransform subRt = subObj.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0f);
        subRt.anchorMax = new Vector2(1f, 0f);
        subRt.pivot = new Vector2(0.5f, 0f);
        subRt.anchoredPosition = new Vector2(0f, 4f);
        subRt.sizeDelta = new Vector2(500f, 16f);

        bossSubtext = subObj.AddComponent<Text>();
        bossSubtext.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bossSubtext.fontSize = 11;
        bossSubtext.fontStyle = FontStyle.Bold;
        bossSubtext.color = Color.white;
        bossSubtext.alignment = TextAnchor.MiddleCenter;
        bossSubtext.text = "280 / 280 HP  •  [FASE 1: THE SAVAGE HUNTER]";
    }

    private void SetupVictoryModalUI()
    {
        GameObject canvasObj = GameObject.Find("Player_UI_Canvas");
        if (canvasObj == null)
        {
            Canvas existing = Object.FindAnyObjectByType<Canvas>();
            if (existing != null) canvasObj = existing.gameObject;
        }
        if (canvasObj == null) return;

        // Pastikan EventSystem aktif agar UI Button dapat diklik
        EnsureEventSystem();

        // Pastikan Canvas memiliki GraphicRaycaster
        if (canvasObj.GetComponent<GraphicRaycaster>() == null)
        {
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        Transform existingModal = canvasObj.transform.Find("Victory_ModalPanel");
        if (existingModal != null)
        {
            victoryModalPanel = existingModal.gameObject;
            victoryStatsText = victoryModalPanel.transform.Find("StatsContent")?.GetComponent<Text>();

            Transform rBtn = victoryModalPanel.transform.Find("Btn_Restart");
            if (rBtn != null)
            {
                btnRestartRect = rBtn.GetComponent<RectTransform>();
                Button b = rBtn.GetComponent<Button>();
                if (b != null)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(RestartGame);
                }
            }

            Transform cBtn = victoryModalPanel.transform.Find("Btn_Explore");
            if (cBtn != null)
            {
                btnCloseRect = cBtn.GetComponent<RectTransform>();
                Button b = cBtn.GetComponent<Button>();
                if (b != null)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(CloseVictoryModal);
                }
            }
            return;
        }

        victoryModalPanel = new GameObject("Victory_ModalPanel", typeof(RectTransform));
        victoryModalPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform modalRt = victoryModalPanel.GetComponent<RectTransform>();
        modalRt.anchorMin = new Vector2(0.5f, 0.5f);
        modalRt.anchorMax = new Vector2(0.5f, 0.5f);
        modalRt.pivot = new Vector2(0.5f, 0.5f);
        modalRt.anchoredPosition = Vector2.zero;
        modalRt.sizeDelta = new Vector2(560f, 380f);

        Image modalBg = victoryModalPanel.AddComponent<Image>();
        modalBg.color = new Color(0.07f, 0.06f, 0.10f, 0.96f);

        // Header Title
        GameObject headObj = new GameObject("VictoryHeader", typeof(RectTransform));
        headObj.transform.SetParent(victoryModalPanel.transform, false);
        RectTransform headRt = headObj.GetComponent<RectTransform>();
        headRt.anchorMin = new Vector2(0f, 1f);
        headRt.anchorMax = new Vector2(1f, 1f);
        headRt.pivot = new Vector2(0.5f, 1f);
        headRt.anchoredPosition = new Vector2(0f, -20f);
        headRt.sizeDelta = new Vector2(520f, 40f);

        Text headText = headObj.AddComponent<Text>();
        headText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headText.fontSize = 24;
        headText.fontStyle = FontStyle.Bold;
        headText.color = new Color(1f, 0.85f, 0.2f);
        headText.alignment = TextAnchor.MiddleCenter;
        headText.raycastTarget = false;
        headText.text = "★ VICTORY! DUNGEON CLEARED! ★";

        // Stats Content Box
        GameObject statsObj = new GameObject("StatsContent", typeof(RectTransform));
        statsObj.transform.SetParent(victoryModalPanel.transform, false);
        RectTransform statsRt = statsObj.GetComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0f, 0.25f);
        statsRt.anchorMax = new Vector2(1f, 0.85f);
        statsRt.offsetMin = new Vector2(30f, 0f);
        statsRt.offsetMax = new Vector2(-30f, 0f);

        victoryStatsText = statsObj.AddComponent<Text>();
        victoryStatsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        victoryStatsText.fontSize = 14;
        victoryStatsText.color = Color.white;
        victoryStatsText.alignment = TextAnchor.UpperLeft;
        victoryStatsText.raycastTarget = false;
        victoryStatsText.text = "Memuat statistik kemenangan...";

        // Tombol Restart
        GameObject btnRestartObj = new GameObject("Btn_Restart", typeof(RectTransform));
        btnRestartObj.transform.SetParent(victoryModalPanel.transform, false);
        RectTransform btnRestartRt = btnRestartObj.GetComponent<RectTransform>();
        btnRestartRt.anchorMin = new Vector2(0.5f, 0f);
        btnRestartRt.anchorMax = new Vector2(0.5f, 0f);
        btnRestartRt.pivot = new Vector2(0.5f, 0f);
        btnRestartRt.anchoredPosition = new Vector2(-110f, 36f);
        btnRestartRt.sizeDelta = new Vector2(190f, 44f);
        btnRestartRect = btnRestartRt;

        Image btnRestartImg = btnRestartObj.AddComponent<Image>();
        btnRestartImg.color = new Color(0.18f, 0.65f, 0.28f);
        btnRestartImg.raycastTarget = true;

        Button btnRestart = btnRestartObj.AddComponent<Button>();
        btnRestart.targetGraphic = btnRestartImg;
        ColorBlock rCb = btnRestart.colors;
        rCb.normalColor = new Color(0.18f, 0.65f, 0.28f);
        rCb.highlightedColor = new Color(0.25f, 0.85f, 0.38f);
        rCb.pressedColor = new Color(0.12f, 0.45f, 0.18f);
        rCb.selectedColor = rCb.highlightedColor;
        btnRestart.colors = rCb;
        btnRestart.onClick.AddListener(RestartGame);

        GameObject btnRestartTxtObj = new GameObject("Text", typeof(RectTransform));
        btnRestartTxtObj.transform.SetParent(btnRestartObj.transform, false);
        RectTransform bRt = btnRestartTxtObj.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.sizeDelta = Vector2.zero;
        Text bTxt = btnRestartTxtObj.AddComponent<Text>();
        bTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bTxt.fontSize = 14;
        bTxt.fontStyle = FontStyle.Bold;
        bTxt.color = Color.white;
        bTxt.alignment = TextAnchor.MiddleCenter;
        bTxt.raycastTarget = false; // TIDAK menghalangi raycast tombol
        bTxt.text = "Main Lagi (Restart)";

        // Tombol Jelajahi Bebas (Close Modal)
        GameObject btnCloseObj = new GameObject("Btn_Explore", typeof(RectTransform));
        btnCloseObj.transform.SetParent(victoryModalPanel.transform, false);
        RectTransform btnCloseRt = btnCloseObj.GetComponent<RectTransform>();
        btnCloseRt.anchorMin = new Vector2(0.5f, 0f);
        btnCloseRt.anchorMax = new Vector2(0.5f, 0f);
        btnCloseRt.pivot = new Vector2(0.5f, 0f);
        btnCloseRt.anchoredPosition = new Vector2(110f, 36f);
        btnCloseRt.sizeDelta = new Vector2(190f, 44f);
        btnCloseRect = btnCloseRt;

        Image btnCloseImg = btnCloseObj.AddComponent<Image>();
        btnCloseImg.color = new Color(0.25f, 0.35f, 0.5f);
        btnCloseImg.raycastTarget = true;

        Button btnClose = btnCloseObj.AddComponent<Button>();
        btnClose.targetGraphic = btnCloseImg;
        ColorBlock cCb = btnClose.colors;
        cCb.normalColor = new Color(0.25f, 0.35f, 0.5f);
        cCb.highlightedColor = new Color(0.35f, 0.5f, 0.72f);
        cCb.pressedColor = new Color(0.18f, 0.25f, 0.38f);
        cCb.selectedColor = cCb.highlightedColor;
        btnClose.colors = cCb;
        btnClose.onClick.AddListener(CloseVictoryModal);

        GameObject btnCloseTxtObj = new GameObject("Text", typeof(RectTransform));
        btnCloseTxtObj.transform.SetParent(btnCloseObj.transform, false);
        RectTransform cRt = btnCloseTxtObj.GetComponent<RectTransform>();
        cRt.anchorMin = Vector2.zero;
        cRt.anchorMax = Vector2.one;
        cRt.sizeDelta = Vector2.zero;
        Text cTxt = btnCloseTxtObj.AddComponent<Text>();
        cTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cTxt.fontSize = 14;
        cTxt.fontStyle = FontStyle.Bold;
        cTxt.color = Color.white;
        cTxt.alignment = TextAnchor.MiddleCenter;
        cTxt.raycastTarget = false; // TIDAK menghalangi raycast tombol
        cTxt.text = "Jelajahi Bebas";

        // Petunjuk Tombol Shortcut di bawah tombol
        GameObject hintObj = new GameObject("ShortcutHint", typeof(RectTransform));
        hintObj.transform.SetParent(victoryModalPanel.transform, false);
        RectTransform hintRt = hintObj.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(1f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 10f);
        hintRt.sizeDelta = new Vector2(520f, 20f);
        Text hintTxt = hintObj.AddComponent<Text>();
        hintTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hintTxt.fontSize = 11;
        hintTxt.color = new Color(0.7f, 0.75f, 0.85f, 0.85f);
        hintTxt.alignment = TextAnchor.MiddleCenter;
        hintTxt.raycastTarget = false;
        hintTxt.text = "Tip: Tekan [R / Enter] untuk Main Lagi  •  [Space / Esc] untuk Jelajahi Bebas";
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
                Debug.Log("<color=cyan>[UI] EventSystem otomatis dibuat agar tombol UI bisa diklik.</color>");
            }
        }
    }

    private void EnsureReferences()
    {
        if (bossEnemy == null)
        {
            foreach (var ai in Object.FindObjectsByType<EnemyAI>())
            {
                if (ai != null && (ai.isBoss || ai.name.ToLower().Contains("maw") || ai.name.ToLower().Contains("boss")))
                {
                    bossEnemy = ai;
                    break;
                }
            }
        }

        if (victoryChest == null)
        {
            victoryChest = Object.FindAnyObjectByType<VictoryChest>();
        }
    }
}
