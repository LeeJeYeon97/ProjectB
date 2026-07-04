//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Unity.Services.Authentication;
//using Unity.Services.CloudCode;
//using Unity.Services.CloudCode.GeneratedBindings;
//using Unity.Services.CloudCode.GeneratedBindings.Project;
//using Unity.Services.Economy;
//using Unity.Services.Economy.Model;
//using UnityEngine;

//public class PlayerEconomyManager
//{
//    //// [1단계 해커 방어] 메모리 변조를 막기 위한 암호화 변수
//    //private int _adKey1 = 1234567;
//    //private int _adKey2 = 7654321;
//    //private int _adStatus = 0;

//    //// 게임 전체에서 접근할 프로퍼티 (암호화 & 복호화 자동 처리)
//    //public bool IsAdsRemoved
//    //{
//    //    get { return _adStatus == (_adKey1 ^ _adKey2); }
//    //    private set
//    //    {
//    //        if (value) _adStatus = _adKey1 ^ _adKey2; // true면 암호 결합
//    //        else _adStatus = 0;                       // false면 0으로 초기화
//    //    }
//    //}

//    // [1] 서버 통신용 바인딩 객체
//    // Cloud Code(서버)에 있는 함수를 원격으로 호출할 수 있게 해주는 연결선
//    public PlayerEconomyServiceBindings playerEconomyServiceBindings { get; private set; }

//    // [2] 로컬 캐시 (내 지갑)
//    // 서버에서 받아온 내 재화(골드 등)와 인벤토리 데이터를 저장해 두는 '복사본'입니다.
//    // get은 누구나 할 수 있지만, set은 이 클래스 내부에서만 가능하도록(private set) 막아두었습니다. (보안)
//    // new Dictionary... 로 초기화해 둔 이유는 처음에 null 에러가 터지는 것을 막기 위한 센스입니다!
//    public PlayerEconomyData EconomyDataLocal { get; private set; } = new PlayerEconomyData()
//    {
//        Currencies = new Dictionary<string, int>(),
//        ItemInventory = new Dictionary<string, int>()
//    };

//    // [3] 편의성 프로퍼티 (Getter)
//    // 게임 내에서 가장 자주 쓰이는 '골드'를 매번 GetCurrencyAmount("GOLD")로 부르기 귀찮으므로,
//    // Managers.PlayerEconomy.Gold 로 짧고 쉽게 부르기 위해 만든 지름길입니다.

//    public int Gold
//    {
//        get => GetCurrencyAmount(Define.k_GoldCurrencyKey);
//    }

//    // [4] 이벤트 방송국 (Action)
//    // PlayerEconomyUpdated: 내 재화/인벤토리가 바뀌었을 때 UI들에게 "화면 새로고침 해!" 라고 알리는 방송
//    // EconomyConfigSynced: UGS 서버의 상점/재화 카탈로그 다운로드가 끝났음을 알리는 방송
//    public event Action<PlayerEconomyData> PlayerEconomyUpdated;
//    public event Action EconomyConfigSynced;

//    public void Init()
//    {
//        // 유니티 서비스 초기화가 끝났다는 방송을 들으면 SetupBindings를 실행하도록 구독합니다.
//        // 이중 구독을 막기 위해 빼기(-=)를 먼저 해준 것은 아주 훌륭한 습관입니다!
//        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
//        Managers.Initialize.OnUnityServiceInit += SetupBindings;
//    }
//    public void Clear()
//    {
//        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
//    }
//    private void SetupBindings()
//    {
//        // 바인딩 객체가 없다면 새로 생성하여 서버와 연결할 준비를 마칩니다.
//        if (playerEconomyServiceBindings == null)
//        {
//            playerEconomyServiceBindings = new PlayerEconomyServiceBindings(CloudCodeService.Instance);
//        }
//        // 로그인 성공 시점에 상점/재화 카탈로그(설정)를 동기화하도록 구독합니다
//        AuthenticationService.Instance.SignedIn -= SyncEconomyConfig;
//        AuthenticationService.Instance.SignedIn += SyncEconomyConfig;
//    }
//    // [6] 카탈로그 동기화 함수
//    // 내가 얼마를 가지고 있는지(Balance)가 아니라, 이 게임에 어떤 화폐가 존재하고 
//    // 어떤 아이템을 파는지(Config)를 UGS 서버에서 다운로드 받는 아주 중요한 과정입니다.
//    //  [수정] private에서 public으로 변경하여, 외부(PlayerData)에서 지시할 수 있게 합니다.
//    public async void SyncEconomyConfig()
//    {
//        try
//        {
//            await EconomyService.Instance.Configuration.SyncConfigurationAsync();
//            Debug.Log("Economy configuration synced (상점 카탈로그 다운로드 완료)");

//            // 카탈로그 다운로드가 끝나면 IAP가 이걸 듣고 InitializeIAPSync를 실행합니다!
//            EconomyConfigSynced?.Invoke();
//        }
//        catch (Exception ex)
//        {
//            Debug.LogException(ex);
//        }
//    }
//    // [7] 지갑 갱신 함수 (유일한 데이터 수정 창구)
//    // 서버 통신(로그인, 결제 등)이 끝난 후, 서버가 "너 최신 데이터 이거야" 하고 던져주면
//    // 이 함수가 받아서 지갑(EconomyDataLocal)을 덮어씌우고 방송을 터뜨립니다.
//    public void HandleEconomyUpdate(PlayerEconomyData economyData)
//    {
//        EconomyDataLocal = economyData;
//        PlayerEconomyUpdated?.Invoke(EconomyDataLocal);
//    }

