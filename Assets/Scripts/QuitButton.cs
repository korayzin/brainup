using UnityEngine;

public class QuitButton : MonoBehaviour
{
    /// <summary>
    /// Butona basıldığında uygulamayı kapatır
    /// Unity Editor'da çalışmaz, sadece build'de çalışır
    /// </summary>
    public void QuitApplication()
    {
        // Unity Editor'da çalışıyorsa
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // Build'de çalışıyorsa
            Application.Quit();
        #endif
    }
}
