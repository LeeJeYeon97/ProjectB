using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>기계 위에 표시되는 업그레이드 말풍선의 버튼과 표시를 관리합니다.</summary>
public class UI_MachineUpgrade : UI_Base
{
    [Header("화면 설정")]
    [SerializeField] private Canvas worldCanvas;
    [SerializeField] private CanvasGroup panelGroup;
    [SerializeField, Min(1f)] private float clickDistance = 100f;
    [SerializeField, Min(0f)] private float tapMoveDistance = 20f;
    [SerializeField] private LayerMask machineLayers = ~0;

    private InteractableBase targetMachine;
    private Camera viewCamera;
    private Button closeButton;
    private Button capacityButton;
    private Button speedButton;
    private TMP_Text titleText;
    private TMP_Text capacityNameText;
    private TMP_Text capacityInfoText;
    private TMP_Text capacityPriceText;
    private TMP_Text speedNameText;
    private TMP_Text speedInfoText;
    private TMP_Text speedPriceText;
    private bool isVisible;
    private Vector2 mouseStartPosition;

    /// <summary>프리팹 안의 버튼과 글자를 연결하고 말풍선을 닫힌 상태로 준비합니다.</summary>
    public override void Init()
    {
        if (_init)
            return;

        base.Init();
        Bind<Button>(typeof(Define.MachineUpgradeButtons));
        Bind<TMP_Text>(typeof(Define.MachineUpgradeTexts));

        closeButton = GetButton((int)Define.MachineUpgradeButtons.Button_Close);
        capacityButton = GetButton((int)Define.MachineUpgradeButtons.Button_Capacity);
        speedButton = GetButton((int)Define.MachineUpgradeButtons.Button_Speed);
        titleText = GetTMP((int)Define.MachineUpgradeTexts.Text_Title);
        capacityNameText = GetTMP((int)Define.MachineUpgradeTexts.Text_CapacityName);
        capacityInfoText = GetTMP((int)Define.MachineUpgradeTexts.Text_CapacityInfo);
        capacityPriceText = GetTMP((int)Define.MachineUpgradeTexts.Text_CapacityPrice);
        speedNameText = GetTMP((int)Define.MachineUpgradeTexts.Text_SpeedName);
        speedInfoText = GetTMP((int)Define.MachineUpgradeTexts.Text_SpeedInfo);
        speedPriceText = GetTMP((int)Define.MachineUpgradeTexts.Text_SpeedPrice);

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
        if (capacityButton != null)
            capacityButton.onClick.AddListener(() => TryBuy(GetUpgradeId(0)));
        if (speedButton != null)
            speedButton.onClick.AddListener(() => TryBuy(GetUpgradeId(1)));

        if (worldCanvas == null)
            worldCanvas = GetComponent<Canvas>();
        if (panelGroup == null)
            panelGroup = GetComponent<CanvasGroup>();

        HideImmediately();
    }

    /// <summary>부모 기계를 연결하고 해당 기계의 업그레이드 정보를 표시합니다.</summary>
    public void SetMachine(InteractableBase machine)
    {
        Init();
        targetMachine = machine;
        if (targetMachine != null)
            transform.position = targetMachine.transform.position + Vector3.up * 2.2f;

        RefreshTexts();
    }

    /// <summary>기계 탭을 감지하고 말풍선 방향과 월드 카메라를 최신 상태로 맞춥니다.</summary>
    private void LateUpdate()
    {
        if (targetMachine != null)
            transform.position = targetMachine.transform.position + Vector3.up * 2.2f;
        else if (isVisible)
            Hide();

        if (viewCamera == null)
            viewCamera = Camera.main;

        if (viewCamera != null)
        {
            if (worldCanvas != null && worldCanvas.worldCamera != viewCamera)
                worldCanvas.worldCamera = viewCamera;

            // 카메라의 화면 좌우 방향을 유지하도록 기계에서 카메라 쪽을 바라보는 회전을 적용합니다.
            // 카메라 위치를 향해 회전시키면 좌우 축이 반전되어 글자와 버튼 배치가 거울처럼 보일 수 있습니다.
            transform.rotation = Quaternion.LookRotation(transform.position - viewCamera.transform.position, viewCamera.transform.up);
        }

        CheckMachineClick();
    }

