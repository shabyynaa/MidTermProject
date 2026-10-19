using UnityEngine;

/// <summary>
/// Komponen untuk Rak Penyimpanan Kunci (Shelf) di Hall 2.
/// Menerima kunci yang dibawa oleh Player dan memunculkan model kunci fisik di rak.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hall2Shelf : MonoBehaviour
{
    [Header("Slot Visual Kunci di Rak")]
    [Tooltip("3 GameObject visual kunci yang diletakkan di rak (akan dimunculkan satu per satu)")]
    public GameObject[] placedKeyVisuals = new GameObject[3];

    [Header("Prompt / Indikator")]
    public TextMesh shelfTextMesh;

    private int currentKeysOnShelf = 0;

    private void Awake()
    {
        // Pastikan ada trigger collider untuk mendeteksi kedekatan Player
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        UpdateShelfVisuals();
    }

    /// <summary>
    /// Menempatkan 1 atau lebih kunci yang dibawa player ke rak ini
    /// </summary>
    public int DepositKeys(int carriedCount)
    {
        if (carriedCount <= 0) return 0;

        int needed = 3 - currentKeysOnShelf;
        int toDeposit = Mathf.Min(carriedCount, needed);

        currentKeysOnShelf += toDeposit;
        UpdateShelfVisuals();

        return toDeposit;
    }

    public void UpdateShelfVisuals()
    {
        if (placedKeyVisuals != null)
        {
            for (int i = 0; i < placedKeyVisuals.Length; i++)
            {
                if (placedKeyVisuals[i] != null)
                {
                    placedKeyVisuals[i].SetActive(i < currentKeysOnShelf);
                }
            }
        }

        if (shelfTextMesh != null)
        {
            if (currentKeysOnShelf >= 3)
            {
                shelfTextMesh.text = "<color=#00ff88>RAK LENGKAP (3/3)\nGERBANG TERBUKA!</color>";
            }
            else
            {
                shelfTextMesh.text = $"RAK KUNCI\n<color=#ffcc00>{currentKeysOnShelf} / 3 Terpasang</color>";
            }
        }
    }
}
