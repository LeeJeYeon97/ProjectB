using System.Collections;
using UnityEngine;


//public class UGSLoginManager
//{

    //private string _GooglePlayGamesToken;

    //    // 누군가 이 Action을 구독해두면, 로그인이 끝났을 때 알려줍니다!
    //    public event Action OnLoginSuccess;

    //    public bool IsLoginFinished { get; private set; } = false;

    //    // 유니티 서비스 초기화
    //    public void Init()
    //    {
    //        // 구글 플레이 게임즈 '자동 로그인(Silent Login)'을 가장 먼저 시도합니다!
    //#if UNITY_ANDROID
    //        Debug.Log("구글 자동 로그인을 시도합니다...");
    //        LoginGooglePlayGames();
    //#else
    //        // 유니티 에디터나 iOS 환경에서는 구글 로그인이 안 되니 바로 익명 로그인으로 빠집니다.
    //        StartAnonymousSignIn();
    //#endif
    //    }
    //    #region 익명 로그인
    //    public async void StartAnonymousSignIn()
    //    {
    //        await SignInAnonymouslyAsync();
    //    }
    //    // 익명 로그인
    //    private async Task SignInAnonymouslyAsync()
    //    {
    //        try
    //        {
    //            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    //            Debug.Log("Sign in anonymously succeeded!");


    //            // Shows how to get the playerID
    //            Debug.Log($"PlayerID: {AuthenticationService.Instance.PlayerId}");

    //            // 이름이 없다면 Guest로 업데이트 요청 (이때 서버에서 Guest#3912 처럼 태그를 붙여줌)
    //            if (string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName))
    //            {
    //                await AuthenticationService.Instance.UpdatePlayerNameAsync("Guest");
    //            }

    //            IsLoginFinished = true;
    //            OnLoginSuccess?.Invoke();
    //        }
    //        catch (AuthenticationException ex)
    //        {
    //            // 핵심: 만약 에러 코드가 '잘못된 세션 토큰'이라면?
    //            if (ex.ErrorCode == AuthenticationErrorCodes.InvalidSessionToken)
    //            {
    //                Debug.LogWarning("유효하지 않은 토큰입니다. 세션을 초기화하고 다시 시도합니다.");

    //                // 유령 열쇠를 버립니다.
    //                AuthenticationService.Instance.SignOut(true);

    //                // 다시 로그인을 시도하면 서버에서 새로운 유저로 만들어줍니다!
    //                await SignInAnonymouslyAsync();
    //            }
    //            else
    //            {
    //                Debug.LogException(ex);
    //            }
    //        }
    //        catch (RequestFailedException ex)
    //        {
    //            // Compare error code to CommonErrorCodes
    //            // Notify the player with the proper error message
    //            Debug.LogException(ex);
    //        }
    //    }

    //    #endregion

    //    #region Unity Player Account
    //    public async void StartUnitySignInAsync()
    //    {
    //        if (PlayerAccountService.Instance.IsSignedIn)
    //        {
    //            SignInOrLinkWithUnity();
    //            return;
    //        }

    //        try
    //        {
    //            await PlayerAccountService.Instance.StartSignInAsync();
    //        }
    //        catch (RequestFailedException ex)
    //        {
    //            Debug.LogException(ex);

    //        }
    //    }

    //    private async void SignInOrLinkWithUnity()
    //    {
    //        try
    //        {
    //            // 1. 플레이어가 인증되지 않았기 때문에 유니티에 가입
    //            if (AuthenticationService.Instance.IsSignedIn == false)
    //            {
    //                Debug.Log("Signing up with Unity Player Account...");
    //                await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
    //                Debug.Log("Successfully signed up with Unity Player Account");
    //                return;
    //            }
    //            // 2. 플레이어가 인증되었지만 링크되어 있지않을때
    //            if (HasUnityID() == false)
    //            {
    //                // 유니티 아이디가 없을 때 연결
    //                Debug.Log("Linking anonymous account to Unity...");
    //                await LinkWithUnityAsync(PlayerAccountService.Instance.AccessToken);
    //                Debug.Log("Successfully linked anonymous account!");
    //                return;
    //            }

    //            // 3. 플레이어가 로그인 되었고 유니티 계정에 연결 되었을 때
    //            Debug.Log("Player is already signed in to their Unity Player Account");
    //        }
    //        catch (RequestFailedException ex)
    //        {
    //            Debug.LogException(ex);
    //        }
    //    }

    //    // 유니티 아이디 확인함수
    //    private bool HasUnityID()
    //    {
    //        return AuthenticationService.Instance.PlayerInfo.GetUnityId() != null;
    //    }
    //    // 유니티 계정 연결
    //    async Task LinkWithUnityAsync(string accessToken)
    //    {
    //        try
    //        {
    //            await AuthenticationService.Instance.LinkWithUnityAsync(accessToken);
    //            Debug.Log("Link is successful.");
    //        }
    //        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
    //        {
    //            // Prompt the player with an error message.
    //            Debug.LogError("This user is already linked with another account. Log in instead.");
    //        }

    //        catch (AuthenticationException ex)
    //        {
    //            // Compare error code to AuthenticationErrorCodes
    //            // Notify the player with the proper error message
    //            Debug.LogException(ex);
    //        }
    //        catch (RequestFailedException ex)
    //        {
    //            // Compare error code to CommonErrorCodes
    //            // Notify the player with the proper error message
    //            Debug.LogException(ex);
    //        }
    //    }

    //    #endregion

    //    #region 구글 플레이 게임 서비스

    //    public void LoginGooglePlayGames()
    //    {
    //        PlayGamesPlatform.Instance.Authenticate((status) =>
    //        {
    //            if (status == SignInStatus.Success)
    //            {
    //                Debug.Log("Login with Google Play games successful.");

    //                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
    //                {
    //                    if (!string.IsNullOrEmpty(code))
    //                    {
    //                        _GooglePlayGamesToken = code;
    //                        SignInOrLinkWithGooglePlayGames();
    //                    }
    //                    else
    //                    {
    //                        // 토큰 요청 실패 시에도 게임은 시켜줘야 하므로 익명 로그인으로 우회
    //                        Debug.LogWarning("Google Token null, switching to Anonymous");
    //                        StartAnonymousSignIn();
    //                    }
    //                });
    //            }
    //            else
    //            {
    //                Debug.Log($"Google Play Games Login Unsuccessful status : {status}");
    //                Debug.Log("신규 유저입니다. 바로 게스트(익명) 로그인을 시작합니다!");
    //                StartAnonymousSignIn();
    //            }
    //        });
    //    }
    //    public void StartSignInWithGooglePlayGames()
    //    {
    //        //  1. [로딩 ON] 유저가 연동 버튼을 누르자마자 로딩 팝업을 띄웁니다!
    //        _LoadingPopup = Managers.UI.ShowPopupUI<UI_LoadingPopup>(); // (대표님의 로딩 팝업 클래스명으로 변경해주세요)

    //        if (PlayGamesPlatform.Instance.IsAuthenticated() == true)
    //        {
    //            Debug.Log("이미 구글에 로그인되어 있습니다. UGS 연동용 토큰을 요청합니다.");

    //            //  [수정된 부분] 토큰을 달라고 구글에 요청한 뒤에 연동 함수를 부릅니다!
    //            PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
    //            {
    //                if (!string.IsNullOrEmpty(code))
    //                {
    //                    Debug.Log("자동 로그인 토큰 발급 완료: " + code);
    //                    _GooglePlayGamesToken = code;
    //                    SignInOrLinkWithGooglePlayGames(); // 이제 토큰이 있으니 정상 작동!
    //                }
    //                else
    //                {
    //                    Debug.LogWarning("자동 로그인은 되어있으나 토큰 발급에 실패했습니다.");
    //                    //  [로딩 OFF] 토큰 발급 실패 시 무한 로딩 방지
    //                    Managers.UI.ClosePopupUI(_LoadingPopup);
    //                }
    //            });
    //            return;
    //        }
    //        // 2. 로그인이 안 되어 있다면 무조건 강제 팝업(수동 로그인)을 띄웁니다!
    //        Debug.LogWarning("구글 로그인 팝업을 강제로 띄웁니다!");

    //        PlayGamesPlatform.Instance.ManuallyAuthenticate((status) =>
    //        {
    //            if (status == SignInStatus.Success)
    //            {
    //                Debug.Log("수동 로그인 성공! 팝업에서 권한을 허락받았습니다.");

    //                // 성공했으니 유니티 서버(UGS)에 넘길 암호(Token)를 달라고 요청합니다.
    //                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
    //                {
    //                    if (!string.IsNullOrEmpty(code))
    //                    {
    //                        Debug.Log("Authorization code: " + code);
    //                        _GooglePlayGamesToken = code;
    //                        SignInOrLinkWithGooglePlayGames();
    //                    }
    //                    else
    //                    {
    //                        // [로딩 OFF] 여기서도 토큰 발급 실패 방어
    //                        Managers.UI.ClosePopupUI(_LoadingPopup);
    //                    }
    //                });
    //            }
    //            else
    //            {
    //                Debug.Log($"수동 로그인 실패 또는 유저가 팝업을 닫음: {status}");
    //                Managers.UI.ClosePopupUI(_LoadingPopup);
    //            }
    //        });
    //    }
    //    private async void SignInOrLinkWithGooglePlayGames()
    //    {
    //        if (string.IsNullOrEmpty(_GooglePlayGamesToken))
    //        {
    //            //앱을 방금 막 켰기 때문에 _GooglePlayGamesToken 변수는 텅 비어(null) 있습니다.
    //            //이 토큰(암호)은 아래에 있는 RequestServerSideAccess를 호출했을 때만 채워지는데,
    //            //이미 로그인이 되어있다고 해서 토큰을 발급받는 과정을 통째로 건너뛰어 버린 것입니다.
    //            Debug.LogWarning("Authorization code is null or empty!");
    //            // [로딩 OFF] 토큰이 비어있어서 튕겨낼 때 로딩 해제
    //            Managers.UI.ClosePopupUI(_LoadingPopup);
    //            return;
    //        }
    //        if (AuthenticationService.Instance.IsSignedIn == false)
    //        {
    //            await SignInWithGooglePlayGamesAsync(_GooglePlayGamesToken);
    //        }
    //        else
    //        {
    //            await LinkWithGooglePlayGamesAsync(_GooglePlayGamesToken);
    //        }
    //    }
    //    /// <summary>기존 플레이어 로그인 또는 신규 플레이어 생성
    //    /// 1. Google Play 게임즈 자격 증명을 사용해 새 Unity Authentication 플레이어 생성
    //    /// 2. Google Play 게임즈 자격 증명을 사용해 기존 플레이어 로그인  
    //    /// 프로젝트에서 자격 증명과 연결된 Unity Authentication 플레이어가 존재하지 않는 경우, 
    //    /// SignInWithGooglePlayGamesAsync가 새 플레이어를 생성합니다. 
    //    /// 프로젝트에서 자격 증명과 연결된 Unity Authentication 플레이어가 존재하는 경우, 
    //    /// SignInWithGooglePlayGamesAsync가 해당 플레이어의 계정으로 로그인합니다. 
    //    /// 이 기능은 캐시된 플레이어를 고려하지 않으며, SignInWithGooglePlayGamesAsync가 캐시된 플레이어를 대체합니다.
    //    /// </summary>
    //    /// <param name="authCode"></param>
    //    /// <returns></returns>
    //    async Task SignInWithGooglePlayGamesAsync(string authCode)
    //    {
    //        try
    //        {
    //            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(authCode);
    //            Debug.Log("SignIn is successful.");

    //            await ChangeNickNameToGoogle();
    //            IsLoginFinished = true;
    //            HandleGoogleLinkSuccess();
    //        }
    //        catch (AuthenticationException ex)
    //        {
    //            Debug.LogException(ex);
    //            //  [핵심 방어막] 구글 로그인이 모종의 이유로 터졌을 때 멈추지 않게 플랜 B 가동!
    //            Debug.LogWarning("구글 로그인 실패! 게스트(익명) 모드로 우회하여 게임을 시작합니다.");
    //            Managers.UI.ClosePopupUI(_LoadingPopup);
    //            StartAnonymousSignIn();
    //        }
    //        catch (RequestFailedException ex)
    //        {
    //            Debug.LogException(ex);
    //            // 네트워크 에러 등의 경우에도 플랜 B 가동!
    //            Debug.LogWarning("네트워크/서버 에러! 게스트(익명) 모드로 우회하여 게임을 시작합니다.");
    //            Managers.UI.ClosePopupUI(_LoadingPopup);
    //            StartAnonymousSignIn();
    //        }
    //    }

    //    /// <summary> 플레이어를 익명 로그인에서 Google Play 게임즈 계정 로그인으로 업데이트
    //    /// 익명 인증을 설정한 후, 
    //    /// 플레이어가 Google Play 게임즈 계정을 생성하고 Google Play 게임즈를 통해 로그인하도록 업그레이드하려는 경우, 
    //    /// 게임이 플레이어에게 Google Play 게임즈 로그인 창을 표시하고 Google에서 일회성 인증 코드를 가져와야 합니다. 
    //    /// 그런 다음 LinkWithGooglePlayGamesAsync API를 호출해 플레이어를 연결합니다.
    //    /// SDK에 캐시된 플레이어가 존재하는 경우, 캐시된 플레이어를 Google Play 게임즈 계정에 연결할 수 있습니다.
    //    /// 
    //    /// 1. SignInAnonymouslyAsync를 사용해 캐시된 플레이어의 계정에 로그인합니다.
    //    /// 2. LinkWithGooglePlayGamesAsync를 사용해 캐시된 플레이어의 계정을 Google Play 게임즈 계정에 연결합니다.
    //    /// </summary>
    //    /// <param name="authCode"></param>
    //    /// <returns></returns>

    //    async Task LinkWithGooglePlayGamesAsync(string authCode)
    //    {
    //        try
    //        {
    //            await AuthenticationService.Instance.LinkWithGooglePlayGamesAsync(authCode);
    //            Debug.Log("Link is successful.");

    //            await ChangeNickNameToGoogle();
    //            IsLoginFinished = true;
    //            HandleGoogleLinkSuccess();
    //        }
    //        catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.AccountAlreadyLinked)
    //        {
    //            // Prompt the player with an error message.
    //            Debug.LogError("This user is already linked with another account. Log in instead.");

    //            Debug.LogWarning("이미 연동된 구글 계정이 있습니다. 기존 데이터를 불러옵니다(복구 시작)!");

    //            // 1. 현재 접속되어 있는 '쓸모없는 쌩초보 익명 계정'에서 로그아웃하고 기기 토큰을 지웁니다.
    //            AuthenticationService.Instance.SignOut(true);

    //            //  2. [핵심 수정] authCode는 이미 방금 전 Link 시도에서 타버렸습니다(일회용)!
    //            // 구글에 다시 '새 암호'를 달라고 요청한 뒤에 SignIn을 시도해야 합니다.
    //            PlayGamesPlatform.Instance.RequestServerSideAccess(true, async newAuthCode =>
    //            {
    //                if (!string.IsNullOrEmpty(newAuthCode))
    //                {
    //                    Debug.Log("복구용 새 구글 토큰 발급 완료! 로그인을 시도합니다.");
    //                    // 새로 발급받은 newAuthCode를 사용해서 로그인(SignIn) 진행
    //                    await SignInWithGooglePlayGamesAsync(newAuthCode);
    //                }
    //                else
    //                {
    //                    Debug.LogError("복구용 새 구글 토큰 발급에 실패했습니다.");
    //                    Managers.UI.ClosePopupUI(_LoadingPopup);
    //                }
    //            });
    //        }

    //        catch (AuthenticationException ex)
    //        {
    //            // Compare error code to AuthenticationErrorCodes
    //            // Notify the player with the proper error message
    //            Debug.LogException(ex);
    //            Managers.UI.ClosePopupUI(_LoadingPopup);
    //        }
    //        catch (RequestFailedException ex)
    //        {
    //            // Compare error code to CommonErrorCodes
    //            // Notify the player with the proper error message
    //            Debug.LogException(ex);
    //            Managers.UI.ClosePopupUI(_LoadingPopup);
    //        }
    //    }
    //    // 내부의 구글 연동 성공 처리 함수
    //    private async void HandleGoogleLinkSuccess()
    //    {
    //        try
    //        {
    //            // 1. 연동 성공 후, 내 최신 계정 정보(Identities 등)를 서버에서 다시 받아옴
    //            await AuthenticationService.Instance.GetPlayerInfoAsync();
    //            Debug.Log("서버에서 최신 유저 정보 갱신 완료!");
    //        }
    //        catch (AuthenticationException ex)
    //        {
    //            Debug.LogError($"플레이어 정보 갱신 실패: {ex.Message}");
    //        }

    //        Managers.UI.ClosePopupUI(_LoadingPopup);
    //        // 2. 정보 갱신이 완전히 끝난 후, UI들에게 "이제 화면 바꿔도 돼!" 하고 방송함
    //        if (OnLoginSuccess != null)
    //            OnLoginSuccess.Invoke();
    //    }

    //    /// <summary> Google Play 계정 연결 해제
    //    ///  플레이어가 Google Play 게임즈 계정 연결을 해제할 수 있도록 UnlinkGooglePlayGamesAsync API를 사용합니다. 
    //    ///  연결이 해제되면 계정이 다른 ID에 연결되지 않은 경우 익명 계정으로 전환됩니다.
    //    /// </summary>
    //    /// <param name="idToken"></param>
    //    /// <returns></returns>
    //    //async Task UnlinkGooglePlayGamesAsync(string idToken)
    //    //{
    //    //    try
    //    //    {
    //    //        await AuthenticationService.Instance.UnlinkGooglePlayGamesAsync(idToken);
    //    //        Debug.Log("Unlink is successful.");
    //    //    }
    //    //    catch (AuthenticationException ex)
    //    //    {
    //    //        // Compare error code to AuthenticationErrorCodes
    //    //        // Notify the player with the proper error message
    //    //        Debug.LogException(ex);
    //    //    }
    //    //    catch (RequestFailedException ex)
    //    //    {
    //    //        // Compare error code to CommonErrorCodes
    //    //        // Notify the player with the proper error message
    //    //        Debug.LogException(ex);
    //    //    }
    //    //}
    //    #endregion

    //    private async Task ChangeNickNameToGoogle()
    //    {
    //#if UNITY_ANDROID
    //        try
    //        {
    //            // 1. 구글 시스템에서 유저의 프로필 이름을 가져옵니다.
    //            string googleName = PlayGamesPlatform.Instance.GetUserDisplayName();

    //            // 2. 이름이 정상적으로 가져와졌는지 확인합니다.
    //            if (!string.IsNullOrEmpty(googleName))
    //            {
    //                // 3. 유니티 서버(UGS)의 PlayerName을 구글 이름으로 덮어씌웁니다.
    //                await AuthenticationService.Instance.UpdatePlayerNameAsync(googleName);
    //                Debug.Log($"구글 닉네임 UGS 동기화 완료: {googleName}");
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            // 닉네임 변경에 실패했다고 로그인이 멈추면 안 되므로, 경고만 남기고 무시합니다.
    //            Debug.LogWarning($"구글 닉네임 동기화 실패 (무시하고 로그인을 계속 진행합니다): {ex.Message}");
    //        }
    //#endif
    //    }
//}
