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
    [SerializeField]
    private List<GameObject> _stackedItems = new List<GameObject>();

    public IReadOnlyList<GameObject> StackedItems => _stackedItems;
    public int StackCount => _stackedItems.Count;
    public bool IsFull => _stackedItems.Count >= maxStackCount;

    private void OnTriggerEnter(Collider other)
    {
        // 아이템 외의 물체는 무시하고, 아이템 정보가 없는 오브젝트도 주울 수 없게 합니다.
        if (!other.CompareTag("Item"))
            return;

        ItemController item = other.GetComponent<ItemController>();
        if (item == null)
            return;

        // 돈처럼 스택에 쌓이지 않는 아이템은 손에 든 물건의 종류나 스택 자리와 상관없이 수집합니다.
        if (item.StackType == Define.StackType.NoStack)
        {
            PickUpItem(other.gameObject);
            return;
        }

        // 스택 아이템만 기존 종류 일치 및 최대 개수 검사를 통과해야 수집합니다.
        if (CheckListItem(other.gameObject))
        {
            PickUpItem(other.gameObject);
        }
    }

    public bool CheckListItem(GameObject go)
    {
        if (go == null) return false;

        ItemController item = go.GetComponent<ItemController>();
        if (item == null)
        {
            return false;
        }

        if (_stackedItems.Count >= maxStackCount)
            return false;

        ItemController newItem = go.GetComponent<ItemController>();
        if (newItem == null) return false;

        // 아이템이 있는데 다른 아이템을 주우려는 경우
        if (_stackedItems.Count > 0)
        {
            ItemController firstItem = _stackedItems[0] != null ? _stackedItems[0].GetComponent<ItemController>() : null;
            if (firstItem != null && firstItem.type != newItem.type)
            {
                return false;
            }
        }
        return true;
    }
    
    public bool CheckListItem(Define.ItemType type)
    {
        if (_stackedItems.Count >= maxStackCount)
            return false;

        // 스택해야 하는 아이템인데 손에 아이템이 있는데 다른 아이템을 주우려는 경우
        if (_stackedItems.Count > 0)
        {
            ItemController firstItem = _stackedItems[0] != null ? _stackedItems[0].GetComponent<ItemController>() : null;
            if (firstItem != null && firstItem.type != type)
            {
                return false;
            }
        }
        return true;
    }

    public void PickUpItem(GameObject go)
    {
        if (go == null) return;
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

    public GameObject PopItem()
    {
        if (_stackedItems.Count == 0) return null;
        int lastIndex = _stackedItems.Count - 1;
        GameObject lastItem = _stackedItems[lastIndex];
        _stackedItems.RemoveAt(lastIndex);

        Managers.Resource.Destroy(lastItem);

        return lastItem;
    }

    public Define.ItemType GetFirstListItemType()
    {
        if (_stackedItems.Count == 0) return Define.ItemType.None;
        int lastIndex = _stackedItems.Count - 1;
        GameObject go = _stackedItems[lastIndex];

        if(go == null) return Define.ItemType.None;

        ItemController lastItem = go.GetComponent<ItemController>();

        if (lastItem == null) return Define.ItemType.None;

        return lastItem.type;

        
    }
}
