using UnityEngine;

public class GoToRegisterMenu : MonoBehaviour
{
    public void GoToRegister() =>
        UnityEngine.SceneManagement.SceneManager.LoadScene("RegisterMenu");
}
