using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using System.Collections.Generic;
#endif

public class UnlockStation : InteractableBase
{
    [Header("씬에 배치한 해금 설정")]
    [SerializeField] private UnlockNodeData nodeData;

    [Header("해금할 대상")]
    [SerializeField] private Define.UnlockType unlockType = Define.UnlockType.SpawnPrefab;
    [SerializeField] private GameObject targetSceneObject;
    [Tooltip("해금할 때 함께 숨길 벽이나 장애물입니다.")]
    [SerializeField] private GameObject hideOnUnlock;
    [Tooltip("활성화할 대상의 크기를 늘리는 연출을 사용할지 설정합니다.")]
    [SerializeField] private bool animateTarget = true;
    [SerializeField] private Transform targetSpawnPoint;
    [Tooltip("이 발판을 해금했을 때 가게 영업을 시작합니다.")]
    [SerializeField] private bool openShopOnUnlock;

    [Header("투척 주기 및 금액 설정")]
    [Tooltip("돈을 던지는 주기 (초)")]
    [SerializeField] private float throwInterval = 0.1f;
    [Tooltip("전체 금액을 나누어 던질 목표 횟수")]
    [Min(1)] [SerializeField] private int targetThrowCount = 10;
    [Tooltip("한 번에 던질 금액을 맞추는 최소 단위")]
    [Min(1)] [SerializeField] private int minimumThrowAmount = 10;

    [Header("연출")]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Transform dropPoint;

    [Header("전용 UI")]
    [SerializeField] private Image fillImage;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Transform uiContainer;

    [Header("UI 튕김 연출")]
    [SerializeField] private float punchScaleRatio = 0.2f;
    [SerializeField] private float punchDuration = 0.2f;

    private UnlockNodeData _nodeData;
    private int _targetPrice;
    private int _moneyPerThrow;
    private int _currentPaid = 0;
    private int _visualPaid = 0;
    private bool _isUnlocked = false;
    private Vector3 _initialScale = Vector3.one;
    private Vector3 _initialStationScale = Vector3.one;
    private Tween _bounceTween;
    private bool _hasSavedSpawn;
    private Vector3 _savedSpawnPosition;
    private Quaternion _savedSpawnRotation;

    // 해금 데이터가 연결된 발판만 관리 대상으로 등록합니다.
    public bool HasSettings => nodeData != null;
    public UnlockNodeData NodeData => nodeData;
    public Define.UnlockType UnlockType => unlockType;
    public GameObject TargetSceneObject => targetSceneObject;
    public GameObject HideOnUnlock => hideOnUnlock;
    public bool AnimateTarget => animateTarget;
    public Vector3 SpawnPosition
    {
        get
        {
            if (_hasSavedSpawn) return _savedSpawnPosition;
            if (targetSpawnPoint != null) return targetSpawnPoint.position;
            return transform.position;
        }
    }
    public Quaternion SpawnRotation
    {
        get
        {
            if (_hasSavedSpawn) return _savedSpawnRotation;
            if (targetSpawnPoint != null) return targetSpawnPoint.rotation;
            return transform.rotation;
        }
    }
    public int Price => nodeData != null ? nodeData.Price : 0;
    public override float Duration => throwInterval;

    protected override bool UsesProgressBubble => false;

    /// <summary>
    /// 발판이 비활성화되면 진행 중인 UI 크기 연출을 정리하고 상호작용 상태를 해제합니다.
    /// </summary>
    protected override void OnDisable()
    {
        KillBounce();
        // 조건 변화로 다시 표시될 때 프리팹의 원래 크기가 유지되도록 복원합니다.
        transform.localScale = _initialStationScale;
        base.OnDisable();
    }


    /// <summary>
    /// 씬에 배치된 발판의 해금 정보를 읽고 금액 표시를 초기 상태로 되돌립니다.
    /// </summary>
    public void Setup()
    {
        // 발판에 연결된 데이터 에셋에서 가격을 읽습니다.
        _nodeData = nodeData;
        _targetPrice = Price;
        _moneyPerThrow = CalculateThrowAmount();
        _currentPaid = 0;
        _visualPaid = 0;
        _isUnlocked = false;
        _hasSavedSpawn = false;
        _initialStationScale = transform.localScale;

        if (dropPoint == null)
        {
            dropPoint = transform;
        }

        // 프리팹에 연결한 UI의 원래 크기를 보관해 연출을 반복해도 크기가 누적되지 않도록 합니다.
        if (uiContainer != null)
        {
            _initialScale = uiContainer.localScale;
        }

        UpdateUI(false);
    }

