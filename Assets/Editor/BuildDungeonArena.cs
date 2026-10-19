using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;

/// <summary>
/// Generator Layout Dungeon "MathGate Tactics"
/// Alur: Start Room → Hall 1 (Gate 1A/1B) → Hall 2 (Gate 2A/2B) → Hall 3 (Gate 3A/3B) → Hall 4 (Gate 4A/4B) → Boss Arena (Altar & Enemy)
/// Menggunakan asset KayKit Dungeon Pack + Collider Solid + Bake NavMesh Otomatis.
/// Menu: Tools → Build MathGate Tactics Dungeon
/// </summary>
[InitializeOnLoad]
public class BuildDungeonArena : EditorWindow
{
    const string KAYKIT_PATH = "Assets/Art/Kaykit_Dungeon/fbx(unity)";
    const float WALL_H = 2.05f;
    const float CEILING_Y = 6.15f;

    static BuildDungeonArena()
    {
        EditorApplication.delayCall += AutoEnsureMathGateTacticsLayout;
    }

    static void AutoEnsureMathGateTacticsLayout()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject existing = GameObject.Find("DungeonArena");
        if (existing == null) return;

        // Cek apakah layout sudah menggunakan versi ruangan diperlebar (Floor_Start_r9_c10 ada di 22m x 20m)
        Transform ground = existing.transform.Find("Ground");
        bool isWidened = ground != null && ground.Find("Floor_Start_r9_c10") != null;
        bool hasExternalNav = AssetDatabase.LoadAssetAtPath<Object>("Assets/Art/DungeonNavMeshData.asset") != null;

