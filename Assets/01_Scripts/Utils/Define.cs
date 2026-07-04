using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    // 서버 키
    public const string k_PlayerDataKey = "PLAYER_DATA";
    public const string k_PlayerNameKey = "PLAYER_NAME";

    // 재화키
    public const string k_GoldCurrencyKey = "GOLD";

    // 인벤토리 아이템 키
    public const string k_RemoveAdItem = "REMOVE_AD_TICKET";

    // 광고 키
    public const string placement_ShopGold = "Shop_Gold";
    public const string placement_InGameCardReload = "InGame_Card_Reload";
    public const string placement_GameOver = "Game_Over";

    // 구매 키
    public const string k_IAP_RemoveAd = "IAP_REMOVE_AD";

    public enum Scene
    {
        Unknown,
        LobbyScene,
        LoginScene,
        GameScene,
    }
    public enum CameraType
    {
        Main,
        Boss,
        Burst,
    }
    public enum Sound
    {
        Bgm,
        Sfx,
        MaxCount,
    }
    public enum SoundID
    {
        None,
        Bgm_Lobby,
        Bgm_Game,
        Sfx_UIButtonClick,
        Sfx_PlayerShot,
        Sfx_Explosion_Hit,
        Sfx_Lightning_Hit,
        Sfx_Reloading,
        Sfx_Levelup,
        Sfx_PlayerHit,
        Sfx_PlayerDie,
        Sfx_FireBullet_Hit,
        Sfx_IceBullet_Hit,
        Sfx_homingTargeting,
        Sfx_homing_Hit,
        Sfx_PierceBullet_Hit,
        Sfx_BurstModeOn,
        Sfx_BurstModeOff,
        Sfx_ShieldHit,
        Sfx_BossDie,
        Sfx_BossWarning,
        Sfx_CountDown,
        Sfx_GameStart,
        Sfx_BurstModeOnAlarm,
        Sfx_NormalBulletHit,
        Sfx_UpgradeLineUpSound,
        Sfx_UpgradeIconOpenSound,
        Sfx_GetGold,
        Sfx_IceExplosion,
        Sfx_MineExplosion,
        Sfx_Poison,
        Sfx_ExplosionMeteorAlaram,
        Sfx_ExplosionMeteor,
        Sfx_EnergyFieldHit,

    }
    public enum UIEvent
    {
        Click,
        Drag,
    }

    public enum MouseEvent
    {
        Press,
        Click,
    }

    public enum CameraMode
    {
        QuarterView,
    }

    public enum GameState
    {
        Ready,
        Playing,
        Pause,
        Resume,
        GameOver,
        GameClear,
    }
    public enum PlayerState
    {
        Idle,
        Playing,
        Die,
    }
    public enum EffectType
    {
        Screen_ShieldHit,
        Screen_BurstMode,
        Screen_PlayerHit,
        Meteor_ShockHit,
        IceBullet_Hit,
        IceBullet_Explosion,
        MineExplosion,
        MeteorPoison,
        MeteorExplosion,
        EnergyFieldHit,
        PlayerFireZone,
    }
    public enum AbilityType
    {
        Unknown = 0,
        // ==========================================
        // [1. 글로벌 패시브] : 모든 총알과 플레이어에게 공통 적용
        // ==========================================
        // 플레이어
        Passive_MaxHpUp = 1,                // 플레이어 체력 증가
        Passive_PlayerSpeedUp = 2,          // 플레이어 이동속도 증가
        Passive_ReloadCountUp = 3,          // 플레이어 재장전 개수 증가
        Passive_ReloadTimeDown = 4,         // 리로드 시간 감소
        Passive_ShotTimeDown = 5,           // 발사 딜레이 감소
        Passive_BurstMode = 6,              // 버스트 모드 활성화 및 충전 시간 감소
        Passive_PlayerShield = 7,           // 플레이어 쉴드
        Passive_PlayerCritical = 8,         // 플레이어 크리티컬
        Passive_PlayerDamageUp = 9,         // 플레이어 데미지 업
        Passive_AllBulletBounceCountUp = 10,// 모든 총알의 바운스(튕기는) 횟수 증가
        Passive_PlayerHeal,
        Passive_PlayerMagnet,
        Passive_AttackRangeUp,
        
        // 특수 기능
        Passive_SplitBullet = 20,           // 분열 기능 업그레이드

        // ==========================================
        // [2. 액티브 무기] : 획득 시 Lv.1, 중복 획득 시 레벨업 (최대 Lv.5)
        // ==========================================
        Weapon_LaserBeam = 100,                // 레이저탄 : TODO
        Weapon_ExplosionBullet = 101,         // 폭발탄 (광역 데미지)
        Weapon_LightningBullet = 102,         // 번개탄 (체인 라이트닝)
        Weapon_PierceBullet = 103,            // 관통탄 (직선 관통)
        Weapon_IceBullet = 104,               // 얼음탄 (슬로우 및 빙결 CC)
        Weapon_HomingBullet = 105,            // 유도탄 (적 추적)
        Weapon_FireBullet = 106,              // 화염탄 (장판 및 화상 DoT)

        // 신규 무기 자리
        Weapon_BlackHoleBullet = 107,         // 블랙홀탄
        Weapon_PoisonBullet = 108,            // 맹독탄
        Weapon_Mine = 109,
        Weapon_EnergyField = 110,
        
    }
    public enum UpgradeType 
    { 
        HP, 
        Damage, 
        Speed, 
        Defense 
    }
    public enum BulletType
    {
        NormalBullet = 0,       // 기본탄
        IceBullet = 1,          // 이속저하 얼음탄
        ExplosionBullet = 2,    // 폭발탄
        LightningBullet = 3,    // 번개탄
        PierceBullet = 4,       // 관통탄
        BurstBullet = 5,        // 버스트모드 불릿
        FireBullet = 6,
        ShockBullet = 7,        // 충격탄
        HomingBullet = 8,
    }
    public enum MeteorType
    {
        NormalMeteor = 0,       // 기본 메테오
        CometMeteor = 1,        // 속도빠른 메테오
        IronMeteor = 2,         // 몸빵 메테오
        FractureMeteor = 3,     // 분열하는 메테오
        MagmaMeteor = 4,        // 마그마 메테오(지나간 자리 화염장판 생성)
        GoldenMeteor = 5,       // 아이템 주는 보너스 메테오
        SludgeMeteor = 6,       // 이속 저하 장판까는 메테오(파괴 됐을 때)
        ClusterMeteor = 7,      // 가시 폭발 메테오 -> 파괴 시 가시나 돌 발사
        GravityMeteor = 8 ,     // 블랙홀 메테오
        FragmentMeteor = 9,     // 분열하는 메테오에서 나오는 파편
        AuraBuffMeteor = 10,    // 메테오들한테 장판 버프 주는 메테오
        PoisonMeteor = 11,      // 독 메테오
        ExplosionMeteor = 12,   // 폭발 메테오

    }
    public enum BossType
    {
        Boss1, 
        Boss2, 
        Boss3, 
        Boss4,
    }
    public enum BossPatternType
    {
        Spiral,
        CircleBurst,
        Shotgun,
        Sniper,
        Pinball,
        WallGap,
        Warp,
        Dash,
        BlackHole,
    }
    public enum ItemType
    {
        RecoveryHp,             // HP 회복
        RecoveryBurst,          // 버스트 게이지 회복
        Gold,
        Exp,
        Magnet,
    }
    public enum ShopItemType
    {
        IAP_REMOVE_AD_TICKET,
        GOLD_FREE,
        GOLD_AD,
        IAP_GOLD_1000,
    }
    public enum ShopCategory
    {
        REMOVE_AD,
        GOLD,
        AD,
        PACKAGE,
    }
    public enum DebuffType
    {
        Slow,
    }
    public enum PhaseType
    {
        Phase1, 
        Phase2, 
        Phase3, 
        Phase4, 
        Phase5
    }

    #region 이벤트(Action) 관련
    // 이벤트 발신용 데이터
    public enum ActionEvent
    {
        PlayerStatusChanged,        // 
        EnableBurstMode,            // 플레이어 버스트 활성화
        ExpChanged,                 // 경험치
        ScoreChanged,               // 점수
        LevelUp,                    // 레벨업
        ReloadStart,                // 리로딩 시작
        ReloadEnd,                  // 리로딩 끝
        BulletBounceCountUp,        // 불릿 튕기는 횟수 증가
        UpdateGameTime,             // 게임 시간 업데이트
        PlayerShot,
        BulletDamageUp,             // 불릿 데미지 업
        GetGold,
        MeteorDie,              
        
    }
    public struct PlayerStatusEvent
    {
        public float hp;
        public float maxHp;
        public float shieldCount;
        public float maxShieldGuage;
        public float currentShieldGuage;
        public float burst;
        public float maxBurst;
    }
    
    #endregion
}

