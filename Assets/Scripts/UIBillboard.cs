using UnityEngine;

/// <summary>
/// UI Text'in sürekli kameraya bakmasını sağlar (Billboard efekti)
/// World Space Canvas'ta kullanılır
/// </summary>
public class UIBillboard : MonoBehaviour
{
    [Tooltip("Hangi kameraya bakacak (boş bırakılırsa Main Camera kullanılır)")]
    public Camera targetCamera;
    
    [Tooltip("Kameraya bakarken X ekseninde döndürme yapılsın mı?")]
    public bool lockX = false;
    
    [Tooltip("Kameraya bakarken Y ekseninde döndürme yapılsın mı?")]
    public bool lockY = false;
    
    [Tooltip("Kameraya bakarken Z ekseninde döndürme yapılsın mı?")]
    public bool lockZ = false;
    
    private void Start()
    {
        // Eğer kamera atanmamışsa Main Camera'yı bul
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }
        }
    }
    
    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            return;
        }
        
        // Kameraya doğru bak
        Vector3 direction = targetCamera.transform.position - transform.position;
        
        // Lock ayarlarına göre rotasyonu ayarla
        if (lockX || lockY || lockZ)
        {
            Vector3 eulerAngles = Quaternion.LookRotation(direction).eulerAngles;
            
            if (lockX) eulerAngles.x = transform.eulerAngles.x;
            if (lockY) eulerAngles.y = transform.eulerAngles.y;
            if (lockZ) eulerAngles.z = transform.eulerAngles.z;
            
            transform.rotation = Quaternion.Euler(eulerAngles);
        }
        else
        {
            transform.LookAt(targetCamera.transform);
            transform.Rotate(0, 180, 0); // Text'i düzelt (ters çevir)
        }
    }
}

