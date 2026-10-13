using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DuckovCustomModel.UI.Utils
{
    public class LinkHandler : MonoBehaviour, IPointerClickHandler
    {
        private void Awake()
        {
            var text = GetComponent<TextMeshProUGUI>();
            if (text != null) text.raycastTarget = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            var textMeshPro = GetComponent<TextMeshProUGUI>();
            if (textMeshPro == null) return;

            var linkIndex = TMP_TextUtilities.FindIntersectingLink(textMeshPro, eventData.position, null);
            if (linkIndex == -1) return;

            var linkInfo = textMeshPro.textInfo.linkInfo[linkIndex];
            var url = linkInfo.GetLinkID();

            if (!string.IsNullOrEmpty(url)) Application.OpenURL(url);
        }
    }
}
