using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager
{

    //public Action<Vector2> OnDragStarted;
    //public Action<Vector2> OnDragging;
    //public Action OnDragEnded;

    //private InputSystem_Actions control;
    //private bool _isPressed = false;

    //public void Init()
    //{
    //    if (control != null) return;
    //    control = new InputSystem_Actions();

    //    control.Player.Attack.started += OnTouchStartedInternal;
    //    control.Player.Attack.canceled += OnTouchCanceledInternal;

    //    control.Enable();
    //}

    //private void OnDisable()
    //{
    //    if (control == null) return;
    //    control.Player.Attack.started -= OnTouchStartedInternal;
    //    control.Player.Attack.canceled -= OnTouchCanceledInternal;
    //    control.Disable();
    //}

    //private void Update()
    //{
    //    if (control == null || !_isPressed) return;

    //    // [수정] 월드 좌표 변환(ScreenToWorldPoint)을 삭제하고 화면 좌표(ScreenPos)를 그대로 넘깁니다!
    //    Vector2 screenPos = control.Player.Point.ReadValue<Vector2>();
    //    OnDragging?.Invoke(screenPos);
    //}

    //private void OnTouchStartedInternal(InputAction.CallbackContext ctx)
    //{
    //    _isPressed = true;

    //    // [수정] 여기도 화면 좌표를 그대로 넘깁니다.
    //    Vector2 screenPos = control.Player.Point.ReadValue<Vector2>();
    //    OnDragStarted?.Invoke(screenPos);
    //}

    //private void OnTouchCanceledInternal(InputAction.CallbackContext ctx)
    //{
    //    _isPressed = false;
    //    OnDragEnded?.Invoke();
    //}

}
