using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manager Tantangan Khusus Hall 4: "The Colossus & The Rune Pressure Plates"
/// 1. Player harus menginjak dan mengunci 3 Pelat Tekanan Runa (Pressure Plates) di Hall 4.
/// 2. Pumpkin Hulk mengejar dengan langkah berat, memiliki Heavy Armor, dan bisa melakukan Bull Charge.
/// 3. Jika Pumpkin Hulk menabrak pilar batu Hall 4 saat menerjang, ia akan ter-STUN 3.5 detik (pertahanan terbuka).
/// 4. Setelah ke-3 pelat terkunci, penghalang gerbang menuju Koridor 4A & 4B (Boss Arena) terbuka,
///    dan armor Pumpkin Hulk hancur berkeping-keping!
/// </summary>
public class Hall4ChallengeManager : MonoBehaviour
{
    public static Hall4ChallengeManager Instance { get; private set; }

    [Header("Challenge Status")]
    public int totalPlatesRequired = 3;
    public int activatedPlatesCount = 0;
    public bool isChallengeCompleted = false;

    [Header("Scene References")]
    public List<Hall4PressurePlate> pressurePlates = new List<Hall4PressurePlate>();
    public GameObject exitBarrier4A;
    public GameObject exitBarrier4B;
    public EnemyAI pumpkinHulkEnemy;

    [Header("UI Feedback")]
    public Text objectiveHUDText;
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

        CheckProximityHUD();
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) p = GameObject.Find("Player");
        if (p != null) playerTransform = p.transform;
    }

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
            Transform existingPanel = canvasObj.transform.Find("Hall4_ObjectivePanel");
            if (existingPanel != null)
            {
                objectivePanel = existingPanel.gameObject;
                objectiveHUDText = objectivePanel.GetComponentInChildren<Text>();
            }
            else
            {
                objectivePanel = new GameObject("Hall4_ObjectivePanel", typeof(RectTransform));
                objectivePanel.transform.SetParent(canvasObj.transform, false);

                RectTransform rt = objectivePanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-25f, -25f);
                rt.sizeDelta = new Vector2(340f, 90f);

                Image bg = objectivePanel.AddComponent<Image>();
                bg.color = new Color(0.08f, 0.08f, 0.12f, 0.88f); // Dark Slate/Obsidian tone

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
        }
    }

    private void CheckProximityHUD()
    {
        if (playerTransform == null) return;

        // Cek apakah player berada di area Hall 4 (X: -0.5..22.5, Z: -24.5..-3.5)
        Vector3 pos = playerTransform.position;
        bool inHall4 = (pos.x >= -0.5f && pos.x <= 22.5f && pos.z >= -24.5f && pos.z <= -3.5f);

        if (objectivePanel != null)
        {
            objectivePanel.SetActive(inHall4);
        }
    }

    public void OnPlateActivated(Hall4PressurePlate plate)
    {
        if (isChallengeCompleted) return;

        // Hitung ulang pelat aktif
        int count = 0;
        foreach (var p in pressurePlates)
        {
            if (p != null && p.isActivated) count++;
        }
        activatedPlatesCount = count;

        UpdateHUD();

        Debug.Log($"<color=#00e5ff>[Hall 4 Manager]</color> Pelat Runa aktif: {activatedPlatesCount}/{totalPlatesRequired}");

        if (activatedPlatesCount >= totalPlatesRequired)
        {
            CompleteChallenge();
        }
    }

    private void CompleteChallenge()
    {
        if (isChallengeCompleted) return;
        isChallengeCompleted = true;

        Debug.Log("<color=#00ff88><b>[Hall 4 SELESAI]</b> Seluruh 3 Pelat Runa Telah Terkunci! Gerbang Menuju Boss Arena Terbuka!</color>");

        // 1. Hilangkan barrier gerbang keluar 4A & 4B
        if (exitBarrier4A != null)
        {
            exitBarrier4A.SetActive(false);
        }
        if (exitBarrier4B != null)
        {
            exitBarrier4B.SetActive(false);
        }

        // 2. Hancurkan armor Pumpkin Hulk dan picu Stun getaran magis
        if (pumpkinHulkEnemy != null && !pumpkinHulkEnemy.isDead)
        {
            pumpkinHulkEnemy.BreakArmor();
            pumpkinHulkEnemy.TriggerStun(4.0f);
            Debug.Log("<color=#ffaa00>[Hall 4 Effect]</color> Gelombang kejut rune memecahkan armor tebal Pumpkin Hulk!");
        }

        UpdateHUD();
    }

    public void UpdateHUD()
    {
        if (objectiveHUDText == null) return;

        if (isChallengeCompleted)
        {
            objectiveHUDText.text =
                "<b><color=#00ff88>★ TANTANGAN RUANGAN 4 SELESAI ★</color></b>\n" +
                "<color=#ffff00>Gerbang Boss Arena Terbuka!</color>\n" +
                "<size=11><color=#cccccc>Pilih Gerbang Math 4A / 4B sebelum Boss Fight!</color></size>";
        }
        else
        {
            objectiveHUDText.text =
                "<b><color=#00e5ff>TANTANGAN: THE RUNES & COLOSSUS</color></b>\n" +
                $"Pelat Runa Terkunci: <b><color=#ffcc00>{activatedPlatesCount} / {totalPlatesRequired}</color></b>\n" +
                "<size=11><color=#cccccc>Injak 3 Pelat! Pancing Hulk menabrak pilar agar ter-stun!</color></size>";
        }
    }

    private void EnsureChallengeSetup()
    {
        // Hubungkan pelat otomatis jika list belum terisi
        if (pressurePlates.Count == 0)
        {
            foreach (var p in GetComponentsInChildren<Hall4PressurePlate>(true))
            {
                if (!pressurePlates.Contains(p)) pressurePlates.Add(p);
            }
        }

        // Hubungkan barriers otomatis jika belum
        Transform arena = transform.parent;
        if (arena != null)
        {
            if (exitBarrier4A == null)
            {
                Transform t = transform.Find("Barrier_Hall4_ExitA");
                if (t != null) exitBarrier4A = t.gameObject;
            }
            if (exitBarrier4B == null)
            {
                Transform t = transform.Find("Barrier_Hall4_ExitB");
                if (t != null) exitBarrier4B = t.gameObject;
            }
        }

        // Hubungkan Pumpkin Hulk otomatis jika belum
        if (pumpkinHulkEnemy == null)
        {
            foreach (var ai in Object.FindObjectsByType<EnemyAI>())
            {
                if (ai != null && (ai.roomPrefix == "H4_" || ai.name.ToLower().Contains("pumpkin")))
                {
                    pumpkinHulkEnemy = ai;
                    break;
                }
            }
        }
    }
}
