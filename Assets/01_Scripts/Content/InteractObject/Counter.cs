using System.Collections.Generic;
using UnityEngine;

public class Counter : InteractableBase
{
    protected override Define.InteractionType DefaultInteractionType => Define.InteractionType.CollectPayment;
    [Header("Money Spawn Settings")]
    [SerializeField] private GameObject moneyPrefab;      // 돈 프리팹 (Item_Money)
    [SerializeField] private Transform moneySpawnPoint;    // 돈이 스폰될 카운터 상판 좌표

    [Header("돈 쌓기 설정")]
    [Tooltip("한 줄에 놓을 돈 묶음 수입니다.")]
    [SerializeField, Min(1)] private int moneyColumns = 3;
    [Tooltip("한 층에 놓을 줄 수입니다. 가로 칸 × 줄 수를 채우면 위층에 놓습니다.")]
    [SerializeField, Min(1)] private int moneyRows = 3;
    [Tooltip("돈 생성 지점 기준의 가로(X), 층 높이(Y), 세로(Z) 간격입니다.")]
    [SerializeField] private Vector3 moneyGap = new Vector3(0.38f, 0.14f, 0.18f);

    // 이 카운터에 남아 있는 돈만 생성 순서대로 보관합니다.
    // 실제 금액은 각 돈의 Value에 그대로 두므로 배치가 바뀌어도 금액은 변하지 않습니다.
    private readonly List<ItemController> moneyItems = new();

    [SerializeField] private Transform payPoint; //손님이 돈을 낼 위치
    public Transform PayPoint => payPoint;

    public Transform MoneySpawnPoint => moneySpawnPoint != null ? moneySpawnPoint : transform;

    [Header("수금 설정")]
    [SerializeField] private float collectDuration = 0.5f;
    private readonly Queue<CustomerController> waitingCustomers = new();

    public override float Duration => collectDuration;
    public bool HasWaitingCustomers => waitingCustomers.Count > 0;

    protected override void Awake()
    {
        base.Awake();
        Managers.Game.Counter = this;
        if (moneySpawnPoint == null)
        {
            var foundPoint = transform.Find("MoneySpawnPoint");
            if (foundPoint != null)
            {
                moneySpawnPoint = foundPoint;
            }
            else
            {
                moneySpawnPoint = transform;
            }
        }
    }

    /// <summary>결제를 기다리는 손님을 카운터 대기 목록에 추가합니다.</summary>
    public void AddWaitingCustomer(CustomerController customer)
    {
        if (customer != null && !waitingCustomers.Contains(customer))
        {
            waitingCustomers.Enqueue(customer);
            // 손님이 결제를 기다리기 시작하면 카운터 수금 작업을 등록합니다.
            Managers.Task.CreateTask(Define.EmployeeTaskType.Casher, this);
        }
    }

    /// <summary>대기 손님이 있고 플레이어나 직원이 범위에 있을 때 수금을 허용합니다.</summary>
    public override bool CanInteract(GameObject actor)
    {
        return waitingCustomers.Count > 0 &&
               (actor.GetComponent<PlayerController>() != null || actor.GetComponent<EmployeeController>() != null);
    }

    /// <summary>상호작용이 끝나면 한 손님의 결제를 받고 손님을 퇴장시킵니다.</summary>
    public override void OnCompleteInteract(GameObject actor)
    {
        if (!CanInteract(actor))
            return;

        CollectForEmployee();
    }

    /// <summary>대기 손님 한 명의 결제를 처리해 플레이어와 직원이 같은 결과를 사용합니다.</summary>
    public bool CollectForEmployee()
    {
        if (!HasWaitingCustomers)
            return false;

        CustomerController customer = waitingCustomers.Dequeue();
        if (customer == null || customer.CurrentState != Define.CustomerState.WaitingToPay)
        {
            Managers.Task.AssignWaitingTasks();
            return false;
        }

        // 손님이 기다린 시간과 식사 중 방해 기록을 리뷰로 바꾸고, 평판이 반영된 금액을 지급합니다.
        ReviewResult review = Managers.Review.CreateReview(
            customer.FoodWaitSeconds,
            customer.MealInterruptionCount,
            customer.payAmount,
            customer.ServedMenuCorrectly);
        CreateMoney(review.FinalPay);
        customer.CompletePayment();
        return true;
    }

