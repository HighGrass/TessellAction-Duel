using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginButton : MonoBehaviour
{
    [SerializeField]
    Button button;

    [SerializeField]
    TMP_InputField usernameInputField;

    [SerializeField]
    int minUsernameLength = 3;

    [SerializeField]
    TMP_InputField passwordInputField;

    [SerializeField]
    TMP_Text buttonText;

    [SerializeField]
    Color buttonDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [SerializeField]
    int minPasswordLength = 6;

    [SerializeField]
    AuthManager authManager;

    void Start() => button.interactable = false;

    void FixedUpdate()
    {
        if (
            usernameInputField.text.Length >= minUsernameLength
            && passwordInputField.text.Length >= minPasswordLength
        )
        {
            button.interactable = true;
            buttonText.color = Color.white;
        }
        else
        {
            buttonText.color = buttonDisabledColor;
            button.interactable = false;
        }
    }

    public void TryLogin()
    {
        Debug.Log("Attempting to log in with username: " + usernameInputField.text);
        authManager.Login(usernameInputField.text, passwordInputField.text);
    }
}
