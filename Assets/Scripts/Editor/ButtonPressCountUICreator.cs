using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Butonların üstüne basım hakkı UI Text'lerini oluşturma tool'u
/// Tools > Create Button Press Count UI ile erişilebilir
/// </summary>
public class ButtonPressCountUICreator : EditorWindow
{
    [MenuItem("Tools/Create Button Press Count UI")]
    public static void ShowWindow()
    {
        GetWindow<ButtonPressCountUICreator>("Button Press Count UI Creator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Button Press Count UI Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox("Bu tool, tüm butonların üstüne basım hakkını gösteren 3D World Space UI Text'ler oluşturur. " +
                                "Her buton için 3'ten başlayıp her basışta azalır. UI'lar perspektif kamera ile görünür ve " +
                                "butonların üstünde 3D dünyada konumlandırılır.", MessageType.Info);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Create Button Press Count UI", GUILayout.Height(40)))
        {
            CreateButtonPressCountUI();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Remove Existing Button Press Count UI", GUILayout.Height(30)))
        {
            RemoveExistingButtonPressCountUI();
        }
    }
    
    /// <summary>
    /// Buton basım hakkı UI'larını oluştur (3D World Space)
    /// </summary>
    private void CreateButtonPressCountUI()
    {
        // World Space Canvas oluştur (eğer yoksa)
        Canvas worldCanvas = null;
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        foreach (Canvas c in allCanvases)
        {
            if (c.renderMode == RenderMode.WorldSpace)
            {
                worldCanvas = c;
                break;
            }
        }
        
        if (worldCanvas == null)
        {
            GameObject canvasObj = new GameObject("WorldSpaceCanvas");
            worldCanvas = canvasObj.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            
            // Canvas boyutunu ayarla (3D dünyada görünür olması için)
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(0.1f, 0.1f); // Küçük boyut (3D dünyada)
            canvasRect.localScale = Vector3.one * 0.01f; // Scale ile boyutu ayarla
            
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
        
        // Tüm ButtonManager'ları bul
        ButtonManager[] allButtons = FindObjectsOfType<ButtonManager>();
        
        if (allButtons.Length == 0)
        {
            EditorUtility.DisplayDialog("Buton Bulunamadı", 
                "Sahnede ButtonManager component'ine sahip GameObject bulunamadı.", 
                "Tamam");
            return;
        }
        
        // Main Camera'yı bul
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        if (mainCamera == null)
        {
            EditorUtility.DisplayDialog("Kamera Bulunamadı", 
                "Sahnede Camera bulunamadı. UI pozisyonları hesaplanamaz.", 
                "Tamam");
            return;
        }
        
        int createdCount = 0;
        
        // Her buton için UI Text oluştur
        foreach (ButtonManager buttonManager in allButtons)
        {
            // Sadece oyun butonlarını işle
            if (buttonManager.buttonType == ButtonManager.ButtonType.Caffeine ||
                buttonManager.buttonType == ButtonManager.ButtonType.Radiation ||
                buttonManager.buttonType == ButtonManager.ButtonType.Lavender ||
                buttonManager.buttonType == ButtonManager.ButtonType.Heat ||
                buttonManager.buttonType == ButtonManager.ButtonType.Melatonin)
            {
                // Mevcut UI Text'i kontrol et
                if (buttonManager.pressCountText != null || buttonManager.pressCountTextLegacy != null)
                {
                    Debug.Log($"Buton {buttonManager.buttonType} için zaten bir UI Text var. Atlanıyor...");
                    continue;
                }
                
                // Butonun pozisyonunu al
                Transform buttonTransform = buttonManager.buttonMesh != null ? buttonManager.buttonMesh : buttonManager.transform;
                Vector3 buttonWorldPos = buttonTransform.position;
                
                // Butonun üstüne offset ekle (3D dünyada)
                Vector3 uiWorldPos = buttonWorldPos + Vector3.up * 0.3f; // Butonun 0.3 birim üstü
                
                // UI Text GameObject'i oluştur (World Space Canvas'ın child'ı olarak)
                GameObject pressCountObj = new GameObject($"PressCountUI_{buttonManager.buttonType}");
                pressCountObj.transform.SetParent(worldCanvas.transform, false);
                
                // World pozisyonunu ayarla
                pressCountObj.transform.position = uiWorldPos;
                
                // Billboard script'i ekle (kameraya sürekli bakması için)
                UIBillboard billboard = pressCountObj.AddComponent<UIBillboard>();
                if (mainCamera != null)
                {
                    billboard.targetCamera = mainCamera;
                }
                
                // RectTransform ayarla
                RectTransform rectTransform = pressCountObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.sizeDelta = new Vector2(0.5f, 0.5f); // 3D dünyada görünür boyut
                rectTransform.localScale = Vector3.one * 0.01f; // Scale ile boyutu ayarla
                
                // TextMeshProUGUI ekle
                TextMeshProUGUI textMesh = pressCountObj.AddComponent<TextMeshProUGUI>();
                textMesh.text = buttonManager.maxPressCount.ToString();
                textMesh.fontSize = 200f; // Büyük font (3D dünyada görünür olması için)
                textMesh.fontStyle = FontStyles.Bold;
                textMesh.color = Color.white;
                textMesh.alignment = TextAlignmentOptions.Center;
                
                // Text ayarları
                textMesh.enableWordWrapping = false;
                textMesh.overflowMode = TextOverflowModes.Overflow;
                textMesh.enableAutoSizing = false;
                
                // ButtonManager'a hem TextMeshProUGUI hem de Transform referansını atan
                buttonManager.pressCountText = textMesh;
                buttonManager.pressCountUITransform = pressCountObj.transform;
                
                EditorUtility.SetDirty(buttonManager);
                
                createdCount++;
                Debug.Log($"Buton {buttonManager.buttonType} için Press Count UI oluşturuldu. World Position: {pressCountObj.transform.position}, Transform Inspector'dan ayarlanabilir.");
            }
        }
        
        if (createdCount > 0)
        {
            Debug.Log($"Toplam {createdCount} buton için Press Count UI oluşturuldu!");
            EditorUtility.DisplayDialog("Başarılı", 
                $"{createdCount} buton için Press Count UI oluşturuldu! UI'lar butonların üstünde 3D dünyada konumlandırıldı.", 
                "Tamam");
        }
        else
        {
            EditorUtility.DisplayDialog("Bilgi", 
                "Tüm butonlar için zaten UI Text mevcut veya uygun buton bulunamadı.", 
                "Tamam");
        }
    }
    
    /// <summary>
    /// Mevcut Button Press Count UI'ları kaldır
    /// </summary>
    private void RemoveExistingButtonPressCountUI()
    {
        // Tüm ButtonManager'ları bul
        ButtonManager[] allButtons = FindObjectsOfType<ButtonManager>();
        
        int removedCount = 0;
        
        foreach (ButtonManager buttonManager in allButtons)
        {
            // TextMeshProUGUI referansını temizle ve GameObject'i sil
            if (buttonManager.pressCountText != null)
            {
                GameObject textObj = buttonManager.pressCountText.gameObject;
                buttonManager.pressCountText = null;
                DestroyImmediate(textObj);
                EditorUtility.SetDirty(buttonManager);
                removedCount++;
            }
            
            // Unity UI Text referansını temizle ve GameObject'i sil
            if (buttonManager.pressCountTextLegacy != null)
            {
                GameObject textObj = buttonManager.pressCountTextLegacy.gameObject;
                buttonManager.pressCountTextLegacy = null;
                DestroyImmediate(textObj);
                EditorUtility.SetDirty(buttonManager);
                removedCount++;
            }
            
            // Transform referansını temizle
            if (buttonManager.pressCountUITransform != null)
            {
                buttonManager.pressCountUITransform = null;
                EditorUtility.SetDirty(buttonManager);
            }
        }
        
        if (removedCount > 0)
        {
            Debug.Log($"{removedCount} Press Count UI kaldırıldı.");
            EditorUtility.DisplayDialog("Başarılı", 
                $"{removedCount} Press Count UI kaldırıldı!", 
                "Tamam");
        }
        else
        {
            EditorUtility.DisplayDialog("Bilgi", 
                "Kaldırılacak Press Count UI bulunamadı.", 
                "Tamam");
        }
    }
}

