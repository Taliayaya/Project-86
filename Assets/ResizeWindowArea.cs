using System.Collections;
using System.Collections.Generic;
using ScriptableObjects.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class ResizeWindowArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private CursorSO _cursorSo;
    public HUDWindow hudWindow;
    
    [SerializeField] private Image borderImage;
    [SerializeField] private Color inactiveColor;
    [SerializeField] private Color activeColor;

    private Vector2 _center;
    
    private void Awake()
    {
        _cursorSo = Resources.Load<CursorSO>("ScriptableObjects/UI/Cursor/Cursor");
    }

    // Start is called before the first frame update
    void Start()
    {
        _center = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        borderImage.raycastTarget = hudWindow.settings.isResizable;
        if (!hudWindow.settings.isResizable)
            return;
        Cursor.SetCursor(_cursorSo.resizeTexture, Vector2.zero, CursorMode.Auto);
        borderImage.color = activeColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!hudWindow.settings.isResizable)
            return;
        Cursor.SetCursor(_cursorSo.defaultTexture, Vector2.zero, CursorMode.Auto);
        borderImage.color = inactiveColor;
    }

    /// <summary>
    /// This method is called as long as the user has the mouse button down and started on the current gameobject.
    /// It will resize the window according to the mouse delta and determine the direction, whether its outside or
    /// inside and will increase or decrease the size.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrag(PointerEventData eventData)
    {
        if (!hudWindow.settings.isResizable)
            return;
        float magnitude = eventData.delta.magnitude * Time.deltaTime * 0.05f;
        
        Vector2 currentDistanceFromCenter = _center - eventData.position;
        float angleToCenter = Vector2.SignedAngle(eventData.delta, currentDistanceFromCenter);
        if (angleToCenter < 0) // drag is in the opposite direction of the center
        {
            hudWindow.SizeMultiplier += magnitude;
        }
        else
        {
            hudWindow.SizeMultiplier -= magnitude;
        }
        //_lastDragPosition = eventData.position;
        //_lastDistanceFromCenter = currentDistanceFromCenter;
    }

    private Vector2 _lastDragPosition;
    private Vector2 _lastDistanceFromCenter;
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        _lastDragPosition = eventData.position;
        _lastDistanceFromCenter = _center - _lastDragPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }
}
