using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

// 서버와 데이터를 주고받기 위한 패킷 구조체 (DTO)
[Serializable]
public class SocialLoginReq
{
    public string loginType; // "Google" 또는 "Guest"
    public string token;    // 구글 AuthCode 또는 기기 고유 ID
}

[Serializable]
public class LoginRes
{
    public string token;    // 서버가 발급해준 JWT 토큰
    public string message;  // "로그인 성공" 등
}

public class LoginManager
{
    // 누군가 이 Action을 구독해두면, 로그인이 끝났을 때 알려줍니다!
    public event Action OnLoginSuccess;

    public bool IsLoginFinished { get; private set; } = false;

    // 핵심: 서버로부터 받은 JWT 토큰을 저장해둘 변수
    public string JwtToken { get; private set; }

    public void Init()
    {
#if UNITY_ANDROID
        LoginGooglePlayGames();
#else
        // 유니티 에디터나 iOS 환경에서는 구글 로그인이 안 되니 바로 익명 로그인으로 빠집니다.
        StartAnonymousSignIn();
#endif
    }

    #region 익명(게스트) 로그인
    public void StartAnonymousSignIn()
    {
        // UGS 대신 기기 고유 식별자(Device ID)를 패스워드처럼 사용하여 서버에 보냅니다.
        string deviceId = SystemInfo.deviceUniqueIdentifier;
        Debug.Log($"게스트 로그인 시도. DeviceID: {deviceId}");

        _ = SendLoginRequestToServer("Guest", deviceId);
    }
    #endregion

    #region 구글 플레이 게임 서비스
#if UNITY_ANDROID
    public void LoginGooglePlayGames()
    {
        PlayGamesPlatform.Instance.Authenticate((status) =>
        {
            if (status == SignInStatus.Success)
            {
                Debug.Log("구글 플레이 서비스 인증 성공. 서버 전용 코드를 요청합니다.");

                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    if (!string.IsNullOrEmpty(code))
                    {
                        Debug.Log("구글 Auth Code 발급 완료! 내 웹 서버로 전송합니다.");
                        // 발급받은 코드를 들고 내 웹 서버로 로그인 요청!
                        SendLoginRequestToServer("Google", code);
                    }
                    else
                    {
                        Debug.LogWarning("구글 코드가 없습니다. 게스트로 우회합니다.");
                        StartAnonymousSignIn();
                    }
                });
            }
            else
            {
                Debug.Log($"구글 로그인 실패 : {status}. 게스트로 우회합니다.");
                StartAnonymousSignIn();
            }
        });
    }
#endif
    #endregion

    #region 내 웹 API 서버와 통신하는 부분 (새로 추가됨)

    // UGS 로그인 대신 실제로 내 서버로 데이터를 쏴주는 함수입니다.
    private async Task SendLoginRequestToServer(string provider, string token)
    {
        // 1. 서버에 보낼 JSON 데이터 만들기
        SocialLoginReq reqObj = new SocialLoginReq
        {
            loginType = provider,
            token = token
        };
        string jsonReq = JsonUtility.ToJson(reqObj);

        // 포스트 방식으로 보내기
        Managers.WebServer.SendPostRequest("auth/social-login", reqObj, LoginSuccessCallback);

        // 만약 나중에 여기서 추가로 비동기 대기를 하고 싶다면 await를 쓸 수 있게 됩니다!
        await Task.CompletedTask; // (Task 반환 구색 맞추기용)
    }

    private void LoginSuccessCallback(UnityWebRequest request)
    {
        //4.응답 결과 처리
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("내 서버 로그인 성공: " + request.downloadHandler.text);

            // JSON 응답을 C# 객체로 변환
            LoginRes resObj = JsonUtility.FromJson<LoginRes>(request.downloadHandler.text);

            //  매우 중요: 다음 패킷 통신들을 위해 JWT 토큰을 메모리에 저장합니다.
            JwtToken = resObj.token;

            IsLoginFinished = true;
            OnLoginSuccess?.Invoke();
        }
        else
        {
            Debug.LogError("내 서버 로그인 실패: " + request.error);
            if (request.downloadHandler != null)
            {
                Debug.LogError("상세 에러: " + request.downloadHandler.text);
            }
        }
    }
    #endregion
}