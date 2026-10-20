using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Mengelola status Player (HP, ATK, Speed, Lives) dan menghubungkannya dengan UI Panel.
/// Juga mengelola sistem Game Over Full Reset ketika Lives habis (3x mati).
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Health & Lives Settings")]
    public float maxHP = 100f;
    public float currentHP;
    
    [Header("Lives Settings (Max 3 Deaths)")]
    public int maxLives = 3;
    public int currentLives;

    [Header("Stats")]
    public float attackPower = 10f;
    public float moveSpeedMultiplier = 1f;

    [Header("UI References")]
    public Slider healthSlider;
    public Text hpText;
    public Text statsText;

    [Header("Game Over UI")]
    public GameObject gameOverModalPanel;
    private RectTransform btnGameOverRestartRect;

    private PlayerController playerController;
    private CharacterController characterController;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    public bool IsGameOverModalOpen()
    {
        return gameOverModalPanel != null && gameOverModalPanel.activeInHierarchy;
    }

    private void Awake()
    {
        currentHP = maxHP;
        currentLives = maxLives; // Inisialisasi awal 3 nyawa
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
    }

    private void Start()
    {
        FindUIReferences();
        UpdateUI();
    }

    private void Update()
    {
        // Jika Modal Game Over terbuka, tangani kursor dan shortcut keyboard
        if (IsGameOverModalOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Shortcut Keyboard (R / Enter / Space) untuk Main Lagi
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                RestartGameFull();
                return;
            }

            // Direct Click Fallback untuk tombol UI
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Input.mousePosition;
                if (btnGameOverRestartRect != null && RectTransformUtility.RectangleContainsScreenPoint(btnGameOverRestartRect, mousePos))
                {
                    RestartGameFull();
                    return;
                }
            }
        }
    }

    public void FindUIReferences()
    {
        if (healthSlider == null)
        {
            GameObject sliderObj = GameObject.Find("Player_HPSlider");
            if (sliderObj != null) healthSlider = sliderObj.GetComponent<Slider>();
        }

        if (hpText == null)
        {
            GameObject textObj = GameObject.Find("Player_HPText");
            if (textObj != null) hpText = textObj.GetComponent<Text>();
        }

        if (statsText == null)
        {
            GameObject statsObj = GameObject.Find("Player_StatsText");
            if (statsObj != null) statsText = statsObj.GetComponent<Text>();
        }
    }

    public void TakeDamage(float damage)
    {
        currentHP = Mathf.Clamp(currentHP - damage, 0f, maxHP);
        UpdateUI();

        Debug.Log($"<color=red>[Player]</color> Took damage {damage}! Remaining HP: {currentHP}");

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Mengeksekusi satu jenis efek gerbang (bisa dipanggil berurutan untuk efek ganda)
    /// </summary>
    public void ApplySingleGateEffect(MathGateType type, float value)
    {
        switch (type)
        {
            case MathGateType.AddHP:
                currentHP = Mathf.Clamp(currentHP + value, 0f, maxHP);
                Debug.Log($"<color=green>[Player Heal]</color> +{value} HP! Current HP: {currentHP}");
                break;

            case MathGateType.SubHP:
                TakeDamage(value);
                Debug.Log($"<color=red>[Player Curse]</color> -{value} HP! Remaining HP: {currentHP}");
                break;

            case MathGateType.MultiplyATK:
                attackPower = Mathf.Round(attackPower * value);
                Debug.Log($"<color=orange>[Player Buff]</color> ATK x{value}! Current ATK: {attackPower}");
                break;

            case MathGateType.DivideATK:
                attackPower = Mathf.Max(1f, Mathf.Round(attackPower / value));
                Debug.Log($"<color=grey>[Player Weakness]</color> ATK /{value}! Current ATK: {attackPower}");
                break;

            case MathGateType.AddSpeed:
                if (playerController != null)
                {
                    playerController.walkSpeed += value;
                    playerController.sprintSpeed += value;
                }
                Debug.Log($"<color=cyan>[Player Speed]</color> +{value} SPD!");
                break;

            case MathGateType.SubSpeed:
                if (playerController != null)
                {
                    playerController.walkSpeed = Mathf.Max(2f, playerController.walkSpeed - value);
                    playerController.sprintSpeed = Mathf.Max(3f, playerController.sprintSpeed - value);
                }
                Debug.Log($"<color=purple>[Player Slow]</color> -{value} SPD!");
                break;
        }

        UpdateUI();
    }

    // Legacy function compatibility
    public void ApplyGateEffect(MathGateType type, float value, float secondaryValue = 0f)
    {
        ApplySingleGateEffect(type, value);
    }

    public void UpdateUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHP;
            healthSlider.value = currentHP;
        }

        if (hpText != null)
        {
            hpText.text = $"HP: {Mathf.CeilToInt(currentHP)} / {maxHP}";
        }

        // Tampilkan ATK, SPD, dan Lives di panel bawah UI
        if (statsText != null)
        {
            float spd = playerController != null ? playerController.walkSpeed : 5f;
            statsText.text = $"ATK: {attackPower}  |  SPD: {spd:F1}  |  Lives: {currentLives}/{maxLives}";
        }
    }

    private void Die()
    {
        currentLives--; // Pengurangan nyawa saat mati
        Debug.Log($"<color=red><b>[DIE]</b> Player defeated! Remaining lives: {currentLives}/{maxLives}</color>");

        if (currentLives <= 0)
        {
            TriggerGameOver();
        }
        else
        {
            Respawn();
        }
    }

    private void TriggerGameOver()
    {
        Debug.Log("<color=red><b>[GAME OVER]</b> Player out of lives! Showing Game Over Modal.</color>");
        
        // Nonaktifkan kontrol pergerakan player
        if (playerController != null) playerController.enabled = false;

        ShowGameOverScreen();
    }

    public void ShowGameOverScreen()
    {
        if (gameOverModalPanel == null) SetupGameOverModalUI();

        if (gameOverModalPanel != null)
        {
            gameOverModalPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f; // Freeze game
        }
    }

    private void SetupGameOverModalUI()
    {
        GameObject canvasObj = GameObject.Find("Player_UI_Canvas");
        if (canvasObj == null)
        {
            Canvas existing = Object.FindAnyObjectByType<Canvas>();
            if (existing != null) canvasObj = existing.gameObject;
        }
        if (canvasObj == null) return;

        EnsureEventSystem();

        Transform existingModal = canvasObj.transform.Find("GameOver_ModalPanel");
        if (existingModal != null)
        {
            gameOverModalPanel = existingModal.gameObject;
            Transform rBtn = gameOverModalPanel.transform.Find("Btn_GameOverRestart");
            if (rBtn != null)
            {
                btnGameOverRestartRect = rBtn.GetComponent<RectTransform>();
                Button b = rBtn.GetComponent<Button>();
                if (b != null)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(RestartGameFull);
                }
            }
            return;
        }

        // Buat Panel Modal Game Over secara runtime
        gameOverModalPanel = new GameObject("GameOver_ModalPanel", typeof(RectTransform));
        gameOverModalPanel.transform.SetParent(canvasObj.transform, false);

        RectTransform modalRt = gameOverModalPanel.GetComponent<RectTransform>();
        modalRt.anchorMin = new Vector2(0.5f, 0.5f);
        modalRt.anchorMax = new Vector2(0.5f, 0.5f);
        modalRt.pivot = new Vector2(0.5f, 0.5f);
        modalRt.anchoredPosition = Vector2.zero;
        modalRt.sizeDelta = new Vector2(500f, 300f);

        Image modalBg = gameOverModalPanel.AddComponent<Image>();
        modalBg.color = new Color(0.12f, 0.02f, 0.02f, 0.96f); // Dark Crimson

        // Header Game Over
        GameObject headObj = new GameObject("GameOverHeader", typeof(RectTransform));
        headObj.transform.SetParent(gameOverModalPanel.transform, false);
        RectTransform headRt = headObj.GetComponent<RectTransform>();
        headRt.anchorMin = new Vector2(0f, 1f);
        headRt.anchorMax = new Vector2(1f, 1f);
        headRt.pivot = new Vector2(0.5f, 1f);
        headRt.anchoredPosition = new Vector2(0f, -25f);
        headRt.sizeDelta = new Vector2(450f, 45f);

        Text headText = headObj.AddComponent<Text>();
        headText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        headText.fontSize = 28;
        headText.fontStyle = FontStyle.Bold;
        headText.color = new Color(1f, 0.2f, 0.2f);
        headText.alignment = TextAnchor.MiddleCenter;
        headText.raycastTarget = false;
        headText.text = "★ GAME OVER ★";

        // Deskripsi
        GameObject descObj = new GameObject("GameOverDesc", typeof(RectTransform));
        descObj.transform.SetParent(gameOverModalPanel.transform, false);
        RectTransform descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0f, 0.5f);
        descRt.anchorMax = new Vector2(1f, 0.5f);
        descRt.pivot = new Vector2(0.5f, 0.5f);
        descRt.anchoredPosition = new Vector2(0f, 10f);
        descRt.sizeDelta = new Vector2(440f, 60f);

        Text descText = descObj.AddComponent<Text>();
        descText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        descText.fontSize = 14;
        descText.color = Color.white;
        descText.alignment = TextAnchor.MiddleCenter;
        descText.raycastTarget = false;
        descText.text = "Seluruh nyawa kamu telah habis!\nApakah kamu ingin mencoba kembali dari awal?";

        // Tombol Main Lagi
        GameObject btnObj = new GameObject("Btn_GameOverRestart", typeof(RectTransform));
        btnObj.transform.SetParent(gameOverModalPanel.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.5f, 0f);
        btnRt.anchorMax = new Vector2(0.5f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.anchoredPosition = new Vector2(0f, 35f);
        btnRt.sizeDelta = new Vector2(210f, 46f);
        btnGameOverRestartRect = btnRt;

        Image btnImg = btnObj.AddComponent<Image>();
        btnImg.color = new Color(0.75f, 0.15f, 0.15f);
        btnImg.raycastTarget = true;

        Button btn = btnObj.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.75f, 0.15f, 0.15f);
        cb.highlightedColor = new Color(0.95f, 0.25f, 0.25f);
        cb.pressedColor = new Color(0.5f, 0.1f, 0.1f);
        cb.selectedColor = cb.highlightedColor;
        btn.colors = cb;
        btn.onClick.AddListener(RestartGameFull);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform bRt = btnTxtObj.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.sizeDelta = Vector2.zero;
        Text bTxt = btnTxtObj.AddComponent<Text>();
        bTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bTxt.fontSize = 15;
        bTxt.fontStyle = FontStyle.Bold;
        bTxt.color = Color.white;
        bTxt.alignment = TextAnchor.MiddleCenter;
        bTxt.raycastTarget = false;
        bTxt.text = "Main Lagi (Restart)";
    }

    public void RestartGameFull()
    {
        Time.timeScale = 1f; // Kembalikan waktu game ke normal sebelum merekam ulang scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
    /// Mengembalikan Player ke posisi spawn awal dengan HP penuh.
    /// Memastikan seluruh barrier dari tantangan yang sudah selesai tetap terbuka.
    /// </summary>
    private void Respawn()
    {
        Debug.Log("<color=yellow><b>[RESPAWN]</b> Player returned to initial position! Remaining lives: " + currentLives + "</color>");

        currentHP = maxHP;

        if (characterController != null)
            characterController.enabled = false;

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        if (characterController != null)
            characterController.enabled = true;

        UpdateUI();
        EnsureCompletedChallengeBarriersOpen();
    }

    /// <summary>
    /// Membuka kembali semua barrier dari tantangan yang sudah diselesaikan player.
    /// Dipanggil saat Respawn biasa (ketika masih ada sisa Lives).
    /// </summary>
    private void EnsureCompletedChallengeBarriersOpen()
    {
        // Hall 2
        if (Hall2ChallengeManager.Instance != null && Hall2ChallengeManager.Instance.isChallengeCompleted)
        {
            if (Hall2ChallengeManager.Instance.exitBarrierA != null)
                Hall2ChallengeManager.Instance.exitBarrierA.SetActive(false);
            if (Hall2ChallengeManager.Instance.exitBarrierB != null)
                Hall2ChallengeManager.Instance.exitBarrierB.SetActive(false);
        }

        // Hall 3
        if (Hall3ChallengeManager.Instance != null && Hall3ChallengeManager.Instance.isChallengeCompleted)
        {
            if (Hall3ChallengeManager.Instance.exitBarrier3A != null)
                Hall3ChallengeManager.Instance.exitBarrier3A.SetActive(false);
            if (Hall3ChallengeManager.Instance.exitBarrier3B != null)
                Hall3ChallengeManager.Instance.exitBarrier3B.SetActive(false);
        }

        // Hall 4
        if (Hall4ChallengeManager.Instance != null && Hall4ChallengeManager.Instance.isChallengeCompleted)
        {
            if (Hall4ChallengeManager.Instance.exitBarrier4A != null)
                Hall4ChallengeManager.Instance.exitBarrier4A.SetActive(false);
            if (Hall4ChallengeManager.Instance.exitBarrier4B != null)
                Hall4ChallengeManager.Instance.exitBarrier4B.SetActive(false);
        }
    }
}