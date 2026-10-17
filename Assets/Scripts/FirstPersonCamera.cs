using UnityEngine;

/// <summary>
/// Kontrol Kamera First Person (POV Mata Player) Bersih & Anti-Tembus Helm.
/// Pasang script ini pada Main Camera.
/// </summary>
public class FirstPersonCamera : MonoBehaviour
{
    [Header("Target & Positioning")]
    [Tooltip("Transform Player (Badan yang akan diputar horizontal)")]
    public Transform playerBody;

    [Tooltip("Tinggi mata Player dari tanah")]
    public float eyeHeight = 1.75f;

    [Tooltip("Jarak kamera maju ke depan agar benar-benar di depan helm")]
    public float forwardOffset = 0.55f;

    [Header("Look Sensitivity")]
    public float mouseSensitivity = 150f;

    [Header("Pitch Limits")]
    public float minPitch = -75f;
    public float maxPitch = 75f;

    private float xRotation = 0f;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.nearClipPlane = 0.01f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) playerBody = p.transform;
        }

        // Opsional: Sembunyikan mesh kepala/helm player jika ada agar tidak pernah menghalangi pandangan sama sekali
        HidePlayerHeadMesh();
    }

    private void HidePlayerHeadMesh()
    {
        if (playerBody == null) return;
        
        // Cari child mesh dengan nama head / helmet
        foreach (Renderer r in playerBody.GetComponentsInChildren<Renderer>())
        {
            string rName = r.name.ToLower();
            if (rName.Contains("head") || rName.Contains("helmet") || rName.Contains("helm"))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
        }
    }

    private void LateUpdate()
    {
        if (playerBody == null) return;

        // 1. Baca Input Mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. Putar Vertikal (Pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

        // 3. Putar Badan Player secara Horizontal (Yaw)
        playerBody.Rotate(Vector3.up * mouseX);

        // 4. Posisikan kamera tepat di depan mata (sedikit maju agar tidak clipping helm)
        Vector3 targetPos = playerBody.position + Vector3.up * eyeHeight + playerBody.forward * forwardOffset;
        transform.position = targetPos;
        transform.localRotation = Quaternion.Euler(xRotation, playerBody.eulerAngles.y, 0f);

        // Escape untuk toggle kursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
        }
    }
}
