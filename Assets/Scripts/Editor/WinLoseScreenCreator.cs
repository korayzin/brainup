using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEngine.EventSystems;

public class WinLoseScreenCreator : EditorWindow
{
    [MenuItem("Tools/Create Win/Lose Screen")]
    public static void CreateWinLoseScreen()
    {
        // Canvas oluştur veya bul
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // EventSystem oluştur
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        
        // Win/Lose Screen Manager GameObject oluştur
        GameObject winLoseManagerObj = new GameObject("WinLoseScreenManager");
        WinLoseScreenManager manager = winLoseManagerObj.AddComponent<WinLoseScreenManager>();
        
        // Win Panel oluştur
        GameObject winPanel = CreatePanel("WinPanel", canvas.transform, new Color(0f, 1f, 0f, 0.8f)); // Yeşil
        winPanel.SetActive(false); // Başlangıçta gizli
        
        // Win Image oluştur
        GameObject winImageObj = new GameObject("WinImage");
        winImageObj.transform.SetParent(winPanel.transform, false);
        Image winImage = winImageObj.AddComponent<Image>();
        RectTransform winImageRect = winImageObj.GetComponent<RectTransform>();
        winImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        winImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        winImageRect.pivot = new Vector2(0.5f, 0.5f);
        winImageRect.sizeDelta = new Vector2(800f, 600f);
        winImageRect.anchoredPosition = Vector2.zero;
        winImage.color = Color.white;
        
        // Win Text oluştur
        GameObject winTextObj = new GameObject("WinText");
        winTextObj.transform.SetParent(winPanel.transform, false);
        TextMeshProUGUI winText = winTextObj.AddComponent<TextMeshProUGUI>();
        RectTransform winTextRect = winTextObj.GetComponent<RectTransform>();
        winTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        winTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        winTextRect.pivot = new Vector2(0.5f, 0.5f);
        winTextRect.sizeDelta = new Vector2(600f, 100f);
        winTextRect.anchoredPosition = new Vector2(0f, 150f);
        winText.text = "YOU WIN!";
        winText.fontSize = 72f;
        winText.alignment = TextAlignmentOptions.Center;
        winText.fontStyle = FontStyles.Bold;
        winText.color = Color.white;
        
        // Lose Panel oluştur
        GameObject losePanel = CreatePanel("LosePanel", canvas.transform, new Color(1f, 0f, 0f, 0.8f)); // Kırmızı
        losePanel.SetActive(false); // Başlangıçta gizli
        
        // Lose Image oluştur
        GameObject loseImageObj = new GameObject("LoseImage");
        loseImageObj.transform.SetParent(losePanel.transform, false);
        Image loseImage = loseImageObj.AddComponent<Image>();
        RectTransform loseImageRect = loseImageObj.GetComponent<RectTransform>();
        loseImageRect.anchorMin = new Vector2(0.5f, 0.5f);
        loseImageRect.anchorMax = new Vector2(0.5f, 0.5f);
        loseImageRect.pivot = new Vector2(0.5f, 0.5f);
        loseImageRect.sizeDelta = new Vector2(800f, 600f);
        loseImageRect.anchoredPosition = Vector2.zero;
        loseImage.color = Color.white;
        
        // Lose Text oluştur
        GameObject loseTextObj = new GameObject("LoseText");
        loseTextObj.transform.SetParent(losePanel.transform, false);
        TextMeshProUGUI loseText = loseTextObj.AddComponent<TextMeshProUGUI>();
        RectTransform loseTextRect = loseTextObj.GetComponent<RectTransform>();
        loseTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        loseTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        loseTextRect.pivot = new Vector2(0.5f, 0.5f);
        loseTextRect.sizeDelta = new Vector2(600f, 100f);
        loseTextRect.anchoredPosition = new Vector2(0f, 150f);
        loseText.text = "YOU LOSE!";
        loseText.fontSize = 72f;
        loseText.alignment = TextAlignmentOptions.Center;
        loseText.fontStyle = FontStyles.Bold;
        loseText.color = Color.white;
        
        // Restart Butonu (Win Panel için)
        GameObject winRestartButton = CreateRestartButton("WinRestartButton", winPanel.transform, new Vector2(0f, -150f));
        
        // Restart Butonu (Lose Panel için)
        GameObject loseRestartButton = CreateRestartButton("LoseRestartButton", losePanel.transform, new Vector2(0f, -150f));
        
        // Hover animasyonu için EventTrigger ekle (Win Panel)
        AddHoverEventTrigger(winPanel, manager, true);
        
        // Hover animasyonu için EventTrigger ekle (Lose Panel)
        AddHoverEventTrigger(losePanel, manager, false);
        
        // Hover animasyonu için EventTrigger ekle (Win Image)
        if (winImage != null)
        {
            AddImageHoverEventTrigger(winImage.gameObject, manager, true);
        }
        
        // Hover animasyonu için EventTrigger ekle (Lose Image)
        if (loseImage != null)
        {
            AddImageHoverEventTrigger(loseImage.gameObject, manager, false);
        }
        
        // Manager'a referansları ata
        manager.winPanel = winPanel;
        manager.losePanel = losePanel;
        manager.winImage = winImage;
        manager.loseImage = loseImage;
        manager.winText = winText;
        manager.loseText = loseText;
        
        // DreamLogicController'ı bul ve bağla
        DreamLogicController dreamLogicController = FindObjectOfType<DreamLogicController>();
        if (dreamLogicController != null)
        {
            manager.dreamLogicController = dreamLogicController;
            Debug.Log("WinLoseScreenCreator: DreamLogicController bulundu ve bağlandı!");
        }
        else
        {
            Debug.LogWarning("WinLoseScreenCreator: DreamLogicController bulunamadı! Manuel olarak bağlamanız gerekecek.");
        }
        
        // Selection'ı manager'a ayarla
        Selection.activeGameObject = winLoseManagerObj;
        
        Debug.Log("Win/Lose Screen başarıyla oluşturuldu! Panel resimlerini Inspector'dan değiştirebilirsiniz.");
    }
    
