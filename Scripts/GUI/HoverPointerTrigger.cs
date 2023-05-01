using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(UITransformAnimator))]
public class HoverPointerTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        GetComponent<UITransformAnimator>().StartAnimationForward();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetComponent<UITransformAnimator>().StartAnimationBackward();
    }
}
