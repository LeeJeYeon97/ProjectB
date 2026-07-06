//using System;
//using System.Collections.Generic;
//using Unity.Services.Authentication;
//using Unity.Services.CloudCode;
//using UnityEngine;
//using Unity.Services.CloudSave;
//using Unity.Services.CloudCode.GeneratedBindings;
//using Newtonsoft.Json;
//using Unity.Services.CloudCode.GeneratedBindings.Project;
//using System.Threading.Tasks;

//public class PlayerDataManager
//{
//    public PlayerDataServiceBindings playerDataServiceBindings;

//    public event Action<PlayerData> PlayerDataUpdated;

//    public PlayerData PlayerDataLocal;

//    public void Init()
//    {
//        Managers.Login.OnLoginSuccess -= InitializePlayer;
//        Managers.Login.OnLoginSuccess += InitializePlayer;

//        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
//        Managers.Initialize.OnUnityServiceInit += SetupBindings;

//    }
//    // 2. 바인딩 세팅용 함수를 따로 만듭니다.
//    private void SetupBindings()
//    {
//        if (playerDataServiceBindings == null)
//        {
//            playerDataServiceBindings = new PlayerDataServiceBindings(CloudCodeService.Instance);
//        }
//    }
//    private async void InitializePlayer()
//    {
//        int attempt = 1;
//        int retryDelay = 1000; // 재시도 간격 (2초)

//        // 현재 떠 있는 로그인 씬을 찾아서 UI를 제어할 준비를 합니다.
//        UI_LoginScene loginScene = UnityEngine.Object.FindAnyObjectByType<UI_LoginScene>();

//        //  팝업 없이 성공할 때까지 무한정 재시도하는 루프입니다.
//        while (true)
//        {
//            try
//            {
//                Debug.Log($"[PlayerDataManager] 서버 데이터 동기화 시도 중... ({attempt}회차)");

//                // 1회차가 아니라 재시도 중이라면, 멈춘 게 아님을 텍스트로 보여줍니다.
//                if (attempt > 1 && loginScene != null)
//                {
//                    // 테이블에 "LoadingText_Reconnecting" (예: "서버 재접속 중...") 키를 만들어서 쓰시거나,
//                    // 당장 급하시다면 아래처럼 임시 텍스트를 직접 던져주셔도 됩니다.
//                    loginScene.UpdateProgress(0.7f, "LoadingText_Reconnecting");
//                }

//                //  원래 서버 통신 로직
//                string myName = AuthenticationService.Instance.PlayerName;
//                var playerDataResponse = await playerDataServiceBindings.HandlePlayerSignIn(myName);

//                // --- 통신 성공 시 데이터 세팅 ---
//                PlayerDataLocal = playerDataResponse.PlayerData;
//                PlayerDataUpdated?.Invoke(PlayerDataLocal);

//                Managers.PlayerEconomy.HandleEconomyUpdate(playerDataResponse.PlayerEconomyData);
//                Managers.PlayerEconomy.CheckAdRemovalStatus();

//                if (playerDataResponse.PlayerUpgradeData != null)
//                {
//                    Managers.Upgrade.InitializeServerData(playerDataResponse.PlayerUpgradeData.UpgradeLevels);
//                }

//                LogResponse(playerDataResponse);
//                Debug.Log("[시스템] 서버 데이터 로딩 완료! 이제 상점 카탈로그와 IAP를 시작합니다.");
//                Managers.PlayerEconomy.SyncEconomyConfig();

//                //  통신에 성공하면 return을 만나 무한 루프를 완전히 탈출합니다!
//                return;
//            }
//            catch (CloudCodeException e)
//            {
//                // 실패 시 로그만 찍고 팝업은 띄우지 않습니다.
//                Debug.LogWarning($"[PlayerDataManager] UGS 데이터 로드 실패 ({attempt}회차): {e.Message}");
//            }
//            catch (System.Exception ex)
//            {
//                Debug.LogException(ex);
//            }

//            // 실패했을 경우 (catch 블록을 거쳐 여기로 옴)
//            // 횟수를 1 올리고 1초를 대기한 뒤 루프의 처음으로 돌아가 다시 시도합니다.
//            attempt++;
//            await Task.Delay(retryDelay);
//        }
//    }

//    //private void ShowNetworkErrorPopup()
//    //{
//    //    // 1. 현재 로그인 씬 UI를 찾아서 유저에게 통신 지연 상태임을 텍스트로 알립니다.
//    //    // (70% 상태로 게이지가 멈춰있더라도 텍스트가 바뀌면 프리징이 아닌 예외 처리 상태로 인식됩니다)
//    //    UI_LoginScene loginScene = UnityEngine.Object.FindAnyObjectByType<UI_LoginScene>();
//    //    if (loginScene != null)
//    //    {
//    //        // 로컬라이제이션 테이블에 "LoadingText_Failure" 또는 "LoadingText_Retry" 등으로 등록해둔 키를 사용하세요.
//    //        // 예: "서버 연결에 실패했습니다. 확인을 눌러 다시 시도하세요."
//    //        loginScene.UpdateProgress(0.7f, "LoadingText_Failure");
//    //    }

//    //    // 2. 대표님이 만드신 시스템 팝업을 UI 매니저를 통해 호출합니다.
//    //    var popup = Managers.UI.ShowPopupUI<UI_SystemPopup>();

//    //    if (popup != null)
//    //    {
//    //        // 번역 테이블에서 알림창에 띄울 문구를 가져옵니다.
//    //        // 예: "서버 응답 시간이 초과되었습니다.\n다시 시도하시겠습니까?"
//    //        string alertMessage = Util.GetLocalizeString("UI", "SystemPopup_NetworkTimeoutRetry");

