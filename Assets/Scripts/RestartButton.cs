using UnityEngine;
using System.Collections;

/// <summary>
/// Restart butonu: Oyunu baştan başlatır, tüm değişiklikleri sıfırlar
/// </summary>
public class RestartButton : MonoBehaviour
{
    [Header("Dream Logic Entegrasyonu")]
    [Tooltip("DreamLogicController referansı (otomatik bulunur veya manuel atanabilir)")]
    public DreamLogicController dreamLogicController;

    [Header("Buton Mesh Ayarları")]
    [Tooltip("Buton olarak kullanılacak mesh (bu GameObject veya child)")]
    public Transform buttonMesh;
    
    [Header("Buton Basma Ayarları")]
    [Tooltip("Butonun ne kadar aşağı ineceği (birim)")]
    public float pressDistance = 0.1f;
    
    [Tooltip("Buton basma/geri dönme süresi (saniye)")]
    public float pressDuration = 0.2f;
    
    [Header("Restart Ayarları")]
    [Tooltip("Restart işlemi buton basıldıktan ne kadar süre sonra yapılsın? (animasyon bitince)")]
    public float restartDelay = 0.3f;
    
    // Özel değişkenler
    private Vector3 originalButtonPosition;
    private bool isPressed = false;
    private Coroutine pressCoroutine;
    private bool isRestarting = false; // Restart işlemi devam ediyor mu?

    void Start()
    {
        // Eğer buttonMesh atanmamışsa, bu GameObject'i kullan
        if (buttonMesh == null)
        {
            buttonMesh = transform;
        }

        // Orijinal pozisyonu kaydet
        originalButtonPosition = buttonMesh.localPosition;

        // DreamLogicController'ı otomatik bul (eğer atanmamışsa)
        if (dreamLogicController == null)
        {
            dreamLogicController = FindObjectOfType<DreamLogicController>();
            if (dreamLogicController == null)
            {
                Debug.LogWarning("RestartButton: DreamLogicController bulunamadı! Restart çalışmayabilir.");
            }
        }

        // Collider kontrolü - OnMouseDown için gerekli
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("RestartButton: OnMouseDown çalışması için bu GameObject'te bir Collider bileşeni olmalı!");
        }
    }

    // Mouse ile tıklama algılama (OnMouseDown için Collider gerekli)
    void OnMouseDown()
    {
        // Eğer zaten restart işlemi devam ediyorsa, tekrar basılamaz
        if (isRestarting)
        {
            return;
        }
        
        if (!isPressed && pressCoroutine == null)
        {
            pressCoroutine = StartCoroutine(PressButton());
        }
    }

    // Buton basma animasyonu
    private IEnumerator PressButton()
    {
        isPressed = true;
        isRestarting = true; // Restart işlemi başladı

        // Butonu Z ekseninde içeri girer
        Vector3 pressedPosition = originalButtonPosition - buttonMesh.forward * pressDistance;
        float elapsed = 0f;

        // Aşağı inme animasyonu
        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressDuration;
            // Smooth step kullanarak daha yumuşak animasyon
            t = t * t * (3f - 2f * t);
            buttonMesh.localPosition = Vector3.Lerp(originalButtonPosition, pressedPosition, t);
            yield return null;
        }

        buttonMesh.localPosition = pressedPosition;

        // Kısa bir bekleme (buton basılı kalır)
        yield return new WaitForSeconds(restartDelay);

        // Restart işlemini başlat
        if (dreamLogicController != null)
        {
            Debug.Log("RestartButton: Oyun yeniden başlatılıyor...");
            dreamLogicController.StartNewGame();
        }
        else
        {
            Debug.LogError("RestartButton: DreamLogicController bulunamadı! Restart yapılamadı.");
        }

        // Butonu geri getir
        elapsed = 0f;
        while (elapsed < pressDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pressDuration;
            // Smooth step kullanarak daha yumuşak animasyon
            t = t * t * (3f - 2f * t);
            buttonMesh.localPosition = Vector3.Lerp(pressedPosition, originalButtonPosition, t);
            yield return null;
        }

        buttonMesh.localPosition = originalButtonPosition;
        isPressed = false;
        isRestarting = false; // Restart işlemi bitti
        pressCoroutine = null;
    }

    /// <summary>
    /// Restart butonunu manuel olarak tetikle (kod ile)
    /// </summary>
    public void TriggerRestart()
    {
        if (!isRestarting && pressCoroutine == null)
        {
            pressCoroutine = StartCoroutine(PressButton());
        }
    }
}

