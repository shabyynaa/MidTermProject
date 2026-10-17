using UnityEngine;
using UnityEditor;

/// <summary>
/// Generator MEGA DUNGEON DENGAN GERBANG TERBUKA PLONG (OPEN ARCHWAYS)
/// Semua pintu sekarang berupa CELAH TERBUKA 100% DIAPIT DUA PILAR MEGAH (Tanpa kusen buntu, 0% macet!)
/// Menu: Tools → Build Clean Solid Mega Dungeon
/// </summary>
public class BuildDungeonArenaKayKit : EditorWindow
{
    const string KAYKIT_PATH = "Assets/Art/Kaykit_Dungeon/fbx(unity)";
    const float WALL_H = 2.05f;

    [MenuItem("Tools/Build Clean Solid Mega Dungeon")]
    public static void BuildArena()
    {
        GameObject existing = GameObject.Find("DungeonArena");
        if (existing != null)
        {
            DestroyImmediate(existing);
        }

        GameObject arena = new GameObject("DungeonArena");
        Undo.RegisterCreatedObjectUndo(arena, "Build Clean Solid Mega Dungeon");

        BuildGround(arena);
        BuildWalls(arena);
        BuildObstacles(arena);
        BuildAIPoints(arena);
        BuildDecorations(arena);
        BuildMathGates(arena);

        // Pasang Collider ke Tembok Biasa & Obstacle (Area Gerbang Terbuka 100% Plong Bebas Collider!)
        AddCleanColliders(arena);

        Selection.activeGameObject = arena;
        SceneView.lastActiveSceneView?.FrameSelected();

        Debug.Log("<color=#00ff88>[MegaDungeon] ✅ SEMUA GERBANG SEKARANG TERBUKA PLONG 100%! Dijamin bisa lewat!</color>");
        EditorUtility.DisplayDialog("✅ Selesai!",
            "SEMUA GERBANG SEKARANG TERBUKA PLONG (OPEN GATEWAYS):\n\n" +
            "• Tidak ada kusen buntu lagi (100% celah terbuka lapang diapit pilar).\n" +
            "• Karakter dan Enemy dijamin BISA LEWAT DENGAN MULUS!\n" +
            "• Tembok luar tetap padat (tidak tembus).\n\n" +
            "Jangan lupa Bake ulang NavMeshSurface di DungeonArena!",
            "Mantap!");
    }

