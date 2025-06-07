using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RegisterButton : MonoBehaviour
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
    TMP_Text buttonText;

    [SerializeField]
    Color buttonDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [SerializeField]
    AuthManager authManager;

    // Start is called before the first frame update
    void Start()
    {
        button.interactable = false;
        UpdateColor();
    }

    // Update is called once per frame
    void Update()
    {
        bool isValid =
            usernameInputField.text.Length >= minUsernameLength
            && passwordInputField.text.Length >= minPasswordLength;
        if (button.interactable != isValid)
        {
            button.interactable = isValid;
            UpdateColor();
        }
    }

    private void UpdateColor()
    {
        buttonText.color = button.interactable ? Color.white : buttonDisabledColor;
    }

    public void TryRegister()
    {
        Debug.Log("A tentar registar com o nome de utilizador: " + usernameInputField.text);
        authManager.Register(usernameInputField.text, passwordInputField.text);
    }
}
