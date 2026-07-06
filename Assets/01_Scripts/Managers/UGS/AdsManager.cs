//using Newtonsoft.Json;
//using System;
//using System.Data;
//using System.Threading.Tasks;
//using Unity.Services.Authentication;
//using Unity.Services.CloudCode;
//using Unity.Services.CloudCode.GeneratedBindings;
//using Unity.Services.LevelPlay;
//using UnityEngine;

//[Serializable]
//public class AdsManager
//{
//    // 아이언소스 대시보드에서 발급받은 App Key를 여기에 넣습니다.
//    [Header("App key")]
//    [SerializeField] private const string androidAppKey = "257e5279d";
//    [SerializeField] private const string iosAppKey = "";

//    [Header("Banner Ad Unit Id")]
//    [SerializeField] private string androidBannerAdUnitId = "hq871f6jphj2tmtu";
//    [SerializeField] private const string iosBannerAdUnitId = "";

//    [Header("Interstitial Ad Unit Id")]
//    [SerializeField] private string androidInterstitialAdUnitId = "r7hx8d6lrtdp0m1h";
//    [SerializeField] private const string iosInterstitialAdUnitId = "";

//    [Header("Rewarded Ad Unit Id")]
//    [SerializeField] private string androidRewardedAdUnitId = "zqhm9b6kdyoaoosw";
//    [SerializeField] private const string iosRewardedAdUnitId = "";

//    private string appKey
//    {
//        get
//        {
//#if UNITY_ANDROID
//            return androidAppKey;
//#elif UNITY_IOS
//            return iosAppKey;
//#else
//            return string.Empty;
//#endif
//        }
//    }
//    private string bannerAdUnitId
//    {
//        get
//        {
//#if UNITY_ANDROID
//            return androidBannerAdUnitId;
//#elif UNITY_IOS
//            return iosBannerAdUnitId;
//#else
//            return string.Empty;
//#endif
//        }
//    }

//    private string interstitialAdUnitId
//    {
//        get
//        {
//#if UNITY_ANDROID
//            return androidInterstitialAdUnitId;
//#elif UNITY_IOS
//            return iosInterstitialAdUnitId;
//#else
//            return string.Empty;
//#endif
//        }
//    }

//    private string rewardedAdUnitId
//    {
//        get
//        {
//#if UNITY_ANDROID
//            return androidRewardedAdUnitId;
//#elif UNITY_IOS
//            return iosRewardedAdUnitId;
//#else
//            return string.Empty;
//#endif
//        }
//    }


//    private bool _IsInit;

//    // 마지막으로 전면/보상형 광고를 본 시간 (게임 시작 시점 기준)
//    private DateTime _lastRewardAdCompletionTime;
//    private float _lastInterstitialAdTime = -999f;

//    // 전면 광고 쿨타임 (예: 180초 = 3분)
//    private float _rewardAdCooldownSeconds = 5f;
//    private float _interstitialAdCooldownSeconds = 600f; // 10분

//    // 레벨플레이 광고 객체
//    private LevelPlayBannerAd bannerAd;
//    private LevelPlayInterstitialAd interstitialAd;
//    private LevelPlayRewardedAd rewardedAd;

//    private AdServiceBindings _adsServiceBindings;

//    private string _lastAdToken;

//    public event Action<bool> AdSuccessfullyCompleted;
//    public event Action<bool> AdAvailable;

//    // 전면 광고가 끝났을 때 실행할 함수를 담아둘 변수 추가
//    private Action _onInterstitialAdClosedCallback;
//    // 1. 현재 실행해야 할 콜백 함수를 저장할 변수 추가
//    private Action<bool> _onCurrentAdCompletedCallback;

//    //  1. 광고를 보기 직전의 TimeScale을 기억해둘 변수
//    private float _previousTimeScale = 1f;

//    // 광고 제거 여부를 게임 전체에서 쉽게 확인할 수 있는 프로퍼티
//    public bool IsAdsRemoved { get; set; } = false;

//    public void Init()
//    {
//        LevelPlay.OnInitSuccess += SdkInitializationCompleted;
//        LevelPlay.OnInitFailed += SdkInitializationFailed;

