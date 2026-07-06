//using GooglePlayGames;
//using GooglePlayGames.BasicApi;
//using System.Threading.Tasks;
//using Unity.Services.Authentication;
//using Unity.Services.Core;
//using Unity.Services.Leaderboards;
//using Unity.Services.Leaderboards.Models;
//using UnityEngine;
//using UnityEngine.SocialPlatforms.Impl;

//public class LeaderboardManager
//{
//    private const string LEADERBOARD_ID = "HIGH_CLEAR_STAGE";

//    //  핵심: 서비스가 널인지, 준비되었는지 확인하는 함수
//    private async Task<bool> EnsureLeaderboardReady()
//    {
//        // 서비스 상태 디버깅
//        Debug.Log($"[UGS DEBUG] 서비스 상태: {UnityServices.State}");

//        // 리더보드 서비스가 아예 null이라면 로그를 남깁니다.
//        var instance = LeaderboardsService.Instance;
//        if (instance == null)
//        {
//            Debug.LogError("[UGS DEBUG] LeaderboardsService.Instance 가 NULL입니다!");
//            // 여기서 더 정확한 정보를 얻기 위해 모든 서비스를 찍어봅니다.
//            Debug.LogError("[UGS DEBUG] UnityServices.Services.Count: " + UnityServices.Services.Count);
//        }

//        return instance != null;
//    }

//    // ==========================================
//    // 1. 점수 등록하기 (게임 오버 시 호출)
//    // ==========================================
//    public async void SubmitScore(int clearStage)
//    {
//        //  1. 사용 전 준비 완료 체크!
//        if (!await EnsureLeaderboardReady()) return;

//        //  [추가된 안전 장치 1] UGS 시스템 자체가 아직 안 켜졌다면 조용히 빠져나감
//        if (UnityServices.State != ServicesInitializationState.Initialized)
//        {
//            Debug.LogWarning("UGS 시스템이 아직 초기화되지 않아 리더보드 등록을 스킵합니다.");
//            return;
//        }

//        //  [안전 장치 2] 로그인이 안 되어 있다면 스킵
//        if (!AuthenticationService.Instance.IsSignedIn)
//        {
//            Debug.LogWarning("로그인이 되어있지 않아 점수를 등록할 수 없습니다.");
//            return;
//        }

//        try
//        {
//            Debug.Log("현재 활성화된 UGS 서비스 상태: " + UnityServices.State);
//            var scoreResponse = await LeaderboardsService.Instance.AddPlayerScoreAsync(LEADERBOARD_ID, clearStage);
//            Debug.Log($"리더보드 갱신 성공! 최고 스테이지: {scoreResponse.Score}");
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"점수 등록 실패: {e}");
//        }
//    }
//    // ==========================================
//    // 2. 탑 랭킹 가져오기 (로비에서 랭킹창 열 때 호출)
//    // ==========================================

//    //  1. 상위 랭커 데이터 가져오기 (예: 상위 50명)
//    public async Task<LeaderboardScoresPage> GetTopScoresAsync(int limit = 50)
//    {
//        try
//        {
//            var options = new GetScoresOptions { Limit = limit };
//            return await LeaderboardsService.Instance.GetScoresAsync(LEADERBOARD_ID, options);
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogWarning($"랭킹 불러오기 실패: {e.Message}");
//            return null;
//        }
//    }

//    //  2. 접속 중인 '나의' 랭킹 데이터 가져오기
//    public async Task<LeaderboardEntry> GetMyScoreAsync()
//    {
//        try
//        {
//            return await LeaderboardsService.Instance.GetPlayerScoreAsync(LEADERBOARD_ID);
//        }
//        catch (System.Exception e)
//        {
//            // 아직 한 판도 플레이하지 않아 점수가 없는 유저일 경우 여기서 에러가 날 수 있습니다.
//            Debug.Log($"내 기록을 찾을 수 없습니다 (신규 유저 등).{e.Message}");
//            return null;
//        }
//    }
//    public async void FetchTopScores()
//    {

//        if (!await EnsureLeaderboardReady()) return;

//        //  [추가된 안전 장치 1] UGS 시스템 자체가 아직 안 켜졌다면 조용히 빠져나감
//        if (UnityServices.State != ServicesInitializationState.Initialized)
//        {
//            Debug.LogWarning("UGS 시스템이 아직 초기화되지 않아 리더보드를 불러올 수 없습니다.");
//            return;
//        }

//        if (!AuthenticationService.Instance.IsSignedIn)
//        {
//            Debug.LogWarning("로그인이 되어있지 않아 랭킹을 불러올 수 없습니다.");
//            return;
//        }

//        try
//        {
//            // 상위 10명의 점수를 가져옵니다.
//            var scoresResponse = await LeaderboardsService.Instance.GetScoresAsync(LEADERBOARD_ID, new GetScoresOptions { Limit = 10 });

//            Debug.Log("=== 명예의 전당 ===");
//            foreach (var entry in scoresResponse.Results)
//            {
//                // LoginManager에서 세팅해 둔 닉네임(구글 이름 or Guest)이 여기서 자동으로 출력됩니다!
//                string displayName = string.IsNullOrEmpty(entry.PlayerName) ? "Unknown" : entry.PlayerName;
//                Debug.Log($"{entry.Rank}등 | 닉네임: {displayName} | 점수: {entry.Score}");
//            }
//        }
//        catch (System.Exception e)
//        {
//            Debug.LogError($"점수 불러오기 실패: {e}");
//        }
//    }
//}