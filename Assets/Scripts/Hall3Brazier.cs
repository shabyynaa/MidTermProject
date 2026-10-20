using UnityEngine;

/// <summary>
/// Komponen Tungku Obor / Brazier di Hall 3.
/// Sebelum dinyalakan: Gelap dan tidak melindungi Player.
/// Setelah dinyalakan: Memancarkan cahaya hangat (Safe Zone) dengan radius ~2.0m.
/// Vampire takut pada cahaya ini dan tidak berani masuk ke lingkaran api.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hall3Brazier : MonoBehaviour
{
    [Header("Brazier Info")]
    public int brazierIndex = 1;
    public string brazierName = "Tungku Api Kuno";

    [Header("Status")]
    public bool isLit = false;
    public float safeRadius = 2.0f;

    [Header("Visual & Light References")]
    public Light flameLight;
    public GameObject flameVisual;
    public TextMesh statusTextMesh;
    public LineRenderer safeZoneCircle;

    private float baseLightIntensity = 3.2f;
    private float flickerSpeed = 12f;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        UpdateVisuals();
    }

    private void Update()
    {
        if (isLit && flameLight != null)
        {
            // Efek kedip api (flickering flame) yang dinamis
            float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, brazierIndex * 10f);
            flameLight.intensity = baseLightIntensity + (noise - 0.5f) * 0.8f;
        }
    }

    /// <summary>
    /// Menyalakan tungku obor ini (dipanggil saat Player menekan Klik Kiri / E di dekatnya)
    /// </summary>
    public void Ignite()
    {
        if (isLit) return;
        isLit = true;

        UpdateVisuals();

        Debug.Log($"<color=#ff9900>[Hall 3 Brazier]</color> {brazierName} #{brazierIndex} telah dinyalakan! Area aman aktif (radius {safeRadius}m).");

        if (Hall3ChallengeManager.Instance != null)
        {
            Hall3ChallengeManager.Instance.OnBrazierLit(this);
        }
    }

    public void UpdateVisuals()
    {
        if (flameLight != null)
        {
            flameLight.enabled = isLit;
            flameLight.color = new Color(1f, 0.62f, 0.18f); // Warm fire orange
            flameLight.range = safeRadius * 1.5f;
            flameLight.intensity = isLit ? baseLightIntensity : 0f;
            flameLight.shadows = LightShadows.None;
        }

        if (flameVisual != null)
        {
            flameVisual.SetActive(isLit);
        }

        if (safeZoneCircle != null)
        {
            safeZoneCircle.enabled = isLit;
            if (isLit)
            {
                UpdateCirclePoints();
            }
        }

        if (statusTextMesh != null)
        {
            if (isLit)
            {
                statusTextMesh.text = "<color=#ff9900>TUNGKU API [MENYALA]</color>\n<size=22><color=#00ff88>★ Area Aman Vampire</color></size>";
            }
            else
            {
                statusTextMesh.text = "<color=#888888>TUNGKU API [PADAM]</color>\n<size=22><color=#ffcc00>[Klik Kiri / E] Nyalakan</color></size>";
            }
        }
    }

    private void UpdateCirclePoints()
    {
        if (safeZoneCircle == null) return;
        int count = safeZoneCircle.positionCount > 0 ? safeZoneCircle.positionCount : 40;
        safeZoneCircle.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * (Mathf.PI * 2f / count);
            safeZoneCircle.SetPosition(i, new Vector3(Mathf.Cos(angle) * safeRadius, 0f, Mathf.Sin(angle) * safeRadius));
        }
    }

    /// <summary>
    /// Memeriksa apakah posisi tertentu berada di dalam radius cahaya aman tungku ini
    /// </summary>
    public bool IsPositionInSafeRadius(Vector3 targetPos)
    {
        if (!isLit) return false;

        Vector3 myPos = transform.position;
        myPos.y = 0f;
        targetPos.y = 0f;

        return Vector3.Distance(myPos, targetPos) <= safeRadius;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isLit ? new Color(1f, 0.6f, 0f, 0.4f) : new Color(0.4f, 0.4f, 0.4f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, safeRadius);
    }
}
