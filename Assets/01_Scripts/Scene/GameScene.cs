using UnityEngine;

public class GameScene : BaseScene
{
    [Header("영업 시작 설정")]
    [SerializeField] private bool openOnStart;

    protected override void Init()
    {
        // 1. 이미 다른 씬(LoginScene 등)에서 로드가 완료되었다면 즉시 초기화 실행
        if (Managers.Resource.IsLoaded)
        {
            InitUnlock();
        }
        else
        {
            // 2. 아직 로드 중이라면 완료 이벤트를 구독하여 대기
            Managers.Resource.assetLoaded += OnResourcesLoaded;
        }
    }
    private void OnResourcesLoaded()
    {
        // 중복 실행 및 메모리 누수 방지를 위해 이벤트 해제
        Managers.Resource.assetLoaded -= OnResourcesLoaded;
        InitUnlock();
    }

    private void InitUnlock()
    {
        // 씬에 배치된 해금 대상부터 복원해 손님이 이동할 시설을 준비합니다.
        Managers.Unlock.Init();

        // 시작 설비가 이미 놓인 매장은 별도 구매 과정 없이 곧바로 영업을 시작합니다.
        // 기존 씬은 기본값이 꺼짐이므로 기존 해금 순서를 그대로 사용합니다.
        if (openOnStart)
            Managers.Game.OpenShop();

        InitUI();
    }

    private void InitUI()
    {
        Managers.UI.ShowSceneUI<UI_GameScene>();
        // 한 화면만 풀에서 꺼내 두고 모든 기계 탭에서 대상만 바꿔 사용합니다.
        Managers.UI.EnsureMachineUpgradeUI();

        // 씬의 상호작용 객체가 UI 프리팹 로드 전에 활성화됐을 수 있어 상태를 다시 표시합니다.
        foreach (InteractableBase interaction in FindObjectsByType<InteractableBase>(FindObjectsSortMode.None))
            interaction.RefreshStatusBubble();
    }
    public override void Clear()
    {
        // 씬 전환 시 에러 방지를 위해 최종 이벤트 해제 보장
        if (Managers.Resource != null)
        {
            Managers.Resource.assetLoaded -= OnResourcesLoaded;
        }
    }
}
