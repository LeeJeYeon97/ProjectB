using System;
using UnityEngine;

public interface IInteractable
{
    // 이 상호작용을 완료하는 데 걸리는 시간 (0이면 밟자마자 즉시 실행)
    float Duration { get; }

    // 현재 캐릭터가 이 상호작용을 수행할 수 있는지 검사 (예: 빈 접시가 있는가? 맥주 잔을 들 공간이 있는가?)
    bool CanInteract(GameObject actor);

    // 상호작용 시작 시 호출 (애니메이션, 사운드 등)
    void OnStartInteract(GameObject actor) { }

    // 상호작용 진행 중 호출 (게이지 진행도 0~1 전달, 지속형 상호작용에 유용)
    void OnUpdateInteract(GameObject actor, float progress) { }

    // 상호작용 완료 시 실제 작업 수행 (맥주 획득, 접시 세척, 돈 정산 등)
    void OnCompleteInteract(GameObject actor);

    // 도중에 발판을 벗어났을 때 처리
    void OnCancelInteract(GameObject actor) { }
}