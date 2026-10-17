using UnityEngine;
using UnityEditor;

/// <summary>
/// Generator MEGA LABYRINTH DUNGEON (8 Ruangan Tinggi 3 Tingkat + Atap Lengkap)
/// Tembok Tinggi 6.15m + Langit-langit Batu Solid + 7 Pasang Dual Math Gates + First Person POV
/// Menu: Tools → Build 8-Room Mega Labyrinth (Tall Walls + Ceiling)
/// </summary>
public class BuildDungeonArenaKayKit : EditorWindow
{
    const string KAYKIT_PATH = "Assets/Art/Kaykit_Dungeon/fbx(unity)";
    const float WALL_H = 2.05f;
    const float CEILING_Y = 6.15f; // Tinggi 3 susun wall

    [MenuItem("Tools/Build 8-Room Mega Labyrinth (Tall Walls + Ceiling)")]
    public static void BuildArena()
    {
        GameObject existing = GameObject.Find("DungeonArena");
        if (existing != null)
        {
            bool ok = EditorUtility.DisplayDialog("Bangun Labirin Megah Bertingkat",
                "Arena lama akan diganti dengan MEGA LABYRINTH TEMBOK TINGGI (3 Tingkat / 6m) + ATAP LENGKAP + 7 PASANGAN DUAL MATH GATES. Lanjutkan?",
                "Gas Bangun!", "Batal");
            if (!ok) return;
            DestroyImmediate(existing);
        }

        GameObject arena = new GameObject("DungeonArena");
        Undo.RegisterCreatedObjectUndo(arena, "Build 8-Room Tall Labyrinth with Ceiling");

        BuildGround(arena);
        BuildWalls(arena);
        BuildCeiling(arena);
        BuildObstacles(arena);
        BuildAIPoints(arena);
        BuildDecorations(arena);
        BuildDualMathGates(arena);

        // Pasang Collider ke Tembok, Props, dan SETIAP UBIN LANTAI (Anti Jatuh!)
        AddSolidFloorAndWallColliders(arena);

        // Otomatis Pasang Setup Kamera POV First Person
        SetupFirstPersonCamera();

        Selection.activeGameObject = arena;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log("<color=#00ff88>[MegaLabyrinth] ✅ Tembok Tinggi 3 Tingkat + Atap Lengkap berhasil dibuat!</color>");
        EditorUtility.DisplayDialog("🏰 Selesai!",
            "Mega Labyrinth Tembok Tinggi & Beratap Berhasil Dibuat:\n\n" +
            "• Tembok dinaikkan jadi 3 tingkat (Tinggi 6.15m) sehingga langit-langit sangat megah!\n" +
            "• Seluruh ruangan memiliki Atap / Langit-langit batu solid (Suasana dungeon sangat imersif).\n" +
            "• Semua ubin lantai memiliki BoxCollider tebal (Anti-jatuh).\n" +
            "• Kamera First Person (POV) siap dimainkan.\n" +
            "• 7 Pasang Dual Math Gates lengkap.\n\n" +
            "Langkah Terakhir:\n" +
            "Pilih 'DungeonArena' di Hierarchy lalu klik 'Bake' di NavMeshSurface!",
            "Keren Banget!");
    }

    [MenuItem("Tools/Toggle Ceiling (Sembunyikan / Munculkan Atap)")]
    public static void ToggleCeiling()
    {
        GameObject arena = GameObject.Find("DungeonArena");
        if (arena == null) return;

        Transform ceiling = arena.transform.Find("Ceiling");
        if (ceiling != null)
        {
            bool active = !ceiling.gameObject.activeSelf;
            ceiling.gameObject.SetActive(active);
            string status = active ? "DIMUNCULKAN" : "DISEMBUNYIKAN (Mudah diedit dari atas)";
            EditorUtility.DisplayDialog("Toggle Atap", $"Atap sekarang {status}!", "OK");
        }
    }

