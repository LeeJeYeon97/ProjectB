using DG.Tweening;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StackingSystem : MonoBehaviour
{
    [Header("Stack Settings")]
    public Transform stackPoint;       // 머리 위 스택 기준점
    public int maxStackCount = 10;     // 한 번에 들 수 있는 최대 개수

    // 현재 들고 있는 아이템들을 관리할 리스트
    private List<GameObject> _stackedItems = new List<GameObject>();

    private void OnTriggerEnter(Collider other)
    {
        // 1. 부딪힌 물체의 태그가 "Item"인지, 그리고 가방(리스트)에 자리가 있는지 확인
        if (other.CompareTag("Item"))
        {
            if(CheckListItem(other.gameObject))
            {
                PickUpItem(other.gameObject);
            }
        }
    }
    private bool CheckListItem(GameObject go)
    {
        if (_stackedItems.Count >= maxStackCount)
            return false;

        // 아이템이 있는데 다른 아이템을 주우려는 경우
        if (_stackedItems.Count > 0)
        {
            if (_stackedItems[0].GetComponent<ItemController>().type != go.GetComponent<ItemController>().type)
            {
                return false;
            }
        }

        return true;
    }

    private void PickUpItem(GameObject go)
    {
        ItemController item = go.GetComponent<ItemController>();

        if (item != null)
        {
            // 스택 가능한 아이템인지 먼저 체크
            if (item.StackType == Define.StackType.Stack)
            {
                // 1. 현재 가방에 들어갈 '예정'인 높이(인덱스)를 먼저 계산
                int targetIndex = _stackedItems.Count;

                // 2. 리스트에 먼저 즉시 추가! (핵심)
                // 이렇게 해야 0.3초 안에 다음 아이템을 먹어도 높이가 정상적으로 올라가고, Max 개수도 초과하지 않습니다.
                _stackedItems.Add(go);

                // 3. 계산된 인덱스를 넘겨주고 아이템 점프 시작
                item.OnPickedUp(stackPoint, targetIndex, () =>
                {
                    // 리스트 추가는 위에서 이미 했으므로 여기는 비워둬도 됩니다.
                    // 나중에 도착 시 먼지가 일거나 캐릭터가 살짝 흔들리는 연출을 넣기 좋습니다.
                });
            }
            else
            {
                // 스택되지 않는 아이템 (예: 돈)
                item.OnPickedUp(stackPoint, 0, null);
            }
        }
    }
}
