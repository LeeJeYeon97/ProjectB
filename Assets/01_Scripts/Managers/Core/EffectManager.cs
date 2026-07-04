using System.Collections.Generic;
using UnityEngine;
using static Define;

public class EffectManager
{
    // Dictionary로 빠른 검색을 지원합니다.
    private Dictionary<EffectType, GameObject> _effectDict = new Dictionary<EffectType, GameObject>();

    [Header("스로틀링 설정")]
    [Tooltip("1프레임당 허용할 최대 폭발 횟수 (화면 전체 기준)")]
    public int maxExplosionsPerFrame = 2; // 거리를 잴 것이므로 5~8 정도로 살짝 늘려주셔도 좋습니다.

    [Tooltip("이 반경 안에서는 폭발이 중복으로 터지지 않음!")]
    public float minDistanceBetweenExplosions = 10.0f;

    

    // 핵심: 이번 프레임에 이미 허락받고 터질 위치들을 기억하는 리스트
    private List<Vector2> _frameExplosionPositions = new List<Vector2>();

    public void Init()
    {
        //// 2. Dictionary에 예쁘게 매핑해 둡니다.
        //foreach (var effect in Managers.Data.EffectData.effectList)
        //{
        //    _effectDict.Add(effect.type, effect.prefab);
        //}
    }

    // 이펙트 재생 핵심 함수
    public GameObject Play(EffectType type, Vector3 position)
    {
        if (_effectDict.TryGetValue(type, out GameObject prefab))
        {
            // Managers.Resource의 풀링 시스템을 이용해 이펙트를 가져옵니다!
            // (이름을 기반으로 프리팹을 Instantiate 하는 기존 기능 활용)
            GameObject effectGo = Managers.Resource.Instantiate(prefab);
            effectGo.transform.position = position;
            return effectGo;
        }
        else
        {
            Debug.LogWarning($"이펙트를 찾을 수 없습니다: {type}");
            return null;
        }
    }

    // 핵심: 모든 업데이트가 끝나는 프레임의 최하단(LateUpdate)에서 카운터를 0으로 초기화합니다.
    public void OnLateUpdate()
    {
        //if(Managers.Game?.currentGameState == GameState.Playing)
        //{
        //    // 프레임이 끝날 때, 기억해둔 위치들을 싹 비워줍니다. (다음 프레임을 위해)
        //    _frameExplosionPositions.Clear();
        //}
    }

    /// <summary>
    /// 이제 허락을 받을 때 '위치'를 함께 물어봅니다!
    /// </summary>
    public bool CanSpawnEffect(Vector2 newPosition)
    {
        // 1. 전체 개수 제한 컷 (화면에 너무 많이 터지는 것 방지)
        if (_frameExplosionPositions.Count >= maxExplosionsPerFrame)
        {
            return false;
        }

        // 2. 공간 기반 제한 컷 (가까운 곳에서 중복으로 터지는 것 방지)
        for (int i = 0; i < _frameExplosionPositions.Count; i++)
        {
            // Vector2.Distance 대신 sqrMagnitude를 쓰면 내부적으로 제곱근 연산을 안 해서 성능이 훨씬 빠릅니다!
            float sqrDistance = (newPosition - _frameExplosionPositions[i]).sqrMagnitude;
            float minSqrDistance = minDistanceBetweenExplosions * minDistanceBetweenExplosions;

            if (sqrDistance < minSqrDistance)
            {
                Debug.Log("이펙트 생성 패스");
                // 이미 터진 곳과 너무 가깝습니다! 기각!
                return false;
            }
        }

        // 3. 모든 검사를 통과했다면? 
        // 이번 프레임에 터질 명단(리스트)에 내 위치를 적어두고 승인!
        _frameExplosionPositions.Add(newPosition);
        return true;
    }
}
