using UnityEngine;

/// <summary>
/// Controller pergerakan Player dengan WASD + Space untuk Attack + Klik Kiri untuk Take Item.
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

    [Header("Combat Settings (Tombol Space)")]
    [Tooltip("Jarak maksimal serangan ke musuh")]
    public float attackRange = 2f;

    [Tooltip("Damage yang dihasilkan per serangan")]
    public float attackDamage = 10f;

    [Tooltip("Jeda waktu antar serangan (cooldown dalam detik)")]
    public float attackCooldown = 0.8f;
    private float lastAttackTime = 0f;

    [Header("Interaction / Take Item Settings (Klik Kiri Mouse)")]
    [Tooltip("Jarak maksimal untuk mengambil barang")]
    public float interactRange = 2.5f;

    [Header("Optional Animation")]
    public Animator animator;

    private CharacterController controller;
    private Vector3 moveDirection;
    private float currentSpeed;
    private float verticalVelocity = 0f;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        // Pastikan Animator Controller Kachujin terpasang
        if (animator != null && animator.runtimeAnimatorController == null)
        {
#if UNITY_EDITOR
            RuntimeAnimatorController rac = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Player/Kachujin_AnimatorController.controller");
            if (rac != null) animator.runtimeAnimatorController = rac;
#endif
        }

        // Pastikan tinggi badan Kachujin gagah dan proporsional (skala 1.3x)
        if (transform.localScale.x < 1.1f)
        {
            transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
        }
    }

    private void Update()
    {
        // 0. Jika Layar Kemenangan terbuka, jangan proses input pemain (WASD, Attack, Interact)
        if (BossArenaManager.Instance != null && BossArenaManager.Instance.IsVictoryModalOpen())
        {
            return;
        }

        // 0b. Jika Main Menu terbuka, jangan proses input pemain
        if (MainMenuManager.Instance != null && MainMenuManager.Instance.IsMenuOpen())
        {
            return;
        }

        // 1. Handle Pergerakan (WASD / Panah)
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical   = Input.GetAxisRaw("Vertical");

        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = (transform.right * horizontal + transform.forward * vertical).normalized;
        moveDirection = move * currentSpeed;

        // 2. Gravitasi
        if (controller.isGrounded)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 finalMove = new Vector3(moveDirection.x, verticalVelocity, moveDirection.z);
        controller.Move(finalMove * Time.deltaTime);

        // 3. Tombol Space -> Attack Enemy (Jika Jarak Dekat)
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= lastAttackTime + attackCooldown)
        {
            TryAttackEnemy();
        }

        // 4. Tombol Klik Kiri Mouse (Mouse 0) atau tombol E -> Take / Pick Up / Interact
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.E))
        {
            TryTakeItem();
        }

        // 5. Update Animasi
        if (animator != null)
        {
            bool isMoving = inputDir.magnitude > 0.1f;
            float speedPercent = isMoving ? (isSprinting ? 1f : 0.5f) : 0f;
            
            if (HasParameter("Speed", animator))
                animator.SetFloat("Speed", speedPercent);
            
            if (HasParameter("IsWalking", animator))
                animator.SetBool("IsWalking", isMoving);

            if (HasParameter("IsRunning", animator))
                animator.SetBool("IsRunning", isMoving && isSprinting);
        }
    }

    /// <summary>
    /// Logika Menyerang Musuh (Space) -> Mengurangi HP EnemyFSM
    /// </summary>
    private void TryAttackEnemy()
    {
        lastAttackTime = Time.time;

        // Trigger animasi Attack jika ada di Animator
        if (animator != null && HasParameter("Attack", animator))
        {
            animator.SetTrigger("Attack");
        }

        PlayerStats stats = GetComponent<PlayerStats>();
        float dmg = (stats != null && stats.attackPower > 0f) ? stats.attackPower : attackDamage;

        // Cari musuh di area depan player dalam jangkauan attackRange
        Vector3 attackCenter = transform.position + transform.forward * 1f;
        Collider[] hitEnemies = Physics.OverlapSphere(attackCenter, attackRange);

        var damagedEnemies = new System.Collections.Generic.HashSet<EnemyAI>();
        var damagedFsmEnemies = new System.Collections.Generic.HashSet<EnemyFSM>();
        bool hitAny = false;

        foreach (Collider col in hitEnemies)
        {
            if (col.gameObject == gameObject) continue;

            EnemyAI enemyAI = col.GetComponentInParent<EnemyAI>();
            if (enemyAI != null && !damagedEnemies.Contains(enemyAI))
            {
                damagedEnemies.Add(enemyAI);
                hitAny = true;
                enemyAI.TakeDamage(dmg);
                Debug.Log($"<color=red>[Attack]</color> Menyerang musuh: {enemyAI.name} ({dmg} DMG)");
            }

            EnemyFSM enemyFsm = col.GetComponentInParent<EnemyFSM>();
            if (enemyFsm != null && !enemyFsm.isDead && !damagedFsmEnemies.Contains(enemyFsm))
            {
                damagedFsmEnemies.Add(enemyFsm);
                hitAny = true;
                enemyFsm.TakeDamage(dmg);
                Debug.Log($"<color=red>[Attack]</color> Menyerang {enemyFsm.enemyName} sebesar {dmg} damage!");
            }
        }

        if (!hitAny)
        {
            Debug.Log("<color=yellow>[Attack]</color> Menyerang angin (tidak ada musuh di dekat player).");
        }
    }

    /// <summary>
    /// Logika Mengambil Barang / Kunci / Interaksi Rak (Klik Kiri Mouse / Tombol E)
    /// </summary>
    private void TryTakeItem()
    {
        // 1. Prioritaskan Kunci terdekat (Hall2KeyItem) di sekitar Player
        foreach (Collider nearby in Physics.OverlapSphere(transform.position, interactRange + 0.5f))
        {
            Hall2KeyItem key = nearby.GetComponentInParent<Hall2KeyItem>();
            if (key != null && !key.isCollected && key.gameObject.activeInHierarchy)
            {
                Debug.Log($"<color=cyan>[Take Key]</color> Mengambil kunci: {key.name}");
                if (animator != null && HasParameter("Take", animator)) animator.SetTrigger("Take");
                key.Collect();
                return;
            }

            Hall2Shelf shelf = nearby.GetComponentInParent<Hall2Shelf>();
            if (shelf != null && Hall2ChallengeManager.Instance != null && Hall2ChallengeManager.Instance.keysCarried > 0)
            {
                Debug.Log("<color=green>[Shelf]</color> Meletakkan kunci ke rak.");
                if (animator != null && HasParameter("Take", animator)) animator.SetTrigger("Take");
                Hall2ChallengeManager.Instance.TryDepositKeysAtShelf();
                return;
            }

            Hall3Brazier brazier = nearby.GetComponentInParent<Hall3Brazier>();
            if (brazier != null && !brazier.isLit)
            {
                Debug.Log($"<color=#ff9900>[Brazier]</color> Menyalakan tungku api: {brazier.brazierName}");
                if (animator != null && HasParameter("Take", animator)) animator.SetTrigger("Take");
                brazier.Ignite();
                return;
            }

            VictoryChest chest = nearby.GetComponentInParent<VictoryChest>();
            if (chest != null && chest.isUnlocked && !chest.isOpened)
            {
                Debug.Log("<color=#ffd700>[Victory Chest]</color> Membuka peti kemenangan!");
                if (animator != null && HasParameter("Take", animator)) animator.SetTrigger("Take");
                chest.OpenChest();
                return;
            }
        }

        // 2. Gunakan Raycast dari posisi kamera / mata ke arah depan untuk objek lainnya
        Camera mainCam = Camera.main;
        Ray ray = mainCam != null ? mainCam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f)) 
                                  : new Ray(transform.position + Vector3.up * 1.5f, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange + 1f))
        {
            GameObject obj = hit.collider.gameObject;

            Hall2KeyItem hitKey = obj.GetComponentInParent<Hall2KeyItem>();
            if (hitKey != null && !hitKey.isCollected && hitKey.gameObject.activeInHierarchy)
            {
                Debug.Log($"<color=cyan>[Take Key]</color> Mengambil kunci lewat Raycast: {hitKey.name}");
                if (animator != null && HasParameter("Take", animator)) animator.SetTrigger("Take");
                hitKey.Collect();
                return;
            }

            Hall2Shelf hitShelf = obj.GetComponentInParent<Hall2Shelf>();
            if (hitShelf != null && Hall2ChallengeManager.Instance != null)
            {
                if (Hall2ChallengeManager.Instance.keysCarried > 0)
                {
                    Debug.Log("<color=green>[Shelf]</color> Meletakkan kunci lewat Raycast.");
                    if (animator != null && HasParameter("Take", animator)) animator.SetTrigger("Take");
                    Hall2ChallengeManager.Instance.TryDepositKeysAtShelf();
                    return;
                }
            }

            Hall3Brazier hitBrazier = obj.GetComponentInParent<Hall3Brazier>();
            if (hitBrazier != null && !hitBrazier.isLit)
            {
                Debug.Log($"<color=#ff9900>[Brazier]</color> Menyalakan tungku api lewat Raycast: {hitBrazier.brazierName}");
                if (animator != null && HasParameter("Take", animator)) animator.SetTrigger("Take");
                hitBrazier.Ignite();
                return;
            }

            VictoryChest hitChest = obj.GetComponentInParent<VictoryChest>();
            if (hitChest != null && hitChest.isUnlocked && !hitChest.isOpened)
            {
                Debug.Log("<color=#ffd700>[Victory Chest]</color> Membuka peti kemenangan lewat Raycast!");
                if (animator != null && HasParameter("Take", animator)) animator.SetTrigger("Take");
                hitChest.OpenChest();
                return;
            }

            // Cek apakah objek ber-tag Item/Interactable atau namanya mengandung kata item/chest/collectible
            if (obj.CompareTag("Item") || obj.CompareTag("Interactable") || 
                obj.name.ToLower().Contains("item") || obj.name.ToLower().Contains("chest") || obj.name.ToLower().Contains("coin"))
            {
                Debug.Log($"<color=cyan>[Take Item]</color> Berhasil mengambil item: {obj.name}");

                if (animator != null && HasParameter("Take", animator))
                {
                    animator.SetTrigger("Take");
                }

                Destroy(obj); 
            }
        }
    }

    // Helper untuk mengecek parameter animator
    private bool HasParameter(string paramName, Animator anim)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName) return true;
        }
        return false;
    }

    // Menampilkan jangkauan Attack (Merah) & Take Item (Biru) di Scene View
    private void OnDrawGizmosSelected()
    {
        // Jangkauan Attack (Merah)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * 1f, attackRange);

        // Jangkauan Take Item (Biru)
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 1.5f, transform.forward * interactRange);
    }
}