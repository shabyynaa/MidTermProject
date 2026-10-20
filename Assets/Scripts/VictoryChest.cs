using UnityEngine;

/// <summary>
/// Peti Emas Kemenangan di Altar Tengah Boss Arena.
/// Muncul bercahaya emas setelah Boss Maw J Laygo dikalahkan.
/// Player dapat membukanya dengan menekan [E / Klik Kiri] untuk memicu Layar Kemenangan (Game Clear).
/// </summary>
public class VictoryChest : MonoBehaviour
{
    [Header("Chest Status")]
    public bool isUnlocked = false;
    public bool isOpened = false;

    [Header("Visual & Light")]
    public Light goldenGlowLight;
    public TextMesh statusTextMesh;
    public GameObject chestLid;

    private Transform playerTransform;

    private void Start()
    {
        FindPlayer();
        UpdateVisuals();
    }

    private void FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p == null) p = GameObject.Find("Player");
        if (p != null) playerTransform = p.transform;
    }

    private void Update()
    {
        if (playerTransform == null) FindPlayer();

        // Pulsing golden glow jika sudah unlock tapi belum dibuka
        if (isUnlocked && !isOpened && goldenGlowLight != null)
        {
            float pulse = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            goldenGlowLight.intensity = 2.0f + pulse * 1.5f;
        }
    }

    /// <summary>
    /// Membuka kunci peti setelah Boss mati (dipanggil oleh BossArenaManager)
    /// </summary>
    public void UnlockChest()
    {
        isUnlocked = true;
        UpdateVisuals();
        Debug.Log("<color=#ffd700><b>[Victory Chest]</b> Peti Kemenangan Emas di Altar telah terbuka segelnya!</color>");
    }

    /// <summary>
    /// Membuka peti saat pemain berinteraksi (Klik Kiri / E)
    /// </summary>
    public void OpenChest()
    {
        if (!isUnlocked || isOpened) return;
        isOpened = true;

        // Buka tutup peti jika ada
        if (chestLid != null)
        {
            chestLid.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
        }

        UpdateVisuals();

        Debug.Log("<color=#00ff88><b>[VICTORY]</b> Pemain membuka Peti Kemenangan Akhir! Menampilkan layar Game Clear!</color>");

        if (BossArenaManager.Instance != null)
        {
            BossArenaManager.Instance.ShowVictoryScreen();
        }
    }

    public void UpdateVisuals()
    {
        if (goldenGlowLight != null)
        {
            goldenGlowLight.enabled = isUnlocked;
            goldenGlowLight.color = new Color(1f, 0.85f, 0.2f);
            goldenGlowLight.range = isOpened ? 6f : 4f;
        }

        if (statusTextMesh != null)
        {
            if (isOpened)
            {
                statusTextMesh.text = "<color=#ffd700><b>PETI KEMENANGAN [TERBUKA]</b></color>\n<size=20><color=#00ff88>★ Harta Dungeon Diperoleh ★</color></size>";
            }
            else if (isUnlocked)
            {
                statusTextMesh.text = "<color=#ffd700><b>★ PETI KEMENANGAN DUNGEON ★</b></color>\n<size=22><color=#ffffff>[Klik Kiri / E] Buka Peti!</color></size>";
            }
            else
            {
                statusTextMesh.text = "<color=#888888>PETI EMAS ALTAR [TERSEGEL]</color>\n<size=20><color=#ff4444>Kalahkan Maw J Laygo!</color></size>";
            }
        }
    }
}