    /// <summary>
    /// 해금이 진행 중이고 플레이어에게 투입할 돈이 남아 있는지 확인합니다.
    /// 한 번에 던질 기본 금액보다 돈이 적어도 보유한 금액만큼 투입할 수 있습니다.
    /// </summary>
    public override bool CanInteract(GameObject actor)
    {
        if (_isUnlocked || _nodeData == null || _currentPaid >= _targetPrice)
            return false;

        PlayerController player = actor.GetComponent<PlayerController>();
        if (player == null)
        {
            return false;
        }

        return GetPaymentAmount(player.money) > 0;
    }

    /// <summary>
    /// 투척 주기가 끝날 때마다 실제 투입액을 차감하고 돈이 날아가는 연출을 시작합니다.
    /// 투입액은 목표 금액과 현재 보유 금액을 넘지 않도록 매번 계산합니다.
    /// </summary>
    public override void OnCompleteInteract(GameObject actor)
    {
        if (!CanInteract(actor)) return;

        PlayerController player = actor.GetComponent<PlayerController>();
        if (player == null) return;

        // 이 투척에서 사용할 금액을 보관해 돈이 도착하는 시점에도 같은 금액을 반영합니다.
        int paymentAmount = GetPaymentAmount(player.money);
        if (paymentAmount <= 0) return;

        // 실제 돈을 먼저 차감하고, 이미 투척한 금액을 기록해 중복 투입을 막습니다.
        if (!player.TrySpendMoney(paymentAmount)) return;
        _currentPaid += paymentAmount;

        // 플레이어 위치에서 발판의 도착 지점까지 돈이 날아가는 연출을 만듭니다.
        Vector3 startPos = player.transform.position + Vector3.up * 1f;
        GameObject flyingMoney = Managers.Resource.Instantiate(moneyPrefab, startPos, Quaternion.identity);

        flyingMoney.transform.DOJump(
            endValue: dropPoint.position,
            jumpPower: 3f,
            numJumps: 1,
            duration: 0.4f
        ).SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            Managers.Resource.Destroy(flyingMoney);

            // 돈이 실제로 도착한 뒤에만 화면의 금액과 게이지를 갱신합니다.
            _visualPaid += paymentAmount;
            UpdateUI(true);

            // 마지막 돈까지 도착했을 때 해금 처리를 한 번 실행합니다.
            if (_visualPaid >= _targetPrice)
            {
                CompleteUnlock();
            }
        });
    }


    /// <summary>
    /// 데이터의 해금 가격을 목표 투척 횟수로 나누고, 최소 금액 단위에 맞춰 올림합니다.
    /// 예를 들어 가격 1,000원, 목표 20회, 최소 단위 10원이면 한 번에 50원씩 던집니다.
    /// </summary>
    private int CalculateThrowAmount()
    {
        int throwCount = Mathf.Max(1, targetThrowCount);
        int minimumAmount = Mathf.Max(1, minimumThrowAmount);

        // 정수 연산으로 올림해 큰 가격에서도 소수점 오차 없이 필요한 투입액을 구합니다.
        long averageAmount = ((long)_targetPrice + throwCount - 1) / throwCount;
        long roundedAmount = ((averageAmount + minimumAmount - 1) / minimumAmount) * minimumAmount;

        // 계산 결과가 int 범위를 넘으면 최댓값으로 제한하고, 실제 투입 때 남은 금액으로 다시 제한합니다.
        if (roundedAmount > int.MaxValue)
        {
            return int.MaxValue;
        }

        return Mathf.Max(minimumAmount, (int)roundedAmount);
    }

    /// <summary>
    /// 한 번에 던질 기본 금액, 남은 해금 금액, 보유 금액 중 가장 작은 값을 반환합니다.
    /// 마지막 투척이나 보유 금액이 적은 경우에도 초과 차감 없이 진행할 수 있습니다.
    /// </summary>
    private int GetPaymentAmount(int availableMoney)
    {
        if (availableMoney <= 0)
        {
            return 0;
        }

        int remainingPrice = _targetPrice - _currentPaid;
        if (remainingPrice <= 0)
        {
            return 0;
        }

        int paymentAmount = Mathf.Min(_moneyPerThrow, remainingPrice);
        return Mathf.Min(paymentAmount, availableMoney);
    }

    private void CompleteUnlock()
    {
        if (_isUnlocked) return;
        _isUnlocked = true;

        // 발판 축소가 자식 생성 지점의 월드 위치를 바꾸기 전에 위치와 방향을 보관합니다.
        _savedSpawnPosition = SpawnPosition;
        _savedSpawnRotation = SpawnRotation;
        _hasSavedSpawn = true;

        // 축소 연출이 끝나면 매니저가 이 발판을 숨기고 해금 대상을 표시합니다.
        transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                KillBounce();
                Managers.Unlock.UnlockNode(_nodeData);

                // 첫 구역을 구매하면 가게의 영업 상태도 함께 엽니다.
                if (openShopOnUnlock)
                {
                    Managers.Game.OpenShop();
                }

                // 해금 결과를 퀘스트에 한 번 보고합니다.
                if (Managers.Quest != null)
                {
                    Managers.Quest.ReportNodeUnlock(_nodeData);
                }
            });
    }

    /// <summary>
    /// 실제로 도착한 금액을 기준으로 게이지와 남은 금액을 갱신합니다.
    /// 돈이 도착한 경우에만 UI 크기 연출을 재생합니다.
    /// </summary>
    /// <param name="animate">돈 도착 연출을 재생할지 여부</param>
    private void UpdateUI(bool animate)
    {
        if (_targetPrice <= 0)
        {
            return;
        }

        // 누적 지불액을 전체 가격으로 나눈 뒤 게이지가 0에서 1 사이에 머물도록 합니다.
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01((float)_visualPaid / _targetPrice);
        }

        // 지불액이 목표를 넘더라도 화면에는 음수 대신 0을 표시합니다.
        if (progressText != null)
        {
            int remainingPrice = Mathf.Max(0, _targetPrice - _visualPaid);
            progressText.text = remainingPrice.ToString();
        }

        if (!animate || uiContainer == null)
        {
            return;
        }

        // 이전 연출을 끝내고 원래 크기에서 다시 시작해 연속 투척에도 같은 크기로 튕기게 합니다.
        KillBounce();
        uiContainer.localScale = _initialScale;

        Vector3 punchAmount = _initialScale * punchScaleRatio;
        _bounceTween = uiContainer.DOPunchScale(punchAmount, punchDuration, 10, 1f)
            .OnKill(() =>
            {
                // 발판이 비활성화되거나 제거될 때도 UI 크기를 원래대로 복원합니다.
                if (uiContainer != null)
                {
                    uiContainer.localScale = _initialScale;
                }
            });
    }

    /// <summary>
    /// 진행 중인 UI 크기 연출을 중지합니다.
    /// </summary>
    private void KillBounce()
    {
        _bounceTween?.Kill();
        _bounceTween = null;
    }

    #region 에디터 미리보기

    /// <summary>
    /// 발판과 생성 위치를 표시하고 편집 중에는 해금될 프리팹의 형태를 함께 그립니다.
    /// </summary>
    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        // 씬 오브젝트 미리보기는 데이터 연결 여부와 관계없이 확인할 수 있습니다.
        if (!Application.isPlaying)
        {
            DrawSceneObjectPreview();
        }