//    //        // 3. 팝업에 문구와 함께 [확인/종료] 버튼을 눌렀을 때 실행될 콜백(Action)을 넘겨줍니다.
//    //        popup.SetInfo(alertMessage, onCloseCallback: () =>
//    //        {
//    //            // [확인] 버튼을 누르면 실행되는 구역:
//    //            Debug.Log("[ShowNetworkErrorPopup] 유저가 재시도를 선택했습니다. 다시 연결을 시도합니다.");

//    //            if (loginScene != null)
//    //            {
//    //                // 로딩 텍스트를 다시 "데이터 불러오는 중..."으로 원상복구 시킵니다.
//    //                loginScene.UpdateProgress(0.7f, "LoadingText_PlayerDataLoad");
//    //            }

//    //            // 다시 처음부터 로그인 및 데이터 로딩 프로세스를 태웁니다!
//    //            // 이미 UGS가 로그인된 상태라면 1번 시도만에 바로 통과되며 100%로 뚫릴 것입니다.
//    //            InitializePlayer();
//    //        });
//    //    }
//    //    else
//    //    {
//    //        Debug.LogError("[PlayerDataManager] UI_SystemPopup을 로드하는 데 실패했습니다. 방어 코드로 재시도를 바로 실행합니다.");
//    //        // 만약 팝업 자체가 안 뜨는 최악의 상황을 대비한 2차 방어선
//    //        InitializePlayer();
//    //    }
//    //}
//    public void UpdatedPlayerData(PlayerData data)
//    {
//        PlayerDataLocal = data;
//        PlayerDataUpdated?.Invoke(PlayerDataLocal);
//    }
    
//    public async Task SavePlayerData(bool isCleared)
//    {
//        try
//        {
//            // =========================================================
//            // 1. [통합된 부분] 이번 세션에서 획득한 골드 저장
//            // =========================================================
//            int sessionGold = Managers.Game.currentSessionGold;
//            if (sessionGold > 0)
//            {
//                Debug.Log($"서버에 {sessionGold} 골드 저장을 요청합니다...");
//                bool success = await Managers.PlayerEconomy.AddGoldAsync(sessionGold);

//                if (success)
//                    Debug.Log("골드 저장 성공!");
//                else
//                    Debug.LogWarning("골드 저장 실패. 네트워크 상태를 확인하세요.");
//            }

//            // =========================================================
//            // 2. 최고 기록(점수, 생존 시간, 스테이지) 저장
//            // =========================================================
//            int finalScore = Managers.Level.Score;
//            int finalTime = Mathf.FloorToInt(Managers.Game.gamePlayTime);

//            int clearStage = isCleared ? Managers.Stage.currentStageLevel : PlayerDataLocal.MaxClearStage;

//            var updatedData = await playerDataServiceBindings.UpdateGameRecord(finalScore, finalTime, clearStage);

//            Debug.Log($"기록 저장 완료! 현재 최고 점수: {updatedData.MaxScore}");

//            PlayerDataLocal = updatedData;
//            PlayerDataUpdated?.Invoke(PlayerDataLocal);

//            // =========================================================
//            //  3. [추가된 부분] 리더보드에 최고 클리어 스테이지 갱신!
//            // =========================================================
//            // 서버에서 방금 업데이트된 가장 정확한 최고 스테이지(MaxClearStage)를 리더보드로 보냅니다.
//            // 리더보드 설정이 'Keep best(최고 기록 유지)'로 되어 있다면, 
//            // 매판마다 기록을 던져도 알아서 최고 스테이지만 저장해 줍니다!
//            if (Managers.Leaderboard != null && isCleared == true)
//            {
//                Managers.Leaderboard.SubmitScore(updatedData.MaxClearStage);
//            }
//        }
//        catch (System.Exception ex)
//        {
//            Debug.LogWarning($"기록 저장 실패: {ex.Message}");
//        }
//    }
//    // =========================================================
//    // [새로 추가] 특정 업그레이드 항목만 서버에 저장하는 전용 함수
//    // =========================================================
//    public async Task<bool> SaveUpgradeDataAsync(Define.UpgradeType type, int newLevel)
//    {
//        try
//        {
//            // Enum을 서버가 알아들을 수 있게 string으로 변환
//            string upgradeTypeStr = type.ToString();

//            // Cloud Code의 "UpdateUpgradeLevel" 함수 호출!
//            var updatedUpgradeData = await playerDataServiceBindings.UpdateUpgradeLevel(upgradeTypeStr, newLevel);

//            Debug.Log($"[PlayerDataManager] 업그레이드 데이터 서버 저장 완료: {upgradeTypeStr} -> Lv.{newLevel}");
//            return true;
//        }
//        catch (System.Exception ex)
//        {
//            Debug.LogError($"[PlayerDataManager] 업그레이드 데이터 서버 저장 실패: {ex.Message}");
//            return false;
//        }
//    }

//    private void LogResponse(PlayerDataResponse response)
//    {
//        string economyJson = JsonConvert.SerializeObject(response.PlayerEconomyData, Formatting.Indented);
//        string upgradeJson = JsonConvert.SerializeObject(response.PlayerUpgradeData, Formatting.Indented); //  추가

//        Debug.Log(
//            $"====== Player Sign-In Response =====\n" +
//            $"Name : {response.PlayerData.DisplayName}\n" +
//            $"New Player : {response.IsNewPlayer} \n" +
//            $"XP : {response.PlayerData.Experience} \n" +
//            $"Economy : {economyJson}\n" +
//            $"Upgrade : {upgradeJson}\n" + //  추가
//            $"==============================="
//            );
//    }
//}
