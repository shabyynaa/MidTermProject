using UnityEngine;

/// <summary>
/// Komponen Pelat Tekanan Runa (Rune Pressure Plate) di Hall 4.
/// Player harus berdiri di atas pelat ini selama ~2.5 detik untuk menguncinya.
/// Begitu terkunci, pelat akan berpendar terang dan mengaktifkan salah satu dari 3 segel.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hall4PressurePlate : MonoBehaviour
{
    [Header("Plate Info")]
    public int plateIndex = 1;
    public string plateName = "Pelat Runa Alfa";

    [Header("Activation Status")]
    public bool isActivated = false;
    [Range(0f, 1f)]
    public float activationProgress = 0f;
    public float activationTimeRequired = 2.5f;
    public bool isPlayerOnPlate = false;

    [Header("Visual & Light References")]
    public Transform sinkingPad;
    public LineRenderer runeCircle;
    public Light plateLight;
    public TextMesh statusTextMesh;

    private float padTargetY = 0.08f;
    private float currentPadY = 0.08f;
    private Color unactivatedColor = new Color(0.15f, 0.75f, 1f, 0.45f); // Soft Cyan
    private Color chargingColor = new Color(1f, 0.85f, 0.2f, 0.85f);    // Charging Golden Yellow
    private Color activatedColor = new Color(0.1f, 1f, 0.5f, 0.95f);     // Solid Bright Emerald/Cyan

    private Transform playerTransform;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        UpdateVisuals();
    }

    private void Start()
    {
        FindPlayer();
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

        // Deteksi jarak fisik player ke pusat pelat (radius 1.35m) sebagai validasi trigger
        if (playerTransform != null)
        {
            Vector3 myPos = transform.position;
            Vector3 pPos = playerTransform.position;
            myPos.y = 0f;
            pPos.y = 0f;
            bool withinDistance = Vector3.Distance(myPos, pPos) <= 1.35f;
            isPlayerOnPlate = withinDistance;
        }

        if (!isActivated)
        {
            if (isPlayerOnPlate)
            {
                // Pemain sedang menginjak pelat: Naikkan progress
                padTargetY = 0.02f; // Pelat turun sedikit saat diinjak
                activationProgress += Time.deltaTime / activationTimeRequired;

                if (activationProgress >= 1f)
                {
                    activationProgress = 1f;
                    LockPlate();
                }
            }
            else
            {
                // Pemain melangkah keluar sebelum 100%: Progress turun perlahan
                padTargetY = 0.08f;
                if (activationProgress > 0f)
                {
                    activationProgress = Mathf.Max(0f, activationProgress - Time.deltaTime * 0.45f);
                }
            }
        }
        else
        {
            padTargetY = 0.02f; // Tetap turun terkunci
        }

        // Animasi sinking pad yang halus
        if (sinkingPad != null)
        {
            currentPadY = Mathf.Lerp(currentPadY, padTargetY, Time.deltaTime * 10f);
            Vector3 localPos = sinkingPad.localPosition;
            localPos.y = currentPadY;
            sinkingPad.localPosition = localPos;
        }

        UpdateVisuals();
    }

    private void LockPlate()
    {
        if (isActivated) return;
        isActivated = true;
        padTargetY = 0.02f;

        Debug.Log($"<color=#00ff88>[Hall 4 Plate]</color> {plateName} #{plateIndex} BERHASIL DIKUNCI!");

        if (Hall4ChallengeManager.Instance != null)
        {
            Hall4ChallengeManager.Instance.OnPlateActivated(this);
        }
    }

    public void UpdateVisuals()
    {
        Color targetColor = isActivated ? activatedColor : (isPlayerOnPlate && activationProgress > 0.05f ? Color.Lerp(unactivatedColor, chargingColor, activationProgress) : unactivatedColor);

        // Update LineRenderer Ring
        if (runeCircle != null)
        {
            runeCircle.startColor = targetColor;
            runeCircle.endColor = targetColor;
            if (runeCircle.material != null)
            {
                runeCircle.material.color = targetColor;
            }
        }

        // Update Point Light
        if (plateLight != null)
        {
            if (isActivated)
            {
                plateLight.enabled = true;
                plateLight.color = new Color(0.2f, 1f, 0.7f);
                plateLight.intensity = 3.5f;
                plateLight.range = 4.5f;
            }
            else if (isPlayerOnPlate && activationProgress > 0.05f)
            {
                plateLight.enabled = true;
                plateLight.color = Color.Lerp(new Color(0.2f, 0.8f, 1f), new Color(1f, 0.9f, 0.3f), activationProgress);
                plateLight.intensity = 1.2f + activationProgress * 2.0f;
                plateLight.range = 2.5f + activationProgress * 1.5f;
            }
            else
            {
                plateLight.enabled = true;
                plateLight.color = new Color(0.15f, 0.75f, 1f);
                // Soft breathing idle glow
                float breathe = (Mathf.Sin(Time.time * 2.5f + plateIndex) + 1f) * 0.5f;
                plateLight.intensity = 0.8f + breathe * 0.6f;
                plateLight.range = 2.4f;
            }
        }

        // Update 3D Floating Status Text
        if (statusTextMesh != null)
        {
            if (isActivated)
            {
                statusTextMesh.text = $"<color=#00ff88><b>{plateName.ToUpper()} [TERKUNCI]</b></color>\n<size=20><color=#aaffaa>★ Energi Aktif</color></size>";
            }
            else if (isPlayerOnPlate && activationProgress > 0f)
            {
                int percent = Mathf.RoundToInt(activationProgress * 100f);
                statusTextMesh.text = $"<color=#ffcc00><b>MENGISI ENERGI RUNA...</b></color>\n<size=22><color=#ffffff>[{percent}%] Tahan Posisi!</color></size>";
            }
            else
            {
                statusTextMesh.text = $"<color=#00e5ff><b>{plateName.ToUpper()}</b></color>\n<size=20><color=#dddddd>[Injak & Tahan 2.5 Detik]</color></size>";
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isActivated ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1.35f);
    }
}
