using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Stat // (또는 BaseStat)
{
    [SerializeField]
    protected float _baseValue;
    [SerializeField]
    protected float _additionalValue;
    [SerializeField]
    protected float _multiplier;

    // 강제 0 스위치
    private bool _isForcedZero = false;

    //  [새로 추가된 기능] 특정 값 강제 고정 스위치
    private bool _isForcedValue = false;
    private float _forcedValue = 0f;

    // 직관적인 퍼센트 값으로 변경 (예: 20을 넣으면 20% 보너스)
    // 기본값은 0(추가 보너스 없음)으로 시작합니다.

    public float TotalValue
    {
        get
        {
            // 1순위: 강제 0 스위치가 켜져 있으면 무조건 0 반환
            if (_isForcedZero) return 0f;

            //  2순위: 특정 값 고정 스위치가 켜져 있으면 그 값을 반환
            if (_isForcedValue) return _forcedValue;

            // 3순위: 기본 계산 로직 실행
            float finalMultiplier = 1f + (_multiplier / 100f);
            float result = (_baseValue + _additionalValue) * finalMultiplier;

            // 쿨타임/시간이 마이너스가 되는 대참사를 막기 위해, 최소 0까지만 내려가게 방어!
            return Mathf.Max(0f, result);
        }
    }

    public virtual void Init(float baseValue)
    {
        _baseValue = baseValue;
        _additionalValue = 0;
        _multiplier = 0f;
    }
    public void SetForceZero(bool active) => _isForcedZero = active;
    public void AddValue(float amount) => _additionalValue += amount;
    public void AddMultiplier(float amount) => _multiplier += amount;
    public void SubValue(float amount) => _additionalValue -= amount;
    public void SubMultiplier(float amount) => _multiplier -= amount;

    //[새로 추가된 함수] 특정 값으로 강제 고정 켜기/끄기
    public void SetForceValue(bool active, float value = 0f)
    {
        _isForcedValue = active;
        _forcedValue = value;
    }
}