//        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
//        Managers.Initialize.OnUnityServiceInit += SetupBindings;
//    }
//    public void SetupBindings()
//    {
//        _adsServiceBindings = new AdServiceBindings(CloudCodeService.Instance);

//        AuthenticationService.Instance.SignedIn += InitializeLevelPlayAds;
//        if (AuthenticationService.Instance.IsSignedIn && _IsInit == false)
//        {
//            InitializeLevelPlayAds();
//        }

//    }
//    // 게임에 로그인하면 레벨플레이 초기화 실행
//    private void InitializeLevelPlayAds()
//    {
//        string userId = AuthenticationService.Instance.PlayerId;

//        //[핵심 변경] 유니티 에디터이거나, 빌드 세팅에서 'Development Build'를 체크했을 때만 컴파일됩니다.
//#if UNITY_EDITOR || DEVELOPMENT_BUILD
//        LevelPlay.SetMetaData("is_test_suite", "enable");
//        Debug.Log("[Ads] Test Suite Enabled (Development Mode)");
//#endif
//        LevelPlay.Init(appKey, userId);
//        LevelPlay.SetPauseGame(true);
//    }
//    // 레벨플레이 초기화 완료시 실행
//    private void SdkInitializationCompleted(LevelPlayConfiguration configuration)
//    {
//        if (_IsInit == true) return;

//        _IsInit = true;
//        Debug.Log("LevelPlay SDK Init Success");

//        // 에디터에서 플레이하거나, 개발용 빌드를 뽑았을 때만 실행됩니다.
//#if UNITY_EDITOR || DEVELOPMENT_BUILD
//        // 1. 연동 검증: "AdMob, Unity Ads 등 내가 붙인 광고들이 잘 연결되었나?" 콘솔에 로그를 쫙 뿌려줍니다.
//        LevelPlay.ValidateIntegration();

//        // 2. 테스트 메뉴 띄우기: 화면에 아이언소스 테스트 UI를 강제로 띄웁니다.
//        LevelPlay.LaunchTestSuite();

//        Debug.Log("[Ads] Launching test suite");
//#endif

//        CreateBannerAd();
//        CreateInterstitialAd();
//        CreateRewardedAd();
//    }
//    private void SdkInitializationFailed(LevelPlayInitError error)
//    {
//        Debug.LogError("LevelPlay Init Failed" + error);
//    }

//    #region 배너광고
//    private void CreateBannerAd()
//    {
//        // 1. Config 객체를 조립하기 위한 Builder 생성
//        var configBuilder = new LevelPlayBannerAd.Config.Builder();

//        // 2. 사이즈 세팅: 기기 화면 비율에 맞추는 스마트 배너(Adaptive)
//        configBuilder.SetSize(LevelPlayAdSize.CreateAdaptiveAdSize());

//        // 3. 위치 세팅: 화면 하단 중앙
//        configBuilder.SetPosition(LevelPlayBannerPosition.BottomCenter);

//        // 4. (선택) 로드 완료 시 즉시 띄울 것인지 설정 (기본값 true)
//        //configBuilder.SetDisplayOnLoad(true);

//        // 5. 조립 완료! Build()를 눌러서 최종 config 객체를 뽑아냅니다.
//        var bannerConfig = configBuilder.Build();

//        // 6. 말씀하신 대로 id와 config 딱 2개만 넣어서 배너 생성!
//        bannerAd = new LevelPlayBannerAd(bannerAdUnitId, bannerConfig);

//        bannerAd.OnAdLoaded += BannerOnAdLoadedEvent;
//        bannerAd.OnAdLoadFailed += BannerOnAdLoadFailedEvent;
//        bannerAd.OnAdDisplayed += BannerOnAdDisplayedEvent;
//        bannerAd.OnAdDisplayFailed += BannerOnAdDisplayFailedEvent;
//        bannerAd.OnAdClicked += BannerOnAdClickedEvent;
//        bannerAd.OnAdCollapsed += BannerOnAdCollapsedEvent;
//        bannerAd.OnAdLeftApplication += BannerOnAdLeftApplicationEvent;
//        bannerAd.OnAdExpanded += BannerOnAdExpandedEvent;

