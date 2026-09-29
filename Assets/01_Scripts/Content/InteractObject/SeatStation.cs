using System.Collections;
using System.Reflection.Metadata;
using UnityEngine;
using static Define;

public class SeatStation : MonoBehaviour, IInteractable
{
    [Header("Seat Configuration")]
    [SerializeField]
    private SeatState currentState = SeatState.Empty;
    public SeatState CurrentState
    {
        get => currentState;
        private set
        {
            currentState = value;
            UpdateState();
        }
    }

    
    [Header("Interaction Settings")]
    private float interactionDuration = 1.5f; // 청소 또는 주문받기에 걸리는 시간
    public float Duration => interactionDuration;
    
    public CustomerController currentCustomer;
    public EmployeeController AssignedCleaner { get; set; }


    [Header("UI")]
    [SerializeField] private UI_Bubble uiBubble;

    private PlayerController _currentPlayer;

    [Header("position")]
    public Transform customerSitPoint; // 손님이 앉을 실제 3D 좌표/회전 기준점
    public Transform foodServePoint;   // 음식을 올려놓을 테이블 위 좌표

    [SerializeField] private Define.InterationType interactType;

    public Define.InterationType InteractionType => interactType;

    private void Start()
    {
        Init();
        UpdateState();
        Managers.Station.RegisterSeat(this);
    }

    private void Init()
    {
        if (uiBubble == null)
        {
            uiBubble = GetComponentInChildren<UI_Bubble>(true);
        }
        if (uiBubble == null)
        {
            GameObject bubblePrefab = Managers.Resource.Load<GameObject>("UI_Bubble");
#if UNITY_EDITOR
            if (bubblePrefab == null)
                bubblePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Prefabs/UI/SubItem/UI_Bubble.prefab");
#endif
            if (bubblePrefab != null)
            {
                GameObject go = Instantiate(bubblePrefab, transform);
                go.name = "UI_Bubble";
                uiBubble = go.GetComponent<UI_Bubble>();
            }
        }
        if (uiBubble != null)
        { 
            uiBubble.transform.localScale = Vector3.one * 0.01f;
            uiBubble.gameObject.SetActive(false);
        }

    }

    public void SetState(SeatState state)
    {
        CurrentState = state;
        UpdateState();
    }
    public void UpdateState()
    {
        if (uiBubble == null)
            uiBubble = GetComponentInChildren<UI_Bubble>(true);
        
        if (currentState == SeatState.Dirty)
        {
            if (uiBubble != null)
            {
                uiBubble.gameObject.SetActive(true);
                uiBubble.SetBubble(Define.BubbleType.Cleaning);
                Managers.Station.RegisterDirtyTable(this);
            }
        }
        else
        {
            if (uiBubble != null)
            {
                uiBubble.Hide();
            }
        }
    }


    private void Update()
    {
        float progress = 0;
        progress += Time.deltaTime;
        //OnUpdateInteract(progress);
        
    }
    public void SetCleanProgress(float ratio)
    {
        
    }

    public void CleanSeat()
    {
        CurrentState = SeatState.Empty;
        
        AssignedCleaner = null;
        
        
        Debug.Log("자리가 깨끗하게 치워졌습니다! 이제 새로운 손님을 받을 수 있습니다.");

        // 여기서 쓰레기통으로 쓰레기를 버릴지 접시를 싱크대로 옮길지 결정
        // TODO: 청소 이펙트 생성, 접시 오브젝트 파괴 / 스테이션 매니저 처리 추가 등

        if(_currentPlayer != null)
        {
            Debug.Log("플레이어가 접시를 주웠습니다.");
            GameObject go = Managers.Resource.Instantiate("Item_BeerEmpty");
            _currentPlayer.Stacking.PickUpItem(go);
        }
    }

    private bool HasMatchingFood(StackingSystem stacking)
    {
        if (stacking == null)
        {
            return false;
        }
        if (currentCustomer == null)
        {
            return false;
        }
        if(stacking.StackCount <= 0)
        {
            return false;
        }
        if (currentCustomer.OrderedType != ItemType.Beer)
        {
            return false;
        }
        // TODO : 리스트의 첫번째 아이템이 주문 타입과 같은지 확인 필요
        return true;
    }

    private void ServeFood(StackingSystem stacking)
    {
        stacking.PopItem();
        currentCustomer.OnFoodServed();
        Debug.Log("음식을 서빙했습니다. 식사를 시작합니다!");
    }

    public bool CanInteract(GameObject actor)
    {
        var player = actor.GetComponent<PlayerController>();
        if (player == null || player.Stacking == null)
        {
            return false;
        }


        // 플레이어 손에 맥주를 더 들 수 있는지 확인
        return player.Stacking.CheckListItem(Define.ItemType.Beer);
    }
    public void OnUpdateInteract(GameObject actor, float progress)
    {
        if (_currentPlayer != null && CurrentState == SeatState.Dirty)
        {
            
            float ratio = interactionDuration > 0f ? Mathf.Clamp01(progress / interactionDuration) : 1f;


            if (progress >= interactionDuration)
            {
                CleanSeat();
                progress = 0f;
            }
        }
    }
    public void OnCompleteInteract(GameObject actor)
    {
        
    }
}