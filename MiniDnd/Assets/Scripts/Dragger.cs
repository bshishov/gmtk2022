using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class Dragger : MonoBehaviour
{
    public bool IsDragging { get; private set; }
    
    [SerializeField]
    private DragLine DragLine;

    public event Action<Vector2> DragCompleted;

    private bool _isShowingLine;
    private Vector2 _startDragPosition;
    private Vector2 _currentDragPosition;
    
    private void Update()
    {
        if (Input.GetMouseButton(0))
        {
            IsDragging = true;
            _currentDragPosition = EventSystem.current.currentInputModule.input.mousePosition;
            
            if (!_isShowingLine)
            {
                DragLine.Start = _currentDragPosition;
                DragLine.End = _currentDragPosition;
                DragLine.Update();
                DragLine.gameObject.SetActive(true);
                _isShowingLine = true;
                _startDragPosition = _currentDragPosition;
            }

            DragLine.End = _currentDragPosition;
        }
        else
        {
            IsDragging = false;
            if (_isShowingLine)
            {
                DragLine.gameObject.SetActive(false);
                _isShowingLine = false;
                DragCompleted?.Invoke(_currentDragPosition - _startDragPosition);
            }
        }
    }
}