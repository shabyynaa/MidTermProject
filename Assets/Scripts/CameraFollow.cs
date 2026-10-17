using UnityEngine;

/// <summary>
/// Script kamera Top-Down yang mengikuti Player secara halus.
/// Pasang script ini pada Main Camera.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Tooltip("Target yang diikuti kamera (biasanya Player)")]
    public Transform target;

    [Tooltip("Jarak kamera terhadap target (X, Y tinggi kamera, Z jarak belakang)")]
    public Vector3 offset = new Vector3(0f, 14f, -10f);

    [Tooltip("Kehalusan pergerakan kamera")]
    public float smoothSpeed = 5f;

    private void Start()
    {
        // Jika target belum di-assign, otomatis cari objek dengan tag Player atau nama 'Player'
        if (target == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) target = p.transform;
        }

        // Otomatis atur sudut kamera menunduk ke arah dungeon
        transform.rotation = Quaternion.Euler(55f, 0f, 0f);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }
}
