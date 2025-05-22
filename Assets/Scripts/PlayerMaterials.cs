using System.Collections.Generic;
using UnityEngine;

public static class PlayerMaterials
{
    public static Material RedPlayerMaterial { get; private set; }
    public static Material RedPlayerInactiveMaterial { get; private set; }

    public static Material BluePlayerMaterial { get; private set; }
    public static Material BluePlayerInactiveMaterial { get; private set; }
    public static Material TargetPieceMaterial { get; private set; }
    public static Material PossibleMoveMaterial { get; private set; }
    public static Material PossibleAttackMaterial { get; private set; }

    public static List<Material> PiecesMaterials { get; private set; }

    static PlayerMaterials()
    {
        RedPlayerMaterial = Resources.Load<Material>("Materials/Player1Material");
        RedPlayerInactiveMaterial = Resources.Load<Material>("Materials/Player1InactiveMaterial");
        BluePlayerMaterial = Resources.Load<Material>("Materials/Player2Material");
        BluePlayerInactiveMaterial = Resources.Load<Material>("Materials/Player2InactiveMaterial");

        PossibleMoveMaterial = Resources.Load<Material>("Materials/PossibleMoveMaterial");
        //PossibleAttackMaterial = Resources.Load<Material>("Materials/PossibleAttackMaterial");

        PiecesMaterials = new List<Material>()
        {
            RedPlayerMaterial,
            RedPlayerInactiveMaterial,
            BluePlayerMaterial,
            BluePlayerInactiveMaterial,
            PossibleMoveMaterial,
            //PossibleAttackMaterial
        };

        Debug.Log($"RedPlayerMaterial loaded: {RedPlayerMaterial != null}");
        Debug.Log($"BluePlayerMaterial loaded: {BluePlayerMaterial != null}");
        Debug.Log($"PossibleMoveMaterial loaded: {PossibleMoveMaterial != null}");
        //Debug.Log($"PossibleAttackMaterial loaded: {PossibleAttackMaterial != null}");
    }
}
