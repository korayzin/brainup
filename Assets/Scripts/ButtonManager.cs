using UnityEngine;
using System.Collections;
using TMPro; // TextMeshPro için

public class ButtonManager : MonoBehaviour
{
    public enum ButtonType
    {
        Caffeine,
        Radiation,
        Lavender,
        Heat,
        Melatonin
    }

    [Header("Buton Tipi")]
    [Tooltip("Bu butonun hangi tipte olduğunu seçin (Dream Logic için)")]
    public ButtonType buttonType = ButtonType.Caffeine;

    [Header("Dream Logic Entegrasyonu")]
    [Tooltip("DreamLogicController referansı (otomatik bulunur veya manuel atanabilir)")]
    public DreamLogicController dreamLogicController;

    [Header("Button Mesh Ayarları")]
    [Tooltip("Buton olarak kullanılacak mesh (bu GameObject veya child)")]
    public Transform buttonMesh;
    
    [Header("Hedef Mesh Ayarları")]
    [Tooltip("Rengi değiştirilecek mesh")]
    public MeshRenderer targetMesh;
    
    [Header("Buton Basma Ayarları")]
    [Tooltip("Butonun ne kadar aşağı ineceği (birim)")]
    public float pressDistance = 0.1f;
    
    [Tooltip("Buton basma/geri dönme süresi (saniye)")]
    public float pressDuration = 0.2f;
    
    [Header("Renk Ayarları")]
    [Tooltip("Tıklamada target mesh'in alacağı renk")]
    public Color targetColor = Color.red;
    
    [Header("Basma Hakkı Sistemi")]
    [Tooltip("Her butonun kaç kere basılma hakkı var?")]
    public int maxPressCount = 3;
    
    [Header("UI Feedback")]
    [Tooltip("Buton basıldığında gösterilecek UI Text (Unity UI Text - Inspector'dan atanabilir)")]
    public UnityEngine.UI.Text feedbackText;
    
    [Tooltip("Buton basıldığında gösterilecek TMP Text (TextMeshPro - Inspector'dan atanabilir)")]
    public TextMeshProUGUI feedbackTextTMP;
    
    [Tooltip("Buton basıldığında gösterilecek mesaj (Inspector'dan özelleştirilebilir, boşsa otomatik ayarlanır)")]
    public string feedbackMessage = "";
    
    // Özel değişkenler
    private Vector3 originalButtonPosition;
    private Color originalTargetColor;
    private bool isPressed = false;
    private bool isButtonActive = false; // Butonun aktif/pasif durumu
    private Coroutine pressCoroutine;
    private int remainingPressCount; // Kalan basma hakkı
    private bool isButtonUsed = false; // Buton kullanıldı mı? (3 kere basıldı mı?)

    void Start()
    {
        // Eğer buttonMesh atanmamışsa, bu GameObject'i kullan
        if (buttonMesh == null)
        {
            buttonMesh = transform;
        }

        // Orijinal pozisyonu kaydet
        originalButtonPosition = buttonMesh.localPosition;

        // Target mesh'in orijinal rengini kaydet
        if (targetMesh != null && targetMesh.material != null)
        {
            originalTargetColor = targetMesh.material.color;
        }

        // DreamLogicController'ı otomatik bul (eğer atanmamışsa)
        if (dreamLogicController == null)
        {
            dreamLogicController = FindObjectOfType<DreamLogicController>();
            if (dreamLogicController == null)
            {
                Debug.LogWarning("ButtonManager: DreamLogicController bulunamadı! Butonlar çalışmayabilir.");
            }
        }

        // Collider kontrolü - OnMouseDown için gerekli
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("ButtonManager: OnMouseDown çalışması için bu GameObject'te bir Collider bileşeni olmalı!");
        }
        
        // Basma hakkını başlat
        remainingPressCount = maxPressCount;
        isButtonUsed = false;
        
