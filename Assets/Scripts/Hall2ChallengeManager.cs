using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manager Tantangan Khusus Hall 2:
/// 1. Player harus mengumpulkan 3 Kunci yang tersebar di Hall 2.
/// 2. Kunci harus diletakkan pada Rak (Shelf).
/// 3. Goblin berpatroli perlahan dengan FOV lebar; Player harus memanfaatkan pilar dan waktu untuk bergerak.
/// 4. Setelah 3 Kunci diletakkan di rak, penghalang ke Gate 2A & 2B terbuka!
/// </summary>
public class Hall2ChallengeManager : MonoBehaviour
{
    public static Hall2ChallengeManager Instance { get; private set; }

    [Header("Challenge Status")]
    public int totalKeysRequired = 3;
    public int keysCarried = 0;
    public int keysOnShelf = 0;
    public bool isChallengeCompleted = false;

    [Header("Scene References (Otomatis dicari jika kosong)")]
    public Hall2Shelf shelf;
    public List<Hall2KeyItem> keyItems = new List<Hall2KeyItem>();
    public GameObject exitBarrierA;
    public GameObject exitBarrierB;

    [Header("UI Feedback")]
    public Text objectiveHUDText;
    public Text promptHUDText;
    private GameObject objectivePanel;

    private Transform playerTransform;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        EnsureChallengeSetup();
    }

    private void Start()
    {
        FindPlayer();
        SetupHUD();
        UpdateHUD();
    }

    private void Update()
    {
        if (playerTransform == null) FindPlayer();

        CheckProximityPrompts();
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) p = GameObject.Find("Player");
        if (p != null) playerTransform = p.transform;
    }

    /// <summary>
    /// Menyiapkan HUD Objective di Canvas Player
    /// </summary>
    private void SetupHUD()
    {
        GameObject canvasObj = GameObject.Find("Player_UI_Canvas");
        if (canvasObj == null)
        {
            Canvas existingCanvas = Object.FindAnyObjectByType<Canvas>();
            if (existingCanvas != null) canvasObj = existingCanvas.gameObject;
        }

        if (canvasObj != null)
        {
            // Buat panel Objective di pojok kanan atas
            Transform existingPanel = canvasObj.transform.Find("Hall2_ObjectivePanel");
            if (existingPanel != null)
            {
                objectivePanel = existingPanel.gameObject;
                objectiveHUDText = objectivePanel.GetComponentInChildren<Text>();
            }
            else
            {
                objectivePanel = new GameObject("Hall2_ObjectivePanel", typeof(RectTransform));
                objectivePanel.transform.SetParent(canvasObj.transform, false);

                RectTransform rt = objectivePanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-25f, -25f);
                rt.sizeDelta = new Vector2(300f, 75f);

                Image bg = objectivePanel.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);

                GameObject textObj = new GameObject("ObjectiveText", typeof(RectTransform));
                textObj.transform.SetParent(objectivePanel.transform, false);
                RectTransform textRt = textObj.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = new Vector2(12f, 8f);
                textRt.offsetMax = new Vector2(-12f, -8f);

                objectiveHUDText = textObj.AddComponent<Text>();
                objectiveHUDText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                objectiveHUDText.fontSize = 13;
                objectiveHUDText.fontStyle = FontStyle.Bold;
                objectiveHUDText.color = Color.white;
                objectiveHUDText.alignment = TextAnchor.MiddleLeft;
            }

            // Buat prompt interaksi di tengah layar bawah
            Transform existingPrompt = canvasObj.transform.Find("InteractionPrompt");
            if (existingPrompt != null)
            {
                promptHUDText = existingPrompt.GetComponent<Text>();
            }
            else
            {
                GameObject promptObj = new GameObject("InteractionPrompt", typeof(RectTransform));
                promptObj.transform.SetParent(canvasObj.transform, false);

                RectTransform prt = promptObj.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.5f, 0.3f);
                prt.anchorMax = new Vector2(0.5f, 0.3f);
                prt.pivot = new Vector2(0.5f, 0.5f);
                prt.sizeDelta = new Vector2(400f, 40f);

                promptHUDText = promptObj.AddComponent<Text>();
                promptHUDText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                promptHUDText.fontSize = 16;
                promptHUDText.fontStyle = FontStyle.Bold;
                promptHUDText.color = new Color(1f, 0.95f, 0.4f);
                promptHUDText.alignment = TextAnchor.MiddleCenter;
            }
        }
    }

    /// <summary>
    /// Memeriksa objek kunci / rak terdekat untuk menampilkan teks prompt [E / Klik Kiri]
    /// </summary>
    private void CheckProximityPrompts()
    {
        if (playerTransform == null || promptHUDText == null) return;

        // Cek apakah player berada di area Hall 2 (X: 30..52, Z: 4..24)
        Vector3 pos = playerTransform.position;
        bool inHall2 = (pos.x >= 29.5f && pos.x <= 52.5f && pos.z >= 3.5f && pos.z <= 24.5f);

        if (objectivePanel != null)
        {
            objectivePanel.SetActive(inHall2);
        }

        if (!inHall2)
        {
            promptHUDText.text = "";
            return;
        }

        // Cek jarak ke kunci terdekat yang belum diambil
        Hall2KeyItem nearestKey = null;
        float minKeyDist = 2.4f;
        foreach (var k in keyItems)
        {
            if (k != null && !k.isCollected && k.gameObject.activeInHierarchy)
            {
                float d = Vector3.Distance(playerTransform.position, k.transform.position);
                if (d < minKeyDist)
                {
                    minKeyDist = d;
                    nearestKey = k;
                }
            }
        }

        if (nearestKey != null)
        {
            promptHUDText.text = "<color=#ffea00>[Klik Kiri / E]</color> Ambil Kunci Dungeon";
            return;
        }

        // Cek jarak ke rak
        if (shelf != null)
        {
            float distToShelf = Vector3.Distance(playerTransform.position, shelf.transform.position);
            if (distToShelf <= 2.8f)
            {
                if (keysCarried > 0 && keysOnShelf < totalKeysRequired)
                {
                    promptHUDText.text = $"<color=#00ff88>[Klik Kiri / E]</color> Letakkan Kunci ke Rak ({keysCarried} dibawa)";
                }
                else if (keysOnShelf >= totalKeysRequired)
                {
                    promptHUDText.text = "<color=#00ff88>Semua kunci sudah terpasang. Gerbang terbuka!</color>";
                }
                else
                {
                    promptHUDText.text = $"Rak Kunci ({keysOnShelf}/{totalKeysRequired} terpasang) - Cari kunci di ruangan!";
                }
                return;
            }
        }

        promptHUDText.text = "";
    }

    /// <summary>
    /// Dipanggil saat Player mengambil Kunci
    /// </summary>
    public void OnKeyCollected(Hall2KeyItem key)
    {
        keysCarried++;
        Debug.Log($"<color=#ffcc00>[Hall 2 Challenge]</color> Kunci diambil! Dibawa: {keysCarried}/{totalKeysRequired}");
        UpdateHUD();
    }

    /// <summary>
    /// Dipanggil saat Player menaruh Kunci di Rak
    /// </summary>
    public bool TryDepositKeysAtShelf()
    {
        if (shelf == null || keysCarried <= 0) return false;

        int deposited = shelf.DepositKeys(keysCarried);
        if (deposited > 0)
        {
            keysCarried -= deposited;
            keysOnShelf += deposited;

            Debug.Log($"<color=#00ff88>[Hall 2 Challenge]</color> {deposited} Kunci diletakkan di rak! Total di rak: {keysOnShelf}/{totalKeysRequired}");

            if (keysOnShelf >= totalKeysRequired)
            {
                CompleteChallenge();
            }

            UpdateHUD();
            return true;
        }

        return false;
    }

    private void CompleteChallenge()
    {
        if (isChallengeCompleted) return;
        isChallengeCompleted = true;

        Debug.Log("<color=#00ff88>[Hall 2 Challenge] ✅ TANTANGAN SELESAI! Gerbang menuju Hall 3 terbuka!</color>");

        // Buka / hancurkan penghalang gerbang menuju Hall 3
        if (exitBarrierA != null) exitBarrierA.SetActive(false);
        if (exitBarrierB != null) exitBarrierB.SetActive(false);

        UpdateHUD();
    }

    public void UpdateHUD()
    {
        if (objectiveHUDText != null)
        {
            if (isChallengeCompleted)
            {
                objectiveHUDText.text = "<b><color=#00ff88>★ TANTANGAN HALL 2 SELESAI!</color></b>\n" +
                                        "3/3 Kunci diletakkan di rak.\n" +
                                        "<color=#ffea00>Gerbang 2A & 2B telah Terbuka!</color>";
            }
            else
            {
                objectiveHUDText.text = "<b><color=#ffcc00>TANTANGAN HALL 2:</color></b>\n" +
                                        "Kumpulkan 3 Kunci & letakkan di Rak\n" +
                                        $"• Kunci Dibawa : <color=#00ff88>{keysCarried}</color>\n" +
                                        $"• Kunci di Rak : <color=#ff8800>{keysOnShelf} / {totalKeysRequired}</color>";
            }
        }
    }

    /// <summary>
    /// Memastikan objek Rak, Kunci, dan Barrier tersedia di Hall 2
    /// </summary>
    public void EnsureChallengeSetup()
    {
        GameObject arena = GameObject.Find("DungeonArena");
        Transform challengeRoot = arena != null ? arena.transform.Find("Hall2_Challenge") : null;
        if (challengeRoot == null && arena != null)
        {
            GameObject cObj = new GameObject("Hall2_Challenge");
            cObj.transform.SetParent(arena.transform, false);
            challengeRoot = cObj.transform;
        }

        // 1. Cari atau hubungkan Rak (Shelves)
        if (shelf == null)
        {
            shelf = Object.FindAnyObjectByType<Hall2Shelf>();
        }

        // 2. Cari atau hubungkan Kunci
        if (keyItems.Count == 0)
        {
            keyItems.AddRange(Object.FindObjectsByType<Hall2KeyItem>(FindObjectsSortMode.None));
        }

        // 3. Hubungkan Barrier
        if (exitBarrierA == null) exitBarrierA = GameObject.Find("Barrier_Hall2_ExitA");
        if (exitBarrierB == null) exitBarrierB = GameObject.Find("Barrier_Hall2_ExitB");
    }
}
