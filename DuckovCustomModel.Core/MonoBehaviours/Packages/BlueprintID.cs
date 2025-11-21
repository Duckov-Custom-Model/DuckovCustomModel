using System;
using UnityEngine;

namespace DuckovCustomModel.Core.MonoBehaviours.Packages
{
    /// <summary>
    ///     Assigns unique identifiers to game objects. Currently a placeholder for future functionality.
    /// </summary>
    [AddComponentMenu("Duckov Custom Model/Blueprint ID")]
    [DisallowMultipleComponent]
    public class BlueprintID : MonoBehaviour
    {
        [Tooltip("Unique identifier for this game object")]
        public string id = string.Empty;

        [ContextMenu("Generate New ID")]
        private void GenerateNewID()
        {
            id = Guid.NewGuid().ToString();
        }
    }
}