    /// <summary>마우스나 첫 번째 터치가 짧은 탭으로 기계를 선택했는지 확인합니다.</summary>
    private void CheckMachineClick()
    {
        // 말풍선이 열린 동안에는 버튼 입력을 우선하고, 닫기는 X 버튼으로만 처리합니다.
        // 열린 말풍선의 버튼 위치가 기계 Collider와 겹치면 같은 입력이 기계 클릭으로도 잡혀 창이 닫힐 수 있습니다.
        if (isVisible)
            return;

        if (!TryGetTappedPointer(out Vector2 pointer, out int pointerId))
            return;

        // 업그레이드 버튼이나 다른 화면 버튼을 누른 입력은 기계 클릭으로 처리하지 않습니다.
        if (IsPointerOverUI(pointer) || viewCamera == null)
            return;

        Ray ray = viewCamera.ScreenPointToRay(pointer);
        // 기계는 자동 상호작용용 Trigger Collider만 가지므로 레이캐스트에서도 Trigger를 포함합니다.
        if (!Physics.Raycast(ray, out RaycastHit hit, clickDistance, machineLayers, QueryTriggerInteraction.Collide))
            return;

        InteractableBase clickedMachine = FindMachineTarget(hit.collider.transform);
        if (clickedMachine == null)
            return;

        SetMachine(clickedMachine);
        Show();
    }

    /// <summary>현재 오브젝트나 부모 오브젝트에 붙은 구성 요소 중 공통 연결부를 찾습니다.</summary>
    private static InteractableBase FindMachineTarget(Transform start)
    {
        Transform current = start;
        while (current != null)
        {
            MonoBehaviour[] components = current.GetComponents<MonoBehaviour>();
            foreach (MonoBehaviour component in components)
            {
                if (component is InteractableBase machine && machine.UpgradeIds.Count > 0)
                    return machine;
            }

            current = current.parent;
        }

        return null;
    }

    /// <summary>손가락이나 마우스를 뗀 순간의 좌표를 가져오고, 드래그 입력은 탭에서 제외합니다.</summary>
    private bool TryGetTappedPointer(out Vector2 pointer, out int pointerId)
    {
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
        {
            pointer = Touchscreen.current.primaryTouch.position.ReadValue();
            pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            Vector2 touchStart = Touchscreen.current.primaryTouch.startPosition.ReadValue();
            return Vector2.Distance(touchStart, pointer) <= tapMoveDistance;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            pointer = Mouse.current.position.ReadValue();
            pointerId = -1;
            return Vector2.Distance(mouseStartPosition, pointer) <= tapMoveDistance;
        }

        pointer = Vector2.zero;
        pointerId = -1;
        return false;
    }