//    }
//    public void ShowBanner()
//    {
//        // [설명] 실제로 배너를 메모리에 올리고 화면에 보여주는 역할을 합니다.
//        // [주의] CreateBannerAd에서 configBuilder.SetDisplayOnLoad(true)가 
//        // 기본값이기 때문에 LoadAd()만 호출해도 화면에 나타납니다.
//        bannerAd.LoadAd();
//    }
//    public void HideBanner()
//    {
//        bannerAd.HideAd();
//    }
//    public void DestroyBanner()
//    {
//        bannerAd.DestroyAd();
//    }
//    // Implement the events
//    void BannerOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
//    void BannerOnAdLoadFailedEvent(LevelPlayAdError ironSourceError) { }
//    void BannerOnAdClickedEvent(LevelPlayAdInfo adInfo) 
//    {
//        Debug.Log("배너 광고 클릭함");
//    }
//    void BannerOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
//    void BannerOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) { }
//    void BannerOnAdCollapsedEvent(LevelPlayAdInfo adInfo) { }
//    void BannerOnAdLeftApplicationEvent(LevelPlayAdInfo adInfo) { }
//    void BannerOnAdExpandedEvent(LevelPlayAdInfo adInfo) { }

//    #endregion

//    #region 전면광고
//    private void CreateInterstitialAd()
//    {

//        interstitialAd = new LevelPlayInterstitialAd(interstitialAdUnitId);

//        interstitialAd.OnAdLoaded += InterstitialOnAdLoadedEvent;
//        interstitialAd.OnAdLoadFailed += InterstitialOnAdLoadFailedEvent;
//        interstitialAd.OnAdDisplayed += InterstitialOnAdDisplayedEvent;
//        interstitialAd.OnAdDisplayFailed += InterstitialOnAdDisplayFailedEvent;
//        interstitialAd.OnAdClicked += InterstitialOnAdClickedEvent;
//        interstitialAd.OnAdClosed += InterstitialOnAdClosedEvent;
//        interstitialAd.OnAdInfoChanged += InterstitialOnAdInfoChangedEvent;

//        LoadInterstitialAd();
//    }
//    // 배너광고와 다르게 미리 불러오고 버튼을 누르면 띄운다.
//    public void LoadInterstitialAd()
//    {
//        interstitialAd.LoadAd();
//        Debug.Log("interstitalAd Loaded");
//    }
//    // 2. Action 콜백을 받을 수 있도록 매개변수 추가
//    public void ShowInterstitialAd(Action onCompleted = null)
//    {
//        // 1. 결제 유저면 광고 없이 즉시 다음 할 일(씬 로드 등) 실행!
//        if (IsAdsRemoved)
//        {
//            Debug.Log("광고 제거 결제 유저이므로 전면 광고를 스킵합니다.");
//            onCompleted?.Invoke();
//            return;
//        }

//        // 2. 쿨타임 체크
//        if (Time.time - _lastInterstitialAdTime >= _interstitialAdCooldownSeconds)
//        {
//            if (interstitialAd.IsAdReady())
//            {
//                // 광고가 준비되었다면, 끝났을 때 실행할 함수를 저장하고 띄움
//                _onInterstitialAdClosedCallback = onCompleted;
//                interstitialAd.ShowAd();
//                _lastInterstitialAdTime = Time.time;
//            }
//            else
//            {
//                // 광고 준비가 안 됐으면, 막히지 않게 즉시 다음 할 일 실행
//                Debug.LogWarning("전면 광고가 아직 로드되지 않아 스킵합니다.");
//                onCompleted?.Invoke();
//            }
//        }
//        else
//        {
//            Debug.Log($"아직 쿨타임입니다. 광고 없이 넘어갑니다.");
//            onCompleted?.Invoke();
//        }
//    }
//    void InterstitialOnAdLoadedEvent(LevelPlayAdInfo adInfo) { }
//    void InterstitialOnAdLoadFailedEvent(LevelPlayAdError error) 
//    { 
//        LoadInterstitialAd();
//    }
//    void InterstitialOnAdDisplayedEvent(LevelPlayAdInfo adInfo) { }
//    void InterstitialOnAdDisplayFailedEvent(LevelPlayAdInfo adInfo, LevelPlayAdError error) 
//    {
//        Debug.LogError($"전면 광고 표시 실패: {error}");
//        // 3. 표시를 실패했을 때도 게임이 멈추면 안 되므로 콜백 실행
//        _onInterstitialAdClosedCallback?.Invoke();
//        _onInterstitialAdClosedCallback = null;
//    }
//    void InterstitialOnAdClickedEvent(LevelPlayAdInfo adInfo) { }
//    void InterstitialOnAdClosedEvent(LevelPlayAdInfo adInfo)
//    {
//        Debug.Log("전면 광고를 닫았습니다.");

