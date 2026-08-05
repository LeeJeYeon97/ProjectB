using UnityEngine;

public abstract class BaseStation : MonoBehaviour
{
    [Header("Base Station Settings")]
    public string stationID;

    // 이 발판이 현재 작동 가능한 상태인지 여부 (해금되기 전엔 false 등)
    public bool isOperational = true;

    // 플레이어나 AI가 발판에 들어왔을 때 호출
    public virtual void OnCharacterEnter(GameObject character) { }

    // 발판 위에 머물러 있을 때 (주기적 상호작용)
    public abstract void OnCharacterStay(GameObject character);

    // 발판에서 나갔을 때 호출
    public virtual void OnCharacterExit(GameObject character) { }
}