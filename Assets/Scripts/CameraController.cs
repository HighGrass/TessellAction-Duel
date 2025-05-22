using Unity.Netcode;
using UnityEngine;

public class CameraController : NetworkBehaviour
{
    private Vector3 defaultRotation;
    private Camera mainCamera;

    [SerializeField]
    private float tiltAmount = 5f; // intensidade da rotação

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

    void Start()
    {
        defaultRotation = GetComponent<Camera>().transform.rotation.eulerAngles;
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("Camera not found on this GameObject.");
            return;
        }
    }

    void Update()
    {
        if (!IsOwner)
            return; // Only the owner can control the camera

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Vector2 mouseDelta = (Vector2)Input.mousePosition - screenCenter;
        Vector2 normalizedDelta = new Vector2(
            Mathf.Clamp(mouseDelta.x / screenCenter.x, -1f, 1f),
            Mathf.Clamp(mouseDelta.y / screenCenter.y, -1f, 1f)
        );

        float yaw = normalizedDelta.x * tiltAmount;
        float pitch = -normalizedDelta.y * tiltAmount;

        Quaternion targetRotation = Quaternion.Euler(
            defaultRotation.x + pitch,
            defaultRotation.y + yaw,
            0
        );
        mainCamera.transform.rotation = Quaternion.Lerp(
            mainCamera.transform.rotation,
            targetRotation,
            Time.deltaTime * 5f
        );
    }
}