#endif

        if (nodeData == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.4f);

        if (targetSpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, targetSpawnPoint.position);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DrawPrefabPreview();
        }
#endif
    }

    #endregion

    #region 에디터 미리보기 함수

#if UNITY_EDITOR
    /// <summary>
    /// 활성화할 씬 오브젝트를 현재 위치에 주황색으로 표시합니다.
    /// 대상 루트가 꺼져 있어도 해금 후 보일 메시를 확인할 수 있습니다.
    /// </summary>
    private void DrawSceneObjectPreview()
    {
        if (unlockType != Define.UnlockType.ToggleActive)
        {
            return;
        }

        if (targetSceneObject == null)
        {
            return;
        }

        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);

        // 꺼진 루트 아래의 메시도 찾되, 해금 후에도 꺼져 있을 자식은 제외합니다.
        MeshFilter[] filters = targetSceneObject.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            if (filter.sharedMesh == null)
            {
                continue;
            }

            MeshRenderer meshRenderer = filter.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                continue;
            }

            if (!meshRenderer.enabled)
            {
                continue;
            }

            if (!IsPreviewChildActive(filter.transform))
            {
                continue;
            }

            // 씬 오브젝트의 실제 월드 위치, 회전, 크기를 그대로 사용합니다.
            Gizmos.matrix = filter.transform.localToWorldMatrix;
            Gizmos.DrawMesh(filter.sharedMesh);
        }

        // 비활성 구역의 발판은 OnDrawGizmos가 호출되지 않으므로 여기서 대신 표시합니다.
        UnlockStation[] stations = targetSceneObject.GetComponentsInChildren<UnlockStation>(true);
        foreach (UnlockStation station in stations)
        {
            if (station.gameObject.activeInHierarchy) continue;
            if (station.unlockType != Define.UnlockType.SpawnPrefab) continue;
            if (station.nodeData == null) continue;
            GameObject childPrefab = station.nodeData.TargetPrefab;
            if (childPrefab == null) continue;

            Matrix4x4 childMatrix = Matrix4x4.TRS(station.SpawnPosition, station.SpawnRotation, childPrefab.transform.localScale);
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.35f);
            DrawPrefabContents(childPrefab, childMatrix, new HashSet<GameObject>());
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    /// <summary>
    /// 대상 루트를 켰을 때 메시까지 이어지는 자식들이 활성 상태인지 확인합니다.
    /// </summary>
    private bool IsPreviewChildActive(Transform child)
    {
        Transform current = child;
        while (current != targetSceneObject.transform)
        {
            if (!current.gameObject.activeSelf)
            {
                return false;
            }

            current = current.parent;
        }

        return true;
    }

    /// <summary>
    /// 실제 오브젝트를 생성하지 않고 프리팹의 메시를 생성 예정 위치에 반투명하게 표시합니다.
    /// </summary>
    private void DrawPrefabPreview()
    {
        if (unlockType != Define.UnlockType.SpawnPrefab)
        {
            return;
        }

        GameObject prefab = nodeData.TargetPrefab;
        if (prefab == null)
        {
            return;
        }

        if (!prefab.activeSelf)
        {
            return;
        }

        // 실제 생성 위치에서 시작해 프리팹 안의 해금 발판까지 차례로 확인합니다.
        Matrix4x4 rootMatrix = Matrix4x4.TRS(SpawnPosition, SpawnRotation, prefab.transform.localScale);
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Color previousColor = Gizmos.color;
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.35f);

        DrawPrefabContents(prefab, rootMatrix, new HashSet<GameObject>());

        // 다른 Gizmo의 표시 설정에 영향을 주지 않도록 원래 값을 복원합니다.
        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    /// <summary>프리팹의 메시와 내부 발판이 나중에 생성할 프리팹을 함께 표시합니다.</summary>
    private static void DrawPrefabContents(GameObject prefab, Matrix4x4 rootMatrix, HashSet<GameObject> parents)
    {
        if (prefab == null || !prefab.activeSelf) return;
        // 데이터가 서로를 다시 가리키는 경우 무한 반복하지 않습니다.
        if (!parents.Add(prefab)) return;

        Matrix4x4 sourceToPreview = rootMatrix * prefab.transform.worldToLocalMatrix;

        // 활성화된 메시와 렌더러만 표시하고 자식의 상대 위치, 회전, 크기를 반영합니다.
        MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter filter in filters)
        {
            MeshRenderer meshRenderer = filter.GetComponent<MeshRenderer>();
            if (filter.sharedMesh == null)
            {
                continue;
            }

            if (meshRenderer == null)
            {
                continue;
            }

            if (!meshRenderer.enabled)
            {
                continue;
            }

            Gizmos.matrix = sourceToPreview * filter.transform.localToWorldMatrix;
            Gizmos.DrawMesh(filter.sharedMesh);
        }

        // 맵 확장처럼 부모가 비활성인 구역의 발판도 편집 중에는 찾습니다.
        UnlockStation[] stations = prefab.GetComponentsInChildren<UnlockStation>(true);
        foreach (UnlockStation station in stations)
        {
            if (station.unlockType != Define.UnlockType.SpawnPrefab) continue;
            if (station.nodeData == null) continue;
            GameObject childPrefab = station.nodeData.TargetPrefab;
            if (childPrefab == null) continue;

            // 실제 생성물은 발판의 자식이 아니므로 발판 부모의 크기는 위치에만 반영합니다.
            Vector3 position = sourceToPreview.MultiplyPoint3x4(station.SpawnPosition);
            Quaternion rotation = sourceToPreview.rotation * station.SpawnRotation;
            Matrix4x4 childMatrix = Matrix4x4.TRS(position, rotation, childPrefab.transform.localScale);
            DrawPrefabContents(childPrefab, childMatrix, parents);
        }

        // 같은 프리팹을 다른 발판에서 다시 사용하는 것은 허용합니다.
        parents.Remove(prefab);
    }
#endif

    #endregion

}
