using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Shrink seviyesini gösteren UI oluşturma tool'u
/// Tools > Create Shrink UI ile erişilebilir
/// </summary>
public class ShrinkUICreator : EditorWindow
{
    [MenuItem("Tools/Create Shrink UI")]
    public static void ShowWindow()
    {
        GetWindow<ShrinkUICreator>("Shrink UI Creator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Shrink UI Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("Bu tool, shrink seviyesini realtime olarak gösteren şık bir UI oluşturur. " +
                                "UI beynin sol üstünde konumlandırılır.", MessageType.Info);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Shrink UI", GUILayout.Height(40)))
        {
            CreateShrinkUI();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Remove Existing Shrink UI", GUILayout.Height(30)))
        {
            RemoveExistingShrinkUI();
        }
    }
    
    /// <summary>
    /// Shrink UI'ı oluştur
    /// </summary>
    private void CreateShrinkUI()
    {
        // Mevcut UI'ı kontrol et
        GameObject existingUI = GameObject.Find("ShrinkUI");
        if (existingUI != null)
        {
            if (EditorUtility.DisplayDialog("Shrink UI Mevcut", 
                "Zaten bir Shrink UI var. Yeniden oluşturmak ister misiniz?", 
                "Evet", "Hayır"))
            {
                DestroyImmediate(existingUI);
            }
            else
            {
                return;
            }
        }
        
        // Canvas oluştur (eğer yoksa)
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Üstte görünsün
            
            // Canvas Scaler ekle
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Graphic Raycaster ekle
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // EventSystem kontrolü
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        
        // ShrinkUI container oluştur
        GameObject shrinkUIContainer = new GameObject("ShrinkUI");
        shrinkUIContainer.transform.SetParent(canvas.transform, false);
        
        RectTransform containerRect = shrinkUIContainer.AddComponent<RectTransform>();
        // Dinamik pozisyonlama için anchor ve pivot ayarları
        containerRect.anchorMin = new Vector2(0f, 1f); // Sol üst
        containerRect.anchorMax = new Vector2(0f, 1f); // Sol üst
        containerRect.pivot = new Vector2(0f, 1f); // Sol üst pivot
        // Başlangıç pozisyonu (dinamik olarak güncellenecek)
        containerRect.anchoredPosition = new Vector2(50f, -50f);
        containerRect.sizeDelta = new Vector2(300f, 120f);
        
        // Arka plan paneli
        GameObject backgroundPanel = new GameObject("BackgroundPanel");
        backgroundPanel.transform.SetParent(shrinkUIContainer.transform, false);
        
        Image bgImage = backgroundPanel.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f); // Yarı saydam siyah
        
        RectTransform bgRect = backgroundPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        bgRect.anchoredPosition = Vector2.zero;
        
        // Kenarlık için Outline ekle
        Outline outline = backgroundPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.7f, 1f, 0.8f); // Mavi kenarlık
        outline.effectDistance = new Vector2(2f, -2f);
        
        // Başlık text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(shrinkUIContainer.transform, false);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "BRAIN SIZE";
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.8f, 0.9f, 1f, 1f); // Açık mavi
        titleText.alignment = TextAlignmentOptions.Left;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(15f, -15f);
        titleRect.sizeDelta = new Vector2(-30f, 30f);
        
        // Shrink değeri text
        GameObject valueObj = new GameObject("ShrinkValue");
        valueObj.transform.SetParent(shrinkUIContainer.transform, false);
        
        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = "0.900";
        valueText.fontSize = 32;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = new Color(1f, 0.7f, 0.3f, 1f); // Turuncu
        valueText.alignment = TextAlignmentOptions.Left;
        
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(1f, 0f);
        valueRect.pivot = new Vector2(0f, 0f);
        valueRect.anchoredPosition = new Vector2(15f, 15f);
        valueRect.sizeDelta = new Vector2(-30f, 50f);
        
        // Progress bar background
        GameObject progressBarBg = new GameObject("ProgressBarBackground");
        progressBarBg.transform.SetParent(shrinkUIContainer.transform, false);
        
        Image progressBgImage = progressBarBg.AddComponent<Image>();
        progressBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Koyu gri
        
        RectTransform progressBgRect = progressBarBg.GetComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0f, 0f);
        progressBgRect.anchorMax = new Vector2(1f, 0f);
        progressBgRect.pivot = new Vector2(0f, 0f);
        progressBgRect.anchoredPosition = new Vector2(15f, 70f);
        progressBgRect.sizeDelta = new Vector2(-30f, 8f);
        
        // Progress bar fill
        GameObject progressBarFill = new GameObject("ProgressBarFill");
        progressBarFill.transform.SetParent(progressBarBg.transform, false);
        
        Image progressFillImage = progressBarFill.AddComponent<Image>();
        progressFillImage.color = new Color(0.3f, 0.7f, 1f, 1f); // Mavi
        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        
        RectTransform progressFillRect = progressBarFill.GetComponent<RectTransform>();
        progressFillRect.anchorMin = Vector2.zero;
        progressFillRect.anchorMax = Vector2.one;
        progressFillRect.sizeDelta = Vector2.zero;
        progressFillRect.anchoredPosition = Vector2.zero;
        
        // ShrinkUIManager script ekle
        ShrinkUIManager manager = shrinkUIContainer.AddComponent<ShrinkUIManager>();
        manager.shrinkValueText = valueText;
        manager.progressBarFill = progressFillImage;
        
        // DreamLogicController'ı bul ve bağla
        DreamLogicController dreamLogic = FindObjectOfType<DreamLogicController>();
        if (dreamLogic != null)
        {
            manager.dreamLogicController = dreamLogic;
            EditorUtility.SetDirty(manager);
        }
        else
        {
            Debug.LogWarning("ShrinkUICreator: DreamLogicController bulunamadı! Lütfen sahnede bir DreamLogicController olduğundan emin olun.");
        }
        
        // Seçili hale getir
        Selection.activeGameObject = shrinkUIContainer;
        
        Debug.Log("Shrink UI başarıyla oluşturuldu! Sol üstte görünecektir.");
    }
    
    /// <summary>
    /// Mevcut Shrink UI'ı kaldır
    /// </summary>
    private void RemoveExistingShrinkUI()
    {
        GameObject existingUI = GameObject.Find("ShrinkUI");
        if (existingUI != null)
        {
            if (EditorUtility.DisplayDialog("Shrink UI Kaldır", 
                "Shrink UI'ı kaldırmak istediğinizden emin misiniz?", 
                "Evet", "Hayır"))
            {
                DestroyImmediate(existingUI);
                Debug.Log("Shrink UI kaldırıldı.");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Shrink UI Bulunamadı", 
                "Sahnede Shrink UI bulunamadı.", 
                "Tamam");
        }
    }
}