//    // 초기화 또는 인벤토리 동기화 완료 시 호출되는 함수
//    public void CheckAdRemovalStatus()
//    {
//        //  핵심 방어막: 이미 구글 IAP 영수증 복구를 통해 광고 제거가 true가 되었다면,
//        // UGS 인벤토리에 없다고 해서 굳이 false로 덮어씌우면 안 됩니다!
//        if (Managers.AD.IsAdsRemoved == true)
//        {
//            Debug.Log("[UGS] 이미 기기 영수증(IAP)으로 광고가 제거된 상태입니다. 인벤토리 체크를 스킵합니다.");
//            return; // 그냥 돌아갑니다!
//        }

//        // Dictionary의 특성을 활용해 foreach 루프 없이 한 번에 검색합니다.
//        if (EconomyDataLocal.ItemInventory.ContainsKey(Define.k_RemoveAdItem))
//        {
//            Managers.AD.IsAdsRemoved = true;
//            Debug.Log(" 광고 제거 유저입니다! 전면/배너 광고를 비활성화합니다.");

//            // 만약 배너 광고가 켜져있다면 여기서 꺼줍니다.
//            // Managers.AD.HideBanner();
//            return;
//        }
//        else
//        {
//            Managers.AD.IsAdsRemoved = false;
//            Debug.Log("일반 유저입니다. (광고 제거 아이템 없음)");
//            return;
//        }
//    }

//    // [8] 재화 조회 유틸리티 함수
//    // 내 지갑에서 특정 재화(예: 다이아)가 얼마나 있는지 물어볼 때 사용합니다.
//    public int GetCurrencyAmount(string currencyKey)
//    {
//        // TryGetValue를 써서 만약 지갑에 해당 재화 기록이 아예 없어도 
//        // 뻗지 않고 안전하게 0을 반환하도록 처리한 훌륭한 방어 코드입니다.
//        if (EconomyDataLocal.Currencies.TryGetValue(currencyKey,out int amount))
//        {
//            return amount;
//        }
//        return 0;
//    }

//    // [추가] 재화 소모 함수 (비동기)
//    // currencyKey: 소모할 재화 ID (예: "GOLD")
//    // amount: 소모할 양
//    public async Task<bool> SpendCurrencyAsync(string currencyKey, int amount)
//    {
//        // 1. [로컬 체크] 서버에 찌르기 전에 내 가방에 돈이 있는지 먼저 확인 (서버 비용 절약)
//        int currentBalance = GetCurrencyAmount(currencyKey);
//        if (currentBalance < amount)
//        {
//            Debug.LogWarning($"[{currencyKey}] 잔액 부족: 현재 {currentBalance}, 필요 {amount}");
//            return false;
//        }

//        try
//        {
//            // 2. [서버 통신] Cloud Code를 통해 서버 DB의 재화를 차감합니다.
//            // 성공하면 서버는 차감 후의 최신 PlayerEconomyData를 반환합니다.
//            if (playerEconomyServiceBindings == null)
//            {
//                Debug.LogError("Service Bindings가 초기화되지 않았습니다.");
//                return false;
//            }

//            // Bindings에 정의된 서버 함수 호출 (HandleSpendCurrency는 서버 함수 이름에 맞춰 변경 가능)
//            var updatedData = await playerEconomyServiceBindings.HandleSpendCurrency(currencyKey, amount);

//            if (updatedData != null)
//            {
//                // 3. [로컬 업데이트] 서버에서 받아온 최신 지갑 데이터로 내 로컬 데이터를 갱신합니다.
//                HandleEconomyUpdate(updatedData);
//                Debug.Log($"[{currencyKey}] 소모 성공! 남은 잔액: {GetCurrencyAmount(currencyKey)}");
//                return true;
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"재화 소모 중 서버 통신 에러: {ex.Message}");
//        }

//        return false;
//    }

//    // [추가] 골드 전용 소모 함수 (지름길)
//    public async Task<bool> SpendGoldAsync(int amount)
//    {
//        return await SpendCurrencyAsync(Define.k_GoldCurrencyKey, amount);
//    }

//    public async Task<bool> AddCurrencyAsync(string currencyKey, int amount)
//    {
//        if (amount <= 0)
//        {
//            Debug.LogWarning("획득할 재화량은 0보다 커야 합니다.");
//            return false;
//        }

//        try
//        {
//            if (playerEconomyServiceBindings == null)
//            {
//                Debug.LogError("Service Bindings가 초기화되지 않았습니다.");
//                return false;
//            }

//            // 1. [서버 통신] Cloud Code의 HandleAddCurrency 함수를 호출하여 증가시킵니다.
//            var updatedData = await playerEconomyServiceBindings.HandleAddCurrency(currencyKey, amount);

//            if (updatedData != null)
//            {
//                // 2. [로컬 업데이트] 서버에서 받아온 최신 지갑 데이터로 로컬을 갱신합니다.
//                HandleEconomyUpdate(updatedData);
//                Debug.Log($"[{currencyKey}] {amount} 획득 성공! 현재 잔액: {GetCurrencyAmount(currencyKey)}");
//                return true;
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"재화 획득 중 서버 통신 에러: {ex.Message}");
//        }

//        return false;
//    }

//    // [추가] 골드 전용 획득 함수 (지름길)
//    public async Task<bool> AddGoldAsync(int amount)
//    {
//        return await AddCurrencyAsync(Define.k_GoldCurrencyKey, amount);
//    }
//}
