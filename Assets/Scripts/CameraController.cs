using Photon.Pun;
using UnityEngine;

public class CameraController : MonoBehaviourPun
{
    private Vector3 defaultRotation;
    private Camera mainCamera;

    [SerializeField]
    private float tiltAmount = 5f; // intensidade da rotação

    void Start()
    {
        mainCamera = GetComponent<Camera>();
        if (mainCamera == null)
        {
            Debug.LogError("Camera not found on this GameObject.");
            enabled = false; // Desativa o script se não houver câmara
            return;
        }

        // Só ativa a câmara e o controlo para o jogador local
        if (photonView.IsMine)
        {
            mainCamera.gameObject.SetActive(true);
            defaultRotation = mainCamera.transform.rotation.eulerAngles;

            // A lógica de posicionamento da câmara precisa ser ajustada para o sistema de ActorNumber do PUN
            // ou diferenciação MasterClient / Cliente.
            // Assumindo um jogo de 2 jogadores, o MasterClient pode ser o Jogador 1.
            if (PhotonNetwork.IsMasterClient)
            {
                mainCamera.transform.position = new Vector3(0, 11.27f, -11.9f);
                mainCamera.transform.rotation = Quaternion.Euler(45, 0, 0);
                Debug.Log($"[CameraController] Câmara ativa para MasterClient (Local). ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
            }
            else
            {
                // Este será o segundo jogador que se junta.
                mainCamera.transform.position = new Vector3(0, 11.27f, 11.9f);
                mainCamera.transform.rotation = Quaternion.Euler(45, 180, 0);
                Debug.Log($"[CameraController] Câmara ativa para Cliente (Local). ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber}");
            }
            defaultRotation = mainCamera.transform.rotation.eulerAngles; // Redefine defaultRotation após o posicionamento
        }
        else
        {
            mainCamera.gameObject.SetActive(false); // Garante que a câmara está desativada para não proprietários
        }
    }

    void Update()
    {
        // Só o proprietário pode controlar a câmara
        if (!photonView.IsMine || mainCamera == null)
            return;

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
