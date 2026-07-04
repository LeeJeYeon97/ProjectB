//using GooglePlayGames;
//using System;
//using System.Threading.Tasks;
//using Unity.Services.Core;
//using Unity.Services.Core.Environments;
//using UnityEngine;
//using UnityEngine.Localization.Settings;
//using UnityEngine.UnityConsent;

//public class InitManager
//{
//    public bool IsInitialized { get; private set; } = false;

//    public event Action<float, string> OnInitProgress;
//    public event Action OnUnityServiceInit;
//    // UI 팝업을 띄워달라고 요청하는 이벤트 (UI 스크립트가 이걸 듣고 팝업을 켭니다)
//    //public System.Action ShowConsentPopupEvent;
//    //
//    //// 유저가 버튼을 누를 때까지 코드를 멈춰두는 마법의 객체
//    //private TaskCompletionSource<bool> _consentWaitTask;



//    //// UI에서 '동의' 버튼을 눌렀을 때 호출
//    //public void OnUserAcceptConsent()
//    //{
//    //    // 멈춰있던 Task에 true를 던져주고 코드를 다시 진행시킵니다!
//    //    _consentWaitTask?.TrySetResult(true);
//    //}
//    //
//    //// UI에서 '거절' 버튼을 눌렀을 때 호출
//    //public void OnUserDeclineConsent()
//    //{
//    //    // 멈춰있던 Task에 false를 던져주고 코드를 다시 진행시킵니다!
//    //    _consentWaitTask?.TrySetResult(false);
//    //}

//    public async Task Init()
//    {
//        if (IsInitialized) return;

//        try
//        {
//            OnInitProgress?.Invoke(0.1f, "LoadingText_SystemInit");

//            // 1. 유니티 서비스 초기화 (필수)
//            if (UnityServices.State == ServicesInitializationState.Uninitialized)
//            {
//                // 2. 프로덕션 환경 옵션 세팅
//                var options = new InitializationOptions();
//                options.SetEnvironmentName("production");

//                Debug.Log("Unity Services Initializing...");
//                await UnityServices.InitializeAsync();
                
//                // 3. 리더보드 및 기타 서비스가 완전히 준비될 때까지 안전하게 대기
//                while (UnityServices.State != ServicesInitializationState.Initialized)
//                {
//                    await Task.Yield();
//                }

//                Debug.Log("Unity Services Initialized Successfully!");
//            }

//            // [수정] 버전 체크 함수를 비동기로 호출하고 결과를 기다립니다.
//            // 프로퍼티 이름 오타(VersisonUpdate) 반영해 드렸습니다!
//            bool isVersionOk = await Managers.VersisonUpdate.InitAsync();

//            if (isVersionOk == false)
//            {
//                // 최신 버전이 아니어서 강제 업데이트 팝업이 떴다면, 
//                // 아래의 로그인 로직을 타지 못하게 여기서 Init 함수를 완전히 종료시킵니다!
//                Debug.LogWarning("구버전 클라이언트 감지: 초기화 및 로그인을 중단합니다.");
//                return;
//            }

//            OnUnityServiceInit?.Invoke();

//            // 2. 애널리틱스 동의 상태 세팅
//            EndUserConsent.SetConsentState(new ConsentState
//            {
//                AnalyticsIntent = ConsentStatus.Granted,
//                //AdsIntent = ConsentStatus.Denied
//            });

//            // 3. 구글 플레이 게임즈 초기화 (최신 v11 방식)
//#if UNITY_ANDROID
//            PlayGamesPlatform.DebugLogEnabled = true;
//            PlayGamesPlatform.Activate();
//#endif
//            OnInitProgress?.Invoke(0.4f, "LoadingText_AccountCheck");

//            IsInitialized = true;
//            Debug.Log("All Systems Initialized");

//            // 로그인 진행
//            Managers.Login.Init();
//        }
//        catch (Exception e)
//        {
//            //Debug.LogError($"Initialization Failed: {e.Message}");
//            Debug.LogException(e);
//        }
//    }
//}
