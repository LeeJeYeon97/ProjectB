using System;
using UnityEngine;

public class ItemController : MonoBehaviour
{
    public Define.StackType StackType;
    public Define.ItemType type;
    [Tooltip("메뉴 아이템일 때만 연결하는 제조 레시피입니다.")]
    [SerializeField] private FoodRecipeData menuRecipe;
    public FoodRecipeData MenuRecipe => menuRecipe;
    public int Value;


    public float itemHeight = 0.5f;    // 아이템 하나가 쌓일 때마다 올라갈 Y축 높이 (아이템 크기에 맞춰 조절)ㄴ


    /// <summary>스택 점프가 끝난 뒤 실행할 아이템 고유 처리를 합니다.</summary>
    public void OnPickedUpComplete(Action onJumpComplete)
    {
        // 스택 점프가 끝난 뒤 아이템 종류별 획득 처리를 실행합니다.
        onJumpComplete?.Invoke();
        if (StackType == Define.StackType.NoStack)
            Managers.Resource.Destroy(gameObject);

        if (type == Define.ItemType.Money && PlayerController.Instance != null)
            PlayerController.Instance.UpdateMoney(Value);
    }
}
