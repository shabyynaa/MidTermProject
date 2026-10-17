using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// Tool Otomatis Setup Karakter Kachujin & Animasi:
/// 1. Ekstrak Tekstur & Buat Material URP Lit (Skin Asli Kachujin Berwarna Penuh)
/// 2. Hapus Model Lama (Knight / KayKit Knight) dari Scene
/// 3. Bangun Animator Controller (Idle ⇄ Walk ⇄ Run + Attack)
/// 4. Pasang Kachujin ke Player dengan Skala Proporsional (Tinggi ~2.1m, Scale 1.3x)
/// 5. Hubungkan FirstPersonCamera (Bisa POV Mata & Third Person dengan tombol V)
/// 
/// Menu: Tools → Setup Kachujin Character & Animations
/// </summary>
[InitializeOnLoad]
public class KachujinSetupEditor : EditorWindow
{
    const string PLAYER_FOLDER    = "Assets/Art/Player";
    const string TEXTURES_FOLDER  = "Assets/Art/Player/Textures";
    const string MATERIALS_FOLDER = "Assets/Art/Player/Materials";
    const string CONTROLLER_PATH  = "Assets/Art/Player/Kachujin_AnimatorController.controller";
    const string FBX_MODEL_PATH   = "Assets/Art/Player/Kachujin G Rosales.fbx";

    static KachujinSetupEditor()
    {
        EditorApplication.delayCall += AutoCheckAndSetup;
    }

    static void AutoCheckAndSetup()
    {
        EnsureDirectories();
        ExtractEmbeddedTextures();
        ConfigureTextureTypes();

        Material matBody = CreateOrUpdateMaterial("kachujin_MAT", "Kachujin_diffuse_body.png", "Kachujin_normal.png");
        Material matHead = CreateOrUpdateMaterial("kachujin_MAT_", "Kachujin_diffuse.png", "Kachujin_normal.png");
        RemapFBXMaterials(matBody, matHead);

        // Hapus sisa-sisa objek knight jika masih ada di scene
        foreach (GameObject rootGo in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (rootGo == null) continue;
            string lName = rootGo.name.ToLower();
            if (lName == "knight" || lName.Contains("character_knight") || lName.Contains("kaykit_character"))
            {
                Debug.Log($"<color=orange>[AutoPurge]</color> Menghapus karakter lama: {rootGo.name}");
                DestroyImmediate(rootGo);
            }
        }
    }

