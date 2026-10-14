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
        // 터치 영역 캔버스를 게임 UI보다 뒤에 두어 버튼 입력이 먼저 도착하게 합니다.
        SetTouchCanvas();

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

    /// <summary>화면 전체 터치 영역을 버튼 캔버스 뒤에 배치합니다.</summary>
    private void SetTouchCanvas()
    {
        Canvas touchCanvas = GetComponent<Canvas>();
        Camera uiCamera = Camera.main;

        // 카메라가 없으면 화면 터치 영역을 바꾸지 않고 원래 설정을 유지합니다.
        if (touchCanvas == null || uiCamera == null)
            return;

        // 게임 UI도 ScreenSpaceCamera이므로 같은 카메라를 쓰고 더 낮은 정렬 순서로 둡니다.
        touchCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        touchCanvas.worldCamera = uiCamera;
        touchCanvas.planeDistance = 1f;
        touchCanvas.overrideSorting = true;
        touchCanvas.sortingOrder = -1;
    }

    /// <summary>화면 터치 시작을 받아 입력 위치를 옮기고 가상 스틱에 전달합니다.</summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_joystickRoot == null || _onScreenStick == null || _canvasGroup == null) return;

        // 터치 좌표를 조이스틱 기준의 로컬 좌표로 바꿔 가상 스틱의 시작점을 정합니다.
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            eventData.position, 
            eventData.pressEventCamera, 
            out localPoint
        );

        // 조이스틱 그림은 계속 투명하게 두고, 실제 입력 기준점만 터치한 곳으로 옮깁니다.
        _joystickRoot.anchoredPosition = localPoint;

        // 터치 이벤트를 OnScreenStick으로 전달해 화면에 보이지 않아도 이동 입력을 만듭니다.
        _onScreenStick.OnPointerDown(eventData);
    }

    /// <summary>손가락을 움직이는 동안 가상 스틱 입력을 계속 갱신합니다.</summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_onScreenStick == null || _canvasGroup == null) return;

        // 시각 표시 여부와 관계없이 드래그 좌표를 가상 스틱에 전달합니다.
        _onScreenStick.OnDrag(eventData);
    }

    /// <summary>손가락을 떼면 가상 스틱 입력을 0으로 돌려 플레이어를 멈춥니다.</summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (_onScreenStick == null) return;

        // 터치 종료를 전달해 가상 스틱의 입력값을 초기화합니다.
        _onScreenStick.OnPointerUp(eventData);

        // 투명 상태를 유지하고 조이스틱 그래픽이 터치를 가로채지 않게 합니다.
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
