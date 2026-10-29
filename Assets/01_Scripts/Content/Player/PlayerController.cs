using System;
using UnityEngine;

public class PlayerController : BaseController
{
    #region 변수

    private const string MoneySaveKey = "ProjectB.Player.Money";
    public static PlayerController Instance { get; private set; }

    public int money = 100;
    public event Action<int> OnMoneyChanged;

    public StackingSystem Stacking;
    [SerializeField] private float baseInteractionTimeScale = 1f;
    private float interactionTimeScale = 1f;
    private ThirdPersonController movement;
    private ScreenMoveInput screenMoveInput;
    private CharacterController characterController;
    private Animator animator;
    private float baseMoveSpeed;
    // 한 플레이어가 동시에 여러 대상의 작업을 시작하지 않도록 현재 대상을 보관합니다.
    private InteractableBase currentInteraction;

    /// <summary>플레이어 능력치 업그레이드가 반영된 상호작용 시간 배율입니다.</summary>
    public float InteractionTimeScale => interactionTimeScale;

    #endregion

    #region 유니티 함수

    private void Awake()
    {
        Instance = this;
        Stacking = GetComponentInChildren<StackingSystem>();
        movement = GetComponent<ThirdPersonController>();
        screenMoveInput = GetComponent<ScreenMoveInput>();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // 이동 코드는 화면 입력과 성장 시스템을 모르며, 플레이어가 두 기능을 연결합니다.
        if (movement != null)
        {
            baseMoveSpeed = movement.MoveSpeed;
            movement.ExtraMoveDirection = ReadScreenMoveDirection;
            movement.MovementDisabled += ClearScreenMove;
        }

        if (Managers.PlayerPrefs.HasKey(MoneySaveKey))
            money = Managers.PlayerPrefs.GetInt(MoneySaveKey, money);
        if (Managers.Growth != null)
            ApplyGrowth(Managers.Growth.GetTotalValue("player_speed"), Managers.Growth.GetTotalValue("player_stack"), Managers.Growth.GetTotalValue("player_interact"));

        // 이 씬의 플레이어를 공용 카메라에 연결해 씬마다 같은 추적 연출을 사용합니다.
        Managers.Camera.SetTarget(transform);
    }

    /// <summary>이동이 끝난 프레임의 속도와 운반 상태를 플레이어 전용 애니메이션에 반영합니다.</summary>
    private void LateUpdate()
    {
        if (animator == null || characterController == null)
            return;

        if (movement == null || !movement.isActiveAndEnabled)
            return;

        Vector3 velocity = characterController.velocity;
        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        animator.SetFloat("Speed", horizontalVelocity.magnitude);

        // 적재 시스템의 아이템만 운반 애니메이션에 포함합니다.
        bool isCarrying = Stacking != null && Stacking.StackCount > 0;
        animator.SetBool("IsCarrying", isCarrying);
    }

    /// <summary>플레이어가 꺼질 때 화면 드래그의 이전 누름 상태를 지웁니다.</summary>
    private void OnDisable()
    {
        // 플레이어가 꺼지면 트리거 퇴장 없이도 진행 중인 작업과 선점권을 정리합니다.
        if (currentInteraction != null)
        {
            currentInteraction.CancelPlayerInteraction(this);
        }
        currentInteraction = null;
        ClearScreenMove();
    }

    private void OnDestroy()
    {
        if (movement != null)
        {
            movement.MovementDisabled -= ClearScreenMove;
            movement.ExtraMoveDirection = null;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>화면이나 앱을 닫기 전에 누적 재화를 디스크에 기록합니다.</summary>
    private void OnApplicationPause(bool paused)
    {
        if (paused)
            Managers.PlayerPrefs.Save();
    }

    /// <summary>앱이 종료되기 전에 누적 재화를 마지막으로 저장합니다.</summary>
    private void OnApplicationQuit()
    {
        Managers.PlayerPrefs.Save();
    }

    #endregion

    #region 상호작용 대상 관리

    /// <summary>다른 대상을 사용 중이지 않으면 이 대상을 현재 작업으로 등록합니다.</summary>
    public bool TryStartInteraction(InteractableBase target)
    {
        if (target == null) return false;
        if (currentInteraction != null && currentInteraction != target) return false;
        currentInteraction = target;
        return true;
    }

    /// <summary>현재 사용 중인 대상만 자신의 작업 등록을 해제할 수 있습니다.</summary>
    public void EndInteraction(InteractableBase target)
    {
        if (currentInteraction == target) currentInteraction = null;
    }

    /// <summary>자동 회수처럼 즉시 끝나는 동작도 다른 작업 중에는 시작하지 않습니다.</summary>
    public bool CanStartInstantInteraction()
    {
        return currentInteraction == null;
    }

    #endregion

    #region 성장과 화면 입력

    /// <summary>구매한 플레이어 능력치를 이동, 적재, 상호작용에 적용합니다.</summary>
    public void ApplyGrowth(float speedBonus, float stackBonus, float interactionReduction)
    {
        if (movement != null)
            movement.MoveSpeed = baseMoveSpeed + Mathf.Max(0f, speedBonus);

        if (Stacking != null)
            Stacking.SetGrowthCapacityBonus(Mathf.RoundToInt(stackBonus));

        interactionTimeScale = Mathf.Clamp(baseInteractionTimeScale - interactionReduction, 0.25f, 1f);
    }

    /// <summary>플레이어 화면 드래그 입력을 이동 코드에 방향 값으로 전달합니다.</summary>
    private Vector2 ReadScreenMoveDirection()
    {
        if (screenMoveInput == null)
            return Vector2.zero;

        if (screenMoveInput.TryGetDirection(out Vector2 direction))
            return direction;

        return Vector2.zero;
    }

    /// <summary>이동 컴포넌트를 끌 때 이전 드래그 입력을 정리합니다.</summary>
    private void ClearScreenMove()
    {
        screenMoveInput?.Clear();
    }

    #endregion

    #region 재화와 저장

    /// <summary>플레이어 재화를 늘리고 저장값과 화면 표시를 갱신합니다.</summary>
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

    #endregion
}
