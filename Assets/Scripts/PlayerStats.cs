using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mengelola status Player (HP, ATK, Speed) dan menghubungkannya dengan UI Health Panel.
/// Pasang script ini pada GameObject Player.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Stats")]
    public float attackPower = 10f;
    public float moveSpeedMultiplier = 1f;

    [Header("UI References (Otomatis dicari jika kosong)")]
    public Slider healthSlider;
    public Text hpText;
    public Text statsText;

    private PlayerController playerController;

    private void Awake()
    {
        currentHP = maxHP;
        playerController = GetComponent<PlayerController>();
    }

    private void Start()
    {
        FindUIReferences();
        UpdateUI();
    }

    /// <summary>
    /// Otomatis mencari komponen UI di Scene jika belum di-assign di Inspector
    /// </summary>
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

    /// <summary>
    /// Dipanggil saat Player terkena serangan Enemy
    /// </summary>
    public void TakeDamage(float damage)
    {
        currentHP = Mathf.Clamp(currentHP - damage, 0f, maxHP);
        UpdateUI();

        Debug.Log($"<color=red>[Player]</color> Terkena damage {damage}! Sisa HP: {currentHP}");

        if (currentHP <= 0f)
        {
            Die();
        }
    }

    /// <summary>
    /// Dipanggil saat Player melewati Math Gate
    /// </summary>
    public void ApplyGateEffect(MathGateType type, float value, float secondaryValue = 0f)
    {
        switch (type)
        {
            case MathGateType.AddHP:
                currentHP = Mathf.Clamp(currentHP + value, 0f, maxHP);
                Debug.Log($"<color=green>[Player Heal]</color> +{value} HP! HP sekarang: {currentHP}");
                break;

            case MathGateType.SubHP:
                TakeDamage(value);
                Debug.Log($"<color=red>[Player Curse]</color> -{value} HP! Sisa HP: {currentHP}");
                break;

            case MathGateType.MultiplyATK:
                attackPower = Mathf.Round(attackPower * value);
                Debug.Log($"<color=orange>[Player Buff]</color> ATK x{value}! ATK sekarang: {attackPower}");
                break;

            case MathGateType.DivideATK:
                attackPower = Mathf.Max(1f, Mathf.Round(attackPower / value));
                Debug.Log($"<color=grey>[Player Weakness]</color> ATK /{value}! ATK sekarang: {attackPower}");
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

            case MathGateType.CustomRisk:
                // +Speed tapi -HP
                if (playerController != null)
                {
                    playerController.walkSpeed += value;
                    playerController.sprintSpeed += value;
                }
                TakeDamage(secondaryValue);
                Debug.Log($"<color=magenta>[Player High Risk]</color> +{value} SPD & -{secondaryValue} HP!");
                break;
        }

        UpdateUI();
    }

    /// <summary>
    /// Memperbarui tampilan bar dan teks UI
    /// </summary>
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

        if (statsText != null)
        {
            float spd = playerController != null ? playerController.walkSpeed : 5f;
            statsText.text = $"ATK: {attackPower}  |  SPD: {spd:F1}";
        }
    }

    private void Die()
    {
        Debug.Log("<color=red><b>[GAME OVER]</b> Player telah dikalahkan!</color>");
        // Tambahkan efek ragdoll / reload scene jika diinginkan
    }
}
