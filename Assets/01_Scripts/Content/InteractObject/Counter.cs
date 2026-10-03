using System.Collections.Generic;
using UnityEngine;

public class Counter : InteractableBase
{
    [Header("Money Spawn Settings")]
    [SerializeField] private GameObject moneyPrefab;      // 돈 프리팹 (Item_Money)
    [SerializeField] private Transform moneySpawnPoint;    // 돈이 스폰될 카운터 상판 좌표

    [SerializeField] private Transform payPoint; //손님이 돈을 낼 위치
    public Transform PayPoint => payPoint;

    public Transform MoneySpawnPoint => moneySpawnPoint != null ? moneySpawnPoint : transform;

    [Header("수금 설정")]
    [SerializeField] private float collectDuration = 0.5f;
    private readonly Queue<CustomerController> waitingCustomers = new();

    public override float Duration => collectDuration;

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
            waitingCustomers.Enqueue(customer);
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

        CustomerController customer = waitingCustomers.Dequeue();
        CreateMoney(customer.payAmount);
        customer.CompletePayment();
    }

    /// <summary>결제 금액에 맞는 돈 오브젝트를 기존 방식대로 생성합니다.</summary>
    private void CreateMoney(int amount)
    {
        Vector3 spawnPos = MoneySpawnPoint.position;

        GameObject moneyGo = null;
        if (moneyPrefab != null)
        {
            moneyGo = Managers.Resource.Instantiate(moneyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            moneyGo = Managers.Resource.Instantiate("Item_Money", spawnPos, Quaternion.identity);
        }

        if (moneyGo != null)
        {
            moneyGo.tag = "Item";

            ItemController item = moneyGo.GetComponent<ItemController>();
            if (item != null)
            {
                item.Value = amount;
            }
        }

        Debug.Log($"[Counter] {amount}원 결제를 받아 위치 {spawnPos}에 돈을 생성했습니다.");
    }
}
