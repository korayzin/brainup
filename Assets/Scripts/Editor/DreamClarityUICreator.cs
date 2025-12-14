using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Dream Clarity (Uyku Netliği) seviyesini gösteren UI oluşturma tool'u
/// Tools > Create Dream Clarity UI ile erişilebilir
/// </summary>
public class DreamClarityUICreator : EditorWindow
{
    [MenuItem("Tools/Create Dream Clarity UI")]
    public static void ShowWindow()
    {
        GetWindow<DreamClarityUICreator>("Dream Clarity UI Creator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Dream Clarity UI Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("Bu tool, uyku netliği (dream clarity) seviyesini realtime olarak gösteren şık bir UI oluşturur. " +
                                "UI dream küresinin yakınında konumlandırılır.", MessageType.Info);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Dream Clarity UI", GUILayout.Height(40)))
        {
            CreateDreamClarityUI();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Remove Existing Dream Clarity UI", GUILayout.Height(30)))
        {
            RemoveExistingDreamClarityUI();
        }
    }
    
    /// <summary>
    /// Dream Clarity UI'ı oluştur
    /// </summary>
    private void CreateDreamClarityUI()
    {
        // Mevcut UI'ı kontrol et
        GameObject existingUI = GameObject.Find("DreamClarityUI");
        if (existingUI != null)
        {
            if (EditorUtility.DisplayDialog("Dream Clarity UI Mevcut", 
                "Zaten bir Dream Clarity UI var. Yeniden oluşturmak ister misiniz?", 
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
        
        // DreamClarityUI container oluştur
        GameObject clarityUIContainer = new GameObject("DreamClarityUI");
        clarityUIContainer.transform.SetParent(canvas.transform, false);
        
        RectTransform containerRect = clarityUIContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(1f, 0.5f); // Sağ orta
        containerRect.anchorMax = new Vector2(1f, 0.5f); // Sağ orta
        containerRect.pivot = new Vector2(1f, 0.5f); // Sağ orta pivot
        containerRect.anchoredPosition = new Vector2(-50f, 0f); // Sağdan 50px offset
        containerRect.sizeDelta = new Vector2(300f, 80f);
        
        // Arka plan paneli
        GameObject backgroundPanel = new GameObject("BackgroundPanel");
        backgroundPanel.transform.SetParent(clarityUIContainer.transform, false);
        
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
        titleObj.transform.SetParent(clarityUIContainer.transform, false);
        
        TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "DREAM CLARITY";
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = new Color(0.8f, 0.9f, 1f, 1f); // Açık mavi
        titleText.alignment = TextAlignmentOptions.Right;
        
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(1f, 1f);
        titleRect.anchoredPosition = new Vector2(-15f, -10f);
        titleRect.sizeDelta = new Vector2(-30f, 25f);
        
        // Clarity değeri text
        GameObject valueObj = new GameObject("ClarityValue");
        valueObj.transform.SetParent(clarityUIContainer.transform, false);
        
        TextMeshProUGUI valueText = valueObj.AddComponent<TextMeshProUGUI>();
        valueText.text = "0%";
        valueText.fontSize = 24;
        valueText.fontStyle = FontStyles.Bold;
        valueText.color = new Color(0.3f, 0.7f, 1f, 1f); // Mavi
        valueText.alignment = TextAlignmentOptions.Right;
        
        RectTransform valueRect = valueObj.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0f, 0f);
        valueRect.anchorMax = new Vector2(1f, 0f);
        valueRect.pivot = new Vector2(1f, 0f);
        valueRect.anchoredPosition = new Vector2(-15f, 10f);
        valueRect.sizeDelta = new Vector2(-30f, 30f);
        
        // Progress bar background (soldan sağa)
        GameObject progressBarBg = new GameObject("ProgressBarBackground");
        progressBarBg.transform.SetParent(clarityUIContainer.transform, false);
        
        Image progressBgImage = progressBarBg.AddComponent<Image>();
        progressBgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f); // Koyu gri
        
        RectTransform progressBgRect = progressBarBg.GetComponent<RectTransform>();
        progressBgRect.anchorMin = new Vector2(0f, 0.5f);
        progressBgRect.anchorMax = new Vector2(1f, 0.5f);
        progressBgRect.pivot = new Vector2(0f, 0.5f);
        progressBgRect.anchoredPosition = new Vector2(15f, 0f);
        progressBgRect.sizeDelta = new Vector2(-30f, 8f);
        
        // Progress bar fill (soldan sağa dolacak)
        GameObject progressBarFill = new GameObject("ProgressBarFill");
        progressBarFill.transform.SetParent(progressBarBg.transform, false);
        
        Image progressFillImage = progressBarFill.AddComponent<Image>();
        progressFillImage.color = new Color(0.3f, 0.7f, 1f, 1f); // Mavi
        progressFillImage.type = Image.Type.Filled;
        progressFillImage.fillMethod = Image.FillMethod.Horizontal;
        progressFillImage.fillOrigin = 0; // Soldan başla
        
        RectTransform progressFillRect = progressBarFill.GetComponent<RectTransform>();
        progressFillRect.anchorMin = new Vector2(0f, 0f);
        progressFillRect.anchorMax = new Vector2(1f, 1f);
        progressFillRect.sizeDelta = Vector2.zero;
        progressFillRect.anchoredPosition = Vector2.zero;
        
        // DreamClarityUIManager script ekle
        DreamClarityUIManager manager = clarityUIContainer.AddComponent<DreamClarityUIManager>();
        manager.clarityValueText = valueText;
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
            Debug.LogWarning("DreamClarityUICreator: DreamLogicController bulunamadı! Lütfen sahnede bir DreamLogicController olduğundan emin olun.");
        }
        
        // Seçili hale getir
        Selection.activeGameObject = clarityUIContainer;
        
        Debug.Log("Dream Clarity UI başarıyla oluşturuldu! Dream küresinin yakınında görünecektir.");
    }
    
    /// <summary>
    /// Mevcut Dream Clarity UI'ı kaldır
    /// </summary>
    private void RemoveExistingDreamClarityUI()
    {
        GameObject existingUI = GameObject.Find("DreamClarityUI");
        if (existingUI != null)
        {
            if (EditorUtility.DisplayDialog("Dream Clarity UI Kaldır", 
                "Dream Clarity UI'ı kaldırmak istediğinizden emin misiniz?", 
                "Evet", "Hayır"))
            {
                DestroyImmediate(existingUI);
                Debug.Log("Dream Clarity UI kaldırıldı.");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Dream Clarity UI Bulunamadı", 
                "Sahnede Dream Clarity UI bulunamadı.", 
                "Tamam");
        }
    }
}

