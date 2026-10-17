using UnityEngine;

public enum MathGateType
{
    AddHP,        // +5 HP
    MultiplyATK,  // x2 ATK
    AddSpeed,     // +2 SPD
    TrapDamage    // -3 HP
}

/// <summary>
/// Komponen Math Gate yang ditempel pada gerbang.
/// Mendeteksi Player atau Enemy yang melewatinya.
/// </summary>
public class MathGate : MonoBehaviour
{
    [Header("Gate Settings")]
    public MathGateType gateType = MathGateType.AddHP;
    public float value = 5f;
    public float cooldownSeconds = 3f;

    [Header("Visual Feedback")]
    public TextMesh labelText;
    private bool isCoolingDown = false;
    private Collider col;
    private Renderer rend;

    private void Awake()
    {
        col = GetComponent<Collider>();
        rend = GetComponent<Renderer>();
        UpdateVisuals();
    }

    public void Setup(MathGateType type, float val)
    {
        gateType = type;
        value = val;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (labelText == null)
            labelText = GetComponentInChildren<TextMesh>();

        string display = "";
        Color gateColor = Color.cyan;

        switch (gateType)
        {
            case MathGateType.AddHP:
                display = $"+{value} HP";
                gateColor = new Color(0.2f, 0.8f, 0.3f, 0.6f); // Hijau
                break;
            case MathGateType.MultiplyATK:
                display = $"x{value} ATK";
                gateColor = new Color(1.0f, 0.3f, 0.1f, 0.6f); // Merah Oranye
                break;
            case MathGateType.AddSpeed:
                display = $"+{value} SPD";
                gateColor = new Color(0.1f, 0.7f, 1.0f, 0.6f); // Biru Muda
                break;
            case MathGateType.TrapDamage:
                display = $"-{value} HP";
                gateColor = new Color(0.6f, 0.1f, 0.8f, 0.6f); // Ungu Racun
                break;
        }

        if (labelText != null)
        {
            labelText.text = display;
        }

        if (rend != null)
        {
            rend.material.color = gateColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCoolingDown) return;

        // Cek apakah yang lewat adalah Player atau Enemy
        if (other.CompareTag("Player") || other.CompareTag("Enemy") || other.name.Contains("Player") || other.name.Contains("Enemy"))
        {
            ApplyMathEffect(other.gameObject);
            StartCoroutine(CooldownRoutine());
        }
    }

    private void ApplyMathEffect(GameObject target)
    {
        Debug.Log($"<color=yellow>[MathGate]</color> {target.name} melewati gate: <b>{labelText?.text}</b>!");

        // Berikan efek langsung ke PlayerStats jika ada
        PlayerStats stats = target.GetComponent<PlayerStats>();
        if (stats == null) stats = target.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyGateEffect(gateType, value);
        }
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        if (col != null) col.enabled = false;
        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = 0.15f; // redup saat cooldown
            rend.material.color = c;
        }

        yield return new WaitForSeconds(cooldownSeconds);

        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = 0.6f;
            rend.material.color = c;
        }
        if (col != null) col.enabled = true;
        isCoolingDown = false;
    }
}
