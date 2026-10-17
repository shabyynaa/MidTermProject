using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Generator Otomatis untuk Player Health UI Panel (Canvas, Health Bar Slider, dan Stat Text)
/// Menu: Tools → Build Player Health UI Panel
/// </summary>
public class UIHealthPanelBuilder : EditorWindow
{
    [MenuItem("Tools/Build Player Health UI Panel")]
    public static void CreateHealthUI()
    {
        // 1. Cek atau Buat Canvas
        GameObject canvasObj = GameObject.Find("Player_UI_Canvas");
        if (canvasObj != null)
        {
            DestroyImmediate(canvasObj);
        }

        canvasObj = new GameObject("Player_UI_Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        // Pastikan ada EventSystem
        if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // 2. Panel Utama di Pojok Kiri Atas
        GameObject panelObj = new GameObject("HealthPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0.1f, 0.1f, 0.15f, 0.75f); // Hitam transparan gelap

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f); // Pojok kiri atas
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(25f, -25f);
        panelRect.sizeDelta = new Vector2(260f, 90f);

        // 3. Judul / Nama Player
        GameObject titleObj = new GameObject("Player_NameText");
        titleObj.transform.SetParent(panelObj.transform, false);
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "KNIGHT (PLAYER)";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 14;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f, 0.85f, 0.4f); // Emas

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(15f, -8f);
        titleRect.sizeDelta = new Vector2(-30f, 20f);

        // 4. Slider HP Bar
        GameObject sliderObj = new GameObject("Player_HPSlider");
        sliderObj.transform.SetParent(panelObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0f, 0.5f);
        sliderRect.anchorMax = new Vector2(1f, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0f, 3f);
        sliderRect.sizeDelta = new Vector2(-30f, 22f);

        // Background Slider
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(sliderObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero;
        faRect.anchorMax = Vector2.one;
        faRect.sizeDelta = Vector2.zero;

        // Fill Bar (Merah / Hijau Darah)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.15f, 0.15f, 1f); // Merah Darah
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        slider.targetGraphic = fillImg;
        slider.fillRect = fillRect;
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;

        // 5. Text Angka HP di atas bar
        GameObject hpTextObj = new GameObject("Player_HPText");
        hpTextObj.transform.SetParent(sliderObj.transform, false);
        Text hpText = hpTextObj.AddComponent<Text>();
        hpText.text = "HP: 100 / 100";
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 12;
        hpText.fontStyle = FontStyle.Bold;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;

        RectTransform hpTextRect = hpTextObj.GetComponent<RectTransform>();
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.sizeDelta = Vector2.zero;

        // 6. Text Stats (ATK & SPD)
        GameObject statsObj = new GameObject("Player_StatsText");
        statsObj.transform.SetParent(panelObj.transform, false);
        Text statsText = statsObj.AddComponent<Text>();
        statsText.text = "ATK: 10  |  SPD: 5.0";
        statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statsText.fontSize = 11;
        statsText.color = new Color(0.8f, 0.8f, 0.9f);

        RectTransform statsRect = statsObj.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0f, 0f);
        statsRect.anchorMax = new Vector2(1f, 0f);
        statsRect.pivot = new Vector2(0f, 0f);
        statsRect.anchoredPosition = new Vector2(15f, 8f);
        statsRect.sizeDelta = new Vector2(-30f, 18f);

        // 7. Otomatis pasangkan script PlayerStats ke Player di scene jika belum ada
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player != null)
        {
            PlayerStats pStats = player.GetComponent<PlayerStats>();
            if (pStats == null) pStats = player.AddComponent<PlayerStats>();

            pStats.healthSlider = slider;
            pStats.hpText = hpText;
            pStats.statsText = statsText;
            pStats.UpdateUI();

            Debug.Log("<color=green>[PlayerStats]</color> Berhasil dipasangkan otomatis ke GameObject Player!");
        }

        Undo.RegisterCreatedObjectUndo(canvasObj, "Build Health UI");
        EditorUtility.DisplayDialog("✅ Berhasil!",
            "Health Panel UI berhasil dibuat di pojok kiri atas layar!\n\n" +
            "• Menampilkan HP Bar merah dinamis.\n" +
            "• Menampilkan angka HP (100 / 100).\n" +
            "• Menampilkan nilai ATK & SPD secara live.\n" +
            "• Otomatis terhubung dengan Player dan Math Gate!",
            "Keren!");
    }
}
