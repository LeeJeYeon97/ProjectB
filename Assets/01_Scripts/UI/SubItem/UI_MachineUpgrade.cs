using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>근처의 조리 기계 한 대를 선택해 강화하는 미리 제작된 화면입니다.</summary>
public class UI_MachineUpgrade : UI_Base
{
    #region 화면 요소와 설정

    private enum Objects { UpgradePanel, OpenButtonRoot }
    private enum Buttons { Button_Open, Button_Buy, Button_Close, Button_Reset }
    private enum Texts { Text_Info, Text_Buy }

    [SerializeField] private MenuMachine machine;
    [SerializeField, Min(0.1f)] private float openDistance = 3f;
    private bool isOpen;
    private bool isNear;

    #endregion

    #region 유니티 함수

    private void Update()
    {
        if (!_init || machine == null) return;
        PlayerController player = PlayerController.Instance;
        bool near = false;
        if (player != null)
            near = Vector3.Distance(player.transform.position, machine.transform.position) <= openDistance;
        if (near != isNear)
        {
            isNear = near;
            if (!isNear) isOpen = false;
            Refresh();
        }
    }

    /// <summary>월드 공간 화면이 카메라를 향하도록 하고 열린 화면의 잔액 상태를 갱신합니다.</summary>
    private void LateUpdate()
    {
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        if (_init && isOpen && isNear) Refresh();
    }

    private void OnDisable()
    {
        isNear = false;
        isOpen = false;
        if (_init) Refresh();
    }

    #endregion

    #region 화면과 구매

    /// <summary>프리팹에 미리 배치된 화면 요소와 버튼을 이름으로 연결합니다.</summary>
    public override void Init()
    {
        if (_init) return;
        base.Init();
        BindObject(typeof(Objects));
        BindButton(typeof(Buttons));
        BindText(typeof(Texts));
        GetButton((int)Buttons.Button_Open).onClick.AddListener(Open);
        GetButton((int)Buttons.Button_Buy).onClick.AddListener(Buy);
        GetButton((int)Buttons.Button_Close).onClick.AddListener(Close);
        GetButton((int)Buttons.Button_Reset).onClick.AddListener(ResetLevel);
        Managers.Growth.OnChanged += Refresh;
        Refresh();
    }

    /// <summary>성장 알림 구독을 해제합니다.</summary>
    public override void Clear()
    {
        if (_init && Managers.Growth != null) Managers.Growth.OnChanged -= Refresh;
        base.Clear();
    }

    private void Open() { isOpen = true; Refresh(); }
    private void Close() { isOpen = false; Refresh(); }

    /// <summary>다시 거리와 가격을 확인해 선택한 기계의 강화만 구매합니다.</summary>
    private void Buy()
    {
        if (!isNear || machine == null) return;
        Managers.Growth.TryUpgradeMachine(machine);
        Refresh();
    }

    /// <summary>테스트 버튼은 현재 기계의 단계만 지우고 사용한 재화는 돌려주지 않습니다.</summary>
    private void ResetLevel()
    {
        if (!isNear || machine == null) return;
        Managers.Growth.ResetMachineLevel(machine.UpgradeId);
        Refresh();
    }

    /// <summary>근접 여부와 현재 단계, 잔액에 맞춰 사전 제작한 UI를 갱신합니다.</summary>
    private void Refresh()
    {
        if (!_init) return;
        GetObject((int)Objects.OpenButtonRoot).SetActive(isNear && !isOpen);
        GetObject((int)Objects.UpgradePanel).SetActive(isNear && isOpen);
        if (!isNear || !isOpen || machine == null) return;
        MachineUpgradeData data = machine.UpgradeData;
        int level = machine.UpgradeLevel;
        if (data == null || string.IsNullOrWhiteSpace(machine.UpgradeId))
        {
            GetText((int)Texts.Text_Info).text = "강화 설정 또는 기계 ID가 없습니다";
            GetText((int)Texts.Text_Buy).text = "구매 불가";
            GetButton((int)Buttons.Button_Buy).interactable = false;
            return;
        }
        float currentScale = data.GetTimeScale(level);
        GetText((int)Texts.Text_Info).text = $"조리 기계 강화  Lv.{level}\n현재 조리 시간 {currentScale * 100f:0}%";
        bool hasNext = data.TryGetStep(level, out MachineUpgradeStep step);
        PlayerController player = PlayerController.Instance;
        if (hasNext)
        {
            GetText((int)Texts.Text_Buy).text = $"{step.TimeScale * 100f:0}%로 강화  {step.Price}";
            GetButton((int)Buttons.Button_Buy).interactable = player != null && player.money >= step.Price;
        }
        else
        {
            GetText((int)Texts.Text_Buy).text = "최대 단계";
            GetButton((int)Buttons.Button_Buy).interactable = false;
        }
    }

    #endregion
}

