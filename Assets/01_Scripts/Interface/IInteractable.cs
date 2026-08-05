public interface IInteractable
{
    // 상호작용을 실행하는 함수
    void Interact(PlayerController player);

    // 현재 상호작용이 가능한 상태인지 체크 (예: 기계에 맥주가 찼는지)
    bool CanInteract(PlayerController player);
}