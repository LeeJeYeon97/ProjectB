using System;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerController : BaseController
{
    private const string MoneySaveKey = "ProjectB.Player.Money";
    public static PlayerController Instance { get; private set; }

    public int money = 100;
    public event Action<int> OnMoneyChanged;

    public float throwInterval = 0.1f; // 0.1초마다 1번씩 돈을 날림
    public int throwAmount = 1;       // 한 번에 1원(또는 지정된 금액)씩 날림

    public StackingSystem Stacking;
    [SerializeField] private float baseInteractionTimeScale = 1f;
    private float interactionTimeScale = 1f;

    /// <summary>플레이어 능력치 업그레이드가 반영된 상호작용 시간 배율입니다.</summary>
    public float InteractionTimeScale => interactionTimeScale;

    private void Awake()
    {
        Instance = this;
        Stacking = GetComponentInChildren<StackingSystem>();
        if (Managers.PlayerPrefs.HasKey(MoneySaveKey))
            money = Managers.PlayerPrefs.GetInt(MoneySaveKey, money);
        if (Managers.Growth != null)
            ApplyGrowth(Managers.Growth.GetTotalValue("player_speed"), Managers.Growth.GetTotalValue("player_stack"), Managers.Growth.GetTotalValue("player_interact"));

        // 이 씬의 플레이어를 공용 카메라에 연결해 씬마다 같은 추적 연출을 사용합니다.
        Managers.Camera.SetTarget(transform);
    }

    /// <summary>구매한 플레이어 능력치를 이동, 적재, 상호작용에 적용합니다.</summary>
    public void ApplyGrowth(float speedBonus, float stackBonus, float interactionReduction)
    {
        ThirdPersonController movement = GetComponent<ThirdPersonController>();
        if (movement != null)
            movement.SetGrowthSpeedBonus(speedBonus);

        if (Stacking != null)
            Stacking.SetGrowthCapacityBonus(Mathf.RoundToInt(stackBonus));

        interactionTimeScale = Mathf.Clamp(baseInteractionTimeScale - interactionReduction, 0.25f, 1f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    public void UpdateMoney(int addMoney)
    {
        money += addMoney;
        SaveMoney();
        OnMoneyChanged?.Invoke(money);
    }

    /// <summary>재화가 충분할 때만 차감해 구매 실패로 잔액이 음수가 되지 않게 합니다.</summary>
    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0 || money < amount)
            return false;

        UpdateMoney(-amount);
        return true;
    }

    public void SetMoney(int newMoney)
    {
        money = Mathf.Max(0, newMoney);
        SaveMoney();
        OnMoneyChanged?.Invoke(money);
    }

    /// <summary>재화 잔액을 저장해 구매한 업그레이드와 잔액이 함께 유지되게 합니다.</summary>
    private void SaveMoney()
    {
        Managers.PlayerPrefs.SetInt(MoneySaveKey, money);
    }

    /// <summary>화면을 닫거나 앱이 백그라운드로 갈 때 누적 재화를 디스크에 기록합니다.</summary>
    private void OnApplicationPause(bool paused)
    {
        if (paused)
            Managers.PlayerPrefs.Save();
    }

    /// <summary>앱을 종료하기 전에 메모리에 반영된 잔액을 마지막으로 저장합니다.</summary>
    private void OnApplicationQuit()
    {
        Managers.PlayerPrefs.Save();
    }
}
