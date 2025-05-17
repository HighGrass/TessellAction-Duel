using Unity.Netcode;
using UnityEngine;

public class CameraController : NetworkBehaviour
{
    Camera mainCamera;

    public override void OnNetworkSpawn()
    {
        mainCamera = GetComponent<Camera>();
        Debug.Log(
            $"[CameraController] OnNetworkSpawn - ClientId={OwnerClientId}, IsOwner={IsOwner}"
        );

        // Only for the local player
        if (!IsOwner)
        {
            Debug.LogWarning("Não é proprietário");
            mainCamera?.gameObject.SetActive(false); // Ensure camera is disabled for non-owners
            return;
        }

        mainCamera.gameObject.SetActive(true);

        if (OwnerClientId == 0)
        {
            mainCamera.transform.position = new Vector3(0, 11.27f, -11.9f);
            mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
        }
        else
        {
            mainCamera.transform.position = new Vector3(0, 11.27f, 11.9f);
            mainCamera.transform.rotation = Quaternion.Euler(45, 180, 0);
        }

        Debug.Log(
            $"[CameraController] Câmera ativa para ClientId={OwnerClientId}, LocalClientId={NetworkManager.Singleton.LocalClientId}"
        );
    }
}
