using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Manager Tantangan Khusus Hall 3: "The Torchbearer & Shadow Hunter"
/// 1. Player harus menyalakan 3 Tungku Api (Braziers) di Hall 3.
/// 2. Setiap tungku yang menyala menciptakan Safe Zone (lingkaran cahaya) tempat Vampire takut masuk.
/// 3. Di luar cahaya, Vampire bergerak sangat cepat (6.2 SPD) dan memiliki Life Steal.
/// 4. Setelah ke-3 tungku menyala, seluruh ruangan terang, Vampire melemah, dan penghalang ke Gate 3A & 3B terbuka!
/// </summary>
public class Hall3ChallengeManager : MonoBehaviour
{
    public static Hall3ChallengeManager Instance { get; private set; }

    [Header("Challenge Status")]
    public int totalBraziersRequired = 3;
    public int litBraziersCount = 0;
    public bool isChallengeCompleted = false;

    [Header("Scene References")]
    public List<Hall3Brazier> braziers = new List<Hall3Brazier>();
    public GameObject exitBarrier3A;
    public GameObject exitBarrier3B;
    public EnemyAI vampireEnemy;

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
            Transform existingPanel = canvasObj.transform.Find("Hall3_ObjectivePanel");
            if (existingPanel != null)
            {
                objectivePanel = existingPanel.gameObject;
                objectiveHUDText = objectivePanel.GetComponentInChildren<Text>();
            }
            else
            {
                objectivePanel = new GameObject("Hall3_ObjectivePanel", typeof(RectTransform));
                objectivePanel.transform.SetParent(canvasObj.transform, false);

                RectTransform rt = objectivePanel.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(1f, 1f);
                rt.anchoredPosition = new Vector2(-25f, -25f);
                rt.sizeDelta = new Vector2(320f, 85f);

                Image bg = objectivePanel.AddComponent<Image>();
                bg.color = new Color(0.10f, 0.05f, 0.08f, 0.88f); // Dark crimson tint for Vampire theme

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

            Transform existingPrompt = canvasObj.transform.Find("InteractionPrompt");
            if (existingPrompt != null)
            {
                promptHUDText = existingPrompt.GetComponent<Text>();
            }
        }
    }

    private void CheckProximityPrompts()
    {
        if (playerTransform == null) return;

        // Cek apakah player berada di area Hall 3 (X: 29.5..52.5, Z: -24.5..-3.5)
        Vector3 pos = playerTransform.position;
        bool inHall3 = (pos.x >= 29.5f && pos.x <= 52.5f && pos.z >= -24.5f && pos.z <= -3.5f);

        if (objectivePanel != null)
        {
            objectivePanel.SetActive(inHall3);
        }

        if (!inHall3 || promptHUDText == null) return;

        // Cek jarak ke tungku obor yang belum menyala
        Hall3Brazier nearestBrazier = null;
        float minBrazierDist = 2.6f;
        foreach (var b in braziers)
        {
            if (b != null && !b.isLit && b.gameObject.activeInHierarchy)
            {
                float d = Vector3.Distance(playerTransform.position, b.transform.position);
                if (d < minBrazierDist)
                {
                    minBrazierDist = d;
                    nearestBrazier = b;
                }
            }
        }

        if (nearestBrazier != null)
        {
            promptHUDText.text = "<color=#ff9900>[Klik Kiri / E]</color> Nyalakan Tungku Api";
            return;
        }

        // Cek jika Player sedang berada di dalam Safe Zone obor yang menyala
        if (IsPositionInSafeLightZone(playerTransform.position, out _, out _))
        {
            promptHUDText.text = "<color=#00ff88>★ Area Aman Api (Vampire tidak berani masuk!)</color>";
            return;
        }

        // Jika tidak ada interaksi di Hall 3 dan tidak ada prompt lain, kosongkan jika sebelumnya diisi oleh kita
        if (promptHUDText.text.Contains("Tungku Api") || promptHUDText.text.Contains("Area Aman Api"))
        {
            promptHUDText.text = "";
        }
    }

    /// <summary>
    /// Dipanggil saat Player menyalakan salah satu tungku api
    /// </summary>
    public void OnBrazierLit(Hall3Brazier brazier)
    {
        litBraziersCount++;
        Debug.Log($"<color=#ff9900>[Hall 3 Challenge]</color> Tungku api dinyalakan! Total: {litBraziersCount}/{totalBraziersRequired}");

        if (litBraziersCount >= totalBraziersRequired)
        {
            CompleteChallenge();
        }

        UpdateHUD();
    }

    private void CompleteChallenge()
    {
        if (isChallengeCompleted) return;
        isChallengeCompleted = true;

        Debug.Log("<color=#00ff88>[Hall 3 Challenge] ✅ TANTANGAN SELESAI! Seluruh tungku menyala! Gerbang ke Hall 4 terbuka!</color>");

        // Buka / nonaktifkan penghalang gerbang menuju Hall 4 (Gate 3A & 3B)
        if (exitBarrier3A != null) exitBarrier3A.SetActive(false);
        if (exitBarrier3B != null) exitBarrier3B.SetActive(false);

        // Melemahkan Vampire karena ruangan sudah terang benderang
        if (vampireEnemy != null)
        {
            vampireEnemy.chaseSpeed = 2.5f;
            vampireEnemy.attackDamage = 4f;
            Debug.Log("<color=#ff66aa>[Hall 3 Challenge]</color> Vampire melemah akibat cahaya terang seluruh ruangan!");
        }

        UpdateHUD();
    }

    public void UpdateHUD()
    {
        if (objectiveHUDText != null)
        {
            if (isChallengeCompleted)
            {
                objectiveHUDText.text = "<b><color=#00ff88>★ TANTANGAN HALL 3 SELESAI!</color></b>\n" +
                                        "3/3 Tungku Api telah Menyala.\n" +
                                        "<color=#ffea00>Gerbang 3A & 3B telah Terbuka!</color>";
            }
            else
            {
                objectiveHUDText.text = "<b><color=#ff66aa>TANTANGAN HALL 3:</color></b>\n" +
                                        "Nyalakan 3 Tungku Api untuk Mengusir Kegelapan\n" +
                                        $"• Tungku Menyala : <color=#ff9900>{litBraziersCount} / {totalBraziersRequired}</color>\n" +
                                        "• Berlindunglah di Area Cahaya dari Vampire!";
            }
        }
    }

    /// <summary>
    /// Mengecek apakah suatu posisi (Player / Vampire) berada di dalam lingkaran cahaya aman tungku obor
    /// </summary>
    public bool IsPositionInSafeLightZone(Vector3 pos, out Vector3 lightCenter, out float safeRadius)
    {
        lightCenter = Vector3.zero;
        safeRadius = 0f;

        pos.y = 0f;
        foreach (var b in braziers)
        {
            if (b != null && b.isLit)
            {
                Vector3 bPos = b.transform.position;
                bPos.y = 0f;
                float d = Vector3.Distance(pos, bPos);
                if (d <= b.safeRadius)
                {
                    lightCenter = b.transform.position;
                    safeRadius = b.safeRadius;
                    return true;
                }
            }
        }

        return false;
    }

    public void EnsureChallengeSetup()
    {
        if (braziers.Count == 0)
        {
            braziers.AddRange(Object.FindObjectsByType<Hall3Brazier>());
        }

        if (exitBarrier3A == null) exitBarrier3A = GameObject.Find("Barrier_Hall3_ExitA");
        if (exitBarrier3B == null) exitBarrier3B = GameObject.Find("Barrier_Hall3_ExitB");

        if (vampireEnemy == null)
        {
            foreach (var ai in Object.FindObjectsByType<EnemyAI>())
            {
                if (ai != null && (ai.roomPrefix == "H3_" || ai.name.ToLower().Contains("vampire")))
                {
                    vampireEnemy = ai;
                    break;
                }
            }
        }
    }
}
