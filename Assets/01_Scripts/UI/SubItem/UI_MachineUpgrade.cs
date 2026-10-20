/// <summary>코어 UI 매니저의 기존 참조를 유지하기 위한 비활성 업그레이드 화면입니다.</summary>
public class UI_MachineUpgrade : UI_Base
{
    /// <summary>기존 UI 매니저 호출과 호환되도록 화면 초기화 함수를 유지합니다.</summary>
    public override void Init()
    {
        base.Init();
    }

    /// <summary>업그레이드 화면 기능이 다시 만들어질 때까지 화면을 닫아 둡니다.</summary>
    public void ResetForPool()
    {
        if (gameObject != null)
            gameObject.SetActive(false);
    }
}
