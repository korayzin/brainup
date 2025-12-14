using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// InfoUI ve RulesUI GameObject'lerine hover efekti ekler
/// </summary>
public class UIHoverEffectCreator : EditorWindow
{
    [MenuItem("Tools/UI/Add Hover Effect to Info & Rules")]
    public static void AddHoverEffectToInfoAndRules()
    {
        // Scene'deki tüm GameObject'leri bul
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        GameObject infoUI = null;
        GameObject rulesUI = null;
        
        // InfoUI ve RulesUI'yi bul
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "InfoUI")
            {
                infoUI = obj;
            }
            else if (obj.name == "RulesUI")
            {
                rulesUI = obj;
            }
        }
        
        if (infoUI == null)
        {
            Debug.LogWarning("InfoUI GameObject bulunamadı!");
        }
        else
        {
            AddHoverEffectToGameObject(infoUI);
            Debug.Log("InfoUI'ye hover efekti eklendi!");
        }
        
        if (rulesUI == null)
        {
            Debug.LogWarning("RulesUI GameObject bulunamadı!");
        }
        else
        {
            AddHoverEffectToGameObject(rulesUI);
            Debug.Log("RulesUI'ye hover efekti eklendi!");
        }
        
        if (infoUI != null || rulesUI != null)
        {
            EditorUtility.DisplayDialog("Başarılı", 
                "InfoUI ve RulesUI'ye hover efekti eklendi!\n\n" +
                "Not: Eğer Image component yoksa, hover efekti çalışmayabilir.", 
                "Tamam");
        }
    }
    
    private static void AddHoverEffectToGameObject(GameObject obj)
    {
        if (obj == null) return;
        
        // Image component'i kontrol et
        Image image = obj.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"{obj.name} üzerinde Image component bulunamadı! Hover efekti için Image component gerekli.");
            return;
        }
        
        // Zaten UIHoverEffect var mı kontrol et
        UIHoverEffect existingEffect = obj.GetComponent<UIHoverEffect>();
        if (existingEffect != null)
        {
            Debug.Log($"{obj.name} üzerinde zaten UIHoverEffect var, atlanıyor.");
            return;
        }
        
        // UIHoverEffect ekle
        UIHoverEffect hoverEffect = obj.AddComponent<UIHoverEffect>();
        
        // Varsayılan ayarları uygula (Inspector'da değiştirilebilir)
        SerializedObject serializedObject = new SerializedObject(hoverEffect);
        serializedObject.FindProperty("hoverScale").floatValue = 1.15f;
        serializedObject.FindProperty("scaleAnimationSpeed").floatValue = 10f;
        serializedObject.FindProperty("useColorChange").boolValue = true;
        serializedObject.FindProperty("hoverColor").colorValue = new Color(1.3f, 1.3f, 1.3f, 1f);
        serializedObject.FindProperty("colorAnimationSpeed").floatValue = 10f;
        serializedObject.ApplyModifiedProperties();
        
        // Raycast Target'ın açık olduğundan emin ol (hover için gerekli)
        image.raycastTarget = true;
        
        Debug.Log($"{obj.name} üzerine UIHoverEffect eklendi.");
    }
}