    /// <summary>수집된 돈을 제외하고 남은 돈을 아래층부터 빈틈없이 다시 배치합니다.</summary>
    private void LateUpdate()
    {
        RefreshMoney();
    }

    /// <summary>카운터를 떠난 돈을 목록에서 빼고, 빈자리가 생겼을 때만 배치를 갱신합니다.</summary>
    private void RefreshMoney()
    {
        bool changed = false;
        for (int i = moneyItems.Count - 1; i >= 0; i--)
        {
            ItemController item = moneyItems[i];
            // 수집이 시작되면 기존 수집 코드가 부모를 플레이어의 적재 지점으로 바꿉니다.
            // 따라서 도착 애니메이션이 끝나기 전부터 해당 자리를 비울 수 있습니다.
            if (item == null || !item.gameObject.activeInHierarchy || item.transform.parent != MoneySpawnPoint)
            {
                moneyItems.RemoveAt(i);
                changed = true;
            }
        }

        if (!changed)
            return;

        for (int i = 0; i < moneyItems.Count; i++)
            moneyItems[i].transform.localPosition = GetMoneyPosition(i);
    }

    /// <summary>순번을 가로 칸, 세로 줄, 층으로 나누어 박스 형태의 배치 위치를 계산합니다.</summary>
    private Vector3 GetMoneyPosition(int index)
    {
        // 저장된 설정이 잘못되어도 나눗셈에 0이 들어가지 않도록 보정합니다.
        int columns = Mathf.Max(1, moneyColumns);
        int rows = Mathf.Max(1, moneyRows);
        int column = index % columns;
        int row = (index / columns) % rows;
        int layer = (index / columns) / rows;

        // 생성 지점을 바닥 면의 중심으로 사용합니다. 가로부터 채운 뒤 다음 줄, 다음 층으로 갑니다.
        // 간격은 양수로 보정하여 실수로 음수를 입력해도 아래쪽으로 쌓이지 않게 합니다.
        return new Vector3(
            (column - (columns - 1) * 0.5f) * Mathf.Max(0.01f, moneyGap.x),
            layer * Mathf.Max(0.01f, moneyGap.y),
            (row - (rows - 1) * 0.5f) * Mathf.Max(0.01f, moneyGap.z));
    }

    /// <summary>결제 한 건의 금액을 돈 묶음 하나에 저장하고 다음 빈칸에 놓습니다.</summary>
    private void CreateMoney(int amount)
    {
        // 같은 프레임에 돈을 주웠다면 비워진 자리를 먼저 정리합니다.
        RefreshMoney();
        Transform point = MoneySpawnPoint;
        Vector3 localPosition = GetMoneyPosition(moneyItems.Count);
        Vector3 spawnPos = point.TransformPoint(localPosition);

        GameObject moneyGo = null;
        if (moneyPrefab != null)
        {
            moneyGo = Managers.Resource.Instantiate(moneyPrefab, spawnPos, point.rotation, point);
        }
        else
        {
            moneyGo = Managers.Resource.Instantiate("Item_Money", spawnPos, point.rotation, point);
        }

        if (moneyGo != null)
        {
            moneyGo.tag = "Item";

            ItemController item = moneyGo.GetComponent<ItemController>();
            if (item != null)
            {
                item.Value = amount;
                moneyItems.Add(item);

                // 풀에서 재사용한 돈도 이전 수집 때 꺼진 충돌체를 다시 켜서 주울 수 있게 합니다.
                Collider moneyCollider = moneyGo.GetComponent<Collider>();
                if (moneyCollider != null)
                    moneyCollider.enabled = true;
            }
        }

        Debug.Log($"[Counter] {amount}원 결제를 받아 위치 {spawnPos}에 돈을 생성했습니다.");
    }
}
