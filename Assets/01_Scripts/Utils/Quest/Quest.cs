using UnityEngine;

/// <summary>퀘스트 하나의 실행 상태와 완료 처리를 관리합니다.</summary>
[System.Serializable]
public class Quest
{
    /// <summary>퀘스트 고정 설정을 보관하는 ScriptableObject입니다.</summary>
    public QuestData Data { get; private set; }

    /// <summary>현재 퀘스트에서 달성한 진행 수치입니다.</summary>
    public int Progress { get; private set; }

    /// <summary>이 퀘스트가 시작되었는지 표시합니다.</summary>
    public bool IsStarted { get; private set; }

    /// <summary>이 퀘스트가 완료되었는지 표시합니다.</summary>
    public bool IsCompleted { get; private set; }

    /// <summary>설정 에셋을 받아 새 퀘스트 실행 상태를 준비합니다.</summary>
    public Quest(QuestData data)
    {
        // ScriptableObject에는 공통 설정만 두고, 플레이 중 값은 이 실행 객체에 보관합니다.
        Data = data;
        Progress = 0;
        IsStarted = false;
        IsCompleted = false;
    }

    /// <summary>퀘스트를 시작하고 진행도를 처음부터 시작합니다.</summary>
    public void Start()
    {
        // 시작되었거나 완료된 퀘스트가 다시 초기화되지 않게 상태를 먼저 확인합니다.
        if (Data == null || IsStarted || IsCompleted)
            return;

        Progress = 0;
        IsStarted = true;
    }

    /// <summary>현재 퀘스트의 진행 수치를 목표 범위 안에서 갱신합니다.</summary>
    public void UpdateProgress(int progress)
    {
        // 시작 전이나 완료된 목표는 외부에서 들어온 진행 알림을 받지 않습니다.
        if (!IsStarted || IsCompleted || Data == null)
            return;

        Progress = Mathf.Clamp(progress, 0, Mathf.Max(0, Data.targetAmount));
    }

    /// <summary>새 행동 횟수를 현재 진행도에 더하고 목표 수량까지만 저장합니다.</summary>
    public void AddProgress(int amount)
    {
        // 진행 중이 아니거나 잘못된 수량이면 퀘스트 상태를 바꾸지 않습니다.
        if (!IsStarted || IsCompleted || Data == null || amount <= 0)
            return;

        // 기존 진행도에 이번 행동 횟수를 더해 요구 수량을 넘지 않도록 제한합니다.
        UpdateProgress(Progress + amount);
    }

    /// <summary>퀘스트의 진행 상태를 완료로 바꿉니다.</summary>
    public void Complete()
    {
        // 완료 상태와 진행 수치를 기록해 같은 퀘스트가 중복 완료되지 않게 합니다.
        if (!IsStarted || IsCompleted || Data == null)
            return;

        Progress = Mathf.Max(0, Data.targetAmount);
        IsCompleted = true;
        IsStarted = false;
    }
}
