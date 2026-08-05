/*
2026-07-08 AI-Tag
This was created with the help of Assistant, a Unity Artificial Intelligence product.
*/
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;

public class UI_DynamicJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [Header("Joystick Settings")]
    [Tooltip("자식으로 배치된 UI_Joystick(조이스틱 배경)의 RectTransform을 연결합니다.")]
    [SerializeField] private RectTransform _joystickRoot;
    
    [Tooltip("Controller(핸들)에 이식한 OnScreenStick 컴포넌트를 연결합니다.")]
    [SerializeField] private OnScreenStick _onScreenStick;

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        if (_joystickRoot != null)
        {
            // CanvasGroup이 없다면 동적으로 추가하여 제어를 유연하게 만듭니다.
            _canvasGroup = _joystickRoot.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = _joystickRoot.gameObject.AddComponent<CanvasGroup>();
            }

            // 시작할 때는 게임 오브젝트를 항상 켜두되, 투명하게 만들고 상호작용을 차단하여 숨깁니다.
            _joystickRoot.gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }

    // 1. 터치하는 순간 호출
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_joystickRoot == null || _onScreenStick == null || _canvasGroup == null) return;

        // 터치한 스크린 좌표를 UI 로컬 좌표로 변환
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform, // 터치 존 기준
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint
        );

        // 조이스틱을 터치한 위치로 순간 이동시키고 시각적으로 활성화합니다.
        _joystickRoot.anchoredPosition = localPoint;
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        // 터치 이벤트를 핸들의 OnScreenStick으로 즉시 전달해 드래그가 시작되게 만듭니다.
        _onScreenStick.OnPointerDown(eventData);
    }

    // 2. 드래그 중일 때 호출
    public void OnDrag(PointerEventData eventData)
    {
        if (_onScreenStick == null || _canvasGroup == null || _canvasGroup.alpha < 0.1f) return;

        // 드래그 움직임을 핸들의 OnScreenStick으로 계속 토스합니다.
        _onScreenStick.OnDrag(eventData);
    }

    // 3. 손가락을 떼었을 때 호출
    public void OnPointerUp(PointerEventData eventData)
    {
        if (_onScreenStick == null) return;

        // 터치가 끝나면 핸들이 복귀하도록 이벤트를 전달합니다.
        _onScreenStick.OnPointerUp(eventData);

        // 조이스틱을 다시 숨깁니다.
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
