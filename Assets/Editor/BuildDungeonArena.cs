using UnityEngine;
using UnityEditor;

/// <summary>
/// Generator MEGA LABYRINTH DUNGEON (8 Ruangan Tinggi 3 Tingkat + Atap Lengkap)
/// Tembok Tinggi 6.15m + Langit-langit Batu Solid + 7 Pasang Dual Math Gates (Kiri/Kanan) + First Person POV
/// Menu: Tools → Build 8-Room Mega Labyrinth (Tall Walls + Ceiling)
/// </summary>
public class BuildDungeonArena : EditorWindow
{
    const string KAYKIT_PATH = "Assets/Art/Kaykit_Dungeon/fbx(unity)";
    const float WALL_H = 2.05f;
    const float CEILING_Y = 6.15f;

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

        AddSolidFloorAndWallColliders(arena);
        SetupFirstPersonCamera();

        Selection.activeGameObject = arena;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log("<color=#00ff88>[MegaLabyrinth] ✅ Tembok Tinggi 3 Tingkat + Atap Lengkap berhasil dibuat!</color>");
        EditorUtility.DisplayDialog("🏰 Selesai!",
            "Mega Labyrinth Tembok Tinggi & Beratap Berhasil Dibuat:\n\n" +
            "• Tembok dinaikkan jadi 3 tingkat (Tinggi 6.15m)\n" +
            "• Atap / Langit-langit batu solid terpasang\n" +
            "• 7 Pasang Dual Math Gates (Kiri & Kanan dengan efek ganda)\n" +
            "• Kamera First Person (POV) siap dimainkan.",
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
            string status = active ? "DIMUNCULKAN" : "DISEMBUNYIKAN";
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
        EditorUtility.DisplayDialog("Sukses!", $"{count} Lampu obor berhasil dioptimalkan!", "Mantap!");
    }

    // ============================================================
    // 1. GROUND
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
    // 2. CEILING
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
    // 3. WALLS
    // ============================================================
    static void BuildWalls(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Walls");
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

        BuildWallX("Wall_Court_Bot", -40f, -10f, 10f, 0f);
        BuildCorner("Corner_Court_SW", new Vector3(-10f, 0f, -40f), 90f);
        BuildCorner("Corner_Court_SE", new Vector3(10f, 0f, -40f), 0f);
        BuildWallZ("Wall_Court_W", -10f, -40f, -26f, 90f);
        BuildWallZ("Wall_Court_E", 10f, -40f, -26f, 270f);

        BuildWallX("Wall_Atrium_Bot_L", -26f, -14f, -10f, 0f);
        BuildWallX("Wall_Atrium_Bot_R", -26f, 10f, 14f, 0f);
        BuildCorner("Corner_Atrium_SW", new Vector3(-14f, 0f, -26f), 90f);
        BuildCorner("Corner_Atrium_SE", new Vector3(14f, 0f, -26f), 0f);

        BuildWallZ("Wall_West_Outer", -34f, -22f, 10f, 90f);
        BuildCorner("Corner_Storage_SW", new Vector3(-34f, 0f, -22f), 90f);
        BuildCorner("Corner_Alchemy_NW", new Vector3(-34f, 0f, 10f), 180f);
        BuildWallX("Wall_Storage_Bot", -22f, -34f, -14f, 0f);
        BuildWallX("Wall_Alchemy_Top", 10f, -34f, -14f, 180f);
        BuildWallX("Divider_West_L", -6f, -34f, -26f, 180f);
        BuildWallX("Divider_West_R", -6f, -22f, -14f, 180f);

        BuildWallZ("Wall_East_Outer", 34f, -22f, 10f, 270f);
        BuildCorner("Corner_Armory_SE", new Vector3(34f, 0f, -22f), 0f);
        BuildCorner("Corner_Crypt_NE", new Vector3(34f, 0f, 10f), 270f);
        BuildWallX("Wall_Armory_Bot", -22f, 14f, 34f, 0f);
        BuildWallX("Wall_Crypt_Top", 10f, 14f, 34f, 180f);
        BuildWallX("Divider_East_L", -6f, 14f, 22f, 180f);
        BuildWallX("Divider_East_R", -6f, 26f, 34f, 180f);

        BuildWallZ("Divider_W_1", -14f, -26f, -20f, 90f);
        BuildWallZ("Divider_W_2", -14f, -12f, -2f, 90f);
        BuildWallZ("Divider_W_3", -14f, 4f, 10f, 90f);
        BuildWallZ("Divider_E_1", 14f, -26f, -20f, 270f);
        BuildWallZ("Divider_E_2", 14f, -12f, -2f, 270f);
        BuildWallZ("Divider_E_3", 14f, 4f, 10f, 270f);

        BuildWallX("Divider_Atrium_Arena_L", -12f, -14f, -3f, 180f);
        BuildWallX("Divider_Atrium_Arena_R", -12f, 3f, 14f, 180f);
        BuildWallX("Wall_Arena_North_L", 16f, -14f, -4f, 180f);
        BuildWallX("Wall_Arena_North_R", 16f, 4f, 14f, 180f);
        BuildCorner("Corner_Arena_NW", new Vector3(-14f, 0f, 16f), 180f);
        BuildCorner("Corner_Arena_NE", new Vector3(14f, 0f, 16f), 270f);

        BuildWallX("Wall_Sanctum_Top", 30f, -12f, 12f, 180f);
        BuildCorner("Corner_Sanctum_NW", new Vector3(-12f, 0f, 30f), 180f);
        BuildCorner("Corner_Sanctum_NE", new Vector3(12f, 0f, 30f), 270f);
        BuildWallZ("Wall_Sanctum_W", -12f, 16f, 30f, 90f);
        BuildWallZ("Wall_Sanctum_E", 12f, 16f, 30f, 270f);
    }

