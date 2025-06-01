using System.Collections;
using System.Collections.Generic;
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
            button.interactable = true;
        else
            button.interactable = false;
    }

    public void TryLogin()
    {
        Debug.Log("Attempting to log in with username: " + usernameInputField.text);
        authManager.Login(usernameInputField.text, passwordInputField.text);
    }
}
