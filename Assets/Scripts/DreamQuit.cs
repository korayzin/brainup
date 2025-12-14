using UnityEngine;

/// <summary>
/// Dream mesh'ine basınca uygulamayı kapatır
/// </summary>
public class DreamQuit : MonoBehaviour
{
    void Start()
    {
        // Collider kontrolü - OnMouseDown için gerekli
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning("DreamQuit: OnMouseDown çalışması için bu GameObject'te bir Collider bileşeni olmalı!");
        }
    }

    void OnMouseDown()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
