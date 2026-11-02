using UnityEngine;

/// <summary>근처 창고의 재고 용량을 강화하는 사전 제작 화면입니다.</summary>
public class UI_StorageUpgrade : UI_Base
{
    #region 화면 요소와 설정

    private enum Objects { UpgradePanel, OpenButtonRoot }
    private enum Buttons { Button_Open, Button_Buy, Button_Close, Button_Reset }
    private enum Texts { Text_Info, Text_Buy }

    [SerializeField] private Storage storage;
    [SerializeField, Min(0.1f)] private float openDistance = 3f;
    private bool isOpen;
    private bool isNear;

    #endregion

    #region 유니티 함수

    /// <summary>플레이어가 가까이 있을 때만 선택 버튼과 강화 화면을 표시합니다.</summary>
    private void Update()
    {
        if (!_init) return;
        if (storage == null) return;
        PlayerController player = PlayerController.Instance;
        bool near = false;
        if (player != null)
            near = Vector3.Distance(player.transform.position, storage.transform.position) <= openDistance;
        if (near == isNear) return;
        isNear = near;
        if (!isNear) isOpen = false;
        Refresh();
    }

    /// <summary>화면을 카메라 방향으로 돌리고 열린 동안 잔액과 용량을 갱신합니다.</summary>
    private void LateUpdate()
    {
        if (Camera.main != null) transform.rotation = Camera.main.transform.rotation;
        if (_init && isOpen && isNear) Refresh();
    }

    /// <summary>창고가 비활성화되면 열린 화면을 닫습니다.</summary>
    private void OnDisable()
    {
        isNear = false;
        isOpen = false;
        if (_init) Refresh();
    }

    #endregion

    #region 화면과 구매

    /// <summary>프리팹에서 미리 만든 요소를 이름으로 연결하고 버튼을 등록합니다.</summary>
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

    /// <summary>파괴될 때 성장 변경 알림을 해제합니다.</summary>
    public override void Clear()
    {
        if (_init && Managers.Growth != null) Managers.Growth.OnChanged -= Refresh;
        base.Clear();
    }

    /// <summary>현재 창고의 강화 화면을 엽니다.</summary>
    private void Open() { isOpen = true; Refresh(); }

    /// <summary>강화 화면을 닫고 근접 버튼으로 돌아갑니다.</summary>
    private void Close() { isOpen = false; Refresh(); }

    /// <summary>거리와 성장 설정을 다시 확인해 이 창고만 강화합니다.</summary>
    private void Buy()
    {
        if (!isNear || storage == null) return;
        Managers.Growth.TryUpgradeStorage(storage);
        Refresh();
    }

    /// <summary>테스트용으로 이 창고의 단계만 초기화하며 재료와 재화는 보존합니다.</summary>
    private void ResetLevel()
    {
        if (!isNear || storage == null) return;
        Managers.Growth.ResetStorageLevel(storage);
        Refresh();
    }

    /// <summary>현재 공용 재고 용량, 다음 단계와 구매 가능 여부를 표시합니다.</summary>
    private void Refresh()
    {
        if (!_init) return;
        GetObject((int)Objects.OpenButtonRoot).SetActive(isNear && !isOpen);
        GetObject((int)Objects.UpgradePanel).SetActive(isNear && isOpen);
        if (!isNear || !isOpen || storage == null) return;
        StorageUpgradeData data = storage.UpgradeData;
        int level = storage.UpgradeLevel;
        if (data == null || string.IsNullOrWhiteSpace(storage.UpgradeId))
        {
            GetText((int)Texts.Text_Info).text = "강화 설정 또는 창고 ID가 없습니다";
            GetText((int)Texts.Text_Buy).text = "구매 불가";
            GetButton((int)Buttons.Button_Buy).interactable = false;
            return;
        }
        int extra = data.GetExtraCapacity(level);
        GetText((int)Texts.Text_Info).text = $"창고 강화  Lv.{level}\n공용 용량 {Managers.Ingredient.Capacity} (이 창고 강화 +{extra})";
        bool hasNext = data.TryGetStep(level, out StorageUpgradeStep step);
        PlayerController player = PlayerController.Instance;
        if (hasNext)
        {
            GetText((int)Texts.Text_Buy).text = $"용량 +{step.ExtraCapacity - extra}  {step.Price}";
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
