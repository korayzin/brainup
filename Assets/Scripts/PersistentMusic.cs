using UnityEngine;

public class PersistentMusic : MonoBehaviour
{
    private static PersistentMusic instance;

    void Awake()
    {
        // Eğer daha önce müzik objesi varsa, bunu sil
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // İlk kez oluşuyorsa
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
