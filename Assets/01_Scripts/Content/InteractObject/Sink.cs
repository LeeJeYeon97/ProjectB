using System;
using System.Collections;
using UnityEngine;

public class Sink : MonoBehaviour
{
    [Header("설거지 설정")]
    [SerializeField] private float workDuration = 3.0f;
    [SerializeField] private int processAmount = 1;

    private bool isWorking;

    public float WorkDuration => workDuration;
    public int ProcessAmount => processAmount;
    public bool IsWorking => isWorking;


    /// <summary>
    /// 설거지 작업을 시작합니다.
    /// </summary>
    /// <param name="dirtyDishCount">현재 가지고 있는 더러운 접시 수</param>
    /// <param name="onComplete">설거지 완료 시 호출할 콜백</param>
    public void StartWashing(int dirtyDishCount, Action<int> onComplete)
    {
        if (isWorking)
            return;

        if (dirtyDishCount <= 0)
            return;

        int washingAmount = Mathf.Min(processAmount, dirtyDishCount);

        StartCoroutine(WashingCoroutine(washingAmount, onComplete));
    }


    private IEnumerator WashingCoroutine(int washingAmount, Action<int> onComplete)
    {
        isWorking = true;

        // 실제 설거지 시간
        yield return new WaitForSeconds(workDuration);

        // 설거지 완료
        onComplete?.Invoke(washingAmount);

        isWorking = false;
    }
}