//        // 다음을 위해 미리 로드
//        LoadInterstitialAd();

//        // 아까 저장해둔 콜백(씬 로드)을 여기서 실행!
//        _onInterstitialAdClosedCallback?.Invoke();
//        _onInterstitialAdClosedCallback = null;
//    }
//    void InterstitialOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) { }
//    #endregion

//    #region 보상형 광고
    
//    private void CreateRewardedAd()
//    {
//        // 보상형 객체 생성
//        rewardedAd = new LevelPlayRewardedAd(rewardedAdUnitId);

//        // 이벤트 구독
//        // Load이벤트
//        rewardedAd.OnAdLoaded += RewardedOnAdLoaded;
//        rewardedAd.OnAdLoadFailed += RewardedOnAdLoadFailed;

//        // DisPlay이벤트
//        rewardedAd.OnAdDisplayed += RewardedOnAdDisplayed;
//        rewardedAd.OnAdDisplayFailed += RewardedOnAdDisplayFailed;

//        // 리워드 이벤트
//        rewardedAd.OnAdRewarded += RewardedOnAdRewarded;

//        // 완료 이벤트
//        rewardedAd.OnAdClosed += RewardedOnAdClosed;

//        // Optional 
//        rewardedAd.OnAdClicked += RewardedOnAdClickedEvent;
//        rewardedAd.OnAdInfoChanged += RewardedOnAdInfoChangedEvent;

//        LoadRewardedAd();
//    }
//    public void LoadRewardedAd()
//    {
//        if(rewardedAd != null)
//        {
//            rewardedAd.LoadAd();
//        }
//    }
//    public void ShowRewardedAd(string placementName, Action<bool> onCompleted = null)
//    {
        
//        if (_IsInit == false || rewardedAd == null)
//        {
//            Debug.LogWarning("SDK not initialized or Ad object null");
//            return;
//        }

//        // 1. 광고 버튼 클릭 즉시 로딩 팝업 ON!
//        // 광고 화면이 로드되어 실제로 화면을 덮기 전까지 유저의 중복 클릭을 막습니다.
//        Managers.UI.ShowPopupUI<UI_LoadingPopup>();

//        bool isAdReady = rewardedAd.IsAdReady();
//        bool isCooldownExpired = HasCooldownExpired();
//        bool isPlacementCapped = LevelPlayRewardedAd.IsPlacementCapped(placementName);

//        // 광고를 보여줄 수 없는 상황이라면 즉시 로딩 OFF
//        if (!isAdReady || isPlacementCapped || !isCooldownExpired)
//        {
//            Debug.LogWarning($"Ad Not Ready: Ready({isAdReady}), Capped({isPlacementCapped}), Cooldown(!{isCooldownExpired})");
//            Managers.UI.ClosePopupUI();
//            return;
//        }

//        _onCurrentAdCompletedCallback = onCompleted;

//        // 2. 광고 호출 직전에 현재 TimeScale을 저장하고, 1로 강제 할당!
//        _previousTimeScale = Time.timeScale;
//        Time.timeScale = 1f;

//        if (string.IsNullOrEmpty(placementName))
//        {
//            Debug.LogWarning("Placement name is empty, showing ad unit without placement");
//            rewardedAd.ShowAd();
//        }
//        else
//        {
//            rewardedAd.ShowAd(placementName);
//        }
//    }
//    private bool HasCooldownExpired()
//    {
//        // 1. 아직 한 번도 광고를 안 본 뉴비라면? -> 무조건 통과!
//        if (_lastRewardAdCompletionTime == default)
//        {
//            return true;
//        }

//        // 2. 현재 시간(UtcNow)에서 마지막으로 광고를 본 시간을 뺍니다.
//        // 예: 현재가 12시 05분이고, 마지막 시청이 12시 00분이면 -> '5분(300초)'이라는 경과 시간이 나옵니다.
//        TimeSpan timeSinceLastAd = DateTime.UtcNow - _lastRewardAdCompletionTime;

