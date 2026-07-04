using System.Collections;
using UnityEngine;

public class EffectController : MonoBehaviour
{
    // 1. 이펙트가 꺼지는 방식을 결정할 Enum
    public enum ReturnMode
    {
        ParticleEnd,  // 파티클이 모두 끝나면 자동 종료
        CustomTime    // 내가 지정한 시간이 지나면 강제 종료
    }

    [Header("설정")]
    public ReturnMode returnMode = ReturnMode.ParticleEnd;

    [Tooltip("ReturnMode가 CustomTime일 때만 작동합니다.")]
    public float customTime = 2.0f;

    private ParticleSystem _particle;
    private Coroutine _returnCoroutine;

    private void Awake()
    {
        // 자신에게 붙어있는 파티클 시스템을 찾습니다.
        _particle = GetComponent<ParticleSystem>();
    }
    private void OnEnable()
    {
        // 켜지자마자 모든 처리를 코루틴 하나에 맡깁니다.
        _returnCoroutine = StartCoroutine(CoPlayAndCheck());
    }
    private IEnumerator CoPlayAndCheck()
    {
        // 마법의 1줄: Hovl 스크립트 등이 위치와 크기를 세팅할 시간을 딱 1프레임 벌어줍니다.
        yield return null;

        // 세팅이 끝난 완벽한 상태에서 재생!
        if (_particle != null)
        {
            _particle.Play(true);
        }

        // 모드에 따라 종료 타이밍 체크
        if (returnMode == ReturnMode.ParticleEnd)
        {
            // 파티클이 끝날 때까지 대기
            while (_particle != null && _particle.IsAlive(true))
            {
                yield return null;
            }
            ReturnToPool();
        }
        else if (returnMode == ReturnMode.CustomTime)
        {
            // 지정한 시간만큼 대기
            yield return new WaitForSeconds(customTime);
            ReturnToPool();
        }
    }
    //private void OnEnable()
    //{
    //    // 1. 이펙트가 켜지면 '본인 스스로' 파티클을 재생시킵니다!
    //    if (_particle != null)
    //    {
    //        // true를 넣으면 자식 파티클들까지 모두 재생됩니다.
    //        _particle.Play(true);
    //    }

    //    // 2. 켜질 때 모드에 따라 다르게 작동!
    //    if (returnMode == ReturnMode.ParticleEnd)
    //    {
    //        if (_particle != null)
    //        {
    //            // 파티클 생존 여부를 실시간으로 추적하는 코루틴 시작
    //            _returnCoroutine = StartCoroutine(CoWaitForParticleEnd());
    //        }
    //        else
    //        {
    //            // 파티클 컴포넌트가 없는데 ParticleEnd 모드라면 안전장치로 1초 뒤 종료
    //            Invoke("ReturnToPool", 1.0f);
    //        }
    //    }
    //    else if (returnMode == ReturnMode.CustomTime)
    //    {
    //        // 지정한 시간이 지나면 종료
    //        Invoke("ReturnToPool", customTime);
    //    }
    //}

    private IEnumerator CoWaitForParticleEnd()
    {
        yield return null;

        // 파티클이 살아서 재생 중이면 다음 프레임까지 대기 (자식 파티클까지 모두 체크)
        while (_particle != null && _particle.IsAlive(true))
        {
            yield return null;
        }

        // 파티클이 완전히 죽으면 루프를 빠져나와 풀로 돌아갑니다.
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        // 유저님의 풀링 반납 시스템 호출
        Managers.Resource.Destroy(gameObject);
    }

    private void OnDisable()
    {
        // 오브젝트가 꺼질 때 꼬이지 않도록 예약된 함수/코루틴 정리
        CancelInvoke();
        if (_returnCoroutine != null)
        {
            StopCoroutine(_returnCoroutine);
            _returnCoroutine = null;
        }
    }
}
