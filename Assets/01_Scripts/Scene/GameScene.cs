using UnityEngine;

public class GameScene : BaseScene
{
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
        Managers.Unlock.Init();
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
