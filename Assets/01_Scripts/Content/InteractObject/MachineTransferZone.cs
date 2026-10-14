using System.Collections.Generic;
using UnityEngine;

/// <summary>기계의 잔 이동 구역에서 플레이어의 물건을 자동으로 옮깁니다. 제조 구역과 겹쳐도 사용할 수 있습니다.</summary>
[RequireComponent(typeof(BoxCollider))]
public class MachineTransferZone : MonoBehaviour
{
    [SerializeField] private Define.MachineTransferType transferType;
    [SerializeField] private Sink sink;
    [SerializeField] private InteractableBase machine;
    [SerializeField, Min(0f)] private float transferInterval = 0.5f;

    private readonly HashSet<Collider> playerCollidersInside = new HashSet<Collider>();
    private float nextTransferTime;

    /// <summary>플레이어의 콜라이더를 기록하고 구역 진입 시 가능한 잔 이동을 시도합니다.</summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() == null)
            return;

        playerCollidersInside.Add(other);
        TryTransfer(other);
    }

    /// <summary>구역을 떠난 콜라이더만 제외해 몸의 다른 부분이 남아 있으면 작업을 이어갑니다.</summary>
    private void OnTriggerExit(Collider other)
    {
        playerCollidersInside.Remove(other);
    }

    /// <summary>플레이어가 가만히 서 있어도 간격이 지나면 남은 잔을 다시 옮깁니다.</summary>
    private void Update()
    {
        if (Time.time < nextTransferTime || playerCollidersInside.Count == 0)
            return;

        // 파괴되거나 꺼진 플레이어 콜라이더는 트리거 퇴장 알림이 없을 수도 있어 정리합니다.
        playerCollidersInside.RemoveWhere(item => item == null || !item.enabled || !item.gameObject.activeInHierarchy);
        foreach (Collider item in playerCollidersInside)
        {
            if (TryTransfer(item))
                break;
        }
    }

    /// <summary>플레이어의 손과 기계 상태를 확인하고 이 구역에 정해진 동작만 실행합니다.</summary>
    private bool TryTransfer(Collider other)
    {
        if (Time.time < nextTransferTime)
            return false;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
            return false;

        bool moved = false;
        switch (transferType)
        {
            case Define.MachineTransferType.TakeCleanDish:
                moved = sink != null && sink.TryTakeCleanDish(player.gameObject);
                break;

            case Define.MachineTransferType.LoadCleanDish:
                // 프리팹에서 연결한 제조 객체의 공통 기능으로 빈 잔을 옮깁니다.
                moved = machine != null && machine.TryLoadCleanDish(player.gameObject);
                break;
        }

        // 성공한 뒤에만 기다려 재고가 나중에 생겼을 때 바로 첫 잔을 옮길 수 있게 합니다.
        if (moved)
            nextTransferTime = Time.time + transferInterval;

        return moved;
    }

    /// <summary>비활성화된 구역이 이전 플레이어 콜라이더를 다시 사용하지 않도록 비웁니다.</summary>
    private void OnDisable()
    {
        playerCollidersInside.Clear();
        nextTransferTime = 0f;
    }

    /// <summary>구역의 콜라이더가 실수로 일반 충돌이 되지 않도록 에디터에서 바로 잡습니다.</summary>
    private void OnValidate()
    {
        BoxCollider zoneCollider = GetComponent<BoxCollider>();
        if (zoneCollider != null)
            zoneCollider.isTrigger = true;
    }
}