    // ============================================================
    // SISTEM COLLIDER: Tembok Biasa Padat, Gerbang Plong
    // ============================================================
    static void AddCleanColliders(GameObject arena)
    {
        Transform walls = arena.transform.Find("Walls");
        if (walls != null)
        {
            foreach (Transform wall in walls)
            {
                // Pasang box collider ke tembok padat
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

    // ============================================================
    // 1. GROUND
    // ============================================================
    static void BuildGround(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Ground");

        PlaceTileGrid(root, "Floor_Center", "floor_tile_large", -10f, 10f, 2f, -8f, 12f, 2f);
        PlaceTileGrid(root, "Floor_South", "floor_tile_large", -8f, 8f, 2f, -20f, -8f, 2f);
        PlaceTileGrid(root, "Floor_West", "floor_tile_large", -26f, -10f, 2f, -6f, 8f, 2f);
        PlaceTileGrid(root, "Floor_East", "floor_tile_large", 10f, 26f, 2f, -6f, 8f, 2f);

        PlaceTileGrid(root, "Floor_Loop_NW", "floor_tile_large", -26f, -10f, 2f, 8f, 12f, 2f);
        PlaceTileGrid(root, "Floor_Loop_NE", "floor_tile_large", 10f, 26f, 2f, 8f, 12f, 2f);

        PlaceTileGrid(root, "Floor_Loop_SW", "floor_tile_large", -26f, -8f, 2f, -16f, -6f, 2f);
        PlaceTileGrid(root, "Floor_Loop_SE", "floor_tile_large", 8f, 26f, 2f, -16f, -6f, 2f);
    }

    // ============================================================
    // 2. WALLS (GERBANG TERBUKA PLONG DIAPIT PILAR MEGAH)
    // ============================================================
    static void BuildWalls(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Walls");

        System.Action<string, float, float, float, float> BuildWallX = (name, z, x1, x2, rot) => {
            PlaceWallRowX(root, name + "_1", "wall", z, 0f, x1, x2, 2f, rot);
            PlaceWallRowX(root, name + "_2", "wall", z, WALL_H, x1, x2, 2f, rot);
        };
        System.Action<string, float, float, float, float> BuildWallZ = (name, x, z1, z2, rot) => {
            PlaceWallRowZ(root, name + "_1", "wall", x, 0f, z1, z2, 2f, rot);
            PlaceWallRowZ(root, name + "_2", "wall", x, WALL_H, z1, z2, 2f, rot);
        };
        System.Action<string, Vector3, float> BuildCorner = (name, pos, rot) => {
            PlaceWallSingle(root, name + "_1", "wall_corner", pos, rot);
            PlaceWallSingle(root, name + "_2", "wall_corner", pos + Vector3.up * WALL_H, rot);
        };

        // KELILING LUAR
        BuildWallX("Wall_Outer_North", 12f, -26f, 26f, 180f);
        BuildCorner("Corner_Outer_NW", new Vector3(-26f, 0f, 12f), 180f);
        BuildCorner("Corner_Outer_NE", new Vector3(26f, 0f, 12f), 270f);

        BuildWallZ("Wall_Outer_West", -26f, -16f, 12f, 90f);
        BuildCorner("Corner_Outer_SW", new Vector3(-26f, 0f, -16f), 90f);

        BuildWallZ("Wall_Outer_East", 26f, -16f, 12f, 270f);
        BuildCorner("Corner_Outer_SE", new Vector3(26f, 0f, -16f), 0f);

        BuildWallX("Wall_Outer_SW_Bot", -16f, -26f, -8f, 0f);
        BuildWallX("Wall_Outer_SE_Bot", -16f, 8f, 26f, 0f);

        BuildWallZ("Wall_South_West_Side", -8f, -20f, -16f, 90f);
        BuildWallZ("Wall_South_East_Side", 8f, -20f, -16f, 270f);
        BuildCorner("Corner_South_SW", new Vector3(-8f, 0f, -20f), 90f);
        BuildCorner("Corner_South_SE", new Vector3(8f, 0f, -20f), 0f);

        // 1. GERBANG ENTRANCE UTAMA (Bawah: Z = -20): Terbuka plong dari X: -2 s/d 2 diapit 2 Pilar
        BuildWallX("Wall_South_Bot_L", -20f, -8f, -2f, 0f);
        BuildWallX("Wall_South_Bot_R", -20f, 2f, 8f, 0f);
        PlaceModel(root, "Entrance_Pillar_L", "pillar", new Vector3(-2f, 0f, -20f));
        PlaceModel(root, "Entrance_Pillar_R", "pillar", new Vector3(2f, 0f, -20f));

        // 2. GERBANG BARAT (West Wing: X = -10): Terbuka plong dari Z: 0 s/d 4 diapit 2 Pilar
        BuildWallZ("Divider_West_Top", -10f, 4f, 12f, 270f);
        BuildWallZ("Divider_West_Bot", -10f, -8f, 0f, 270f);
        PlaceModel(root, "Gate_West_Pillar_B", "pillar", new Vector3(-10f, 0f, 0f));
        PlaceModel(root, "Gate_West_Pillar_T", "pillar", new Vector3(-10f, 0f, 4f));

        // 3. GERBANG TIMUR (East Crypt: X = 10): Terbuka plong dari Z: 0 s/d 4 diapit 2 Pilar
        BuildWallZ("Divider_East_Top", 10f, 4f, 12f, 90f);
        BuildWallZ("Divider_East_Bot", 10f, -8f, 0f, 90f);
        PlaceModel(root, "Gate_East_Pillar_B", "pillar", new Vector3(10f, 0f, 0f));
        PlaceModel(root, "Gate_East_Pillar_T", "pillar", new Vector3(10f, 0f, 4f));

        // 4. GERBANG SELATAN (South Hall: Z = -8): Terbuka plong dari X: -2 s/d 2 diapit 2 Pilar
        BuildWallX("Divider_South_L", -8f, -10f, -2f, 180f);
        BuildWallX("Divider_South_R", -8f, 2f, 10f, 180f);
        PlaceModel(root, "Gate_South_Pillar_L", "pillar", new Vector3(-2f, 0f, -8f));
        PlaceModel(root, "Gate_South_Pillar_R", "pillar", new Vector3(2f, 0f, -8f));

        BuildWallZ("Divider_Corr_SW", -10f, -14f, -8f, 90f);
        BuildWallZ("Divider_Corr_SE", 10f, -14f, -8f, 270f);
    }

    // ============================================================
    // 3. OBSTACLES
    // ============================================================
    static void BuildObstacles(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Obstacles");

        GameObject centerHall = CreateChild(root, "CenterHall_Pillars");
        PlaceModel(centerHall, "Pillar_NW", "pillar", new Vector3(-5f, 0f, 6f));
        PlaceModel(centerHall, "Pillar_NE", "pillar", new Vector3(5f, 0f, 6f));
        PlaceModel(centerHall, "Pillar_SW", "pillar", new Vector3(-5f, 0f, -2f));
        PlaceModel(centerHall, "Pillar_SE", "pillar", new Vector3(5f, 0f, -2f));

        GameObject westProps = CreateChild(root, "WestWing_Props");
        PlaceModel(westProps, "Crates_W1", "crates_stacked", new Vector3(-18f, 0f, 2f));
        PlaceModel(westProps, "Barrel_W1", "barrel_large", new Vector3(-20f, 0f, 4f));
        PlaceModel(westProps, "Barrel_W2", "barrel_small", new Vector3(-20f, 0f, 0f));
        PlaceModel(westProps, "Barrel_W3", "barrel_small_stack", new Vector3(-16f, 0f, -2f));

        GameObject eastProps = CreateChild(root, "EastCrypt_Props");
        PlaceModel(eastProps, "Barrier_1", "barrier", new Vector3(18f, 0f, 2f), 0f);
        PlaceModel(eastProps, "Barrier_2", "barrier", new Vector3(20f, 0f, 2f), 0f);
        PlaceModel(eastProps, "Gold_Chest", "chest_gold", new Vector3(22f, 0f, 5f), 45f);
    }

    // ============================================================
    // 4. AI POINTS
    // ============================================================
    static void BuildAIPoints(GameObject arena)
    {
        GameObject root = CreateChild(arena, "AI_Points");

        CreatePoint(root, "PlayerSpawn",    new Vector3(0f, 0f, -14f));
        CreatePoint(root, "EnemySpawn",     new Vector3(18f, 0f, 0f));

        CreatePoint(root, "PatrolPoint_W1", new Vector3(-18f, 0f, 2f));
        CreatePoint(root, "PatrolPoint_W2", new Vector3(0f, 0f, 8f));
        CreatePoint(root, "PatrolPoint_W3", new Vector3(18f, 0f, -8f));
        CreatePoint(root, "PatrolPoint_W4", new Vector3(-18f, 0f, -10f));

        CreatePoint(root, "SafePoint",      new Vector3(22f, 0f, 5f));
    }

    // ============================================================
    // 5. DEKORASI
    // ============================================================
    static void BuildDecorations(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Decorations");

        PlaceModel(root, "Torch_S1", "torch_mounted", new Vector3(-7.8f, 1.2f, -14f), 90f);
        PlaceModel(root, "Torch_S2", "torch_mounted", new Vector3(7.8f, 1.2f, -14f), 270f);

        PlaceModel(root, "Torch_C1", "torch_mounted", new Vector3(-4f, 1.2f, 11.8f), 180f);
        PlaceModel(root, "Torch_C2", "torch_mounted", new Vector3(4f, 1.2f, 11.8f), 180f);

        PlaceModel(root, "Torch_W",  "torch_mounted", new Vector3(-25.8f, 1.2f, 2f), 90f);
        PlaceModel(root, "Torch_E",  "torch_mounted", new Vector3(25.8f, 1.2f, 2f), 270f);

        PlaceModel(root, "Banner_C", "banner_shield_red", new Vector3(0f, 1.2f, 11.6f), 180f);
    }

    // ============================================================
    // 6. MATH GATES (Trigger Murni, Otomatis Tembus)
    // ============================================================
    static void BuildMathGates(GameObject arena)
    {
        GameObject root = CreateChild(arena, "MathGates");

        // Celah gerbang selebar 4 unit di tengah-tengah 2 pilar
        CreateGateTrigger(root, "Gate_West_Speed", new Vector3(-10f, 1.2f, 2f), MathGateType.AddSpeed, 2f, true);
        CreateGateTrigger(root, "Gate_East_Healing", new Vector3(10f, 1.2f, 2f), MathGateType.AddHP, 5f, true);
        CreateGateTrigger(root, "Gate_South_Attack", new Vector3(0f, 1.2f, -8f), MathGateType.MultiplyATK, 2f, false);
    }

    static void CreateGateTrigger(GameObject parent, string name, Vector3 pos, MathGateType type, float val, bool facingX)
    {
        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = name;
        gate.transform.SetParent(parent.transform);
        gate.transform.localPosition = pos;

        if (facingX)
            gate.transform.localScale = new Vector3(0.05f, 2.5f, 3.8f);
        else
            gate.transform.localScale = new Vector3(3.8f, 2.5f, 0.05f);

        // WAJIB IS TRIGGER AGAR BISA DITEMBUS 100%
        BoxCollider box = gate.GetComponent<BoxCollider>();
        box.isTrigger = true;

        MathGate gateComp = gate.AddComponent<MathGate>();

        GameObject textObj = new GameObject("CardLabel");
        textObj.transform.SetParent(gate.transform);
        textObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        textObj.transform.localRotation = Quaternion.Euler(0f, facingX ? 90f : 0f, 0f);
        textObj.transform.localScale = new Vector3(0.18f, 0.18f, 0.18f);

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 28;
        tm.fontStyle = FontStyle.Bold;

        gateComp.labelText = tm;
        gateComp.Setup(type, val);
    }

    // ============================================================
    // HELPER METHODS
    // ============================================================
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
