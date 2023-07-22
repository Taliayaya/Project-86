using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjects.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MoveWindowArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler
{
    private CursorSO _cursorSo;
    private HUDWindow _hudWindow;

    public HUDWindow hudWindow
    {
        get => _hudWindow;
        set
        {
            _hudWindow = value;
            if (!hudWindow.IsActivated)
            {
                _rawImage.raycastTarget = _hudWindow.settings.isDraggable;
                _rawImage.color = new Color(0, 0, 0, 0.1f);
            }

        }
    }

    private RawImage _rawImage;
    private void Awake()
    {
        _cursorSo = Resources.Load<CursorSO>("ScriptableObjects/UI/Cursor/Cursor");
        _rawImage = GetComponent<RawImage>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!hudWindow.settings.isDraggable)
            return;
        Cursor.SetCursor(_cursorSo.dragTexture, Vector2.zero, CursorMode.Auto);
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!hudWindow.settings.isDraggable)
            return;
        Cursor.SetCursor(_cursorSo.defaultTexture, Vector2.zero, CursorMode.Auto);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!hudWindow.settings.isDraggable)
            return;
        hudWindow.Position += eventData.delta;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        hudWindow.transform.SetAsLastSibling();
        hudWindow.settings.orderInLayer = hudWindow.transform.GetSiblingIndex();
    }
}