    [MenuItem("Tools/Setup Kachujin Character & Animations")]
    public static void SetupKachujin()
    {
        Debug.Log("<color=cyan>[KachujinSetup]</color> Memulai setup lengkap karakter Kachujin...");

        // 1. Siapkan folder Textures & Materials
        EnsureDirectories();

        // 2. Ekstrak PNG tekstur dari FBX jika belum ada
        ExtractEmbeddedTextures();

        // 3. Konfigurasi normal map texture import
        ConfigureTextureTypes();

        // 4. Buat / Update URP Lit Materials (kachujin_MAT & kachujin_MAT_)
        Material matBody = CreateOrUpdateMaterial("kachujin_MAT", "Kachujin_diffuse_body.png", "Kachujin_normal.png");
        Material matHead = CreateOrUpdateMaterial("kachujin_MAT_", "Kachujin_diffuse.png", "Kachujin_normal.png");

        // 5. Hubungkan material ke ModelImporter FBX
        RemapFBXMaterials(matBody, matHead);

        // 6. Set Loop Time animasi pada FBX
        ConfigureAnimationLoop("Kachujin G Rosales@Idle.fbx");
        ConfigureAnimationLoop("Kachujin G Rosales@Walking.fbx");
        ConfigureAnimationLoop("Kachujin G Rosales@Running.fbx");
        ConfigureAnimationLoop("Kachujin G Rosales@Jogging.fbx");

        // 7. Bangun Animator Controller
        AnimatorController controller = CreateOrGetAnimatorController();

        // 8. Hapus karakter lama (Knight) dan pasang Kachujin di Scene
        ApplyKachujinToPlayer(controller, matBody, matHead);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Setup Kachujin Selesai!",
            "Karakter Kachujin & Animasi Berhasil Dikonfigurasi!\n\n" +
            "✔ Karakter lama (Knight) sudah dihapus bersih dari scene.\n" +
            "✔ Skin asli Kachujin sudah berwarna (URP Lit Material Body & Head).\n" +
            "✔ Tinggi karakter disesuaikan gagah & proporsional (Scale 1.3x, Tinggi ~2.1m).\n" +
            "✔ Animasi Idle, Walk, Run (Shift), dan Attack (Space) sudah terhubung.\n" +
            "✔ Kamera mendukung Third Person (bisa lihat badan karakter) dan First Person (tekan tombol V / Scroll Mouse).\n\n" +
            "Silakan cek di Scene View & klik Play!", "Mantap!");
    }

    static void EnsureDirectories()
    {
        if (!Directory.Exists(TEXTURES_FOLDER))
        {
            Directory.CreateDirectory(TEXTURES_FOLDER);
        }
        if (!Directory.Exists(MATERIALS_FOLDER))
        {
            Directory.CreateDirectory(MATERIALS_FOLDER);
        }
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Ekstrak 4 file PNG tertanam dari file FBX langsung ke Assets/Art/Player/Textures
    /// </summary>
    static void ExtractEmbeddedTextures()
    {
        string[] targetNames = new string[]
        {
            "Kachujin_diffuse_body.png",
            "Kachujin_diffuse.png",
            "Kachujin_specular.png",
            "Kachujin_normal.png"
        };

        bool allExist = true;
        foreach (var name in targetNames)
        {
            if (!File.Exists(Path.Combine(TEXTURES_FOLDER, name)))
            {
                allExist = false;
                break;
            }
        }

        if (allExist) return;

        if (!File.Exists(FBX_MODEL_PATH))
        {
            Debug.LogWarning($"[KachujinSetup] File {FBX_MODEL_PATH} tidak ditemukan!");
            return;
        }

        byte[] fbxData = File.ReadAllBytes(FBX_MODEL_PATH);
        byte[] pngHeader = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        byte[] iendBytes = new byte[] { 0x49, 0x45, 0x4E, 0x44 }; // IEND

        int searchIdx = 0;
        int extractedCount = 0;

        while (extractedCount < targetNames.Length)
        {
            int start = FindByteSequence(fbxData, pngHeader, searchIdx);
            if (start == -1) break;

            int iendPos = FindByteSequence(fbxData, iendBytes, start);
            if (iendPos == -1) break;

            int end = iendPos + 8; // 4 bytes IEND + 4 bytes CRC
            int length = end - start;

            byte[] pngData = new byte[length];
            System.Array.Copy(fbxData, start, pngData, 0, length);

            string outPath = Path.Combine(TEXTURES_FOLDER, targetNames[extractedCount]);
            File.WriteAllBytes(outPath, pngData);
            Debug.Log($"[KachujinSetup] Berhasil mengekstrak {targetNames[extractedCount]} ({length} bytes)");

            searchIdx = end;
            extractedCount++;
        }

        AssetDatabase.Refresh();
    }

    static int FindByteSequence(byte[] source, byte[] pattern, int startIndex)
    {
        for (int i = startIndex; i <= source.Length - pattern.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }
            if (match) return i;
        }
        return -1;
    }

    static void ConfigureTextureTypes()
    {
        string normalPath = $"{TEXTURES_FOLDER}/Kachujin_normal.png";
        TextureImporter normalImporter = AssetImporter.GetAtPath(normalPath) as TextureImporter;
        if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
    }

    static Material CreateOrUpdateMaterial(string matName, string diffuseTexName, string normalTexName)
    {
        string matPath = $"{MATERIALS_FOLDER}/{matName}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);

        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        if (mat == null)
        {
            mat = new Material(urpShader);
            mat.name = matName;
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = urpShader;
        }

        // Texture Albedo
        string diffusePath = $"{TEXTURES_FOLDER}/{diffuseTexName}";
        Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(diffusePath);
        if (albedo != null)
        {
            mat.SetTexture("_BaseMap", albedo);
            mat.SetTexture("_MainTex", albedo);
            mat.SetColor("_BaseColor", Color.white);
            mat.color = Color.white;
        }

        // Texture Normal
        if (!string.IsNullOrEmpty(normalTexName))
        {
            string normalPath = $"{TEXTURES_FOLDER}/{normalTexName}";
            Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.EnableKeyword("_NORMALMAP");
            }
        }

        mat.SetFloat("_Smoothness", 0.35f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void RemapFBXMaterials(Material matBody, Material matHead)
    {
        ModelImporter importer = AssetImporter.GetAtPath(FBX_MODEL_PATH) as ModelImporter;
        if (importer == null) return;

        importer.materialLocation = ModelImporterMaterialLocation.External;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

        if (matBody != null)
        {
            var idBody = new AssetImporter.SourceAssetIdentifier(typeof(Material), "kachujin_MAT");
            importer.AddRemap(idBody, matBody);
        }

        if (matHead != null)
        {
            var idHead = new AssetImporter.SourceAssetIdentifier(typeof(Material), "kachujin_MAT_");
            importer.AddRemap(idHead, matHead);
        }

        importer.SaveAndReimport();
    }

    static void ConfigureAnimationLoop(string fileName)
    {
        string path = $"{PLAYER_FOLDER}/{fileName}";
        ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
        if (importer == null) return;

        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        if (clips == null || clips.Length == 0) return;

        bool changed = false;
        foreach (var clip in clips)
        {
            if (!clip.loopTime)
            {
                clip.loopTime = true;
                changed = true;
            }
        }

        if (changed)
        {
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }
    }

    static AnimatorController CreateOrGetAnimatorController()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
        }

        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsWalking", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "IsRunning", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);

        AnimationClip idleClip   = LoadClipFromFBX("Kachujin G Rosales@Idle.fbx");
        AnimationClip walkClip   = LoadClipFromFBX("Kachujin G Rosales@Walking.fbx");
        AnimationClip runClip    = LoadClipFromFBX("Kachujin G Rosales@Running.fbx");
        AnimationClip attackClip = LoadClipFromFBX("Kachujin G Rosales@Standing Melee Attack Horizontal.fbx");

        var rootStateMachine = controller.layers[0].stateMachine;

        ChildAnimatorState[] existingStates = rootStateMachine.states;
        AnimatorState idleState = null;
        AnimatorState walkState = null;
        AnimatorState runState = null;
        AnimatorState attackState = null;

        foreach (var s in existingStates)
        {
            if (s.state.name == "Idle") idleState = s.state;
            else if (s.state.name == "Walk") walkState = s.state;
            else if (s.state.name == "Run") runState = s.state;
            else if (s.state.name == "Attack") attackState = s.state;
        }

        if (idleState == null) idleState = rootStateMachine.AddState("Idle");
        if (walkState == null) walkState = rootStateMachine.AddState("Walk");
        if (runState == null)  runState  = rootStateMachine.AddState("Run");
        if (attackState == null) attackState = rootStateMachine.AddState("Attack");

        if (idleClip != null) idleState.motion = idleClip;
        if (walkClip != null) walkState.motion = walkClip;
        if (runClip != null)  runState.motion  = runClip;
        if (attackClip != null) attackState.motion = attackClip;

        rootStateMachine.defaultState = idleState;

        idleState.transitions = new AnimatorStateTransition[0];
        walkState.transitions = new AnimatorStateTransition[0];
        runState.transitions  = new AnimatorStateTransition[0];
        attackState.transitions = new AnimatorStateTransition[0];

        // Idle ⇄ Walk
        var t_IdleToWalk = idleState.AddTransition(walkState);
        t_IdleToWalk.hasExitTime = false;
        t_IdleToWalk.duration = 0.15f;
        t_IdleToWalk.AddCondition(AnimatorConditionMode.If, 0, "IsWalking");

        var t_WalkToIdle = walkState.AddTransition(idleState);
        t_WalkToIdle.hasExitTime = false;
        t_WalkToIdle.duration = 0.15f;
        t_WalkToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");

        // Walk ⇄ Run
        var t_WalkToRun = walkState.AddTransition(runState);
        t_WalkToRun.hasExitTime = false;
        t_WalkToRun.duration = 0.15f;
        t_WalkToRun.AddCondition(AnimatorConditionMode.If, 0, "IsRunning");

        var t_RunToWalk = runState.AddTransition(walkState);
        t_RunToWalk.hasExitTime = false;
        t_RunToWalk.duration = 0.15f;
        t_RunToWalk.AddCondition(AnimatorConditionMode.IfNot, 0, "IsRunning");

        // Run -> Idle
        var t_RunToIdle = runState.AddTransition(idleState);
        t_RunToIdle.hasExitTime = false;
        t_RunToIdle.duration = 0.15f;
        t_RunToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsWalking");

        // AnyState -> Attack
        var t_AnyToAttack = rootStateMachine.AddAnyStateTransition(attackState);
        t_AnyToAttack.hasExitTime = false;
        t_AnyToAttack.duration = 0.1f;
        t_AnyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");

        // Attack -> Idle
        var t_AttackToIdle = attackState.AddTransition(idleState);
        t_AttackToIdle.hasExitTime = true;
        t_AttackToIdle.exitTime = 0.85f;
        t_AttackToIdle.duration = 0.2f;

        EditorUtility.SetDirty(controller);
        return controller;
    }

    static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == paramName) return;
        }
        controller.AddParameter(paramName, type);
    }

    static AnimationClip LoadClipFromFBX(string fileName)
    {
        string path = $"{PLAYER_FOLDER}/{fileName}";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var a in assets)
        {
            if (a is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                return clip;
            }
        }
        return null;
    }

    static void ApplyKachujinToPlayer(AnimatorController controller, Material matBody, Material matHead)
    {
        // 1. Cari titik spawn
        Vector3 spawnPos = new Vector3(0f, 0.08f, -34f);
        GameObject spawnObj = GameObject.Find("PlayerSpawn");
        if (spawnObj != null)
        {
            spawnPos = spawnObj.transform.position;
        }

        // 2. HAPUS TOTAL objek ksatria lama (Knight) di scene
        foreach (GameObject rootGo in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            string lName = rootGo.name.ToLower();
            bool isOldKnight = lName == "knight" || lName.Contains("character_knight") || lName.Contains("kaykit_character");

            // Cek apakah prefab source-nya adalah Knight
            GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(rootGo);
            if (prefabSource != null && prefabSource.name.ToLower().Contains("knight"))
            {
                isOldKnight = true;
            }

            if (isOldKnight)
            {
                Debug.Log($"<color=orange>[KachujinSetup]</color> Menghapus karakter lama: {rootGo.name}");
                DestroyImmediate(rootGo);
            }
        }

        // 3. Ambil atau Buat GameObject Player yang bersih
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        // Jika player yang ada masih mengandung mesh selain Kachujin, bersihkan
        if (player != null)
        {
            for (int i = player.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = player.transform.GetChild(i);
                if (!child.name.ToLower().Contains("camera"))
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        else
        {
            player = new GameObject("Player");
            player.transform.position = spawnPos;
        }

        player.name = "Player";
        player.tag = "Player";

        // 4. Komponen PlayerController & CharacterController
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null) pc = player.AddComponent<PlayerController>();
        pc.walkSpeed = 5f;
        pc.sprintSpeed = 8.5f;
        pc.rotationSpeed = 12f;
        pc.gravity = 20f;
        pc.attackDamage = 25f;

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 2.1f;
        cc.radius = 0.45f;
        cc.center = new Vector3(0f, 1.05f, 0f);

        PlayerStats stats = player.GetComponent<PlayerStats>();
        if (stats == null) stats = player.AddComponent<PlayerStats>();
        stats.maxHP = 100f;
        stats.currentHP = 100f;
        stats.attackPower = 10f;
        stats.FindUIReferences();
        stats.UpdateUI();

        // 5. Instansiasi Model Visual Kachujin
        GameObject kachujinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FBX_MODEL_PATH);
        if (kachujinPrefab != null)
        {
            GameObject kachujinVisual = (GameObject)PrefabUtility.InstantiatePrefab(kachujinPrefab);
            kachujinVisual.name = "Visual_Kachujin";
            kachujinVisual.transform.SetParent(player.transform, false);
            kachujinVisual.transform.localPosition = Vector3.zero;
            kachujinVisual.transform.localRotation = Quaternion.identity;

            // Skala 1.3x agar tinggi badan gagah & proporsional (~2.1 meter)
            kachujinVisual.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

            // Pasang Material URP Lit Berwarna ke seluruh SkinnedMeshRenderer
            Renderer[] renderers = kachujinVisual.GetComponentsInChildren<Renderer>(true);
            foreach (var rend in renderers)
            {
                if (matBody != null && matHead != null)
                {
                    rend.sharedMaterials = new Material[] { matBody, matHead };
                }
                rend.enabled = true;
            }

            // Pasang Animator
            Animator anim = kachujinVisual.GetComponent<Animator>();
            if (anim == null) anim = kachujinVisual.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            pc.animator = anim;

            Debug.Log("<color=green>[KachujinSetup]</color> Kachujin berskin URP Lit & Scale 1.3x berhasil dipasang!");
        }

        // 6. Hubungkan Main Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            FirstPersonCamera fpsCam = mainCam.GetComponent<FirstPersonCamera>();
            if (fpsCam == null) fpsCam = mainCam.gameObject.AddComponent<FirstPersonCamera>();
            fpsCam.playerBody = player.transform;
            fpsCam.eyeHeight = 1.85f;
            fpsCam.distance = 3.2f; // Default Third Person agar animasi terlihat gagah
            fpsCam.shoulderOffset = 0.35f;
        }

        Selection.activeGameObject = player;

        // Tandai scene telah berubah dan simpan otomatis
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
    }
}
