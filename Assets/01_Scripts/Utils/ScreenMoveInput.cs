using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>게임 화면에서 시작한 드래그의 방향만 이동 코드에 제공합니다.</summary>
public class ScreenMoveInput : MonoBehaviour
{
    [Tooltip("화면 높이에서 드래그 시작으로 인정할 이동 거리의 비율입니다.")]
    [SerializeField, Range(0f, 0.1f)] private float dragStartRatio = 0.015f;

    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private bool isActive;
    private bool isDragging;
    private bool isTouch;
    private int touchId;
    private Vector2 pressPosition;

    private void OnDisable()
    {
        // 입력을 잠시 끄는 동안 누르고 있던 손가락을 잊어 다시 켰을 때 이동하지 않게 합니다.
        Clear();
    }

    /// <summary>이동 모드가 중단되면 이전 손가락과 마우스 누름 기록을 지웁니다.</summary>
    public void Clear()
    {
        isActive = false;
        isDragging = false;
    }

    /// <summary>UI 밖에서 시작한 입력이 충분히 드래그됐을 때만 방향을 반환합니다.</summary>
    public bool TryGetDirection(out Vector2 direction)
    {
        direction = Vector2.zero;

        // 이미 선택한 손가락의 입력만 따라가 다른 손가락의 버튼 조작과 섞이지 않게 합니다.
        if (isActive)
        {
            if (isTouch)
            {
                Touchscreen screen = Touchscreen.current;
                if (screen != null && screen.primaryTouch.press.isPressed)
                {
                    int currentId = screen.primaryTouch.touchId.ReadValue();
                    if (currentId == touchId)
                    {
                        Vector2 currentPosition = screen.primaryTouch.position.ReadValue();
                        return TryReadDrag(currentPosition, out direction);
                    }
                }
            }
            else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                Vector2 currentPosition = Mouse.current.position.ReadValue();
                return TryReadDrag(currentPosition, out direction);
            }

            // 손가락을 떼거나 입력 장치가 사라지면 즉시 드래그를 끝냅니다.
            Clear();
            return false;
        }

        Touchscreen touchScreen = Touchscreen.current;
        if (touchScreen != null && touchScreen.primaryTouch.press.wasPressedThisFrame)
        {
            Vector2 touchPosition = touchScreen.primaryTouch.position.ReadValue();
            if (IsOverUI(touchPosition))
                return false;

            isTouch = true;
            touchId = touchScreen.primaryTouch.touchId.ReadValue();
            isActive = true;
            pressPosition = touchPosition;
            return false;
        }

        // 에디터에서는 마우스로 동일한 누름 및 드래그 조작을 확인할 수 있습니다.
        if (Application.isMobilePlatform || Mouse.current == null)
            return false;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return false;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (IsOverUI(mousePosition))
            return false;

        isTouch = false;
        isActive = true;
        pressPosition = mousePosition;
        return false;
    }

    /// <summary>처음 누른 위치에서 현재 위치까지의 벡터를 이동 방향으로 바꿉니다.</summary>
    private bool TryReadDrag(Vector2 currentPosition, out Vector2 direction)
    {
        direction = Vector2.zero;
        Vector2 distance = currentPosition - pressPosition;
        float minDistance = Mathf.Max(1f, Screen.height * dragStartRatio);

        if (!isDragging)
        {
            if (distance.sqrMagnitude < minDistance * minDistance)
                return false;

            isDragging = true;
        }

        // 시작점으로 다시 돌아오면 드래그가 유지돼도 캐릭터는 멈춥니다.
        if (distance.sqrMagnitude < minDistance * minDistance)
            return false;

        direction = distance.normalized;
        return true;
    }

    /// <summary>입력 시작 위치에 버튼이나 팝업 같은 UI가 있는지 현재 좌표로 확인합니다.</summary>
    private bool IsOverUI(Vector2 position)
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
            return false;

        // 입력 액션 콜백의 이전 프레임 상태에 의존하지 않도록 현재 위치를 직접 검사합니다.
        PointerEventData pointer = new PointerEventData(eventSystem);
        pointer.position = position;
        uiHits.Clear();
        eventSystem.RaycastAll(pointer, uiHits);

        foreach (RaycastResult hit in uiHits)
        {
            if (hit.module is GraphicRaycaster)
                return true;
        }

        return false;
    }
}