//        // 3. 목표 쿨타임(180초)에서 지나간 시간(300초)을 뺍니다.
//        // 180 - 300 = -120초 (쿨타임이 이미 120초나 지났다는 뜻입니다)
//        float remaining = _rewardAdCooldownSeconds - (float)timeSinceLastAd.TotalSeconds;

//        // 4. 남은 시간이 마이너스(-)로 떨어지는 것을 막기 위해 수학 함수를 씁니다.
//        // 0과 -120 중 더 큰 값(Max)을 고르므로, remaining은 깔끔하게 '0'이 됩니다.
//        remaining = Math.Max(0f, remaining);

//        // 5. 판정: 남은 시간이 0초보다 크면? (아직 쿨타임 중!)
//        if (remaining > 0f)
//        {
//            Debug.Log($"Ad still on cooldown for {remaining:F1} seconds");
//            return false; // 쿨타임 안 지났음!
//        }

//        // 6. 남은 시간이 0초 이하면? (쿨타임 끝!)
//        return true;
//    }
//    void RewardedOnAdLoaded(LevelPlayAdInfo adInfo) 
//    {
//        AdAvailable?.Invoke(true);
//        Debug.Log($"Rewarded ad loaded : {adInfo.AdNetwork}");
//    }
//    private async void RewardedOnAdLoadFailed(LevelPlayAdError error) 
//    {
//        Debug.LogError($"Rewarded ad failed to load : {error.ErrorMessage} (Code : {error.ErrorCode}");

//        // Different retry strategies based on actual LevelPlay error codes
//        // Note: Some error codes may not apply to format - https://developers.is.com/ironsource-mobile/air/supersonic-sdk-error-codes/
//        switch (error.ErrorCode)
//        {
//            case 509: // Tried waterfall, all networks say "no inventory"
//                Debug.LogWarning("No ads to show, will retry");
//                await Task.Delay(5000); // 5000밀리초(5초) 대기
//                LoadRewardedAd();       // 5초 뒤에 실행됨!
//                break;

//            case 520:
//                Debug.LogWarning("No internet connection");
//                AdAvailable?.Invoke(false);
//                // Could implement connectivity-based retry logic here
//                break;

//            case 524:
//                Debug.LogWarning("Placement is capped, will not retry loading");
//                AdAvailable?.Invoke(false);
//                // Don't retry - placement is capped
//                break;

//            case 526:
//                Debug.LogWarning("Ad unit has reached daily cap, will not retry");
//                AdAvailable?.Invoke(false);
//                // Don't retry - daily cap reached
//                break;

//            // 1022: Cannot show an Rewarded Video (RV) while another RV is showing
//            // 1023: Show RV called when there are no available ads to show, check IsAdReady before calling ShowAd

//            default:
//                Debug.LogWarning($"Unknown error code {error.ErrorCode}, retrying with standard delay");
//                await Task.Delay(2000); // 5000밀리초(5초) 대기
//                LoadRewardedAd();       // 5초 뒤에 실행됨!
//                break;
//        }
//    }

//    // 광고 표시 실패 시
//    void RewardedOnAdDisplayed(LevelPlayAdInfo adInfo) 
//    { 
//        Debug.Log("Rewarded ad displayed"); 
//    }
//    void RewardedOnAdDisplayFailed(LevelPlayAdInfo adInfo, LevelPlayAdError error) 
//    {
//        Debug.LogError($"Rewarded ad failed to display : {error}");

//        //  2. 광고가 뜨지 않았으므로 로딩 OFF
//        Managers.UI.ClosePopupUI();
//        AdSuccessfullyCompleted?.Invoke(false);
//        _onCurrentAdCompletedCallback = null;
//        Time.timeScale = _previousTimeScale;
//    }

//    // 보상 지급
//    private async void RewardedOnAdRewarded(LevelPlayAdInfo adInfo, LevelPlayReward reward) 
//    {
//        try
//        {
//            // 광고 화면이 닫히면서 유니티로 돌아올 때, 로딩이 이미 떠 있거나 다시 떠야 합니다.
//            // 보통 광고가 닫히기 전 이 이벤트가 먼저 들어오므로 로딩은 켜져 있는 상태입니다.

            
//            Debug.Log($"Validating ad reward : {reward.Name} amount : {reward.Amount}");

