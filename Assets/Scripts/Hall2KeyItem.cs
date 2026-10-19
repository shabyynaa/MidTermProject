using UnityEngine;

/// <summary>
/// Komponen untuk Kunci Kolektibel di Hall 2.
/// Memberikan efek animasi melayang, berputar pelan, dan cahaya keemasan.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hall2KeyItem : MonoBehaviour
{
    [Header("Key Info")]
    public int keyIndex = 1;
    public string keyName = "Dungeon Key";

    [Header("Visual Effects")]
    public float rotateSpeed = 60f;
    public float bobSpeed = 2.5f;
    public float bobHeight = 0.12f;

    [Header("Status")]
    public bool isCollected = false;

    private Vector3 basePosition;
    private Light keyGlowLight;

    private void Awake()
    {
        basePosition = transform.position;

        // Pastikan ada Collider sebagai trigger agar mudah di-interact
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // Buat cahaya glow lembut warna emas agar mudah terlihat di dungeon
        GameObject lightObj = new GameObject("KeyGlowLight");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        keyGlowLight = lightObj.AddComponent<Light>();
        keyGlowLight.type = LightType.Point;
        keyGlowLight.color = new Color(1f, 0.85f, 0.2f); // Golden yellow
        keyGlowLight.range = 3.5f;
        keyGlowLight.intensity = 1.8f;
        keyGlowLight.shadows = LightShadows.None;
    }

    private void Update()
    {
        if (isCollected) return;

        // Putar kunci secara horizontal
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // Gerakan melayang naik-turun (bobbing)
        float newY = basePosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    /// <summary>
    /// Dipanggil saat Player mengambil kunci ini
    /// </summary>
    public void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        if (Hall2ChallengeManager.Instance != null)
        {
            Hall2ChallengeManager.Instance.OnKeyCollected(this);
        }

        gameObject.SetActive(false);
    }
}