    /// <summary>마우스 누름 시작 위치를 기록해 드래그로 열린 UI를 방지합니다.</summary>
    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            mouseStartPosition = Mouse.current.position.ReadValue();
    }

    /// <summary>현재 입력이 기존 UI 위에 있는지 확인해 겹치는 클릭을 막습니다.</summary>
    private static bool IsPointerOverUI(Vector2 pointer)
    {
        if (EventSystem.current == null)
            return false;

        // 전체 화면 조이스틱 터치 영역은 월드 기계 탭도 함께 받을 수 있게 통과시킵니다.
        // 조이스틱과 실제 화면 버튼이 겹친 경우에는 버튼 입력을 우선하도록 막습니다.
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = pointer
        };
        List<RaycastResult> uiHits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, uiHits);

        foreach (RaycastResult uiHit in uiHits)
        {
            // 조이스틱 터치 영역과 업그레이드 말풍선의 빈 배경은 월드 기계 클릭을 막지 않습니다.
            if (uiHit.gameObject.GetComponentInParent<UI_DynamicJoystick>() != null)
                continue;

            UI_MachineUpgrade hitUpgradeUI = uiHit.gameObject.GetComponentInParent<UI_MachineUpgrade>();
            bool hitUpgradeButton = uiHit.gameObject.GetComponentInParent<Button>() != null;
            if (hitUpgradeUI != null && !hitUpgradeButton)
                continue;

            // 업그레이드 버튼이나 다른 화면 UI 위에 누른 경우는 해당 UI 입력으로 처리합니다.
            return true;
        }

        return false;
    }

    /// <summary>다른 기계 말풍선을 닫고 이 기계의 업그레이드 화면을 엽니다.</summary>
    private void Show()
    {
        if (targetMachine == null || panelGroup == null)
            return;
        isVisible = true;
        panelGroup.alpha = 1f;
        panelGroup.interactable = true;
        panelGroup.blocksRaycasts = true;
        RefreshTexts();
    }

    /// <summary>현재 기계의 업그레이드 화면을 닫고 다른 버튼의 입력을 통과시킵니다.</summary>
    private void Hide()
    {
        isVisible = false;

        if (panelGroup != null)
        {
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
        }
    }

    /// <summary>첫 화면 표시 전에도 캔버스와 버튼이 입력을 막지 않도록 닫습니다.</summary>
    private void HideImmediately()
    {
        isVisible = false;
        if (panelGroup == null)
            return;

        panelGroup.alpha = 0f;
        panelGroup.interactable = false;
        panelGroup.blocksRaycasts = false;
    }

    /// <summary>화면을 풀에 돌려주기 전에 선택한 기계와 버튼 입력 상태를 비웁니다.</summary>
    public void ResetForPool()
    {
        Init();
        HideImmediately();
        targetMachine = null;
        viewCamera = null;
    }

    /// <summary>선택한 기계 업그레이드를 매니저에 요청하고 구매 결과를 화면에 반영합니다.</summary>
    private void TryBuy(string growthId)
    {
        if (targetMachine == null || Managers.Growth == null || string.IsNullOrEmpty(growthId))
            return;

        if (!Managers.Growth.TryUpgradeMachine(targetMachine, growthId))
            Debug.Log("기계 업그레이드 실패: 재화가 부족하거나 최대 단계입니다.");

        RefreshTexts();
    }

    /// <summary>연결된 기계가 제공한 업그레이드 단계와 다음 가격을 각 화면 행에 표시합니다.</summary>
    private void RefreshTexts()
    {
        if (targetMachine == null || Managers.Growth == null)
            return;

        if (titleText != null)
            titleText.text = "기계 업그레이드";

        // 현재 프리팹에 배치된 두 행에 기계가 제공한 업그레이드 ID를 순서대로 연결합니다.
        RefreshUpgradeRow(GetUpgradeId(0), capacityNameText, capacityInfoText, capacityPriceText, capacityButton);
        RefreshUpgradeRow(GetUpgradeId(1), speedNameText, speedInfoText, speedPriceText, speedButton);
    }

    /// <summary>기계 목록에서 화면 행 순서에 해당하는 업그레이드 ID를 가져옵니다.</summary>
    private string GetUpgradeId(int index)
    {
        return targetMachine != null && targetMachine.UpgradeIds != null && index < targetMachine.UpgradeIds.Count
            ? targetMachine.UpgradeIds[index]
            : null;
    }

    /// <summary>업그레이드 한 종류의 현재 단계와 카탈로그에 설정된 다음 효과 문구를 표시합니다.</summary>
    private void RefreshUpgradeRow(string growthId, TMP_Text nameText, TMP_Text infoText, TMP_Text priceText, Button button)
    {
        bool isShown = !string.IsNullOrEmpty(growthId);
        if (nameText != null) nameText.transform.parent.gameObject.SetActive(isShown);
        if (!isShown)
            return;

        GrowthItem item = Managers.Growth.GetItem(growthId);
        if (item == null || item.prices == null || item.values == null)
            return;

        int level = Managers.Growth.GetMachineLevel(targetMachine.MachineId, growthId);
        int maxLevel = Mathf.Min(item.prices.Length, item.values.Length);
        bool isMaxLevel = level >= maxLevel;

        if (nameText != null)
            nameText.text = item.title;

        if (isMaxLevel)
        {
            if (infoText != null)
                infoText.text = $"Lv.{level} / {maxLevel} · 최대 단계";
            if (priceText != null)
                priceText.text = "최대";
            if (button != null)
                button.interactable = false;
            return;
        }

        float nextValue = item.values[level];
        string effectText = string.IsNullOrEmpty(item.effectFormat)
            ? $"효과 +{nextValue:0.##}"
            : string.Format(item.effectFormat, nextValue * item.effectValueScale);
        int price = Managers.Growth.GetMachineNextPrice(targetMachine.MachineId, growthId);

        if (infoText != null)
            infoText.text = $"Lv.{level} / {maxLevel} · 다음 효과 {effectText}";
        if (priceText != null)
            priceText.text = $"{price:N0}원 구매";
        if (button != null)
            button.interactable = price > 0;
    }

    /// <summary>화면이 제거될 때 버튼에 등록한 입력 이벤트를 정리합니다.</summary>
    public override void Clear()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
        if (capacityButton != null)
            capacityButton.onClick.RemoveAllListeners();
        if (speedButton != null)
            speedButton.onClick.RemoveAllListeners();
    }
}