        // Eğer feedback mesajı boşsa, buton tipine göre otomatik ayarla
        if (string.IsNullOrEmpty(feedbackMessage))
        {
            switch (buttonType)
            {
                case ButtonType.Caffeine:
                    feedbackMessage = "Caffeine Up!";
                    break;
                case ButtonType.Radiation:
                    feedbackMessage = "Radiation Active!";
                    break;
                case ButtonType.Lavender:
                    feedbackMessage = "Lavender Applied!";
                    break;
                case ButtonType.Heat:
                    feedbackMessage = "Heat On!";
                    break;
                case ButtonType.Melatonin:
                    feedbackMessage = "Melatonin Injected!";
                    break;
            }
        }
    }

    // Mouse ile tıklama algılama (OnMouseDown için Collider gerekli)
    void OnMouseDown()
    {
        // Eğer buton kullanıldıysa (3 kere basıldıysa) veya basma hakkı yoksa çalışmasın
        if (isButtonUsed || remainingPressCount <= 0)
        {
            return;
        }
        
        // Eğer bu buton son basılan butonsa, tekrar basılamaz (ama ilk basışta kontrol yapma)
        if (dreamLogicController != null && 
            dreamLogicController.HasAnyButtonBeenPressed() && 
            dreamLogicController.GetLastPressedButton() == buttonType)
        {
            return; // Aynı butona üst üste basılamaz
        }
        
        if (!isPressed && pressCoroutine == null)
        {
            // Basma hakkını azalt
            remainingPressCount--;
            
            // Buton durumunu aktif et (artık toggle değil, sadece aktif)
            isButtonActive = true;
            
            // Eğer tüm haklar bittiyse butonu kullanıldı olarak işaretle
            if (remainingPressCount <= 0)
            {
                isButtonUsed = true;
                Debug.Log($"Buton {buttonType} kullanıldı! Kalan hak: {remainingPressCount}");
            }
            else
            {
                Debug.Log($"Buton {buttonType} basıldı. Kalan hak: {remainingPressCount}/{maxPressCount}");
            }
            
            pressCoroutine = StartCoroutine(PressButton());
            
            // DreamLogicController'a durumu bildir
            NotifyDreamLogicController();
            
            // UI Feedback göster
            ShowFeedback();
            
            // DreamLogicController'a buton basıldığını bildir (oyun kontrolü için)
            if (dreamLogicController != null)
            {
                dreamLogicController.OnButtonPressed(buttonType);
            }
        }
    }

    // Buton basma animasyonu
    private IEnumerator PressButton()
    {
        isPressed = true;

        // Butonu aşağı indir
        Vector3 pressedPosition = originalButtonPosition - new Vector3(0, pressDistance, 0);
        float elapsedTime = 0f;

        // Aşağı inme
        while (elapsedTime < pressDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / pressDuration;
            buttonMesh.localPosition = Vector3.Lerp(originalButtonPosition, pressedPosition, t);
            yield return null;
        }

        // Target mesh'in rengini değiştir (buton aktifse - artık her zaman aktif kalır)
        if (targetMesh != null && targetMesh.material != null)
        {
            targetMesh.material.color = targetColor; // Aktif olduğunda renk değişir ve kalır
        }

        // Kısa bir bekleme (basılı kalma hissi)
        yield return new WaitForSeconds(0.1f);

        // Butonu geri yukarı çıkar
        elapsedTime = 0f;
        while (elapsedTime < pressDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / pressDuration;
            buttonMesh.localPosition = Vector3.Lerp(pressedPosition, originalButtonPosition, t);
            yield return null;
        }

        buttonMesh.localPosition = originalButtonPosition;
        isPressed = false;
        pressCoroutine = null;
    }

    // Manuel olarak buton basma fonksiyonu (kod ile çağrılabilir)
    public void PressButtonManually()
    {
        if (!isPressed && pressCoroutine == null)
        {
            pressCoroutine = StartCoroutine(PressButton());
        }
    }

    // Target mesh'in rengini sıfırlama
    public void ResetTargetColor()
    {
        if (targetMesh != null && targetMesh.material != null)
        {
            targetMesh.material.color = originalTargetColor;
        }
    }

    /// <summary>
    /// DreamLogicController'a buton durumunu bildir
    /// Yeni sistem: Her basışta değeri artırır (birikimli)
    /// Lavanta ve Melatonin yarım güçte (0.7), diğerleri tam güçte (1.0)
    /// </summary>
    private void NotifyDreamLogicController()
    {
        if (dreamLogicController != null)
        {
            // Her basışta değeri artır (birikimli)
            float incrementValue = 0f;
            switch (buttonType)
            {
                case ButtonType.Caffeine:
                    incrementValue = 1f / maxPressCount; // Her basışta 1/3 artar
                    dreamLogicController.caffeineSmell += incrementValue;
                    dreamLogicController.caffeineSmell = Mathf.Clamp01(dreamLogicController.caffeineSmell);
                    break;
                case ButtonType.Radiation:
                    incrementValue = 1f / maxPressCount;
                    dreamLogicController.emf += incrementValue;
                    dreamLogicController.emf = Mathf.Clamp01(dreamLogicController.emf);
                    break;
                case ButtonType.Lavender:
                    // Lavanta %70 güçte (0.7) - kombinasyonları zorlaştırır
                    incrementValue = 0.7f / maxPressCount; // Her basışta 0.7/3 artar
                    dreamLogicController.lavenderSmell += incrementValue;
                    dreamLogicController.lavenderSmell = Mathf.Clamp01(dreamLogicController.lavenderSmell);
                    break;
                case ButtonType.Heat:
                    incrementValue = 1f / maxPressCount;
                    dreamLogicController.warmAir += incrementValue;
                    dreamLogicController.warmAir = Mathf.Clamp01(dreamLogicController.warmAir);
                    break;
                case ButtonType.Melatonin:
                    // Melatonin %70 güçte (0.7) - kombinasyonları zorlaştırır
                    incrementValue = 0.7f / maxPressCount; // Her basışta 0.7/3 artar
                    dreamLogicController.melatonin += incrementValue;
                    dreamLogicController.melatonin = Mathf.Clamp01(dreamLogicController.melatonin);
                    break;
            }
        }
    }

    /// <summary>
    /// Buton durumunu manuel olarak ayarla
    /// </summary>
    public void SetButtonActive(bool active)
    {
        if (isButtonActive != active)
        {
            isButtonActive = active;
            NotifyDreamLogicController();
            
            // Görsel geri bildirim
            if (targetMesh != null && targetMesh.material != null)
            {
                targetMesh.material.color = isButtonActive ? targetColor : originalTargetColor;
            }
        }
    }

    /// <summary>
    /// Butonun aktif olup olmadığını döndür
    /// </summary>
    public bool IsButtonActive()
    {
        return isButtonActive;
    }

    /// <summary>
    /// Buton tipini döndür
    /// </summary>
    public ButtonType GetButtonType()
    {
        return buttonType;
    }
    
    /// <summary>
    /// UI Feedback göster
    /// </summary>
    private void ShowFeedback()
    {
        // Unity UI Text kullanılıyorsa
        if (feedbackText != null)
        {
            feedbackText.text = feedbackMessage;
            feedbackText.gameObject.SetActive(true);
            
            // 2 saniye sonra gizle
            StartCoroutine(HideFeedbackAfterDelay(2f, false));
        }
        // TextMeshPro kullanılıyorsa
        else if (feedbackTextTMP != null)
        {
            feedbackTextTMP.text = feedbackMessage;
            feedbackTextTMP.gameObject.SetActive(true);
            
            // 2 saniye sonra gizle
            StartCoroutine(HideFeedbackAfterDelay(2f, true));
        }
    }
    
    /// <summary>
    /// Feedback'i belirli bir süre sonra gizle
    /// </summary>
    private IEnumerator HideFeedbackAfterDelay(float delay, bool isTMP)
    {
        yield return new WaitForSeconds(delay);
        if (isTMP)
        {
            if (feedbackTextTMP != null)
            {
                feedbackTextTMP.gameObject.SetActive(false);
            }
        }
        else
        {
            if (feedbackText != null)
            {
                feedbackText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Kalan basma hakkını döndür
    /// </summary>
    public int GetRemainingPressCount()
    {
        return remainingPressCount;
    }
    
    /// <summary>
    /// Butonun kullanıldığını kontrol et
    /// </summary>
    public bool IsButtonUsed()
    {
        return isButtonUsed;
    }
    
    /// <summary>
    /// Buton haklarını sıfırla (oyun restart için)
    /// </summary>
    public void ResetButton()
    {
        remainingPressCount = maxPressCount;
        isButtonUsed = false;
        isButtonActive = false;
        
        // DreamLogicController'a input değerini sıfırla
        if (dreamLogicController != null)
        {
            switch (buttonType)
            {
                case ButtonType.Caffeine:
                    dreamLogicController.caffeineSmell = 0f;
                    break;
                case ButtonType.Radiation:
                    dreamLogicController.emf = 0f;
                    break;
                case ButtonType.Lavender:
                    dreamLogicController.lavenderSmell = 0f;
                    break;
                case ButtonType.Heat:
                    dreamLogicController.warmAir = 0f;
                    break;
                case ButtonType.Melatonin:
                    dreamLogicController.melatonin = 0f;
                    break;
            }
        }
        
        // Görsel geri bildirim
        if (targetMesh != null && targetMesh.material != null)
        {
            targetMesh.material.color = originalTargetColor;
        }
    }
}

