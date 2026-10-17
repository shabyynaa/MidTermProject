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

    [Header("Perspective & Distance (Tekan 'V' untuk Toggle)")]
    [Tooltip("Jarak kamera dari player. 0 = First Person (POV Mata), 3.2 = Third Person")]
    public float distance = 3.2f;

    [Tooltip("Offset horizontal agar pandangan over-the-shoulder terasa nyaman (saat Third Person)")]
    public float shoulderOffset = 0.35f;

    [Header("Look Sensitivity")]
    public float mouseSensitivity = 150f;

    [Header("Pitch Limits")]
    public float minPitch = -65f;
    public float maxPitch = 70f;

    private float xRotation = 0f;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.nearClipPlane = 0.05f;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Default ke Third Person (Jarak 3.5m) agar seluruh badan & animasi karakter terlihat jelas
        if (distance < 1.0f)
        {
            distance = 3.5f;
        }
        xRotation = 12f; // Sudut pandang kamera menunduk sedikit menghadap badan karakter

        if (playerBody == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) playerBody = p.transform;
        }

        UpdateMeshVisibility();
    }

    private void UpdateMeshVisibility()
    {
        if (playerBody == null) return;

        bool isFirstPerson = distance < 0.5f;

        foreach (Renderer r in playerBody.GetComponentsInChildren<Renderer>(true))
        {
            string rName = r.name.ToLower();
            if (isFirstPerson && (rName.Contains("head") || rName.Contains("helmet") || rName.Contains("helm")))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            }
            else
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.enabled = true;
            }
        }
    }

    private void LateUpdate()
    {
        if (playerBody == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p == null) p = GameObject.Find("Player");
            if (p != null) playerBody = p.transform;
            if (playerBody == null) return;
        }

        // 1. Toggle 1st / 3rd Person dengan tombol 'V'
        if (Input.GetKeyDown(KeyCode.V))
        {
            distance = (distance < 0.5f) ? 3.5f : 0f;
            UpdateMeshVisibility();
        }

        // 2. Zoom in / out dengan Scroll Mouse
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float prevDist = distance;
            distance = Mathf.Clamp(distance - scroll * 4f, 0f, 6.5f);
            if ((prevDist < 0.5f && distance >= 0.5f) || (prevDist >= 0.5f && distance < 0.5f))
            {
                UpdateMeshVisibility();
            }
        }

        // 3. Baca Input Mouse
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 4. Putar Vertikal (Pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);

        // 5. Putar Badan Player secara Horizontal (Yaw)
        playerBody.Rotate(Vector3.up * mouseX);

        // 6. Hitung Posisi Kamera (First Person vs Third Person)
        Quaternion camRotation = Quaternion.Euler(xRotation, playerBody.eulerAngles.y, 0f);
        Vector3 targetPivot = playerBody.position + Vector3.up * eyeHeight;

        Vector3 targetPos;
        if (distance < 0.5f)
        {
            // First Person POV (Mata)
            targetPos = targetPivot + playerBody.forward * forwardOffset;
        }
        else
        {
            // Third Person (Di belakang bahu Player)
            Vector3 offset = playerBody.right * shoulderOffset - (camRotation * Vector3.forward * distance);
            targetPos = targetPivot + offset;

            // Anti-tembus dinding sederhana (Raycast dari mata ke posisi kamera)
            if (Physics.Raycast(targetPivot, (targetPos - targetPivot).normalized, out RaycastHit hit, distance))
            {
                if (!hit.collider.isTrigger && !hit.collider.CompareTag("Player"))
                {
                    targetPos = hit.point + hit.normal * 0.2f;
                }
            }
        }

        transform.position = targetPos;
        transform.rotation = camRotation;

        // Escape untuk toggle kursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = (Cursor.lockState == CursorLockMode.Locked) ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = (Cursor.lockState != CursorLockMode.Locked);
        }
    }
}
