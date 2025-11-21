using System;
using UnityEngine;

namespace DuckovCustomModel.Core.MonoBehaviours.Packages
{
    [AddComponentMenu("Duckov Custom Model/Blueprint ID")]
    [DisallowMultipleComponent]
    public class BlueprintID : MonoBehaviour
    {
        public string id = string.Empty;

        [ContextMenu("Generate New ID")]
        private void GenerateNewID()
        {
            id = Guid.NewGuid().ToString();
        }
    }
}
