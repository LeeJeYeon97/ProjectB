using System.Collections.Generic;
using UnityEngine;
using static Define;

public class CustomerManager
{
    // 손님 열 명 중 한 명 정도를 세 가지 방해 유형 가운데 하나로 생성합니다.
    private const float DisturberSpawnChance = 0.1f;
    private static readonly Define.CustomerType[] DisturberTypes =
    {
        Define.CustomerType.QueueNoiser,
        Define.CustomerType.SeatNoiser,
        Define.CustomerType.SeatHog
    };

    [Header("Spawn Configuration")]
    public GameObject customerPrefab;       // 손님 프리팹 (Synty 캐릭터 기반)
    public Transform spawnPoint;            // 최초 스폰 위치 (상점 밖)
    public Transform exitPoint;             // 완전 퇴장 위치 (파괴 구역)
    public List<Transform> entranceQueue;   // 가게 앞 대기줄 순서대로 좌표 리스트

    [Header("Shop Infrastructure")]
    
    private List<CustomerController> waitingCustomers = new List<CustomerController>();
    // 현재 사용할 수 있는 기계의 레시피만 보관합니다. 같은 메뉴의 기계가 여러 대여도 한 번만 넣습니다.
    private readonly List<FoodRecipeData> availableRecipes = new List<FoodRecipeData>();
    private float spawnTimer = 0f;
    public float spawnInterval = 7f;        // 스폰 간격

    public void Init()
    {
        // 1. 이미 다른 씬(LoginScene 등)에서 로드가 완료되었다면 즉시 초기화 실행
        if (Managers.Resource.IsLoaded)
        {
            customerPrefab = Managers.Resource.Load<GameObject>("Prefab_Customer");
        }
        else
        {
            // 2. 아직 로드 중이라면 완료 이벤트를 구독하여 대기
            Managers.Resource.assetLoaded += ()=> 
            { 
                customerPrefab = Managers.Resource.Load<GameObject>("Prefab_Customer"); 
            };
        }
    }
    public void SetScenePositions(Transform spawn, List<Transform> queue, Transform exit = null)
    {
        spawnPoint = spawn;
        exitPoint = exit != null ? exit : spawn; // 퇴장 위치
      
        entranceQueue = queue;

        Debug.Log("CustomerManager: 씬 배치 포지션 캐싱 완료!");
    }

    public void OnUpdate()
    {
        if (Managers.Game.IsOpen != true)
        {
            return;
        }
        // 1. 대기열 손님 추가 스폰
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnCustomer();
        }

        // 2. 비어있는 자리 매칭 시스템
        MatchWaitingCustomersToSeats();
    }

    /// <summary>씬에서 실제로 사용할 수 있는 메뉴 기계를 찾아 주문 후보를 다시 만듭니다.</summary>
    public void RefreshAvailableRecipes()
    {
        // 저장된 해금 상태를 적용한 뒤 호출해야 잠긴 기계가 주문 후보에 섞이지 않습니다.
        availableRecipes.Clear();
        MenuMachine[] machines = UnityEngine.Object.FindObjectsByType<MenuMachine>(FindObjectsSortMode.None);

        foreach (MenuMachine machine in machines)
        {
            if (!machine.CanOfferRecipe)
                continue;

            FoodRecipeData recipe = machine.Recipe;
            if (availableRecipes.Contains(recipe))
                continue;

            availableRecipes.Add(recipe);
        }
    }

    /// <summary>현재 사용할 수 있는 기계의 레시피 중 손님이 주문할 메뉴 하나를 고릅니다.</summary>
    public bool TryGetRandomRecipe(out FoodRecipeData recipe)
    {
        recipe = null;

        // 현재 예약되지 않은 재고로 만들 수 있는 메뉴만 이번 주문 후보에 넣습니다.
        List<FoodRecipeData> recipesWithStock = new List<FoodRecipeData>();
        foreach (FoodRecipeData availableRecipe in availableRecipes)
        {
            if (availableRecipe == null)
                continue;

            if (!Managers.Ingredient.HasIngredients(availableRecipe.Ingredients))
                continue;

            recipesWithStock.Add(availableRecipe);
        }

        if (recipesWithStock.Count == 0)
            return false;

        int index = Random.Range(0, recipesWithStock.Count);
        recipe = recipesWithStock[index];
        return true;
    }

    private void TrySpawnCustomer()
    {
        // 입구 대기열의 최대치보다 손님이 많으면 스폰하지 않음
        if (waitingCustomers.Count >= entranceQueue.Count) return;

        GameObject go = Managers.Resource.Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
        CustomerController customer = go.GetComponent<CustomerController>();

        if (customer != null)
        {
            // 입구 대기 대상을 설정하고 대기 대열 끝부분 배치
            int currentQueueIndex = waitingCustomers.Count;
            Transform targetQueuePoint = entranceQueue[currentQueueIndex];
            Define.CustomerType customerType = Define.CustomerType.Normal;
            if (Random.value < DisturberSpawnChance)
            {
                // 방해 손님은 대기줄 소음, 착석 소음, 자리 독점 중 하나만 맡습니다.
                int typeIndex = Random.Range(0, DisturberTypes.Length);
                customerType = DisturberTypes[typeIndex];
            }

            customer.Init(targetQueuePoint, exitPoint, customerType);

            waitingCustomers.Add(customer);

            // 방해 손님은 행동 유형을 함께 출력해 스폰 여부와 종류를 콘솔에서 바로 확인하게 합니다.
            if (customerType != Define.CustomerType.Normal)
                Debug.Log($"[CustomerManager] 방해 손님 스폰: {GetCustomerTypeName(customerType)} ({customerType})", customer);
        }
    }

    /// <summary>손님 유형을 개발 로그에서 쉽게 알아볼 수 있는 한국어 이름으로 바꿉니다.</summary>
    private string GetCustomerTypeName(Define.CustomerType customerType)
    {
        switch (customerType)
        {
            case Define.CustomerType.QueueNoiser:
                return "대기줄 소음 손님";
            case Define.CustomerType.SeatNoiser:
                return "착석 소음 손님";
            case Define.CustomerType.SeatHog:
                return "자리 독점 손님";
            default:
                return "일반 손님";
        }
    }

    private void MatchWaitingCustomersToSeats()
    {
        if (waitingCustomers.Count == 0) return;

        // 입구 맨 앞줄에 서 있는 손님 타겟팅
        CustomerController firstInLine = waitingCustomers[0];

        // 앞 손님이 아직 줄 위치로 이동 중이거나 소음을 해결하지 못했다면
        // 뒤 손님을 포함한 대기열 전체가 그 자리에 머무릅니다.
        if (firstInLine == null || !firstInLine.CanLeaveQueue)
            return;

        // 비어있는 자리가 있는지 탐색
        SeatStation emptySeat = Managers.Station.FindAvailableSeat();

        if (emptySeat != null)
        {
            // 자리가 있으면 첫 번째 손님을 배정 후 이동시킴
            firstInLine.AssignSeat(emptySeat);
            waitingCustomers.RemoveAt(0);

            // 뒤에 남아있던 손님들을 대기열 한 칸씩 앞으로 당겨서 재정렬
            RearrangeEntranceQueue();
        }
    }

    private void RearrangeEntranceQueue()
    {
        for (int i = 0; i < waitingCustomers.Count; i++)
        {
            // 한 칸씩 앞당긴 대기열 좌표로 재목적지 설정
            // 재정렬 후 새 줄 위치에 다시 도착해야 대기 완료로 취급합니다.
            waitingCustomers[i].MoveToQueuePoint(entranceQueue[i]);
        }
    }
}