    [MenuItem("Tools/Setup First Person Camera (POV Mata Player)")]
    public static void SetupFirstPersonCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = GameObject.Find("Main Camera");
            if (camObj != null) mainCam = camObj.GetComponent<Camera>();
        }

        if (mainCam != null)
        {
            CameraFollow oldFollow = mainCam.GetComponent<CameraFollow>();
            if (oldFollow != null) DestroyImmediate(oldFollow);

            FirstPersonCamera fpsCam = mainCam.GetComponent<FirstPersonCamera>();
            if (fpsCam == null) fpsCam = mainCam.gameObject.AddComponent<FirstPersonCamera>();

            mainCam.nearClipPlane = 0.01f;
            fpsCam.forwardOffset = 0.55f;
            fpsCam.eyeHeight = 1.75f;

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null)
            {
                fpsCam.playerBody = player.transform;
            }

            Debug.Log("<color=cyan>[Camera]</color> Main Camera berhasil diset menjadi First Person POV!");
        }
    }

    [MenuItem("Tools/Fix Enemy Setup (Pastikan Seluruh Badan Enemy Bergerak)")]
    public static void FixEnemySetup()
    {
        EnsureTagExists("Enemy");

        GameObject enemy = Selection.activeGameObject;
        if (enemy == null || (!enemy.name.ToLower().Contains("enemy") && !enemy.name.ToLower().Contains("skeleton")))
        {
            enemy = GameObject.Find("Enemy");
            if (enemy == null) enemy = GameObject.Find("Skeleton_Warrior");
            if (enemy == null) enemy = GameObject.Find("Skeleton_Minion");
            if (enemy == null) enemy = GameObject.Find("Skeleton_Rogue");
            if (enemy == null) enemy = GameObject.Find("Skeleton_Mage");
            
            if (enemy == null)
            {
                foreach (GameObject go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
                {
                    if (go.name.ToLower().Contains("enemy") || go.name.ToLower().Contains("skeleton"))
                    {
                        enemy = go.transform.root.gameObject;
                        break;
                    }
                }
            }
        }

        if (enemy != null)
        {
            enemy.tag = "Enemy";

            foreach (Transform child in enemy.GetComponentsInChildren<Transform>())
            {
                if (child.gameObject != enemy)
                {
                    UnityEngine.AI.NavMeshAgent childAgent = child.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (childAgent != null) DestroyImmediate(childAgent);

                    EnemyAI childAI = child.GetComponent<EnemyAI>();
                    if (childAI != null) DestroyImmediate(childAI);
                }
            }

            UnityEngine.AI.NavMeshAgent agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent == null) agent = enemy.AddComponent<UnityEngine.AI.NavMeshAgent>();

            agent.updateRotation = true;
            agent.updatePosition = true;
            agent.speed = 4f;
            agent.angularSpeed = 240f;
            agent.radius = 0.5f;
            agent.height = 1.8f;

            EnemyAI ai = enemy.GetComponent<EnemyAI>();
            if (ai == null) ai = enemy.AddComponent<EnemyAI>();
            ai.FindSceneReferences();

            Selection.activeGameObject = enemy;

            EditorUtility.DisplayDialog("Sukses!", 
                $"Enemy '{enemy.name}' berhasil diperbaiki!\nNavMeshAgent & EnemyAI sudah terpasang di ROOT badan utama.", "Keren!");
        }
        else
        {
            EditorUtility.DisplayDialog("Pilih Enemy", 
                "Tidak menemukan objek Enemy otomatis. Silakan KLIK GameObject Enemy kamu di Hierarchy, lalu jalankan menu ini lagi!", "OK");
        }
    }

    [MenuItem("Tools/Optimize Scene Lights (Hilangkan Warning & Bikin Game Enteng)")]
    public static void OptimizeSceneLights()
    {
        int count = 0;
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (l.type == LightType.Point)
            {
                l.shadows = LightShadows.None;
                count++;
            }
        }

        EditorUtility.DisplayDialog("Sukses!", 
            $"{count} Lampu obor berhasil dioptimalkan!\nShadow dinonaktifkan sehingga FPS game kamu jauh lebih enteng.", "Mantap!");
    }

    static void EnsureTagExists(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tagName)) { found = true; break; }
        }

        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty n = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            n.stringValue = tagName;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"<color=green>[TagManager]</color> Tag '{tagName}' otomatis didaftarkan ke Unity!");
        }
    }

    // ============================================================
    // 1. GROUND (Lantai Solid)
    // ============================================================
    static void BuildGround(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Ground");

        PlaceTileGrid(root, "Floor_Courtyard", "floor_tile_large", -10f, 10f, 2f, -40f, -26f, 2f);
        PlaceTileGrid(root, "Floor_Atrium", "floor_tile_large", -14f, 14f, 2f, -26f, -12f, 2f);
        PlaceTileGrid(root, "Floor_Storage", "floor_tile_large", -34f, -14f, 2f, -22f, -6f, 2f);
        PlaceTileGrid(root, "Floor_Armory", "floor_tile_large", 14f, 34f, 2f, -22f, -6f, 2f);
        PlaceTileGrid(root, "Floor_Alchemy", "floor_tile_large", -34f, -14f, 2f, -6f, 10f, 2f);
        PlaceTileGrid(root, "Floor_Crypt", "floor_tile_large", 14f, 34f, 2f, -6f, 10f, 2f);
        PlaceTileGrid(root, "Floor_GrandArena", "floor_tile_large", -14f, 14f, 2f, -12f, 16f, 2f);
        PlaceTileGrid(root, "Floor_Sanctum", "floor_tile_large", -12f, 12f, 2f, 16f, 30f, 2f);
    }

    // ============================================================
    // 2. CEILING (Atap / Langit-langit Batu Solid di Ketinggian 6.15m)
    // ============================================================
    static void BuildCeiling(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Ceiling");

        System.Action<string, float, float, float, float, float> PlaceCeilingGrid = (prefix, x1, x2, stepX, z1, z2) => {
            int r = 0;
            for (float z = z1; z < z2 - 0.01f; z += 2f)
            {
                int c = 0;
                for (float x = x1; x < x2 - 0.01f; x += stepX)
                {
                    GameObject tile = PlaceModel(root, $"{prefix}_c{c}_r{r}", "floor_tile_large",
                        new Vector3(x + stepX * 0.5f, CEILING_Y, z + 1f), 0f);
                    
                    // Putar 180 derajat di sumbu X agar sisi batu menghadap ke bawah (ke dalam dungeon)
                    tile.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                    c++;
                }
                r++;
            }
        };

        PlaceCeilingGrid("Ceil_Courtyard", -10f, 10f, 2f, -40f, -26f);
        PlaceCeilingGrid("Ceil_Atrium", -14f, 14f, 2f, -26f, -12f);
        PlaceCeilingGrid("Ceil_Storage", -34f, -14f, 2f, -22f, -6f);
        PlaceCeilingGrid("Ceil_Armory", 14f, 34f, 2f, -22f, -6f);
        PlaceCeilingGrid("Ceil_Alchemy", -34f, -14f, 2f, -6f, 10f);
        PlaceCeilingGrid("Ceil_Crypt", 14f, 34f, 2f, -6f, 10f);
        PlaceCeilingGrid("Ceil_GrandArena", -14f, 14f, 2f, -12f, 16f);
        PlaceCeilingGrid("Ceil_Sanctum", -12f, 12f, 2f, 16f, 30f);
    }

    // ============================================================
    // 3. WALLS (Tembok 3 Tingkat / Triple Height ~6.15 Meter)
    // ============================================================
    static void BuildWalls(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Walls");

        // Helper Membangun Tembok 3 Tingkat
        System.Action<string, float, float, float, float> BuildWallX = (name, z, x1, x2, rot) => {
            PlaceWallRowX(root, name + "_1", "wall", z, 0f, x1, x2, 2f, rot);
            PlaceWallRowX(root, name + "_2", "wall", z, WALL_H, x1, x2, 2f, rot);
            PlaceWallRowX(root, name + "_3", "wall", z, WALL_H * 2f, x1, x2, 2f, rot);
        };
        System.Action<string, float, float, float, float> BuildWallZ = (name, x, z1, z2, rot) => {
            PlaceWallRowZ(root, name + "_1", "wall", x, 0f, z1, z2, 2f, rot);
            PlaceWallRowZ(root, name + "_2", "wall", x, WALL_H, z1, z2, 2f, rot);
            PlaceWallRowZ(root, name + "_3", "wall", x, WALL_H * 2f, z1, z2, 2f, rot);
        };
        System.Action<string, Vector3, float> BuildCorner = (name, pos, rot) => {
            PlaceWallSingle(root, name + "_1", "wall_corner", pos, rot);
            PlaceWallSingle(root, name + "_2", "wall_corner", pos + Vector3.up * WALL_H, rot);
            PlaceWallSingle(root, name + "_3", "wall_corner", pos + Vector3.up * (WALL_H * 2f), rot);
        };

        // --- 1. COURTYARD (SELATAN) ---
        BuildWallX("Wall_Court_Bot", -40f, -10f, 10f, 0f);
        BuildCorner("Corner_Court_SW", new Vector3(-10f, 0f, -40f), 90f);
        BuildCorner("Corner_Court_SE", new Vector3(10f, 0f, -40f), 0f);
        BuildWallZ("Wall_Court_W", -10f, -40f, -26f, 90f);
        BuildWallZ("Wall_Court_E", 10f, -40f, -26f, 270f);

        // Atrium Outer Corners
        BuildWallX("Wall_Atrium_Bot_L", -26f, -14f, -10f, 0f);
        BuildWallX("Wall_Atrium_Bot_R", -26f, 10f, 14f, 0f);
        BuildCorner("Corner_Atrium_SW", new Vector3(-14f, 0f, -26f), 90f);
        BuildCorner("Corner_Atrium_SE", new Vector3(14f, 0f, -26f), 0f);

        // --- 2. SAYAP BARAT (STORAGE & ALCHEMY) ---
        BuildWallZ("Wall_West_Outer", -34f, -22f, 10f, 90f);
        BuildCorner("Corner_Storage_SW", new Vector3(-34f, 0f, -22f), 90f);
        BuildCorner("Corner_Alchemy_NW", new Vector3(-34f, 0f, 10f), 180f);
        BuildWallX("Wall_Storage_Bot", -22f, -34f, -14f, 0f);
        BuildWallX("Wall_Alchemy_Top", 10f, -34f, -14f, 180f);

        BuildWallX("Divider_West_L", -6f, -34f, -26f, 180f);
        BuildWallX("Divider_West_R", -6f, -22f, -14f, 180f);

        // --- 3. SAYAP TIMUR (ARMORY & CRYPT) ---
        BuildWallZ("Wall_East_Outer", 34f, -22f, 10f, 270f);
        BuildCorner("Corner_Armory_SE", new Vector3(34f, 0f, -22f), 0f);
        BuildCorner("Corner_Crypt_NE", new Vector3(34f, 0f, 10f), 270f);
        BuildWallX("Wall_Armory_Bot", -22f, 14f, 34f, 0f);
        BuildWallX("Wall_Crypt_Top", 10f, 14f, 34f, 180f);

        BuildWallX("Divider_East_L", -6f, 14f, 22f, 180f);
        BuildWallX("Divider_East_R", -6f, 26f, 34f, 180f);

        // --- 4. DINDING PEMISAH TENGAH ---
        BuildWallZ("Divider_W_1", -14f, -26f, -20f, 90f);
        BuildWallZ("Divider_W_2", -14f, -12f, -2f, 90f);
        BuildWallZ("Divider_W_3", -14f, 4f, 10f, 90f);

        BuildWallZ("Divider_E_1", 14f, -26f, -20f, 270f);
        BuildWallZ("Divider_E_2", 14f, -12f, -2f, 270f);
        BuildWallZ("Divider_E_3", 14f, 4f, 10f, 270f);

        BuildWallX("Divider_Atrium_Arena_L", -12f, -14f, -3f, 180f);
        BuildWallX("Divider_Atrium_Arena_R", -12f, 3f, 14f, 180f);

        // --- 5. CENTRAL GRAND ARENA KE NORTH SANCTUM ---
        BuildWallX("Wall_Arena_North_L", 16f, -14f, -4f, 180f);
        BuildWallX("Wall_Arena_North_R", 16f, 4f, 14f, 180f);
        BuildCorner("Corner_Arena_NW", new Vector3(-14f, 0f, 16f), 180f);
        BuildCorner("Corner_Arena_NE", new Vector3(14f, 0f, 16f), 270f);

        // --- 6. NORTH BOSS SANCTUM ---
        BuildWallX("Wall_Sanctum_Top", 30f, -12f, 12f, 180f);
        BuildCorner("Corner_Sanctum_NW", new Vector3(-12f, 0f, 30f), 180f);
        BuildCorner("Corner_Sanctum_NE", new Vector3(12f, 0f, 30f), 270f);
        BuildWallZ("Wall_Sanctum_W", -12f, 16f, 30f, 90f);
        BuildWallZ("Wall_Sanctum_E", 12f, 16f, 30f, 270f);
    }

    // ============================================================
    // 4. OBSTACLES (Pilar & Props)
    // ============================================================
    static void BuildObstacles(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Obstacles");

        // Grand Arena: 4 Pilar Berjarak Lapang (Tinggi 3 tingkat)
        GameObject arenaHall = CreateChild(root, "GrandArena_Pillars");
        PlaceTallPillar(arenaHall, "Pillar_NW", new Vector3(-7f, 0f, 9f));
        PlaceTallPillar(arenaHall, "Pillar_NE", new Vector3(7f, 0f, 9f));
        PlaceTallPillar(arenaHall, "Pillar_SW", new Vector3(-7f, 0f, -2f));
        PlaceTallPillar(arenaHall, "Pillar_SE", new Vector3(7f, 0f, -2f));

        GameObject storageProps = CreateChild(root, "Storage_Props");
        PlaceModel(storageProps, "Crates_1", "crates_stacked", new Vector3(-26f, 0f, -15f));
        PlaceModel(storageProps, "Barrel_1", "barrel_large", new Vector3(-23f, 0f, -17f));

        GameObject alchemyProps = CreateChild(root, "Alchemy_Props");
        PlaceModel(alchemyProps, "Table_Alch", "table_long_decorated_A", new Vector3(-24f, 0f, 2f), 90f);
        PlaceModel(alchemyProps, "Trunk_Alch", "trunk_medium_A", new Vector3(-28f, 0f, 5f), 45f);

        GameObject armoryProps = CreateChild(root, "Armory_Props");
        PlaceModel(armoryProps, "Table_Arm", "table_medium_decorated_A", new Vector3(24f, 0f, -15f));
        PlaceModel(armoryProps, "Chest_Arm", "trunk_large_A", new Vector3(28f, 0f, -17f), 45f);

        GameObject cryptProps = CreateChild(root, "Crypt_Props");
        PlaceModel(cryptProps, "Barrier_Crypt_1", "barrier", new Vector3(22f, 0f, 2f), 0f);
        PlaceModel(cryptProps, "Barrier_Crypt_2", "barrier", new Vector3(24f, 0f, 2f), 0f);

        GameObject sanctumProps = CreateChild(root, "Sanctum_Props");
        PlaceModel(sanctumProps, "Throne_Barrier_L", "barrier", new Vector3(-4f, 0f, 25f), 90f);
        PlaceModel(sanctumProps, "Throne_Barrier_R", "barrier", new Vector3(4f, 0f, 25f), 90f);
        PlaceModel(sanctumProps, "Gold_Chest", "chest_gold", new Vector3(0f, 0f, 27f), 0f);
    }

    static void PlaceTallPillar(GameObject parent, string name, Vector3 pos)
    {
        PlaceModel(parent, name + "_1", "pillar_decorated", pos);
        PlaceModel(parent, name + "_2", "pillar", pos + Vector3.up * WALL_H);
        PlaceModel(parent, name + "_3", "pillar", pos + Vector3.up * (WALL_H * 2f));
    }

    // ============================================================
    // 5. AI POINTS
    // ============================================================
    static void BuildAIPoints(GameObject arena)
    {
        GameObject root = CreateChild(arena, "AI_Points");

        CreatePoint(root, "PlayerSpawn",    new Vector3(0f, 0f, -34f));
        CreatePoint(root, "EnemySpawn",     new Vector3(0f, 0f, 23f));

        CreatePoint(root, "PatrolPoint_W1", new Vector3(-24f, 0f, -14f));
        CreatePoint(root, "PatrolPoint_W2", new Vector3(0f, 0f, 4f));
        CreatePoint(root, "PatrolPoint_W3", new Vector3(24f, 0f, -14f));
        CreatePoint(root, "PatrolPoint_W4", new Vector3(0f, 0f, -20f));

        CreatePoint(root, "SafePoint",      new Vector3(0f, 0f, 27f));
    }

    // ============================================================
    // 6. DEKORASI & OBOR (POINT LIGHT)
    // ============================================================
    static void BuildDecorations(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Decorations");

        System.Action<string, Vector3, float> PlaceTorchWithLight = (tName, pos, rotY) => {
            GameObject torch = PlaceModel(root, tName, "torch_mounted", pos, rotY);
            
            GameObject lightObj = new GameObject(tName + "_Light");
            lightObj.transform.SetParent(torch.transform);
            lightObj.transform.localPosition = new Vector3(0f, 0.4f, 0.3f);

            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.65f, 0.25f);
            pLight.range = 10f;
            pLight.intensity = 3.0f;
            pLight.shadows = LightShadows.None; // Optimal tanpa warning
        };

        // Obor di sekeliling dungeon
        PlaceTorchWithLight("Torch_Court_L", new Vector3(-9.8f, 1.8f, -32f), 90f);
        PlaceTorchWithLight("Torch_Court_R", new Vector3(9.8f, 1.8f, -32f), 270f);
        PlaceTorchWithLight("Torch_Atrium_L", new Vector3(-13.8f, 1.8f, -20f), 90f);
        PlaceTorchWithLight("Torch_Atrium_R", new Vector3(13.8f, 1.8f, -20f), 270f);

        PlaceTorchWithLight("Torch_Storage_1", new Vector3(-33.8f, 1.8f, -15f), 90f);
        PlaceTorchWithLight("Torch_Alchemy_1", new Vector3(-33.8f, 1.8f, 3f), 90f);

        PlaceTorchWithLight("Torch_Armory_1", new Vector3(33.8f, 1.8f, -15f), 270f);
        PlaceTorchWithLight("Torch_Crypt_1", new Vector3(33.8f, 1.8f, 3f), 270f);

        PlaceTorchWithLight("Torch_Arena_NW", new Vector3(-13.8f, 1.8f, 12f), 90f);
        PlaceTorchWithLight("Torch_Arena_NE", new Vector3(13.8f, 1.8f, 12f), 270f);
        PlaceTorchWithLight("Torch_Arena_SW", new Vector3(-13.8f, 1.8f, -8f), 90f);
        PlaceTorchWithLight("Torch_Arena_SE", new Vector3(13.8f, 1.8f, -8f), 270f);

        PlaceTorchWithLight("Torch_Sanctum_L", new Vector3(-11.8f, 1.8f, 24f), 90f);
        PlaceTorchWithLight("Torch_Sanctum_R", new Vector3(11.8f, 1.8f, 24f), 270f);

        PlaceModel(root, "Banner_Sanctum_1", "banner_shield_red", new Vector3(-4f, 2.4f, 29.8f), 180f);
        PlaceModel(root, "Banner_Sanctum_2", "banner_shield_red", new Vector3(4f, 2.4f, 29.8f), 180f);
    }

    // ============================================================
    // 7. 7 PASANG DUAL MATH GATES
    // ============================================================
    static void BuildDualMathGates(GameObject arena)
    {
        GameObject root = CreateChild(arena, "MathGates");

        CreateMathGatePair(root, "Gate_1_SafeHP", new Vector3(-14f, 1.2f, -14.5f), MathGateType.AddHP, 10f, true);
        CreateMathGatePair(root, "Gate_1_RiskSpeed", new Vector3(-14f, 1.2f, -17.5f), MathGateType.CustomRisk, 3f, true, "+3 SPD\n-5 HP", 5f);

        CreateMathGatePair(root, "Gate_2_BuffATK", new Vector3(14f, 1.2f, -14.5f), MathGateType.MultiplyATK, 2f, true);
        CreateMathGatePair(root, "Gate_2_SlowDebuff", new Vector3(14f, 1.2f, -17.5f), MathGateType.SubSpeed, 2f, true);

        CreateMathGatePair(root, "Gate_3_Heal", new Vector3(-25f, 1.2f, -6f), MathGateType.AddHP, 15f, false);
        CreateMathGatePair(root, "Gate_3_Poison", new Vector3(-23f, 1.2f, -6f), MathGateType.SubHP, 10f, false);

        CreateMathGatePair(root, "Gate_4_BoostATK", new Vector3(23f, 1.2f, -6f), MathGateType.MultiplyATK, 2f, false);
        CreateMathGatePair(root, "Gate_4_WeakenATK", new Vector3(25f, 1.2f, -6f), MathGateType.DivideATK, 2f, false);

        CreateMathGatePair(root, "Gate_5_Agility", new Vector3(-14f, 1.2f, 2.5f), MathGateType.AddSpeed, 2f, true);
        CreateMathGatePair(root, "Gate_5_Damage", new Vector3(-14f, 1.2f, -0.5f), MathGateType.SubHP, 10f, true);

        CreateMathGatePair(root, "Gate_6_Fury", new Vector3(14f, 1.2f, 2.5f), MathGateType.MultiplyATK, 2f, true);
        CreateMathGatePair(root, "Gate_6_Curse", new Vector3(14f, 1.2f, -0.5f), MathGateType.SubSpeed, 3f, true);

        CreateMathGatePair(root, "Gate_7_GodSpeed", new Vector3(-2f, 1.2f, 16f), MathGateType.AddSpeed, 4f, false);
        CreateMathGatePair(root, "Gate_7_MaxHeal",  new Vector3( 2f, 1.2f, 16f), MathGateType.AddHP, 30f, false);
    }

    static void CreateMathGatePair(GameObject parent, string name, Vector3 pos, MathGateType type, float val, bool facingX, string customTxt = "", float secVal = 0f)
    {
        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = name;
        gate.transform.SetParent(parent.transform);
        gate.transform.localPosition = pos;

        if (facingX)
            gate.transform.localScale = new Vector3(0.08f, 2.4f, 2.2f);
        else
            gate.transform.localScale = new Vector3(2.2f, 2.4f, 0.08f);

        BoxCollider box = gate.GetComponent<BoxCollider>();
        box.isTrigger = true;

        MathGate gateComp = gate.AddComponent<MathGate>();

        GameObject textObj = new GameObject("CardLabel");
        textObj.transform.SetParent(gate.transform);
        textObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        textObj.transform.localRotation = Quaternion.Euler(0f, facingX ? 90f : 0f, 0f);
        textObj.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 26;
        tm.fontStyle = FontStyle.Bold;

        gateComp.labelText = tm;
        gateComp.Setup(type, val, customTxt, secVal);
    }

    // ============================================================
    // COLLIDER & HELPER METHODS
    // ============================================================
    static void AddSolidFloorAndWallColliders(GameObject arena)
    {
        Transform ground = arena.transform.Find("Ground");
        if (ground != null)
        {
            foreach (Transform tile in ground)
            {
                BoxCollider bc = tile.GetComponent<BoxCollider>();
                if (bc == null) bc = tile.gameObject.AddComponent<BoxCollider>();
                bc.size = new Vector3(2f, 0.5f, 2f);
                bc.center = new Vector3(0f, -0.25f, 0f);
            }
        }

        Transform walls = arena.transform.Find("Walls");
        if (walls != null)
        {
            foreach (Transform wall in walls)
            {
                EnsureSolidCollider(wall.gameObject);
            }
        }

        Transform obs = arena.transform.Find("Obstacles");
        if (obs != null)
        {
            foreach (Transform group in obs)
            {
                foreach (Transform item in group)
                {
                    EnsureSolidCollider(item.gameObject);
                }
            }
        }
    }

    static void EnsureSolidCollider(GameObject go)
    {
        if (go.GetComponent<Collider>() != null) return;

        MeshFilter mf = go.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            MeshCollider mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }
        else
        {
            go.AddComponent<BoxCollider>();
        }
    }

    static GameObject PlaceModel(GameObject parent, string goName, string assetName, Vector3 pos, float rotY = 0f)
    {
        string path = $"{KAYKIT_PATH}/{assetName}.fbx";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        GameObject go;
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = goName;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"{goName}_PLACEHOLDER";
            go.transform.localScale = Vector3.one;
        }

        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        return go;
    }

    static void PlaceWallSingle(GameObject parent, string goName, string assetName, Vector3 pos, float rotY)
    {
        PlaceModel(parent, goName, assetName, pos, rotY);
    }

    static void PlaceWallRowZ(GameObject parent, string prefix, string assetName,
                              float x, float wy, float fromZ, float toZ, float stepZ, float rotY)
    {
        int idx = 0;
        for (float z = fromZ; z < toZ - 0.01f; z += stepZ)
        {
            PlaceModel(parent, $"{prefix}_{idx}", assetName,
                       new Vector3(x, wy, z + stepZ * 0.5f), rotY);
            idx++;
        }
    }

    static void PlaceWallRowX(GameObject parent, string prefix, string assetName,
                              float z, float wy, float fromX, float toX, float stepX, float rotY)
    {
        int idx = 0;
        for (float x = fromX; x < toX - 0.01f; x += stepX)
        {
            PlaceModel(parent, $"{prefix}_{idx}", assetName,
                       new Vector3(x + stepX * 0.5f, wy, z), rotY);
            idx++;
        }
    }

    static void PlaceTileGrid(GameObject parent, string prefix, string assetName,
                               float startX, float endX, float stepX,
                               float startZ, float endZ, float stepZ)
    {
        int r = 0;
        for (float z = startZ; z < endZ - 0.01f; z += stepZ)
        {
            int c = 0;
            for (float x = startX; x < endX - 0.01f; x += stepX)
            {
                PlaceModel(parent, $"{prefix}_r{r}_c{c}", assetName,
                           new Vector3(x + stepX * 0.5f, 0f, z + stepZ * 0.5f), 0f);
                c++;
            }
            r++;
        }
    }

    static GameObject CreateChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = Vector3.zero;
        return go;
    }

    static void CreatePoint(GameObject parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;
    }
}