//            // 핵심 분기점: 인게임 리롤 같은 '휘발성 보상'은 서버 검증을 스킵합니다!
//            if (adInfo.PlacementName == Define.placement_InGameCardReload ||
//                adInfo.PlacementName == Define.placement_GameOver)
                
//            {
//                Debug.Log("인게임 광고보상 : 서버 검증 없이 즉시 클라이언트 보상을 지급합니다.");
//                _onCurrentAdCompletedCallback?.Invoke(true);
//                return;
//            }

//            string adToken = GenerateAdToken(adInfo, reward);
//            DateTime completionTime = DateTime.UtcNow;

//            _lastAdToken = adToken;
//            _lastRewardAdCompletionTime = completionTime;

//            var playerEconomyData = await _adsServiceBindings.HandleGrantVideoAdReward(adToken);

//            Managers.PlayerEconomy.HandleEconomyUpdate(playerEconomyData);
            
//            AdSuccessfullyCompleted?.Invoke(true);

//            Debug.Log($"Ad reward granted successfully : {reward.Name} x{reward.Amount}");
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Failed to validate ad reward : {e.Message}");
//            AdSuccessfullyCompleted?.Invoke(false);
//        }
//    }
//    private string GenerateAdToken(LevelPlayAdInfo adInfo, LevelPlayReward reward)
//    {
//        // Validate required data is present
//        if (adInfo == null)
//        {
//            throw new ArgumentException("Ad info cannot be null for token generation");
//        }

//        if (reward == null)
//        {
//            throw new ArgumentException("Reward info cannot be null for token generation");
//        }

//        if (string.IsNullOrEmpty(adInfo.InstanceId))
//        {
//            throw new ArgumentException("Ad instance ID cannot be null or empty for token generation");
//        }

//        // 1. 아이언소스 대시보드에서 설정한 진짜 보상 이름을 가져옵니다.
//        string finalRewardName = reward.Name;

//        // 2. 유니티 에디터에서 테스트할 때는 "editor_reward"로 오기 때문에, 테스트용 기본 재화로 덮어씌웁니다.
//#if UNITY_EDITOR
//        if (finalRewardName == "editor_reward")
//        {
//            finalRewardName = "GOLD"; // 에디터 테스트 시 기본 지급할 재화 ID
//        }
//#endif

//        // Create token with ad info, reward data, and timestamp
//        var tokenData = new
//        {
//            // Store as ticks for consistent serialization
//            Timestamp = DateTime.UtcNow.Ticks,
//            InstanceId = adInfo.InstanceId,
//            InstanceName = adInfo.InstanceName,
//            AdNetwork = adInfo.AdNetwork,
//            PlacementName = adInfo.PlacementName,
//            RewardName = finalRewardName, // Can use RewardName = reward.Name, but in editor reward is "editor_reward" and will fail validation
//            RewardAmount = reward.Amount
//        };

//        string json = JsonConvert.SerializeObject(tokenData);
//        Debug.Log($"Generated ad token: {json}");
//        return json;
//    }

//    void RewardedOnAdClosed(LevelPlayAdInfo adInfo) 
//    {
//        Debug.Log("Rewarded ad closed");

//        //5. 혹시나 보상 단계(Rewarded)를 거치지 않고 그냥 닫혔을 경우를 대비한 안전장치
//        Managers.UI.ClosePopupUI();
//        _onCurrentAdCompletedCallback = null;
//        Time.timeScale = _previousTimeScale;
//        LoadRewardedAd();
//    }
//    void RewardedOnAdClickedEvent(LevelPlayAdInfo adInfo) { Debug.Log("Rewarded ad clicked"); }
//    void RewardedOnAdInfoChangedEvent(LevelPlayAdInfo adInfo) 
//    {
//        Debug.Log($"Rewarded ad info changed : {adInfo.AdNetwork}");
//        if(adInfo != null)
//        {
//            Debug.Log($"Updated ad info - Network : {adInfo.AdNetwork}, Instance : {adInfo.InstanceId}");
//            // Could trigger UI Update here
//        }
//    }
//    #endregion
//}

