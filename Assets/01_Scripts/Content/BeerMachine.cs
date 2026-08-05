using UnityEngine;
using static Define;

public class BeerMachine : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController player)
    {
        // 1. 기계에서 맥주 1잔 감소
        // 2. 플레이어의 인벤토리(손)에 맥주 추가
        //player.PickUpItem(ItemType.Beer);
        Debug.Log("맥주를 챙겼습니다!");
    }

    public bool CanInteract(PlayerController player)
    {
        //return !player.HasItem(); // 플레이어 손이 비어있을 때만 가능
        return true;
    }
}