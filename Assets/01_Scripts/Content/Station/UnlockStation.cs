using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnlockStation : BaseStation
{
    private string uniqueUnlockID; // 앵커로부터 전달받을 고유 인스턴스 ID
    private UnlockNodeData nodeData;
    private int currentPaid = 0;
    private int visualPaid = 0;
    private int targetPrice = 0; // 적용될 가격
    private bool isUnlocked = false;

    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Transform dropPoint;

    [Header("UI 연동")]
    [SerializeField] private Image fillImage;         // 게이지용 Fill Image
    [SerializeField] private TMP_Text progressText;   // 금액 표시 텍스트
    [SerializeField] private Transform uiContainer;   // 바운스 애니메이션을 적용할 UI 컨테이너 (Canvas 또는 내부 패널)

    private Vector3 _initialUiScale = Vector3.one;
    private Tween _bounceTween;


    // Setup 시 NodeData가 아닌 Anchor 자체를 넘겨받습니다.
    public void Setup(UnlockAnchor anchor)
    {
        nodeData = anchor.NodeData;
        targetPrice = anchor.Price; // 개별 가격 오버라이드 반영
        currentPaid = 0;
        visualPaid = 0;
        isUnlocked = false;
        uniqueUnlockID = anchor.UniqueID;

        if (dropPoint == null) dropPoint = this.transform;
        if (uiContainer != null) _initialUiScale = uiContainer.localScale;

        UpdateUI(animate: false);
    }

    public override void OnCharacterStay(GameObject character)
    {
        TryThrowMoney(character.transform, 10);
    }


    public void TryThrowMoney(Transform playerTransform, int throwAmount)
    {
        if (isUnlocked || nodeData == null || currentPaid >= nodeData.price) return;

        // 실제 플레이어 돈 검증 로직 (PlayerController에서 처리하거나 매니저 활용)
        PlayerController player = playerTransform.GetComponent<PlayerController>();
        if (player == null || player.money < throwAmount) return;

        // 돈 차감
        player.UpdateMoney(-throwAmount);

        currentPaid += throwAmount;

        // 가짜 돈 날아가는 연출
        Vector3 startPos = playerTransform.position + Vector3.up * 1f;
        GameObject flyingMoney = Instantiate(moneyPrefab, startPos, Quaternion.identity);

        flyingMoney.transform.DOJump(
            endValue: dropPoint.position,
            jumpPower: 3f,
            numJumps: 1,
            duration: 0.5f
        ).SetEase(Ease.OutQuad)
        .OnComplete(() =>
        {
            Destroy(flyingMoney);


            // 3. 돈이 도착했을 때 시각적 금액을 증가시키고 UI를 업데이트
            visualPaid += throwAmount;
            UpdateUI(animate: true);

            // 언락 검사는 시각적 금액(visualPaid)이 다 찼을 때 실행하여 연출이 끝난 후 언락되게 함
            if (visualPaid >= targetPrice)
            {
                Unlock();
            }
        });
    }

    private void UpdateUI(bool animate)
    {
        if (nodeData == null) return;

        // UI 표시는 모두 visualPaid를 기준으로 계산합니다.
        if (fillImage != null)
            fillImage.fillAmount = (float)visualPaid / targetPrice;

        if (progressText != null)
        {
            int remainingPrice = Mathf.Max(0, targetPrice - visualPaid); // 수정
            progressText.text = remainingPrice.ToString();
        }

        // 3. 돈이 누적될 때 팅기는 펀치 애니메이션 효과
        if (animate && uiContainer != null)
        {
            _bounceTween?.Kill();
            uiContainer.localScale = _initialUiScale; // 원래 스케일로 즉시 초기화

            // 월드 스페이스 크기를 고려하여 초기 스케일의 20% 만큼 팅기도록 설정
            Vector3 punchAmount = _initialUiScale * 0.2f;
            _bounceTween = uiContainer.DOPunchScale(punchAmount, 0.2f, 10, 1f)
                .OnKill(() =>
                {
                    if (uiContainer != null) uiContainer.localScale = _initialUiScale;
                });
        }
    }

    private void Unlock()
    {
        if (isUnlocked) return;
        isUnlocked = true;

        // [중요] SO 데이터가 아니라 고유 인스턴스 ID를 전달하여 해금!
        Managers.Unlock.UnlockNode(uniqueUnlockID);

        // 카운터 해금 시 오픈 처리
        if (uniqueUnlockID == "Stool_0")
        {
            Managers.Game.IsOpen = true;
        }

        transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                _bounceTween?.Kill();
                Destroy(gameObject);
            });
    }
}
