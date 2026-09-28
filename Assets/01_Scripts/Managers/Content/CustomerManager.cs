using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static Define;

public class CustomerManager
{
    [Header("Spawn Configuration")]
    public GameObject customerPrefab;       // 손님 프리팹 (Synty 캐릭터 기반)
    public Transform spawnPoint;            // 최초 스폰 위치 (상점 밖)
    public Transform exitPoint;             // 완전 퇴장 위치 (파괴 구역)
    public List<Transform> entranceQueue;   // 가게 앞 대기줄 순서대로 좌표 리스트

    [Header("Shop Infrastructure")]
    
    private List<CustomerController> waitingCustomers = new List<CustomerController>();
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
            customer.Init(targetQueuePoint, exitPoint);

            waitingCustomers.Add(customer);
        }
    }

    private void MatchWaitingCustomersToSeats()
    {
        if (waitingCustomers.Count == 0) return;

        // 입구 맨 앞줄에 서 있는 손님 타겟팅
        CustomerController firstInLine = waitingCustomers[0];

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
            NavMeshAgent agent = waitingCustomers[i].GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.destination = entranceQueue[i].position;
            }
        }
    }
}