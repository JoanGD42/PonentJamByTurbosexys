using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HoverHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler {
    public Image targetImage; // button background image to tint
    public Color normalColor = Color.white;
    public Color highlightColor = new Color(0.85f, 0.85f, 0.65f);

    Button btn;

    void Awake() {
        btn = GetComponent<Button>();
        targetImage ??= GetComponent<Image>();
        targetImage?.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        targetImage?.color = highlightColor;
    }

    public void OnPointerExit(PointerEventData eventData) {
        targetImage?.color = normalColor;
    }

    public void OnSelect(BaseEventData eventData) {
        targetImage?.color = highlightColor;
    }

    public void OnDeselect(BaseEventData eventData) {
        targetImage?.color = normalColor;
    }
}
