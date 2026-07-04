using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class CameraManager 
{
   ////  딕셔너리를 사용하여 Enum 키값 하나로 모든 카메라를 관리합니다.
   //private Dictionary<Define.CameraType, CinemachineCamera> _cameras = new Dictionary<Define.CameraType, CinemachineCamera>();
   //
   ///// <summary>
   ///// 씬이 로드되었을 때 (예: Managers.Init() 등에서) 딱 한 번 호출해주세요.
   ///// 본인이 직접 씬을 뒤져서 카메라들을 딕셔너리에 등록합니다.
   ///// </summary>
   //public void Init()
   //{
   //    _cameras.Clear(); // 씬 재시작 시 중복 등록 방지
   //
   //    // 1. 씬에 존재하는 모든 시네머신 카메라 자동 검색
   //    CinemachineCamera[] foundCameras = Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
   //
   //    // 2. 이름 기준으로 Enum과 매칭하여 딕셔너리에 저장
   //    foreach (var cam in foundCameras)
   //    {
   //        //  주의: 유니티 하이어라키(Hierarchy)의 게임오브젝트 이름과 정확히 일치해야 합니다!
   //        switch (cam.gameObject.name)
   //        {
   //            case "MainPlayerCamera": // 기본 카메라 이름
   //                _cameras[Define.CameraType.Main] = cam;
   //                break;
   //            case "BossCamera": // 보스 카메라 이름
   //                _cameras[Define.CameraType.Boss] = cam;
   //                break;
   //        }
   //    }
   //
   //    Debug.Log($"[CameraManager] 셋업 완료! 등록된 카메라 개수: {_cameras.Count}개");
   //}
   //
   ///// <summary>
   ///// 원하는 카메라로 부드럽게 화면을 전환합니다.
   ///// 사용 예: Managers.Camera.ChangeCamera(CameraType.Boss);
   ///// </summary>
   //public void ChangeCamera(Define.CameraType type)
   //{
   //    if (_cameras.Count == 0)
   //    {
   //        Debug.LogWarning("[CameraManager] 등록된 카메라가 없습니다. Init()이 호출되었는지 확인해주세요.");
   //        return;
   //    }
   //
   //    // 1. 딕셔너리에 있는 모든 카메라의 우선순위를 일괄적으로 낮춥니다 (다 꺼버림)
   //    foreach (var cam in _cameras.Values)
   //    {
   //        if (cam != null)
   //        {
   //            cam.Priority = INACTIVE_PRIORITY;
   //        }
   //    }
   //
   //    // 2. 요청받은 카메라만 딕셔너리에서 꺼내서 우선순위를 높입니다 (화면 뺏어오기)
   //    if (_cameras.TryGetValue(type, out CinemachineCamera targetCamera))
   //    {
   //        targetCamera.Priority = ACTIVE_PRIORITY;
   //    }
   //    else
   //    {
   //        Debug.LogWarning($"[CameraManager] {type} 카메라를 찾을 수 없습니다. 이름이 틀렸거나 씬에 없습니다.");
   //    }
   //}
   //
   ///// <summary>
   ///// 등록된 모든 카메라의 추적 대상(Follow)을 한 번에 설정합니다.
   ///// 플레이어가 스폰된 직후 호출해 주세요.
   ///// </summary>
   //public void SetTarget(Transform targetTransform)
   //{
   //    if (_cameras.Count == 0)
   //    {
   //        Debug.LogWarning("[CameraManager] 카메라가 없습니다. Init()이 먼저 호출되었는지 확인하세요.");
   //        return;
   //    }
   //
   //    // 딕셔너리에 등록된 모든 카메라를 순회하며 타겟을 쥐어줍니다.
   //    foreach (var cam in _cameras.Values)
   //    {
   //        if (cam != null)
   //        {
   //            cam.Follow = targetTransform;
   //            // 만약 카메라 회전(LookAt)도 타겟을 봐야 한다면 아래 코드도 주석 해제하세요.
   //            // cam.LookAt = targetTransform; 
   //        }
   //    }
   //
   //    Debug.Log($"[CameraManager] {_cameras.Count}개의 카메라 타겟이 {targetTransform.name}(으)로 설정되었습니다!");
   //}
}
