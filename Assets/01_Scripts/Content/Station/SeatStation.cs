using System.Collections;
using System.Reflection.Metadata;
using UnityEngine;
using static Define;

public class SeatStation : BaseStation
{
    [Header("Seat Configuration")]
    [SerializeField]
    private SeatState currentState = SeatState.Empty;
    public SeatState CurrentState
    {
        get => currentState;
        set
        {
            currentState = value;
            UpdateUIState();
        }
    }

    public Transform customerSitPoint; // 손님이 앉을 실제 3D 좌표/회전 기준점
    public Transform foodServePoint;   // 음식을 올려놓을 테이블 위 좌표

    [Header("Interaction Settings")]
    public float interactionDuration = 1.5f; // 청소 또는 주문받기에 걸리는 시간
    private float currentProgress = 0f;

    public CustomerController currentCustomer;
    public EmployeeController AssignedCleaner { get; set; }

    [Header("UI")]
    [SerializeField] private UI_Bubble uiBubble;
    [SerializeField] private UI_InteractionProgress progressUI_1;
    [SerializeField] private Vector3 bubbleOffset = new Vector3(0f, 1.8f, 0f);
    [SerializeField] private Vector3 progressOffset = new Vector3(0f, 1.4f, 0f);

    private PlayerController _currentPlayer;

    private void Awake()
    {
        InitUI();
    }

    private void Start()
    {
        InitUI();
        UpdateUIState();
        Managers.Station.RegisterSeat(this);
    }

    private void InitUI()
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
            uiBubble.transform.localPosition = bubbleOffset;
            uiBubble.transform.localScale = Vector3.one * 0.01f;
            uiBubble.gameObject.SetActive(false);
        }

        if (progressUI_1 == null)
        {
            progressUI_1 = GetComponentInChildren<UI_InteractionProgress>(true);
        }
        if (progressUI_1 == null)
        {
            GameObject progPrefab = Managers.Resource.Load<GameObject>("UI_InteractionProgress");
#if UNITY_EDITOR
            if (progPrefab == null)
                progPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Prefabs/UI/SubItem/UI_InteractionProgress.prefab");
#endif
            if (progPrefab != null)
            {
                GameObject go = Instantiate(progPrefab, transform);
                go.name = "UI_InteractionProgress";
                progressUI_1 = go.GetComponent<UI_InteractionProgress>();
            }
        }
        if (progressUI_1 != null)
        {
            progressUI_1.transform.localPosition = progressOffset;
            progressUI_1.transform.localScale = Vector3.one * 0.01f;
            progressUI_1.gameObject.SetActive(false);
        }
    }

    public void UpdateUIState()
    {
        if (uiBubble == null)
            uiBubble = GetComponentInChildren<UI_Bubble>(true);
        if (progressUI_1 == null)
            progressUI_1 = GetComponentInChildren<UI_InteractionProgress>(true);

        if (currentState == SeatState.Dirty)
        {
            if (uiBubble != null)
            {
                uiBubble.gameObject.SetActive(true);
                uiBubble.SetCleanImage();
                Managers.Station.RegisterDirtyTable(this);
            }
        }
        else
        {
            if (uiBubble != null)
            {
                uiBubble.gameObject.SetActive(false);
            }
            if (progressUI_1 != null)
            {
                progressUI_1.SetProgress(0f);
                progressUI_1.gameObject.SetActive(false);
            }
        }
    }

    public void SetState(SeatState state)
    {
        CurrentState = state;
    }

    private void Update()
    {
        if (!isOperational) return;

        // 플레이어가 발판 위에 있고 청소 필요한 상태일 때 청소 진행
        if (_currentPlayer != null && CurrentState == SeatState.Dirty)
        {
            currentProgress += Time.deltaTime;
            float ratio = interactionDuration > 0f ? Mathf.Clamp01(currentProgress / interactionDuration) : 1f;

            if (progressUI_1 != null)
            {
                if (!progressUI_1.gameObject.activeSelf)
                {
                    progressUI_1.gameObject.SetActive(true);
                    progressUI_1.SetVisible(true);
                }
                progressUI_1.SetProgress(ratio);
            }

            if (currentProgress >= interactionDuration)
            {
                CleanSeat();
                currentProgress = 0f;
            }
        }
    }
    public void SetCleanProgress(float ratio)
    {
        if (progressUI_1 != null)
        {
            if (!progressUI_1.gameObject.activeSelf)
            {
                progressUI_1.gameObject.SetActive(true);
                progressUI_1.SetVisible(true);
            }
            progressUI_1.SetProgress(ratio);
        }
    }

    public void CleanSeat()
    {
        CurrentState = SeatState.Empty;
        currentProgress = 0f;
        AssignedCleaner = null;
        if (progressUI_1 != null)
        {
            progressUI_1.SetProgress(0f);
            progressUI_1.gameObject.SetActive(false);
        }
        if (uiBubble != null)
        {
            uiBubble.gameObject.SetActive(false);
        }
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
}