    // ============================================================
    // 4. OBSTACLES
    // ============================================================
    static void BuildObstacles(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Obstacles");
        GameObject arenaHall = CreateChild(root, "GrandArena_Pillars");
        PlaceTallPillar(arenaHall, "Pillar_NW", new Vector3(-7f, 0f, 9f));
        PlaceTallPillar(arenaHall, "Pillar_NE", new Vector3(7f, 0f, 9f));
        PlaceTallPillar(arenaHall, "Pillar_SW", new Vector3(-7f, 0f, -2f));
        PlaceTallPillar(arenaHall, "Pillar_SE", new Vector3(7f, 0f, -2f));
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
        CreatePoint(root, "PlayerSpawn", new Vector3(0f, 0f, -34f));
        CreatePoint(root, "EnemySpawn", new Vector3(0f, 0f, 23f));
    }

    // ============================================================
    // 6. DECORATIONS
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
            pLight.shadows = LightShadows.None;
        };

        PlaceTorchWithLight("Torch_Court_L", new Vector3(-9.8f, 1.8f, -32f), 90f);
        PlaceTorchWithLight("Torch_Court_R", new Vector3(9.8f, 1.8f, -32f), 270f);
    }

    // ============================================================
    // 7. DUAL MATH GATES (Kiri & Kanan Koridor dengan Efek Ganda)
    // ============================================================
    static void BuildDualMathGates(GameObject arena)
    {
        GameObject root = CreateChild(arena, "MathGates");

        CreateBalancedMathGatePair(root, "GatePair_1", new Vector3(0f, 1.2f, -22f), true,
            "+15 HP\n-2 SPD", MathGateType.AddHP, 15f, MathGateType.SubSpeed, 2f,
            "+2 ATK\n-10 HP", MathGateType.MultiplyATK, 2f, MathGateType.SubHP, 10f);

        CreateBalancedMathGatePair(root, "GatePair_2", new Vector3(-24f, 1.2f, -14f), false,
            "+3 SPD\n-5 HP", MathGateType.AddSpeed, 3f, MathGateType.SubHP, 5f,
            "+25 HP\n-1 ATK", MathGateType.AddHP, 25f, MathGateType.DivideATK, 2f);

        CreateBalancedMathGatePair(root, "GatePair_3", new Vector3(24f, 1.2f, -14f), false,
            "+2 ATK\n-2 SPD", MathGateType.MultiplyATK, 2f, MathGateType.SubSpeed, 2f,
            "+10 HP\n+1 SPD", MathGateType.AddHP, 10f, MathGateType.AddSpeed, 1f);

        CreateBalancedMathGatePair(root, "GatePair_4", new Vector3(-24f, 1.2f, 2f), false,
            "+20 HP\n-10 HP", MathGateType.AddHP, 20f, MathGateType.SubHP, 10f,
            "+4 SPD\n-3 ATK", MathGateType.AddSpeed, 4f, MathGateType.DivideATK, 2f);

        CreateBalancedMathGatePair(root, "GatePair_5", new Vector3(24f, 1.2f, 2f), false,
            "+2 ATK\n-5 HP", MathGateType.MultiplyATK, 2f, MathGateType.SubHP, 5f,
            "+2 SPD\n-1 ATK", MathGateType.AddSpeed, 2f, MathGateType.DivideATK, 2f);

        CreateBalancedMathGatePair(root, "GatePair_6", new Vector3(0f, 1.2f, 14f), true,
            "+30 HP\n-4 SPD", MathGateType.AddHP, 30f, MathGateType.SubSpeed, 4f,
            "+3 ATK\n-15 HP", MathGateType.MultiplyATK, 3f, MathGateType.SubHP, 15f);

        CreateBalancedMathGatePair(root, "GatePair_7", new Vector3(0f, 1.2f, 22f), true,
            "+50 HP\n-5 SPD", MathGateType.AddHP, 50f, MathGateType.SubSpeed, 5f,
            "+4 ATK\n-20 HP", MathGateType.MultiplyATK, 4f, MathGateType.SubHP, 20f);
    }

    static void CreateBalancedMathGatePair(GameObject parent, string pairName, Vector3 centerPos, bool alongXAxis, 
        string leftText, MathGateType leftType1, float leftVal1, MathGateType leftType2, float leftVal2,
        string rightText, MathGateType rightType1, float rightVal1, MathGateType rightType2, float rightVal2)
    {
        GameObject pairObj = CreateChild(parent, pairName);
        Vector3 leftPos, rightPos;
        Vector3 gateScale;

        if (alongXAxis)
        {
            leftPos = centerPos + new Vector3(0f, 0f, 1.5f);
            rightPos = centerPos + new Vector3(0f, 0f, -1.5f);
            gateScale = new Vector3(0.08f, 2.4f, 2.2f);
        }
        else
        {
            leftPos = centerPos + new Vector3(-1.5f, 0f, 0f);
            rightPos = centerPos + new Vector3(1.5f, 0f, 0f);
            gateScale = new Vector3(2.2f, 2.4f, 0.08f);
        }

        SpawnSingleMathGate(pairObj, "Gate_Left", leftPos, gateScale, alongXAxis, leftText, leftType1, leftVal1, leftType2, leftVal2);
        SpawnSingleMathGate(pairObj, "Gate_Right", rightPos, gateScale, alongXAxis, rightText, rightType1, rightVal1, rightType2, rightVal2);
    }

    static void SpawnSingleMathGate(GameObject parent, string name, Vector3 pos, Vector3 scale, bool alongXAxis, 
        string customTxt, MathGateType type1, float val1, MathGateType type2, float val2)
    {
        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = name;
        gate.transform.SetParent(parent.transform);
        gate.transform.localPosition = pos;
        gate.transform.localScale = scale;

        BoxCollider box = gate.GetComponent<BoxCollider>();
        box.isTrigger = true;

        MathGate gateComp = gate.AddComponent<MathGate>();

        GameObject textObj = new GameObject("CardLabel");
        textObj.transform.SetParent(gate.transform);
        textObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        textObj.transform.localRotation = Quaternion.Euler(0f, alongXAxis ? 90f : 0f, 0f);
        textObj.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 26;
        tm.fontStyle = FontStyle.Bold;

        gateComp.labelText = tm;
        gateComp.SetupDouble(type1, val1, type2, val2, customTxt);
    }

    // ============================================================
    // COLLIDER & HELPERS
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
            PlaceModel(parent, $"{prefix}_{idx}", assetName, new Vector3(x, wy, z + stepZ * 0.5f), rotY);
            idx++;
        }
    }

    static void PlaceWallRowX(GameObject parent, string prefix, string assetName,
                            float z, float wy, float fromX, float toX, float stepX, float rotY)
    {
        int idx = 0;
        for (float x = fromX; x < toX - 0.01f; x += stepX)
        {
            PlaceModel(parent, $"{prefix}_{idx}", assetName, new Vector3(x + stepX * 0.5f, wy, z), rotY);
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
                PlaceModel(parent, $"{prefix}_r{r}_c{c}", assetName, new Vector3(x + stepX * 0.5f, 0f, z + stepZ * 0.5f), 0f);
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