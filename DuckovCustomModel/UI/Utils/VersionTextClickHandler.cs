using UnityEngine;
using UnityEngine.EventSystems;

namespace DuckovCustomModel.UI.Utils
{
    public class VersionTextClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            ConfigWindow.Instance?.ShowPanel(1);
        }
    }
}
