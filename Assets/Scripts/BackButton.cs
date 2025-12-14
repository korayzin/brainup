using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButton : MonoBehaviour
{
    public void GoBackToIndex0()
    {
        SceneManager.LoadScene(0);
    }
}
