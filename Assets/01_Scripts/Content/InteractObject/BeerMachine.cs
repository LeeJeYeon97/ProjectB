using DG.Tweening;
using UnityEngine;

public class BeerMachine : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject beerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float produceDuration = 1.5f;

    public float Duration => produceDuration;

    public bool CanInteract(GameObject actor)
    {
        var player = actor.GetComponent<PlayerController>();
        if (player == null || player.Stacking == null) return false;

        // 플레이어 손에 맥주를 더 들 수 있는지 확인
        return player.Stacking.CheckListItem(Define.ItemType.Beer);
    }

    public void OnCompleteInteract(GameObject actor)
    {
        var player = actor.GetComponent<PlayerController>();
        if (player == null) return;

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        GameObject beer = Managers.Resource.Instantiate("Item_Beer", transform.position, Quaternion.identity);

        player.Stacking.PickUpItem(beer);

        Debug.Log("[BeerStrategy] 맥주 생산 완료!");
    }
}