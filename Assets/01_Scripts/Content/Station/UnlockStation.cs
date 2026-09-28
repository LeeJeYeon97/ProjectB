using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnlockStation : MonoBehaviour, IInteractable
{
    [Header("투척 주기 및 금액 설정")]
    [Tooltip("돈을 던지는 주기 (초)")]
    [SerializeField] private float throwInterval = 0.1f;
    [Tooltip("한 번 던질 때 날아가는 금액")]
    [SerializeField] private int throwAmount = 10;

    [Header("연출")]
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Transform dropPoint;

    [Header("전용 UI")]
    [SerializeField] private UI_UnlockStation uiUnlock;

    private string _uniqueUnlockID;
    private UnlockNodeData _nodeData;
    private int _targetPrice;
    private int _currentPaid = 0;
    private int _visualPaid = 0;
    private bool _isUnlocked = false;

    public float Duration => throwInterval;

    public void Setup(UnlockAnchor anchor)
    {
        _nodeData = anchor.NodeData;
        _targetPrice = anchor.Price;
        _uniqueUnlockID = anchor.UniqueID;
        _currentPaid = 0;
        _visualPaid = 0;
        _isUnlocked = false;

        if (dropPoint == null) dropPoint = transform;
        if (uiUnlock == null) uiUnlock = GetComponentInChildren<UI_UnlockStation>();

        if (uiUnlock != null)
        {
            uiUnlock.Init();
            uiUnlock.UpdateUI(_visualPaid, _targetPrice, animate: false);
        }
    }

    public bool CanInteract(GameObject actor)
    {
        if (_isUnlocked || _nodeData == null || _currentPaid >= _targetPrice)
            return false;

        PlayerController player = actor.GetComponent<PlayerController>();
        return player != null && player.money >= throwAmount;
    }

    public void OnStartInteract(GameObject actor) { }

    public void OnUpdateInteract(GameObject actor, float progress) { }

    // 발판의 Duration(0.1초) 주기가 완료될 때마다 1번씩 실행
    public void OnCompleteInteract(GameObject actor)
    {
        if (!CanInteract(actor)) return;

        PlayerController player = actor.GetComponent<PlayerController>();
        if (player == null) return;

        // 1. 플레이어 돈 차감
        player.UpdateMoney(-throwAmount);
        _currentPaid += throwAmount;

        // 2. 날아가는 돈 연출 생성
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

            // 3. 돈이 도착했을 때 시각적 금액 반영 및 UI 갱신
            _visualPaid += throwAmount;
            if (uiUnlock != null)
            {
                uiUnlock.UpdateUI(_visualPaid, _targetPrice, animate: true);
            }

            // 4. 모든 금액이 지불되었으면 언락
            if (_visualPaid >= _targetPrice)
            {
                CompleteUnlock();
            }
        });
    }

    public void OnCancelInteract(GameObject actor) { }

    private void CompleteUnlock()
    {
        if (_isUnlocked) return;
        _isUnlocked = true;

        // 매니저에 언락 알림
        Managers.Unlock.UnlockNode(_uniqueUnlockID);

        // 카운터 특정 ID 해금 시 오픈 처리 (기존 로직 유지)
        if (_uniqueUnlockID == "Stool_0")
        {
            Managers.Game.IsOpen = true;
        }

        // 오브젝트 축소 연출 후 삭제
        transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (uiUnlock != null) uiUnlock.KillTweens();
                Managers.Resource.Destroy(gameObject);
            });
    }
}
