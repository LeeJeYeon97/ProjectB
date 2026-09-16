using UnityEngine;
using static Define;

public class BeerMachine : BaseStation, IInteractable
{
    [Header("Beer Production Settings")]
    [SerializeField] private GameObject beerPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float produceInterval = 2.0f; // 2초 뒤에 맥주 생성

    private float _timer = 0f;
    private PlayerController _currentPlayer;

    private void Awake()
    {
        if (spawnPoint == null)
        {
            // spawnPoint 자식이 있으면 찾고, 없으면 생성
            Transform found = transform.Find("SpawnPoint");
            if (found != null)
            {
                spawnPoint = found;
            }
            else
            {
                GameObject sp = new GameObject("SpawnPoint");
                sp.transform.SetParent(transform);
                sp.transform.localPosition = new Vector3(0f, 0.5f, 0f);
                spawnPoint = sp.transform;
            }
        }
    }

    private void Update()
    {
        if (_currentPlayer != null)
        {
            ProcessProduction(_currentPlayer);
        }
    }

    public override void OnCharacterEnter(GameObject character)
    {
        PlayerController player = character.GetComponent<PlayerController>();
        if (player != null)
        {
            _currentPlayer = player;
            _timer = 0f;
        }
    }

    public override void OnCharacterStay(GameObject character)
    {
        PlayerController player = character.GetComponent<PlayerController>();
        if (player != null)
        {
            _currentPlayer = player;
        }
    }

    public override void OnCharacterExit(GameObject character)
    {
        PlayerController player = character.GetComponent<PlayerController>();
        if (player != null && _currentPlayer == player)
        {
            _currentPlayer = null;
            _timer = 0f;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            _currentPlayer = player;
            _timer = 0f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            _currentPlayer = player;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && _currentPlayer == player)
        {
            _currentPlayer = null;
            _timer = 0f;
        }
    }

    private void ProcessProduction(PlayerController player)
    {
        if (!isOperational) return;
        if (!CanInteract(player))
        {
            _timer = 0f;
            return;
        }

        _timer += Time.deltaTime;

        if (_timer >= produceInterval)
        {
            _timer = 0f;
            ProduceBeer(player);
        }
    }

    private void ProduceBeer(PlayerController player)
    {
        GameObject prefabToSpawn = beerPrefab;

        if (prefabToSpawn == null)
        {
#if UNITY_EDITOR
            prefabToSpawn = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03_Prefabs/Item_Beer.prefab");
#endif
            if (prefabToSpawn == null)
            {
                prefabToSpawn = Resources.Load<GameObject>("Prefabs/Item_Beer");
            }
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError("[BeerMachine] Item_Beer 프리팹을 찾을 수 없습니다!");
            return;
        }

        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : (transform.position + Vector3.up * 0.5f);
        GameObject beer = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);

        if (player.Stacking != null)
        {
            player.Stacking.PickUpItem(beer);
            Debug.Log("[BeerMachine] 맥주를 생성하여 플레이어에게 스택했습니다!");
        }
        else
        {
            Debug.LogWarning("[BeerMachine] Player에게 StackingSystem이 없습니다.");
        }
    }

    public void Interact(PlayerController player)
    {
        if (CanInteract(player))
        {
            ProduceBeer(player);
        }
    }

    public bool CanInteract(PlayerController player)
    {
        if (player == null || player.Stacking == null) return false;
        return player.Stacking.CheckListItem(ItemType.Beer);
    }
}