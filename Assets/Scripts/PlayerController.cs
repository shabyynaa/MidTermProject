using UnityEngine;

/// <summary>
/// Controller pergerakan Player dengan WASD / Tombol Panah.
/// Membutuhkan komponen CharacterController pada GameObject Player.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Kecepatan jalan normal")]
    public float walkSpeed = 5f;

    [Tooltip("Kecepatan lari (tahan Left Shift)")]
    public float sprintSpeed = 8f;

    [Tooltip("Kecepatan putar karakter saat berbelok")]
    public float rotationSpeed = 12f;

    [Tooltip("Gravitasi agar karakter menempel di lantai")]
    public float gravity = 20f;

    [Header("Optional Animation")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 moveDirection;
    private float currentSpeed;
    private float verticalVelocity = 0f; // Kecepatan vertikal dipisah agar tidak menumpuk

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // Otomatis cari Animator jika belum di-assign di Inspector
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        // 1. Ambil Input dari WASD atau Tombol Panah
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical   = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        // 2. Cek apakah menekan Shift untuk Sprint
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // 3. Gerakan relatif terhadap arah pandangan mata (FPS Movement)
        // W/S = Maju/Mundur mengikuti transform.forward, A/D = Melangkah samping mengikuti transform.right
        Vector3 move = (transform.right * horizontal + transform.forward * vertical).normalized;
        moveDirection = move * currentSpeed;

        // 4. Gravitasi: Reset saat grounded, akumulasi saat melayang
        if (controller.isGrounded)
        {
            verticalVelocity = -2f; // Dorong kecil ke bawah agar selalu menempel lantai
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime; // Jatuh bebas
        }

        // 5. Gabungkan horizontal + vertikal lalu gerakkan
        Vector3 finalMove = new Vector3(moveDirection.x, verticalVelocity, moveDirection.z);
        controller.Move(finalMove * Time.deltaTime);

        // 6. Update Animasi (Jika ada parameter di Animator)
        if (animator != null)
        {
            float speedPercent = inputDir.magnitude * (isSprinting ? 1f : 0.5f);
            
            // Coba set parameter umum jika ada (Speed / IsWalking / IsRunning)
            if (HasParameter("Speed", animator))
                animator.SetFloat("Speed", speedPercent);
            
            if (HasParameter("IsWalking", animator))
                animator.SetBool("IsWalking", inputDir.magnitude > 0.1f && !isSprinting);

            if (HasParameter("IsRunning", animator))
                animator.SetBool("IsRunning", inputDir.magnitude > 0.1f && isSprinting);
        }
    }

    // Helper untuk mengecek apakah parameter animator tersedia
    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }
}
