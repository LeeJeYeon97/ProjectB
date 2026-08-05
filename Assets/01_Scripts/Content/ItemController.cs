using DG.Tweening;
using System;
using UnityEngine;

public class ItemController : MonoBehaviour
{
    public Define.StackType StackType;
    public Define.ItemType type;
    private Collider _collider;


    public float itemHeight = 0.5f;    // 아이템 하나가 쌓일 때마다 올라갈 Y축 높이 (아이템 크기에 맞춰 조절)


    private void Awake()
    {
        // 콜라이더를 미리 캐싱하여 사용합니다.
        _collider = GetComponent<Collider>();
    }
    public void OnPickedUp(Transform stackPoint, int currentStackCount, Action onJumpComplete)
    {
        if (_collider != null)
        {
            _collider.enabled = false;
        }

        // 스택 아이템 처리
        // 2. 아이템의 부모를 플레이어의 머리 위(stackPoint)로 변경
        transform.SetParent(stackPoint);

        // 3. 몇 번째 아이템인지 계산하여 Y축 위치(높이) 정해주기
        int currentCount = currentStackCount;
        Vector3 targetPosition = new Vector3(0, currentCount * itemHeight, 0);

        // 4. 위치와 회전값 초기화 적용
        transform.DOLocalJump(targetPosition, jumpPower: 2f, numJumps: 1, duration: 0.3f)
            .OnComplete(() =>
            {
                transform.localRotation = Quaternion.identity; // 회전값 초기화

                if (StackType == Define.StackType.Stack)
                {
                    // 완료 콜백 실행 (필요한 경우)
                    onJumpComplete?.Invoke();
                }
                else
                {
                    // 스택 되지 않는 아이템
                    Destroy(this.gameObject);
                }

                // 타입별 처리
                switch (type)
                {
                    case Define.ItemType.Money:
                        Debug.Log("돈 먹었어요");
                        break;
                }
            });
        transform.localRotation = Quaternion.identity; // 회전값 초기화
    }
}
