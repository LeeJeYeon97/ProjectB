using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class UpdateManager
{
//    // 현재 앱 버전을 저장할 변수
//    public string currentAppVersion = "1.0.0";

//    // Remote Config 요청 시 필요한 빈 구조체 (UGS 필수 스펙)
//    public struct userAttributes { }
//    public struct appAttributes { }

//    /// <summary>
//    /// 플레이어 진입 전 최신 버전을 체크하는 비동기 함수
//    /// </summary>
//    /// <returns>로그인을 진행해도 되면 true, 강제 업데이트로 막아야 하면 false</returns>
//    public async Task<bool> InitAsync()
//    {
//        // 빌드된 클라이언트 앱의 실제 버전을 가져옵니다 (Player Settings 기준)
//        currentAppVersion = Application.version;

//        try
//        {
//            // 1. UGS Remote Config 서버에서 최신 버전 정보 긁어오기
//            await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());

//            // 2. 대시보드에 설정해둔 최소 필수 버전과 최신 추천 버전 가져오기
//            string minVersionStr = RemoteConfigService.Instance.appConfig.GetString("MinRequiredVersion", "1.0.0");
//            string latestVersionStr = RemoteConfigService.Instance.appConfig.GetString("LatestVersion", "1.0.0");

//            // 3. 자릿수가 다른 버전(예: 1.0.10 vs 1.0.2)도 정확히 연산해주는 Version 객체로 변환
//            Version current = new Version(currentAppVersion);
//            Version min = new Version(minVersionStr);
//            Version latest = new Version(latestVersionStr);

//            // 4. 버전 비교 분석 시작
//            if (current < min)
//            {
//                // [강제 업데이트] 최소 구동 버전보다 낮으므로 무조건 진입을 막아야 함
//                Debug.LogWarning($"[업데이트] 필수 업데이트 필요! 현재 버전: {current} / 최소 버전: {min}");
//                ShowUpdatePopup(isForced: true);
//                return false; // 다음 단계(로그인 등) 진행을 차단하기 위해 false 반환
//            }
//            else if (current < latest)
//            {
//                // [선택 업데이트] 구동은 가능하나 최신 버전이 존재함
//                Debug.Log($"[업데이트] 권장 업데이트 버전이 존재합니다. 현재 버전: {current} / 최신 버전: {latest}");
//                ShowUpdatePopup(isForced: false);
//                return true; // 차단할 필요는 없으므로 일단 게임 진행 허용(true 반환)
//            }

//            // 최신 버전 클라이언트인 경우 패스
//            Debug.Log("[업데이트] 최신 버전 클라이언트입니다. 검증 완료.");
//            return true;
//        }
//        catch (Exception e)
//        {
//            // 네트워크 연결 실패 등 예외 발생 시, 라이브 환경에서는 우선 게임을 정상 진입시키는 것이 일반적입니다.
//            Debug.LogError($"버전 체크 중 오류 발생 (네트워크 상태 확인 필요): {e.Message}");
//            return true;
//        }
//    }

//    /// <summary>
//    /// 조건에 따라 업데이트 유도 UI 팝업을 생성하는 함수
//    /// </summary>
//    /// <param name="isForced">true면 취소/닫기 버튼이 없는 강제 팝업, false면 '나중에 하기'가 있는 선택 팝업</param>
//    private void ShowUpdatePopup(bool isForced)
//    {
//        // 이미 구성되어 있는 UI 시스템(Managers.UI)을 사용하여 제작하신 업데이트 팝업을 띄우시면 됩니다.
//        // 예시: 

//        string textKey = "UpdatePopup_Text";
//        string text = LocalizationSettings.StringDatabase.GetLocalizedString("UI", textKey);
//        var popup = Managers.UI.ShowPopupUI<UI_SystemPopup>();

//        if (popup != null) popup.SetInfo(text, OpenStoreURL);
//    }

//    /// <summary>
//    /// 실제 스토어 마켓으로 앱을 리다이렉트 시키는 함수 (업데이트 버튼에 연결하세요)
//    /// </summary>
//    public void OpenStoreURL()
//    {
//#if UNITY_ANDROID
//        // 안드로이드: 현재 앱의 패키지명(Application.identifier)을 기반으로 구글 플레이 스토어 앱 인텐트 실행
//        Application.OpenURL("market://details?id=" + Application.identifier);
//#elif UNITY_IOS
//        // iOS: 앱스토어 링크 연결 (출시 후 커넥트에서 발급받는 고유 넘버 입력 필요)
//        string appleAppID = "여기에_고유_애플_앱_아이디_숫자만_입력";
//        Application.OpenURL("itms-apps://itunes.apple.com/app/id" + appleAppID);
//#endif

//        // 스토어로 화면이 전환되었으므로, 구버전 클라이언트 세션이 유지되지 않도록 앱을 완전히 종료합니다.
//        Application.Quit();
//    }
}