        if (!isWidened || !hasExternalNav)
        {
            Debug.Log("<color=#00ff88>[MathGateTactics] Memperlebar ruangan dungeon (Start, Hall 1..4, Boss Arena) & memperbarui NavMesh...</color>");
            BuildArenaInternal(silent: true);
        }
    }

    [MenuItem("Tools/Build MathGate Tactics Dungeon")]
    [MenuItem("Tools/Build 8-Room Mega Labyrinth (Tall Walls + Ceiling)")]
    public static void BuildArena()
    {
        GameObject existing = GameObject.Find("DungeonArena");
        if (existing != null)
        {
            bool ok = EditorUtility.DisplayDialog("Bangun Dungeon MathGate Tactics",
                "Layout dungeon akan diperbarui mengikuti referensi MathGate Tactics (Ruangan Diperlebar):\n\n" +
                "Start Room → Hall 1 (1A/1B) → Hall 2 (2A/2B) → Hall 3 (3A/3B) → Hall 4 (4A/4B) → Boss Arena.\n\nLanjutkan?",
                "Bangun Layout!", "Batal");
            if (!ok) return;
        }

        BuildArenaInternal(silent: false);
    }

    public static void BuildArenaInternal(bool silent)
    {
        GameObject existing = GameObject.Find("DungeonArena");
        if (existing != null)
        {
            DestroyImmediate(existing);
        }

        GameObject arena = new GameObject("DungeonArena");
        Undo.RegisterCreatedObjectUndo(arena, "Build MathGate Tactics Dungeon");

        BuildGround(arena);
        BuildWalls(arena);
        BuildCeiling(arena);
        BuildObstacles(arena);
        BuildAIPoints(arena);
        BuildRoomBoundaries(arena);
        BuildDecorations(arena);
        BuildDualMathGates(arena);

        AddSolidFloorAndWallColliders(arena);
        RepositionCharactersAndCamera();
        BakeDungeonNavMesh(arena);

        Selection.activeGameObject = arena;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("<color=#00ff88>[MathGateTactics] ✅ Layout Dungeon MathGate Tactics (Ruangan Lega) berhasil dibangun!</color>");
        if (!silent)
        {
            EditorUtility.DisplayDialog("Selesai!",
                "Layout Dungeon MathGate Tactics Berhasil Diperbarui:\n\n" +
                "• Start Room & Hall 1–4 diperlebar menjadi 22m x 20m\n" +
                "• Boss Arena diperlebar menjadi 36m x 28m\n" +
                "• Lebar lorong tetap ideal (4m) dengan Gate 1A/1B, 2A/2B, 3A/3B, 4A/4B\n" +
                "• Collider Lantai/Tembok & NavMesh sudah diperbarui.",
                "Mantap!");
        }
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

    // ============================================================
    // 1. GROUND (Ruangan Diperlebar: 22m x 20m, Boss Arena: 36m x 28m, Lorong Tetap 8m x 4m)
    // ============================================================
    static void BuildGround(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Ground");

        // Start Room (22m x 20m) & Koridor ke Hall 1 (8m x 4m)
        PlaceTileGrid(root, "Floor_Start", "floor_tile_large", -30f, -8f, 2f, 4f, 24f, 2f);
        PlaceTileGrid(root, "Floor_Corr_Start_H1", "floor_tile_large", -8f, 0f, 2f, 12f, 16f, 2f);

        // Hall 1 (22m x 20m) & Koridor 1A (Atas) + 1B (Bawah) (8m x 4m)
        PlaceTileGrid(root, "Floor_Hall1", "floor_tile_large", 0f, 22f, 2f, 4f, 24f, 2f);
        PlaceTileGrid(root, "Floor_Corr_1A", "floor_tile_large", 22f, 30f, 2f, 18f, 22f, 2f);
        PlaceTileGrid(root, "Floor_Corr_1B", "floor_tile_large", 22f, 30f, 2f, 6f, 10f, 2f);

        // Hall 2 (22m x 20m) & Koridor Vertikal 2A (Kiri) + 2B (Kanan) (4m x 8m)
        PlaceTileGrid(root, "Floor_Hall2", "floor_tile_large", 30f, 52f, 2f, 4f, 24f, 2f);
        PlaceTileGrid(root, "Floor_Corr_2A", "floor_tile_large", 32f, 36f, 2f, -4f, 4f, 2f);
        PlaceTileGrid(root, "Floor_Corr_2B", "floor_tile_large", 46f, 50f, 2f, -4f, 4f, 2f);

        // Hall 3 (22m x 20m) & Koridor 3A (Atas) + 3B (Bawah) (8m x 4m)
        PlaceTileGrid(root, "Floor_Hall3", "floor_tile_large", 30f, 52f, 2f, -24f, -4f, 2f);
        PlaceTileGrid(root, "Floor_Corr_3A", "floor_tile_large", 22f, 30f, 2f, -10f, -6f, 2f);
        PlaceTileGrid(root, "Floor_Corr_3B", "floor_tile_large", 22f, 30f, 2f, -22f, -18f, 2f);

        // Hall 4 (22m x 20m) & Koridor 4A (Atas) + 4B (Bawah) (8m x 4m)
        PlaceTileGrid(root, "Floor_Hall4", "floor_tile_large", 0f, 22f, 2f, -24f, -4f, 2f);
        PlaceTileGrid(root, "Floor_Corr_4A", "floor_tile_large", -8f, 0f, 2f, -10f, -6f, 2f);
        PlaceTileGrid(root, "Floor_Corr_4B", "floor_tile_large", -8f, 0f, 2f, -22f, -18f, 2f);

        // Boss Arena (36m x 28m)
        PlaceTileGrid(root, "Floor_BossArena", "floor_tile_large", -44f, -8f, 2f, -28f, 0f, 2f);
    }

    // ============================================================
    // 2. CEILING
    // ============================================================
    static void BuildCeiling(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Ceiling");
        System.Action<string, float, float, float, float> PlaceCeilingGrid = (prefix, x1, x2, z1, z2) =>
        {
            int r = 0;
            for (float z = z1; z < z2 - 0.01f; z += 2f)
            {
                int c = 0;
                for (float x = x1; x < x2 - 0.01f; x += 2f)
                {
                    GameObject tile = PlaceModel(root, $"{prefix}_r{r}_c{c}", "floor_tile_large",
                        new Vector3(x + 1f, CEILING_Y, z + 1f), 0f);
                    tile.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                    c++;
                }
                r++;
            }
        };

        PlaceCeilingGrid("Ceil_Start", -30f, -8f, 4f, 24f);
        PlaceCeilingGrid("Ceil_Corr_Start_H1", -8f, 0f, 12f, 16f);
        PlaceCeilingGrid("Ceil_Hall1", 0f, 22f, 4f, 24f);
        PlaceCeilingGrid("Ceil_Corr_1A", 22f, 30f, 18f, 22f);
        PlaceCeilingGrid("Ceil_Corr_1B", 22f, 30f, 6f, 10f);
        PlaceCeilingGrid("Ceil_Hall2", 30f, 52f, 4f, 24f);
        PlaceCeilingGrid("Ceil_Corr_2A", 32f, 36f, -4f, 4f);
        PlaceCeilingGrid("Ceil_Corr_2B", 46f, 50f, -4f, 4f);
        PlaceCeilingGrid("Ceil_Hall3", 30f, 52f, -24f, -4f);
        PlaceCeilingGrid("Ceil_Corr_3A", 22f, 30f, -10f, -6f);
        PlaceCeilingGrid("Ceil_Corr_3B", 22f, 30f, -22f, -18f);
        PlaceCeilingGrid("Ceil_Hall4", 0f, 22f, -24f, -4f);
        PlaceCeilingGrid("Ceil_Corr_4A", -8f, 0f, -10f, -6f);
        PlaceCeilingGrid("Ceil_Corr_4B", -8f, 0f, -22f, -18f);
        PlaceCeilingGrid("Ceil_BossArena", -44f, -8f, -28f, 0f);
    }

    // ============================================================
    // 3. WALLS (3 Tingkat = 6.15m, Rapat & Tanpa Celah)
    // ============================================================
    static void BuildWalls(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Walls");
        System.Action<string, float, float, float, float> BuildWallX = (name, z, x1, x2, rot) =>
        {
            PlaceWallRowX(root, name + "_1", "wall", z, 0f, x1, x2, 2f, rot);
            PlaceWallRowX(root, name + "_2", "wall", z, WALL_H, x1, x2, 2f, rot);
            PlaceWallRowX(root, name + "_3", "wall", z, WALL_H * 2f, x1, x2, 2f, rot);
        };
        System.Action<string, float, float, float, float> BuildWallZ = (name, x, z1, z2, rot) =>
        {
            PlaceWallRowZ(root, name + "_1", "wall", x, 0f, z1, z2, 2f, rot);
            PlaceWallRowZ(root, name + "_2", "wall", x, WALL_H, z1, z2, 2f, rot);
            PlaceWallRowZ(root, name + "_3", "wall", x, WALL_H * 2f, z1, z2, 2f, rot);
        };

        // --- A. START ROOM (X: -30..-8, Z: 4..24) ---
        BuildWallX("Wall_Start_N", 24f, -30f, -8f, 180f);
        BuildWallX("Wall_Start_S", 4f, -30f, -8f, 0f);
        BuildWallZ("Wall_Start_W", -30f, 4f, 24f, 90f);
        BuildWallZ("Wall_Start_E_Bot", -8f, 4f, 12f, 270f);
        BuildWallZ("Wall_Start_E_Top", -8f, 16f, 24f, 270f);

        // --- B. CORRIDOR START -> HALL 1 (X: -8..0, Z: 12..16) ---
        BuildWallX("Wall_Corr0_N", 16f, -8f, 0f, 180f);
        BuildWallX("Wall_Corr0_S", 12f, -8f, 0f, 0f);

        // --- C. HALL 1 (X: 0..22, Z: 4..24) ---
        BuildWallX("Wall_H1_N", 24f, 0f, 22f, 180f);
        BuildWallX("Wall_H1_S", 4f, 0f, 22f, 0f);
        BuildWallZ("Wall_H1_W_Bot", 0f, 4f, 12f, 90f);
        BuildWallZ("Wall_H1_W_Top", 0f, 16f, 24f, 90f);
        BuildWallZ("Wall_H1_E_Bot", 22f, 4f, 6f, 270f);
        BuildWallZ("Wall_H1_E_Mid", 22f, 10f, 18f, 270f); // Pemisah Gate 1A & 1B
        BuildWallZ("Wall_H1_E_Top", 22f, 22f, 24f, 270f);

        // --- D. CORRIDORS 1A & 1B (X: 22..30, Z: 18..22 & 6..10) ---
        BuildWallX("Wall_Corr1A_N", 22f, 22f, 30f, 180f);
        BuildWallX("Wall_Corr1A_S", 18f, 22f, 30f, 0f);
        BuildWallX("Wall_Corr1B_N", 10f, 22f, 30f, 180f);
        BuildWallX("Wall_Corr1B_S", 6f, 22f, 30f, 0f);

        // --- E. HALL 2 (X: 30..52, Z: 4..24) ---
        BuildWallX("Wall_H2_N", 24f, 30f, 52f, 180f);
        BuildWallZ("Wall_H2_E", 52f, 4f, 24f, 270f);
        BuildWallZ("Wall_H2_W_Bot", 30f, 4f, 6f, 90f);
        BuildWallZ("Wall_H2_W_Mid", 30f, 10f, 18f, 90f);
        BuildWallZ("Wall_H2_W_Top", 30f, 22f, 24f, 90f);
        BuildWallX("Wall_H2_S_L", 4f, 30f, 32f, 0f);
        BuildWallX("Wall_H2_S_Mid", 4f, 36f, 46f, 0f); // Pemisah Gate 2A & 2B
        BuildWallX("Wall_H2_S_R", 4f, 50f, 52f, 0f);

        // --- F. CORRIDORS 2A & 2B (Z: -4..4, X: 32..36 & 46..50) ---
        BuildWallZ("Wall_Corr2A_W", 32f, -4f, 4f, 90f);
        BuildWallZ("Wall_Corr2A_E", 36f, -4f, 4f, 270f);
        BuildWallZ("Wall_Corr2B_W", 46f, -4f, 4f, 90f);
        BuildWallZ("Wall_Corr2B_E", 50f, -4f, 4f, 270f);

        // --- G. HALL 3 (X: 30..52, Z: -24..-4) ---
        BuildWallX("Wall_H3_N_L", -4f, 30f, 32f, 180f);
        BuildWallX("Wall_H3_N_Mid", -4f, 36f, 46f, 180f);
        BuildWallX("Wall_H3_N_R", -4f, 50f, 52f, 180f);
        BuildWallZ("Wall_H3_E", 52f, -24f, -4f, 270f);
        BuildWallX("Wall_H3_S", -24f, 30f, 52f, 0f);
        BuildWallZ("Wall_H3_W_Bot", 30f, -24f, -22f, 90f);
        BuildWallZ("Wall_H3_W_Mid", 30f, -18f, -10f, 90f); // Pemisah Gate 3A & 3B
        BuildWallZ("Wall_H3_W_Top", 30f, -6f, -4f, 90f);

        // --- H. CORRIDORS 3A & 3B (X: 22..30, Z: -10..-6 & -22..-18) ---
        BuildWallX("Wall_Corr3A_N", -6f, 22f, 30f, 180f);
        BuildWallX("Wall_Corr3A_S", -10f, 22f, 30f, 0f);
        BuildWallX("Wall_Corr3B_N", -18f, 22f, 30f, 180f);
        BuildWallX("Wall_Corr3B_S", -22f, 22f, 30f, 0f);

        // --- I. HALL 4 (X: 0..22, Z: -24..-4) ---
        BuildWallX("Wall_H4_N", -4f, 0f, 22f, 180f);
        BuildWallX("Wall_H4_S", -24f, 0f, 22f, 0f);
        BuildWallZ("Wall_H4_E_Bot", 22f, -24f, -22f, 270f);
        BuildWallZ("Wall_H4_E_Mid", 22f, -18f, -10f, 270f);
        BuildWallZ("Wall_H4_E_Top", 22f, -6f, -4f, 270f);
        BuildWallZ("Wall_H4_W_Bot", 0f, -24f, -22f, 90f);
        BuildWallZ("Wall_H4_W_Mid", 0f, -18f, -10f, 90f); // Pemisah Gate 4A & 4B
        BuildWallZ("Wall_H4_W_Top", 0f, -6f, -4f, 90f);

        // --- J. CORRIDORS 4A & 4B (X: -8..0, Z: -10..-6 & -22..-18) ---
        BuildWallX("Wall_Corr4A_N", -6f, -8f, 0f, 180f);
        BuildWallX("Wall_Corr4A_S", -10f, -8f, 0f, 0f);
        BuildWallX("Wall_Corr4B_N", -18f, -8f, 0f, 180f);
        BuildWallX("Wall_Corr4B_S", -22f, -8f, 0f, 0f);

        // --- K. BOSS ARENA (X: -44..-8, Z: -28..0) ---
        BuildWallX("Wall_Boss_N", 0f, -44f, -8f, 180f);
        BuildWallX("Wall_Boss_S", -28f, -44f, -8f, 0f);
        BuildWallZ("Wall_Boss_W", -44f, -28f, 0f, 90f);
        BuildWallZ("Wall_Boss_E_Top", -8f, -6f, 0f, 270f);
        BuildWallZ("Wall_Boss_E_Mid", -8f, -18f, -10f, 270f);
        BuildWallZ("Wall_Boss_E_Bot", -8f, -28f, -22f, 270f);
    }

    // ============================================================
    // 4. OBSTACLES (Pilar di Tiap Hall & Altar di Boss Arena)
    // ============================================================
    static void BuildObstacles(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Obstacles");

        // Hall 1 Pillars
        GameObject h1 = CreateChild(root, "Hall1_Pillars");
        PlaceTallPillar(h1, "Pillar_H1_Top", new Vector3(5f, 0f, 19f));
        PlaceTallPillar(h1, "Pillar_H1_Bot", new Vector3(5f, 0f, 9f));

        // Hall 2 Pillars
        GameObject h2 = CreateChild(root, "Hall2_Pillars");
        PlaceTallPillar(h2, "Pillar_H2_L", new Vector3(35f, 0f, 19f));
        PlaceTallPillar(h2, "Pillar_H2_R", new Vector3(47f, 0f, 19f));

        // Hall 3 Pillars
        GameObject h3 = CreateChild(root, "Hall3_Pillars");
        PlaceTallPillar(h3, "Pillar_H3_Top", new Vector3(47f, 0f, -9f));
        PlaceTallPillar(h3, "Pillar_H3_Bot", new Vector3(47f, 0f, -19f));

        // Hall 4 Pillars
        GameObject h4 = CreateChild(root, "Hall4_Pillars");
        PlaceTallPillar(h4, "Pillar_H4_Top", new Vector3(17f, 0f, -9f));
        PlaceTallPillar(h4, "Pillar_H4_Bot", new Vector3(17f, 0f, -19f));

        // Boss Arena Pillars & Central Altar
        GameObject boss = CreateChild(root, "BossArena_Obstacles");
        PlaceTallPillar(boss, "Pillar_Boss_NW", new Vector3(-34f, 0f, -7f));
        PlaceTallPillar(boss, "Pillar_Boss_NE", new Vector3(-18f, 0f, -7f));
        PlaceTallPillar(boss, "Pillar_Boss_SW", new Vector3(-34f, 0f, -21f));
        PlaceTallPillar(boss, "Pillar_Boss_SE", new Vector3(-18f, 0f, -21f));

        // Altar di tengah Boss Arena (-26, 0, -14)
        PlaceModel(boss, "Altar_Table", "table_long_tablecloth_decorated_A", new Vector3(-26f, 0f, -14f), 90f);
        PlaceModel(boss, "Altar_Chest", "chest_gold", new Vector3(-26f, 1.0f, -14f), 90f);
    }

    static void PlaceTallPillar(GameObject parent, string name, Vector3 pos)
    {
        PlaceModel(parent, name + "_1", "pillar_decorated", pos);
        PlaceModel(parent, name + "_2", "pillar", pos + Vector3.up * WALL_H);
        PlaceModel(parent, name + "_3", "pillar", pos + Vector3.up * (WALL_H * 2f));
    }

    // ============================================================
    // 5. AI POINTS (PlayerSpawn di Start, Waypoints & SafePoint di Hall 2, 3, 4, & Boss Arena)
    // ============================================================
    static void BuildAIPoints(GameObject arena)
    {
        GameObject root = CreateChild(arena, "AI_Points");
        CreatePoint(root, "PlayerSpawn", new Vector3(-24f, 0f, 14f));

        // Hall 2 (X: 30..52, Z: 4..24, Pillars at (35,19) & (47,19))
        CreatePoint(root, "H2_Patrol_W1", new Vector3(36f, 0f, 14f));
        CreatePoint(root, "H2_Patrol_W2", new Vector3(46f, 0f, 14f));
        CreatePoint(root, "H2_Patrol_W3", new Vector3(46f, 0f, 8f));
        CreatePoint(root, "H2_Patrol_W4", new Vector3(36f, 0f, 8f));
        CreatePoint(root, "H2_SafePoint", new Vector3(49f, 0f, 21f));

        // Hall 3 (X: 30..52, Z: -24..-4, Pillars at (47,-9) & (47,-19))
        CreatePoint(root, "H3_Patrol_W1", new Vector3(35f, 0f, -9f));
        CreatePoint(root, "H3_Patrol_W2", new Vector3(43f, 0f, -9f));
        CreatePoint(root, "H3_Patrol_W3", new Vector3(43f, 0f, -19f));
        CreatePoint(root, "H3_Patrol_W4", new Vector3(35f, 0f, -19f));
        CreatePoint(root, "H3_SafePoint", new Vector3(49f, 0f, -21f));

        // Hall 4 (X: 0..22, Z: -24..-4, Pillars at (17,-9) & (17,-19))
        CreatePoint(root, "H4_Patrol_W1", new Vector3(5f, 0f, -9f));
        CreatePoint(root, "H4_Patrol_W2", new Vector3(13f, 0f, -9f));
        CreatePoint(root, "H4_Patrol_W3", new Vector3(13f, 0f, -19f));
        CreatePoint(root, "H4_Patrol_W4", new Vector3(5f, 0f, -19f));
        CreatePoint(root, "H4_SafePoint", new Vector3(3f, 0f, -21f));

        // Boss Arena (X: -44..-8, Z: -28..0, Pillars at (-34,-7), (-18,-7), (-34,-21), (-18,-21))
        CreatePoint(root, "EnemySpawn", new Vector3(-39f, 0f, -14f));
        CreatePoint(root, "PatrolPoint_W1", new Vector3(-31f, 0f, -4f));
        CreatePoint(root, "PatrolPoint_W2", new Vector3(-21f, 0f, -4f));
        CreatePoint(root, "PatrolPoint_W3", new Vector3(-21f, 0f, -24f));
        CreatePoint(root, "PatrolPoint_W4", new Vector3(-31f, 0f, -24f));
        CreatePoint(root, "SafePoint", new Vector3(-40f, 0f, -24f));
    }

    // ============================================================
    // 5B. ROOM BOUNDARIES (Trigger BoxCollider per Room agar Enemy tidak keluar ruangan)
    // ============================================================
    public static void BuildRoomBoundaries(GameObject arena)
    {
        if (arena == null) return;
        Transform existing = arena.transform.Find("RoomBoundaries");
        if (existing != null) DestroyImmediate(existing.gameObject);

        GameObject root = CreateChild(arena, "RoomBoundaries");

        // Hall 2 (X: 30..52, Z: 4..24)
        CreateRoomBoundaryBox(root, "RoomBoundary_Hall2", new Vector3(41f, 3f, 14f), new Vector3(21.3f, 6f, 19.3f));
        // Hall 3 (X: 30..52, Z: -24..-4)
        CreateRoomBoundaryBox(root, "RoomBoundary_Hall3", new Vector3(41f, 3f, -14f), new Vector3(21.3f, 6f, 19.3f));
        // Hall 4 (X: 0..22, Z: -24..-4)
        CreateRoomBoundaryBox(root, "RoomBoundary_Hall4", new Vector3(11f, 3f, -14f), new Vector3(21.3f, 6f, 19.3f));
        // Boss Arena (X: -44..-8, Z: -28..0)
        CreateRoomBoundaryBox(root, "RoomBoundary_BossArena", new Vector3(-26f, 3f, -14f), new Vector3(35.3f, 6f, 27.3f));
    }

    static void CreateRoomBoundaryBox(GameObject parent, string name, Vector3 center, Vector3 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = center;
        go.transform.localRotation = Quaternion.identity;
        go.layer = 2; // Ignore Raycast

        BoxCollider box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = size;
    }

    // ============================================================
    // 6. DECORATIONS (Obor Penerangan Sesuai Titik Kuning Referensi)
    // ============================================================
    static void BuildDecorations(GameObject arena)
    {
        GameObject root = CreateChild(arena, "Decorations");
        System.Action<string, Vector3, float> PlaceTorchWithLight = (tName, pos, rotY) =>
        {
            GameObject torch = PlaceModel(root, tName, "torch_mounted", pos, rotY);
            GameObject lightObj = new GameObject(tName + "_Light");
            lightObj.transform.SetParent(torch.transform);
            lightObj.transform.localPosition = new Vector3(0f, 0.4f, 0.3f);
            Light pLight = lightObj.AddComponent<Light>();
            pLight.type = LightType.Point;
            pLight.color = new Color(1.0f, 0.68f, 0.3f);
            pLight.range = 16f;
            pLight.intensity = 3.2f;
            pLight.shadows = LightShadows.None;
        };

        // Start Room Torches
        PlaceTorchWithLight("Torch_Start_1", new Vector3(-23f, 2.2f, 23.8f), 180f);
        PlaceTorchWithLight("Torch_Start_2", new Vector3(-15f, 2.2f, 23.8f), 180f);

        // Hall 1 Torches
        PlaceTorchWithLight("Torch_H1_1", new Vector3(7f, 2.2f, 23.8f), 180f);
        PlaceTorchWithLight("Torch_H1_2", new Vector3(15f, 2.2f, 23.8f), 180f);

        // Hall 2 Torches
        PlaceTorchWithLight("Torch_H2_1", new Vector3(37f, 2.2f, 23.8f), 180f);
        PlaceTorchWithLight("Torch_H2_2", new Vector3(45f, 2.2f, 23.8f), 180f);

        // Hall 3 Torches
        PlaceTorchWithLight("Torch_H3_1", new Vector3(38f, 2.2f, -4.2f), 180f);
        PlaceTorchWithLight("Torch_H3_2", new Vector3(44f, 2.2f, -4.2f), 180f);

        // Hall 4 Torches
        PlaceTorchWithLight("Torch_H4_1", new Vector3(7f, 2.2f, -4.2f), 180f);
        PlaceTorchWithLight("Torch_H4_2", new Vector3(15f, 2.2f, -4.2f), 180f);

        // Boss Arena Torches
        PlaceTorchWithLight("Torch_Boss_1", new Vector3(-34f, 2.2f, -0.2f), 180f);
        PlaceTorchWithLight("Torch_Boss_2", new Vector3(-18f, 2.2f, -0.2f), 180f);
    }

    // ============================================================
    // 7. DUAL MATH GATES (1A/1B, 2A/2B, 3A/3B, 4A/4B)
    // ============================================================
    static void BuildDualMathGates(GameObject arena)
    {
        GameObject root = CreateChild(arena, "MathGates");

        // Hall 1 -> Hall 2 (Menghadap -X ke arah Hall 1, rotY = 90)
        MathGate g1A = SpawnSingleMathGate(root, "Gate_1A", new Vector3(23.5f, 1.3f, 20f), 90f,
            "1A: Gerbang Ganda\n+ATK x2 | -HP x0.5", MathGateType.MultiplyATK, 2f, MathGateType.SubHP, 25f);
        MathGate g1B = SpawnSingleMathGate(root, "Gate_1B", new Vector3(23.5f, 1.3f, 8f), 90f,
            "1B: Gerbang Tameng\n+HP x1.5 | -ATK x0.7", MathGateType.AddHP, 50f, MathGateType.DivideATK, 1.4f);
        LinkGatePair(g1A, g1B);

        // Hall 2 -> Hall 3 (Menghadap +Z ke arah Hall 2, rotY = 180)
        MathGate g2A = SpawnSingleMathGate(root, "Gate_2A", new Vector3(34f, 1.3f, 2.5f), 180f,
            "2A: Gerbang Angin\n+SPD x1.4 | -20 HP", MathGateType.AddSpeed, 2f, MathGateType.SubHP, 20f);
        MathGate g2B = SpawnSingleMathGate(root, "Gate_2B", new Vector3(48f, 1.3f, 2.5f), 180f,
            "2B: Gerbang Vampir\n+Heal 20% | -HP Max", MathGateType.AddHP, 20f, MathGateType.SubHP, 15f);
        LinkGatePair(g2A, g2B);

        // Hall 3 -> Hall 4 (Menghadap +X ke arah Hall 3, rotY = 270)
        MathGate g3A = SpawnSingleMathGate(root, "Gate_3A", new Vector3(28.5f, 1.3f, -8f), 270f,
            "3A: Gerbang Meriam\n+ATK x1.8 | -CD x1.5", MathGateType.MultiplyATK, 1.8f, MathGateType.SubSpeed, 1f);
        MathGate g3B = SpawnSingleMathGate(root, "Gate_3B", new Vector3(28.5f, 1.3f, -20f), 270f,
            "3B: Gerbang Kilat\n+CD x0.6 | -ATK x0.6", MathGateType.AddSpeed, 2f, MathGateType.DivideATK, 1.6f);
        LinkGatePair(g3A, g3B);

        // Hall 4 -> Boss Arena (Menghadap +X ke arah Hall 4, rotY = 270)
        MathGate g4A = SpawnSingleMathGate(root, "Gate_4A", new Vector3(-1.5f, 1.3f, -8f), 270f,
            "4A: Gerbang Baja\n+DEF 30% | -SPD x0.8", MathGateType.AddHP, 30f, MathGateType.SubSpeed, 1f);
        MathGate g4B = SpawnSingleMathGate(root, "Gate_4B", new Vector3(-1.5f, 1.3f, -20f), 270f,
            "4B: Gerbang Berserk\n+ATK x1.5 | -DEF 25%", MathGateType.MultiplyATK, 1.5f, MathGateType.SubHP, 15f);
        LinkGatePair(g4A, g4B);
    }

    static void LinkGatePair(MathGate gateA, MathGate gateB)
    {
        if (gateA != null && gateB != null)
        {
            gateA.pairedGate = gateB;
            gateB.pairedGate = gateA;
            EditorUtility.SetDirty(gateA);
            EditorUtility.SetDirty(gateB);
        }
    }

    static MathGate SpawnSingleMathGate(GameObject parent, string name, Vector3 pos, float rotY,
        string customTxt, MathGateType type1, float val1, MathGateType type2, float val2)
    {
        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = name;
        gate.transform.SetParent(parent.transform);
        gate.transform.localPosition = pos;
        gate.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
        gate.transform.localScale = new Vector3(3.6f, 2.6f, 0.12f);

        BoxCollider box = gate.GetComponent<BoxCollider>();
        box.isTrigger = true;

        MathGate gateComp = gate.AddComponent<MathGate>();

        GameObject textObj = new GameObject("CardLabel");
        textObj.transform.SetParent(gate.transform);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.65f);
        textObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        textObj.transform.localScale = new Vector3(0.14f, 0.2f, 1f);

        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.fontSize = 48;
        tm.characterSize = 0.08f;
        tm.fontStyle = FontStyle.Bold;

        gateComp.labelText = tm;
        gateComp.SetupDouble(type1, val1, type2, val2, customTxt);
        return gateComp;
    }

    // ============================================================
    // 8. COLLIDER, POSISI KARAKTER, DAN NAVMESH
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
            foreach (MeshFilter mf in walls.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<Collider>() == null && mf.sharedMesh != null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }

        Transform obstacles = arena.transform.Find("Obstacles");
        if (obstacles != null)
        {
            foreach (MeshFilter mf in obstacles.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<Collider>() == null && mf.sharedMesh != null)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
            }
        }
    }

    static void RepositionCharactersAndCamera()
    {
        // Posisikan Player (Kachujin) di Start Room menghadap ke Timur (+X, menuju Hall 1)
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.transform.position = new Vector3(-24f, 0.08f, 14f);
            player.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            if (cc != null) cc.enabled = true;
            EditorUtility.SetDirty(player.transform);
        }

        // Hapus karakter enemy lama jika masih ada
        GameObject oldSkel = GameObject.Find("Skeleton_Warrior");
        if (oldSkel != null) DestroyImmediate(oldSkel);
        GameObject oldRogue = GameObject.Find("Rogue");
        if (oldRogue != null) DestroyImmediate(oldRogue);

        // Perbarui referensi waypoint untuk seluruh EnemyAI di scene
        foreach (EnemyAI ai in Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None))
        {
            if (ai != null)
            {
                ai.patrolWaypoints = null;
                ai.safePoint = null;
                ai.FindSceneReferences();
                EditorUtility.SetDirty(ai);
            }
        }
    }

    static void BakeDungeonNavMesh(GameObject arena)
    {
        Transform ceiling = arena.transform.Find("Ceiling");
        Transform mathGates = arena.transform.Find("MathGates");
        Transform roomBoundaries = arena.transform.Find("RoomBoundaries");

        if (ceiling != null) ceiling.gameObject.SetActive(false);
        if (mathGates != null) mathGates.gameObject.SetActive(false);
        if (roomBoundaries != null) roomBoundaries.gameObject.SetActive(false);

        NavMeshSurface surface = arena.GetComponent<NavMeshSurface>();
        if (surface == null) surface = arena.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();

        if (surface.navMeshData != null && !AssetDatabase.Contains(surface.navMeshData))
        {
            string navAssetPath = "Assets/Art/DungeonNavMeshData.asset";
            AssetDatabase.DeleteAsset(navAssetPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navAssetPath);
            AssetDatabase.SaveAssets();
        }

        EditorSettings.serializationMode = SerializationMode.ForceText;

        if (ceiling != null) ceiling.gameObject.SetActive(true);
        if (mathGates != null) mathGates.gameObject.SetActive(true);
        if (roomBoundaries != null) roomBoundaries.gameObject.SetActive(true);
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