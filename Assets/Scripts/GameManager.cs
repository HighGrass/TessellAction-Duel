// GameManager.cs - Nova versão
using Photon.Pun;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform[] spawnPoints;

    // Método que o SimplePunLauncher pode chamar para obter um ponto de spawn
    public Transform GetSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            return null;
        }

        // Lógica simples para escolher um spawn point baseado no ActorNumber
        int playerIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        if (playerIndex < 0) playerIndex = 0;

        return spawnPoints[playerIndex % spawnPoints.Length];
    }
}
