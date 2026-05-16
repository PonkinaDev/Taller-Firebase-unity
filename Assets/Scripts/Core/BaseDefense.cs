using UnityEngine;

namespace TowerDefense.Core
{
    /// <summary>
    /// Adjuntar al GameObject "Base" (pirámide).
    /// La detección de colisión la hace Enemy.cs.
    /// Este script existe para que GameManager tenga referencia tipada.
    /// </summary>
    public class BaseDefense : MonoBehaviour
    {
        // Intencionalmente vacío.
        // Enemy detecta Tag "Base" y llama GameManager.OnBaseReached().
    }
}
