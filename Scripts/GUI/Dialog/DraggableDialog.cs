﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableDialog : Dialog, IDraggable
{
    public RectTransform hitbox;
    private Vector2 offset;
    private Vector2 startingPosition;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void InterruptDrag()
    {
        transform.position = startingPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (hitbox != null)
            if (!RectTransformUtility.RectangleContainsScreenPoint(hitbox, Mouse.current.position.ReadValue()))
            {
                eventData.pointerDrag = null;
                return;
            }

        startingPosition = (Vector2)transform.position;
        offset = startingPosition - Mouse.current.position.ReadValue();

        if (flavorImage != null)
            flavorImage.color = new Color(1, 1, 1, 0.5f);

        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Mouse.current.position.ReadValue() + offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (flavorImage != null)
            flavorImage.color = new Color(1, 1, 1, 1f);
    }

    public new void SetContent(DialogContent content)
    {
        throw new System.NotImplementedException();
    }
}
