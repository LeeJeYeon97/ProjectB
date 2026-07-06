using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class StackingSystem : MonoBehaviour
{
    [Header("Stack Settings")]
    public Transform stackPoint;       // 머리 위 스택 기준점
    public int maxStackCount = 10;     // 한 번에 들 수 있는 최대 개수
    public float itemHeight = 0.5f;    // 아이템 하나가 쌓일 때마다 올라갈 Y축 높이 (아이템 크기에 맞춰 조절)

    // 현재 들고 있는 아이템들을 관리할 리스트
    private List<GameObject> _stackedItems = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        // 1. 부딪힌 물체의 태그가 "Item"인지, 그리고 가방(리스트)에 자리가 있는지 확인
        if (other.CompareTag("Item") && _stackedItems.Count < maxStackCount)
        {
            PickUpItem(other.gameObject);
        }
    }

    private void PickUpItem(GameObject item)
    {
        // 1. 이미 주운 아이템이 또 트리거되는 것을 방지하기 위해 콜라이더 끄기
        Collider col = item.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 2. 아이템의 부모를 플레이어의 머리 위(stackPoint)로 변경
        item.transform.SetParent(stackPoint);

        // 3. 몇 번째 아이템인지 계산하여 Y축 위치(높이) 정해주기
        int currentCount = _stackedItems.Count;
        Vector3 targetPosition = new Vector3(0, currentCount * itemHeight, 0);
        // (DOTween 사용 시) 순간이동 대신 부드럽게 점프해서 쌓이기
        // 4. 위치와 회전값 초기화 적용
        //item.transform.localPosition = targetPosition;
        item.transform.DOLocalJump(targetPosition, jumpPower: 2f, numJumps: 1, duration: 0.3f);

        item.transform.localRotation = Quaternion.identity; // 회전값 초기화(똑바로 서게)

        // 5. 관리 리스트에 추가
        _stackedItems.Add(item);

        Debug.Log($"아이템 획득! 현재 개수: {_stackedItems.Count}");
    }
}
