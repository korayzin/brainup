using UnityEngine;
using System.Collections;

public class ButtonManager : MonoBehaviour
{
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
    
    // Özel değişkenler
    private Vector3 originalButtonPosition;
    private Color originalTargetColor;
    private bool isPressed = false;
    private Coroutine pressCoroutine;

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

        // Collider kontrolü - OnMouseDown için gerekli
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("ButtonManager: OnMouseDown çalışması için bu GameObject'te bir Collider bileşeni olmalı!");
        }
    }

    // Mouse ile tıklama algılama (OnMouseDown için Collider gerekli)
    void OnMouseDown()
    {
        if (!isPressed && pressCoroutine == null)
        {
            pressCoroutine = StartCoroutine(PressButton());
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

        // Target mesh'in rengini değiştir
        if (targetMesh != null && targetMesh.material != null)
        {
            targetMesh.material.color = targetColor;
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
}