    private static GameObject CreatePanel(string name, Transform parent, Color backgroundColor)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        // RectTransform ayarları
        RectTransform rectTransform = panel.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        
        // Image component (arka plan için)
        Image image = panel.AddComponent<Image>();
        image.color = backgroundColor;
        
        return panel;
    }
    
    private static GameObject CreateRestartButton(string name, Transform parent, Vector2 position)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        
        // RectTransform ayarları
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(300f, 80f);
        rectTransform.anchoredPosition = position;
        
        // Image component (buton arka planı)
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f, 1f); // Koyu gri
        
        // Button component
        Button button = buttonObj.AddComponent<Button>();
        
        // Buton text'i
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        buttonText.text = "RESTART";
        buttonText.fontSize = 36f;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        
        return buttonObj;
    }
    
    private static void AddHoverEventTrigger(GameObject panel, WinLoseScreenManager manager, bool isWin)
    {
        if (panel == null || manager == null) return;
        
        EventTrigger trigger = panel.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = panel.AddComponent<EventTrigger>();
        }
        
        // Pointer Enter Event
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            if (isWin)
            {
                manager.OnWinPanelEnter();
            }
            else
            {
                manager.OnLosePanelEnter();
            }
        });
        trigger.triggers.Add(enterEntry);
        
        // Pointer Exit Event
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {
            if (isWin)
            {
                manager.OnWinPanelExit();
            }
            else
            {
                manager.OnLosePanelExit();
            }
        });
        trigger.triggers.Add(exitEntry);
    }
    
    private static void AddImageHoverEventTrigger(GameObject imageObj, WinLoseScreenManager manager, bool isWin)
    {
        if (imageObj == null || manager == null) return;
        
        EventTrigger trigger = imageObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = imageObj.AddComponent<EventTrigger>();
        }
        
        // Pointer Enter Event
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            if (isWin)
            {
                manager.OnWinImageEnter();
            }
            else
            {
                manager.OnLoseImageEnter();
            }
        });
        trigger.triggers.Add(enterEntry);
        
        // Pointer Exit Event
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => {
            if (isWin)
            {
                manager.OnWinImageExit();
            }
            else
            {
                manager.OnLoseImageExit();
            }
        });
        trigger.triggers.Add(exitEntry);
    }
}

