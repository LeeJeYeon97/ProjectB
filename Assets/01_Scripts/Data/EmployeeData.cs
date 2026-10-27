using System.Collections.Generic;
using UnityEngine;

/// <summary>고용 화면에 표시할 직원 한 명의 설정과 업무 능력을 보관합니다.</summary>
[CreateAssetMenu(fileName = "Employee_", menuName = "Content/Employee Data")]
public class EmployeeData : ScriptableObject
{
    #region 변수

    [Tooltip("고용 중복 확인에 사용하는 고유 ID입니다.")]
    public string Id;

    [Tooltip("후보 목록에서 먼저 표시할 순서입니다.")]
    public int Order;

    [Tooltip("고용 화면에 표시할 직원 이름입니다.")]
    public string Name;

    [Min(0)]
    [Tooltip("직원을 고용할 때 지불할 금액입니다. 0이면 테스트용 무료 고용입니다.")]
    public int HirePrice;

    [Tooltip("직원 인스턴스를 생성할 프리팹입니다. 현재 후보들은 같은 프리팹을 공유합니다.")]
    public GameObject Prefab;

    [Min(0.1f)]
    [Tooltip("이 직원의 기본 이동 속도입니다.")]
    public float MoveSpeed = 3.8f;

    [Range(0.25f, 1f)]
    [Tooltip("기본 작업 시간 배율입니다. 낮을수록 작업이 빠릅니다.")]
    public float WorkTimeScale = 1f;

    [Tooltip("이 직원이 맡을 수 있는 업무만 넣습니다.")]
    public List<Define.EmployeeTaskType> Tasks = new();

    #endregion
}
