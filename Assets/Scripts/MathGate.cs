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
/// Komponen Math Gate yang ditempel pada gerbang.
/// Mendeteksi Player atau Enemy yang melewatinya dan menampilkan efek matematika (Plus/Minus).
/// </summary>
public class MathGate : MonoBehaviour
{
    [Header("Gate Settings")]
    public MathGateType gateType = MathGateType.AddHP;
    public float value = 5f;
    public float secondaryValue = 5f; // Dipakai untuk custom misal minus HP
    public string customLabel = "";
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

    public void Setup(MathGateType type, float val, string customTxt = "", float secVal = 0f)
    {
        gateType = type;
        value = val;
        secondaryValue = secVal;
        customLabel = customTxt;
        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        if (labelText == null)
            labelText = GetComponentInChildren<TextMesh>();

        string display = customLabel;
        Color gateColor = Color.cyan;

        if (string.IsNullOrEmpty(display))
        {
            switch (gateType)
            {
                case MathGateType.AddHP:
                    display = $"+{value} HP";
                    gateColor = new Color(0.2f, 0.9f, 0.3f, 0.7f); // Hijau cerah
                    break;
                case MathGateType.SubHP:
                    display = $"-{value} HP";
                    gateColor = new Color(0.8f, 0.1f, 0.2f, 0.7f); // Merah tua
                    break;
                case MathGateType.MultiplyATK:
                    display = $"x{value} ATK";
                    gateColor = new Color(1.0f, 0.45f, 0.1f, 0.7f); // Oranye
                    break;
                case MathGateType.DivideATK:
                    display = $"/{value} ATK";
                    gateColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Abu-abu
                    break;
                case MathGateType.AddSpeed:
                    display = $"+{value} SPD";
                    gateColor = new Color(0.1f, 0.8f, 1.0f, 0.7f); // Cyan
                    break;
                case MathGateType.SubSpeed:
                    display = $"-{value} SPD";
                    gateColor = new Color(0.6f, 0.2f, 0.8f, 0.7f); // Ungu
                    break;
                case MathGateType.CustomRisk:
                    display = $"+{value} SPD\n-{secondaryValue} HP";
                    gateColor = new Color(0.9f, 0.2f, 0.7f, 0.7f); // Magenta
                    break;
            }
        }
        else
        {
            // Jika ada custom label
            if (gateType == MathGateType.AddHP || gateType == MathGateType.AddSpeed || gateType == MathGateType.MultiplyATK)
                gateColor = new Color(0.2f, 0.9f, 0.4f, 0.7f);
            else
                gateColor = new Color(0.85f, 0.2f, 0.2f, 0.7f);
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

        if (other.CompareTag("Player") || other.name.Contains("Player") || other.CompareTag("Enemy") || other.name.Contains("Enemy"))
        {
            ApplyMathEffect(other.gameObject);
            StartCoroutine(CooldownRoutine());
        }
    }

    private void ApplyMathEffect(GameObject target)
    {
        Debug.Log($"<color=yellow>[MathGate]</color> {target.name} melewati gate: <b>{labelText?.text?.Replace("\n", " ")}</b>!");

        PlayerStats stats = target.GetComponent<PlayerStats>();
        if (stats == null) stats = target.GetComponentInParent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyGateEffect(gateType, value, secondaryValue);
        }
    }

    private System.Collections.IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        if (col != null) col.enabled = false;
        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = 0.15f;
            rend.material.color = c;
        }

        yield return new WaitForSeconds(cooldownSeconds);

        if (rend != null)
        {
            Color c = rend.material.color;
            c.a = 0.7f;
            rend.material.color = c;
        }
        if (col != null) col.enabled = true;
        isCoolingDown = false;
    }
}
