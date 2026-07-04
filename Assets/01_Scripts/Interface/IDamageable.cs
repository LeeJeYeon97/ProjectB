using UnityEngine;

public interface IDamageable
{
    // 데미지를 받는 함수를 무조건 가지고 있어야 함!
    void OnDamage(float damage, bool isCrit = false, GameObject attacker = null);
}