using System;
using UnityEngine;

public class Counter : MonoBehaviour
{
    [Header("Money Spawn Settings")]
    [SerializeField] private GameObject moneyPrefab;      // 돈 프리팹 (Item_Money)
    [SerializeField] private Transform moneySpawnPoint;    // 돈이 스폰될 카운터 상판 좌표

    [SerializeField] private Transform payPoint; //손님이 돈을 낼 위치
    public Transform PayPoint => payPoint;

    public Transform MoneySpawnPoint => moneySpawnPoint != null ? moneySpawnPoint : transform;

    private void Awake()
    {
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

    /// <summary>
    /// 손님이 결제했을 때 호출되어 지정된 위치에 돈을 생성합니다.
    /// </summary>
    public void ReceivePayment(int amount)
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

        Debug.Log($"[CounterController] 손님으로부터 {amount}원 결제를 수령하여 위치 {spawnPos}에 돈을 생성했습니다.");
    }
}
