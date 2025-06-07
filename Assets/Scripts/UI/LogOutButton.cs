using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogOutButton : MonoBehaviour
{
    public void PerformLogout()
    {
        if (AuthManager.Instance != null)
            AuthManager.Instance.LogOut();
        else
            Debug.LogError("AuthManager não foi encontrado! Não é possível fazer logout.");
    }
}
