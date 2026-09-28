using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CustomerSceneBinder : MonoBehaviour
{
    [Header("Spawn Positions")]
    public Transform spawnPosition;
    public List<Transform> entranceQueue;
    public Transform exitPoint;

    private void Awake()
    {
        if (spawnPosition == null) spawnPosition = transform;
        if (exitPoint == null) exitPoint = spawnPosition;
        // 게임이 시작될 때 전역 CustomerManager에 씬의 실제 포지션들을 넘겨줍니다.
        Managers.Customer.SetScenePositions(spawnPosition, entranceQueue, exitPoint);
    }
    // 유니티 에디터 씬(Scene) 뷰에서 기즈모를 그려주는 내장 함수
    private void OnDrawGizmos()
    {
        // 1. 스폰 위치 기즈모 (파란색 와이어 구체)
        if (spawnPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(spawnPosition.position, 0.5f);
        }

        // 2. 대기열(entranceQueue) 기즈모 (초록색 구체)
        if (entranceQueue != null && entranceQueue.Count > 0)
        {
            for (int i = 0; i < entranceQueue.Count; i++)
            {
                Transform queuePoint = entranceQueue[i];

                // 리스트 내부에 빈 값(null)이 있을 경우를 대비한 방어 코드
                if (queuePoint != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawSphere(queuePoint.position, 0.3f); // 반경 0.3짜리 구체

#if UNITY_EDITOR
                    // 씬 뷰에 대기열 번호 텍스트 띄우기 (글자가 오브젝트에 안 가려지게 살짝 위(Vector3.up)로 올림)
                    Handles.Label(queuePoint.position + Vector3.up * 0.5f, $"Queue {i + 1}");
#endif
                }
            }
        }
    }
}
