using DG.Tweening;
using System.Threading.Tasks;
using UnityEngine;

public static class UIEffectUtil
{
    //  매번 찾으면 느려지므로, 게임 중 처음 한 번만 찾아두고 재사용할 캐싱 변수들입니다.
    private static GameObject _coinPrefab;
    private static RectTransform _endPoint;
    private static Transform _canvasTransform;

    /// <summary>
    /// 어디서나 단 한 줄로 호출하는 매개변수 최소화 버전 코인 연출
    /// </summary>
    /// <param name="startPosition">코인이 뿜어져 나올 월드 또는 스크린 좌표 (Vector3)</param>
    /// <param name="coinCount">생성할 코인 개수</param>
    public static async Task PlayCoinFlyEffect(Vector3 startPosition, int coinCount)
    {
        // 1. 코인 프리팹 자동 로드 (최초 1회)
        if (_coinPrefab == null)
        {
            // Assets/Resources/Prefabs/CoinUI.prefab 경로에서 자동으로 로드합니다.
            _coinPrefab = Resources.Load<GameObject>("Prefabs/UI/SubItem/UI_Gold");
            if (_coinPrefab == null)
            {
                Debug.LogError("[UIEffectUtil] 'Resources/Prefabs/CoinUI' 경로에서 프리팹을 찾을 수 없습니다. 폴더와 이름을 확인해주세요.");
                return;
            }
        }

        // 2. 씬에 있는 캔버스를 '이름'으로 자동 검색 (최초 1회)
        if (_canvasTransform == null)
        {
            //  대표님의 실제 메인 캔버스 이름으로 "Canvas" 부분을 수정해 주세요!
            GameObject canvasGo = GameObject.Find("UI_LobbyScene");
            if (canvasGo != null)
            {
                _canvasTransform = canvasGo.transform;
            }
            else
            {
                Debug.LogError("[UIEffectUtil] 씬에서 'Canvas'라는 이름의 오브젝트를 찾을 수 없습니다. 이름을 확인해주세요.");
                return;
            }
        }

        // 3. 목적지(코인 바) UI 오브젝트 이름으로 자동 검색 (최초 1회)
        if (_endPoint == null)
        {
            // 씬 전체에서 이름이 "CoinDisplayBar"인 오브젝트를 싹 뒤져서 가져옵니다.
            GameObject endPointGo = GameObject.Find("Text_Coins");
            if (endPointGo != null)
            {
                _endPoint = endPointGo.GetComponent<RectTransform>();
            }
            else
            {
                Debug.LogError("[UIEffectUtil] 씬에서 'CoinDisplayBar'라는 이름의 오브젝트를 찾을 수 없습니다. UI 이름을 맞춰주세요.");
                return;
            }
        }

        // ---------------------------------------------------------------------
        // 4. 연출 로직 실행
        // ---------------------------------------------------------------------
        float popDuration = 0.4f;

        // [수정됨] 목적지로 날아가는 시간: 0.5초 -> 0.8초 (빨려 들어가는 과정을 확실히 보여줌)
        float flyDuration = 0.8f;

        // [수정됨] 코인 연속 생성 간격: 50ms -> 100ms (0.1초 간격으로 스폰되어 더 촤르르륵 나옴)
        int spawnDelayMs = 100;

        for (int i = 0; i < coinCount; i++)
        {
            //  [핵심 방어막 1] 비동기 대기(Task.Delay) 도중 씬이 넘어가거나 UI가 파괴되었다면 연출 즉시 중단!
            if (_canvasTransform == null || _endPoint == null)
            {
                Debug.LogWarning("[UIEffectUtil] 타겟 UI가 파괴되어 코인 연출을 안전하게 중단합니다.");
                break;
            }

            GameObject coin = Object.Instantiate(_coinPrefab, _canvasTransform);
            Debug.Log(_canvasTransform.gameObject.name);
            RectTransform coinRect = coin.GetComponent<RectTransform>();

            if (coinRect == null) continue;

            coinRect.position = startPosition;
            coinRect.localScale = Vector3.zero;

            //  [수정됨] 월드 좌표(position) 대신 로컬 좌표(localPosition)를 기준으로 픽셀 오프셋을 더합니다.
            Vector3 randomPixelOffset = new Vector3(Random.Range(-100f, 100f), Random.Range(50f, 150f), 0f);
            Vector3 popLocalPos = coinRect.localPosition + randomPixelOffset;

            Sequence seq = DOTween.Sequence();

            //  [핵심 방어막 2] SetLink를 걸어두면, 팝업이 닫혀서 coin이 파괴될 때 에러 없이 애니메이션을 자동 취소합니다.
            seq.SetLink(coin);

            //  [수정됨] DOJump 대신 DOLocalJump를 사용합니다!
            // 이렇게 하면 100f, 50f 같은 수치들이 완벽하게 '픽셀(UI 스케일)' 기준으로 작동하여 예쁘게 튕깁니다.
            seq.Append(coinRect.DOLocalJump(popLocalPos, 50f, 1, popDuration).SetEase(Ease.OutQuad));
            seq.Join(coinRect.DOScale(Vector3.one, popDuration).SetEase(Ease.OutBack));

            seq.AppendInterval(0.1f);

            // [Fly]
            seq.Append(coinRect.DOMove(_endPoint.position, flyDuration).SetEase(Ease.InBack));
            seq.Join(coinRect.DOScale(Vector3.one * 0.5f, flyDuration).SetEase(Ease.InBack));

            seq.OnComplete(() =>
            {
                if (coin != null) Object.Destroy(coin);

                //  [핵심 방어막 3] 코인이 도착한 시점에 코인 바(_endPoint)가 아직 살아있는지 확인 후 꿀렁임 적용
                if (_endPoint != null)
                {
                    _endPoint.DOKill(true);
                    _endPoint.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.15f, 10, 1f);
                }
            });

            await Task.Delay(spawnDelayMs);
        }
    }

    // =========================================================================
    //  2. 나중에 추가될 다양한 이펙트 확장 구역 (예시: 보석 획득)
    // =========================================================================
    public static async Task PlayGemFlyEffect(GameObject gemPrefab, RectTransform startPoint, RectTransform endPoint, Transform canvasTransform, int gemCount)
    {
        // 코인과는 다르게 사방으로 둥글게 퍼졌다가(원형 확산) 호선을 그리며 날아가는 등
        // 보석만의 연출 공식을 이곳에 추가하여 확장해 나가시면 됩니다!
        Debug.Log($"보석 {gemCount}개 연출 시작 (미구현)");
        await Task.CompletedTask;
    }

    // =========================================================================
    // ?? 3. 텍스트 팝업 연출 예시 (데미지 스킨이나 획득 알림 text)
    // =========================================================================
    public static void PlayTextPop(RectTransform textRect, string message)
    {
        // 텍스트가 위로 스르륵 올라가면서 투명해지는 연출 등도 유틸에 모아두면 편합니다.
    }
}
