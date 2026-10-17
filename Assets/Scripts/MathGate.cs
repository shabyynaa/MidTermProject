using UnityEngine;

public enum MathGateType
{
    AddHP,        // +HP (Healing)
    SubHP,        // -HP (Damage/Curse)
    MultiplyATK,  // x ATK (Buff Serangan)
    DivideATK,    // / ATK (Debuff Lemah)
    AddSpeed,     // + SPD (Speed Boost)
    SubSpeed,     // - SPD (Slowness Trap)
    CustomRisk    // Custom: +SPD tapi -HP
}

/// <summary>
/// Komponen Math Gate untuk 1 papan gerbang (berisi 2 efek: plus & minus).
/// Mengunci gerbang ini dan gerbang pasangannya jika salah satu dilewati.
/// </summary>
public class MathGate : MonoBehaviour
{
    [Header("Gerbang Pasangan (Misal Papan Kiri ke Papan Kanan)")]
    [Tooltip("Tarik GameObject gerbang sebelahnya ke sini di Inspector!")]
    public MathGate pairedGate; 

    [Header("Efek 1 (Misal Plus / Buff)")]
    public MathGateType primaryGateType = MathGateType.MultiplyATK;
    public float primaryValue = 2f;

    [Header("Efek 2 (Misal Minus / Debuff dalam 1 Gate)")]
    public bool useSecondaryEffect = true;
    public MathGateType secondaryGateType = MathGateType.SubSpeed;
    public float secondaryValue = 2f;

    [Header("Custom Label & Settings")]
    public string customLabel = "";

    [Header("Status Gerbang")]
    public bool isUsed = false;

    [Header("UI Text Mesh")]
    public TextMesh labelText;

    private Collider col;
    private Renderer rend;

    private void Awake()
    {
        col = GetComponent<Collider>();
        rend = GetComponent<Renderer>();
        
        SetupTextMesh();
        UpdateVisuals();
    }

    private void OnValidate()
    {
        SetupTextMesh();
        UpdateVisuals();
    }

    /// <summary>
    /// Memastikan TextMesh selalu muncul di DEPAN papan dan mudah dibaca dari kamera
    /// </summary>
    private void SetupTextMesh()
    {
        if (labelText == null)
            labelText = GetComponentInChildren<TextMesh>();

        if (labelText != null)
        {
            labelText.anchor = TextAnchor.MiddleCenter;
            labelText.alignment = TextAlignment.Center;
            labelText.fontSize = 48;
            labelText.characterSize = 0.08f;

            // Memajukan posisi teks sedikit ke depan permukaan papan agar tidak terbenam/hilang
            labelText.transform.localPosition = new Vector3(0f, 0f, -0.15f);

            // Diputar 180 derajat agar terbaca dari arah kamera Player
            labelText.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }

    public void Setup(MathGateType type, float val, string customTxt = "", float secVal = 0f)
    {
        primaryGateType = type;
        primaryValue = val;
        
        if (secVal != 0f)
        {
            useSecondaryEffect = true;
            secondaryValue = secVal;
        }

        customLabel = customTxt;
        UpdateVisuals();
    }

    public void SetupDual(MathGateType primaryType, float primaryVal, MathGateType secondaryType, float secondaryVal, string customTxt = "")
    {
        primaryGateType = primaryType;
        primaryValue = primaryVal;
        useSecondaryEffect = true;
        secondaryGateType = secondaryType;
        secondaryValue = secondaryVal;
        customLabel = customTxt;
        UpdateVisuals();
    }

    // ⭐ TAMBAHAN METHOD ALIAS SUPAYA MENYESUAIKAN DENGAN BUILDER ⭐
    public void SetupDouble(MathGateType primaryType, float primaryVal, MathGateType secondaryType, float secondaryVal, string customTxt = "")
    {
        SetupDual(primaryType, primaryVal, secondaryType, secondaryVal, customTxt);
    }

    public void UpdateVisuals()
    {
        if (labelText == null) SetupTextMesh();

        string display = customLabel;
        Color gateColor = isUsed ? new Color(0.2f, 0.2f, 0.2f, 0.3f) : new Color(0.9f, 0.45f, 0.1f, 0.85f);

        if (string.IsNullOrEmpty(display))
        {
            string primaryStr = GetEffectString(primaryGateType, primaryValue);
            
            if (useSecondaryEffect)
            {
                string secondaryStr = GetEffectString(secondaryGateType, secondaryValue);
                display = $"{primaryStr}\n{secondaryStr}";
            }
            else
            {
                display = primaryStr;
            }
        }

        if (labelText != null)
        {
            labelText.text = isUsed ? "CLOSED" : display;
        }

        if (rend != null)
        {
            rend.material.color = gateColor;
        }
    }

    private string GetEffectString(MathGateType type, float val)
    {
        switch (type)
        {
            case MathGateType.AddHP: return $"+{val} HP";
            case MathGateType.SubHP: return $"-{val} HP";
            case MathGateType.MultiplyATK: return $"x{val} ATK";
            case MathGateType.DivideATK: return $"/{val} ATK";
            case MathGateType.AddSpeed: return $"+{val} SPD";
            case MathGateType.SubSpeed: return $"-{val} SPD";
            default: return "";
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isUsed) return;

        if (other.CompareTag("Player") || other.name.Contains("Player") || other.CompareTag("Enemy") || other.name.Contains("Enemy"))
        {
            // Terapkan efek (Plus & Minus sekaligus) ke Player
            ApplyMathEffect(other.gameObject);
            
            // Kunci gerbang ini DAN gerbang pasangannya (sebelahnya)
            DisableGate();
            if (pairedGate != null)
            {
                pairedGate.DisableGate();
            }
        }
    }

    private void ApplyMathEffect(GameObject target)
    {
        Debug.Log($"<color=yellow>[MathGate]</color> {target.name} memilih gerbang: {labelText?.text?.Replace("\n", " ")}");

        PlayerStats stats = target.GetComponent<PlayerStats>();
        if (stats == null) stats = target.GetComponentInParent<PlayerStats>();
        
        if (stats != null)
        {
            // Terapkan Efek 1 (misal: x2 ATK)
            stats.ApplySingleGateEffect(primaryGateType, primaryValue);

            // Terapkan Efek 2 (misal: -2 SPD)
            if (useSecondaryEffect)
            {
                stats.ApplySingleGateEffect(secondaryGateType, secondaryValue);
            }
        }
    }

    /// <summary>
    /// Mematikan gerbang dan menampilkan teks CLOSED
    /// </summary>
    public void DisableGate()
    {
        isUsed = true;
        if (col != null) col.enabled = false;
        
        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = 0.2f;
            rend.material.color = c;
        }
        
        if (labelText != null)
        {
            labelText.text = "CLOSED";
        }
    }
}