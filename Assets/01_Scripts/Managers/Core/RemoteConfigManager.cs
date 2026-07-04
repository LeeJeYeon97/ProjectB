//using System;
//using System.Threading.Tasks;
//using Unity.Services.Core;
//using Unity.Services.RemoteConfig;
//using UnityEngine;

//public class RemoteConfigManager
//{
//    // UGS RemoteConfig에서 데이터를 요청할 때 규칙상 빈 구조체(껍데기)를 같이 보내야 합니다.
//    public struct userAttributes { }
//    public struct appAttributes { }

//    /// <summary>
//    /// 게임이 켜질 때 딱 한 번 호출해주면 되는 초기화 및 데이터 요청 함수입니다.
//    /// </summary>
//    public async Task InitAsync()
//    {
//        // 1. UGS가 초기화되어 있는지 확인 (만약 다른 곳에서 안 했다면 여기서 처리)
//        if (UnityServices.State == ServicesInitializationState.Uninitialized)
//            await UnityServices.InitializeAsync();

//        // 2. 서버에서 데이터 다운로드가 '완료(FetchCompleted)'되었을 때, 
//        // 자동으로 ApplyRemoteConfig 함수가 실행되도록 구독(+=)해 둡니다.
//        RemoteConfigService.Instance.FetchCompleted += ApplyRemoteConfig;
//        await FetchDataAsync();
//    }

//    /// <summary>
//    /// 실제 서버로 데이터를 달라고 통신을 시도하는 비동기 함수입니다.
//    /// </summary>
//    private async Task FetchDataAsync()
//    {
//        // 빈 껍데기 구조체를 넣어서 서버에 최신 Config(설정값)를 요청합니다.
//        await RemoteConfigService.Instance.FetchConfigsAsync(new userAttributes(), new appAttributes());
//    }
//    /// <summary>
//    /// 데이터 다운로드가 끝나면 자동으로 실행되는 콜백 함수입니다.
//    /// </summary>
//    private void ApplyRemoteConfig(ConfigResponse response)
//    {
//        // 4. 응답을 확인합니다. 
//        // ConfigOrigin.Remote : 찐으로 인터넷 서버에서 최신 데이터를 받아왔다는 뜻!
//        // (만약 인터넷이 끊겼거나 변경사항이 없으면 Cached나 Default가 뜹니다)
//        if (response.requestOrigin == ConfigOrigin.Remote)
//        {
//            ParseConfigData(); // 찐 서버 데이터가 맞으니 덮어씌우러 가자!
//        }
//    }

//    /// <summary>
//    /// 다운받은 JSON 텍스트를 게임 내 ScriptableObject에 덮어씌우는 핵심 함수입니다.
//    /// </summary>
//    private void ParseConfigData()
//    {
//        // 1. StageBalanceData 덮어씌우기 (기존 코드)
//        string stageJson = RemoteConfigService.Instance.appConfig.GetJson("StageBalanceData");
//        if (!string.IsNullOrEmpty(stageJson) && Managers.Data.StageData != null)
//        {
//            JsonUtility.FromJsonOverwrite(stageJson, Managers.Data.StageData);
//        }

//        // 2. GameData 덮어씌우기 (추가된 코드)
//        string gameJson = RemoteConfigService.Instance.appConfig.GetJson("GameData");
//        if (!string.IsNullOrEmpty(gameJson) && Managers.Data.GameData != null)
//        {
//            JsonUtility.FromJsonOverwrite(gameJson, Managers.Data.GameData);
//        }

//        string playerJson = RemoteConfigService.Instance.appConfig.GetJson("PlayerStatData");
//        if (!string.IsNullOrEmpty(playerJson) && Managers.Data.playerStatData != null)
//        {
//            JsonUtility.FromJsonOverwrite(playerJson, Managers.Data.playerStatData);
//        }

//        UpdateMeteorData();
//        UpdateBulletData();
//        UpdateAbilityData();
//        UpdateBossStatData();
//        UpdateBossPatternData();
//        UpdateUpgradeData();
//    }
//    private void UpdateMeteorData()
//    {
//        //  4. 메테오 딕셔너리 패치 (추가된 부분) 
//        string meteorJson = RemoteConfigService.Instance.appConfig.GetJson("MeteorStats");

//        if (!string.IsNullOrEmpty(meteorJson))
//        {
//            // 1) 아까 만든 그릇(Wrapper)에 서버 JSON 데이터를 담아서 C# 리스트로 변환합니다.
//            MeteorConfigWrapper wrapper = JsonUtility.FromJson<MeteorConfigWrapper>(meteorJson);

//            if (wrapper != null && wrapper.meteorList != null)
//            {
//                // 2) 서버에서 보내준 메테오 목록을 하나씩 꺼내봅니다.
//                foreach (MeteorBalanceData serverData in wrapper.meteorList)
//                {
//                    // 3) 마법의 번역기: string("Magma")을 Define.MeteorType으로 변환!
//                    if (Enum.TryParse(serverData.Type, out Define.MeteorType parsedMeteorType))
//                    {
//                        // 번역된 Enum 키값으로 딕셔너리에서 SO를 찾습니다.
//                        if (Managers.Data.MeteorStatDataDict.TryGetValue(parsedMeteorType, out MeteorStatDataSO targetSO))
//                        {
//                            // 스탯 덮어씌우기
//                            targetSO.MaxHp = serverData.MaxHp;
//                            targetSO.isExclude = serverData.IsExclude;
//                            targetSO.MaxSpeed = serverData.MaxSpeed;
//                            targetSO.MinSpeed = serverData.MinSpeed;
//                            targetSO.Damage = serverData.Damage;
//                            targetSO.Score = serverData.Score;
//                            targetSO.Exp = serverData.Exp;
//                            targetSO.targetChase = serverData.targetChase;



//                            // 3. PhaseType Enum 변환
//                            if (Enum.TryParse(serverData.minPhase, true, out Define.PhaseType parsedMinPhaseType))
//                            {
//                                targetSO.minPhase = parsedMinPhaseType;
//                            }
//                            if (Enum.TryParse(serverData.maxPhase, true, out Define.PhaseType parsedMaxPhaseType))
//                            {
//                                targetSO.maxPhase = parsedMaxPhaseType;
//                            }
//                            //targetSO.weight = serverData.weight;

//                            // 2. 특수 기믹 스탯 덮어씌우기
//                            targetSO.magmaTick = serverData.magmaTick;
//                            targetSO.auraRadius = serverData.auraRadius;

//                            targetSO.poisonTick = serverData.poisonTick;
//                            targetSO.poisonDamage = serverData.poisonDamage;
//                            targetSO.poisonRadius = serverData.poisonRadius;

//                            targetSO.explosionRadius = serverData.explosionRadius;
//                            targetSO.explosionDelay = serverData.explosionDelay;
//                            targetSO.explosionTargetRadius = serverData.explosionTargetRadius;

//                            // 아이템 드랍 테이블도 string -> Enum으로 변환해서 다시 덮어씌워 줍니다.
//                            if (serverData.dropTable != null)
//                            {
//                                targetSO.dropTable.Clear(); // 기존 테이블 싹 비우기
//                                foreach (var dropStr in serverData.dropTable)
//                                {
//                                    if (Enum.TryParse(dropStr.itemType, out Define.ItemType parsedItemType))
//                                    {
//                                        targetSO.dropTable.Add(new DropItemRate
//                                        {
//                                            itemType = parsedItemType,
//                                            dropRate = dropStr.dropRate
//                                        });
//                                    }
//                                }
//                            }
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogWarning($"JSON 변환 에러: {serverData.Type} 이라는 메테오 타입은 존재하지 않습니다. 오타를 확인하세요!");
//                    }
//                }
//                Debug.Log("모든 메테오의 밸런스 패치가 성공적으로 적용되었습니다!");
//            }
//        }
//    }
//    private void UpdateBulletData()
//    {
//        string bulletJson = RemoteConfigService.Instance.appConfig.GetJson("BulletStatData");
//        if (!string.IsNullOrEmpty(bulletJson))
//        {
//            BulletConfigWrapper wrapper = JsonUtility.FromJson<BulletConfigWrapper>(bulletJson);

//            if (wrapper != null && wrapper.bulletList != null)
//            {
//                foreach (BulletBalanceData serverData in wrapper.bulletList)
//                {
//                    if (Enum.TryParse(serverData.type, true, out Define.BulletType parsedType))
//                    {
//                        if (Managers.Data.BulletDataDict.TryGetValue(parsedType, out BulletStatDataSO targetSO))
//                        {
//                            // 1. 공통 스탯 설정
//                            targetSO.isReload = serverData.isReload;

//                            //  [중요] C# 구조체(Struct)는 값 타입이므로 바로 수정이 안 됩니다. 
//                            // 임시 변수에 빼서 값을 고친 뒤 다시 집어넣어야 합니다! (Sprite 보존의 핵심)
//                            BaseBulletStatData tempBase = targetSO.stats;

//                            tempBase.chance = serverData.chance;
//                            tempBase.damage = serverData.damage;
//                            tempBase.speed = serverData.speed;
//                            tempBase.bounceCount = serverData.bounceCount;

//                            targetSO.stats = tempBase;

//                            // 2. 자식 클래스(특수 능력)에 맞게 캐스팅해서 개별 스탯 꽂아주기
//                            if (targetSO is NormalBulletStatDataSO normalSO)
//                            {
//                                var tempPierce = normalSO.pierceBulletStat;
//                                tempPierce.pierceCount = serverData.pierceCount;
//                                tempPierce.pierceDamageDecreaseValue = serverData.pierceDamageDecreaseValue;
//                                normalSO.pierceBulletStat = tempPierce;
//                            }
//                            else if (targetSO is LightningBulletStatDataSO lightningSO)
//                            {
//                                var tempLightning = lightningSO.lightningStat;
//                                tempLightning.lightningDamageValue = serverData.lightningDamageValue;
//                                tempLightning.lightningCount = serverData.lightningCount;
//                                tempLightning.lightningRange = serverData.lightningRange;
//                                lightningSO.lightningStat = tempLightning;                                
//                            }
//                            else if (targetSO is ExplosionBulletStatDataSO explosionSO)
//                            {
//                                var tempEx = explosionSO.explosionStat;
//                                tempEx.explosionRange = serverData.explosionRange;
//                                tempEx.explosionDamageValue = serverData.explosionDamageValue;
//                                explosionSO.explosionStat = tempEx;
//                            }
//                            else if(targetSO is IceBulletStatDataSO iceBulletSO)
//                            {
//                                var tempEx = iceBulletSO.iceBulletStat;
//                                tempEx.slowValue = serverData.slowValue;
//                                tempEx.slowTime = serverData.slowTime;
//                                tempEx.freezeTime = serverData.freezeTime;
//                                iceBulletSO.iceBulletStat = tempEx;
                                
//                            }
//                            else if (targetSO is BurstBulletStatDataSO burstBulletSO)
//                            {

//                            }
//                            else if (targetSO is HomingBulletStatDataSO homingBulletSO)
//                            {
//                                var tempEx = homingBulletSO.homingBulletStat;
//                                tempEx.homingShotDelay = serverData.homingShotDelay;
//                                homingBulletSO.homingBulletStat = tempEx;
//                            }
//                            else if (targetSO is FireBulletStatDataSO fireBulletSO)
//                            {
//                                var tempEx = fireBulletSO.fireStat;
//                                tempEx.fireDamageValue = serverData.fireDamageValue;
//                                tempEx.fireZoneDestroyTime = serverData.fireZoneDestroyTime;
//                                tempEx.fireRemainTime = serverData.fireRemainTime;
//                                tempEx.fireZoneSize = serverData.fireZoneSize;
//                                fireBulletSO.fireStat = tempEx;
//                            }
//                        }
//                    }
//                }
//            }
//        }
//    }
//    private void UpdateAbilityData()
//    {
//        string abilityJson = RemoteConfigService.Instance.appConfig.GetJson("AbilityStatData");
//        if (!string.IsNullOrEmpty(abilityJson))
//        {
//            AbilityConfigWrapper wrapper = JsonUtility.FromJson<AbilityConfigWrapper>(abilityJson);

//            if (wrapper != null && wrapper.abilityList != null)
//            {
//                foreach (AbilityBalanceData serverData in wrapper.abilityList)
//                {
//                    // 1. 어빌리티 타입(키값) 번역
//                    if (Enum.TryParse(serverData.type, true, out Define.AbilityType parsedAbilityType))
//                    {
//                        if (Managers.Data.AbilityDataDict.TryGetValue(parsedAbilityType, out AbilityDataSO targetSO))
//                        {
//                            // 2. 공통 스탯 (AbilityDataSO 영역) 업데이트
//                            targetSO.maxLevel = serverData.maxLevel;
//                            targetSO.isExclude = serverData.isExclude;

//                            if (Enum.TryParse(serverData.requiredAbility, true, out Define.AbilityType reqAbility))
//                            {
//                                targetSO._requiredAbility = reqAbility;
//                            }

//                            // ==============================================================
//                            // [3-A. 액티브 무기 (BulletAbilityDataSO) 캐스팅 및 적용]
//                            // ==============================================================
//                            if (targetSO is BulletAbilityDataSO bulletSO)
//                            {
//                                if (Enum.TryParse(serverData.bulletType, true, out Define.BulletType bType))
//                                {
//                                    bulletSO.bulletType = bType;
//                                }

//                                // 4. 대망의 baseStats(레벨 리스트) 안전하게 덮어씌우기 (Sprite 보호!)
//                                if (serverData.baseStats != null)
//                                {
//                                    for (int i = 0; i < serverData.baseStats.Count; i++)
//                                    {
//                                        if (i < bulletSO.baseStats.Count)
//                                        {
//                                            var tempBase = bulletSO.baseStats[i];
//                                            tempBase.chance = serverData.baseStats[i].chance;
//                                            tempBase.damage = serverData.baseStats[i].damage;
//                                            tempBase.speed = serverData.baseStats[i].speed;
//                                            tempBase.bounceCount = serverData.baseStats[i].bounceCount;
//                                            bulletSO.baseStats[i] = tempBase;
//                                        }
//                                    }
//                                }

//                                // 5. 가장 끝단 자식 클래스 캐스팅 (특수 어빌리티 스탯 적용)
//                                if (bulletSO is ExplosionBulletAbilityDataSO explosionSO && serverData.explosionBulletStats != null)
//                                {
//                                    explosionSO.stats = serverData.explosionBulletStats;
//                                }
//                                else if (bulletSO is FireBulletAbilityDataSO fireSO && serverData.fireBulletStats != null)
//                                {
//                                    fireSO.stats = serverData.fireBulletStats;
//                                }
//                                else if (bulletSO is IceBulletAbilityDataSO iceSO && serverData.iceBulletStats != null)
//                                {
//                                    iceSO.stats = serverData.iceBulletStats;
//                                }
//                                else if (bulletSO is LightningBulletAbilityDataSO lightningSO && serverData.lightningBulletStats != null)
//                                {
//                                    lightningSO.stats = serverData.lightningBulletStats;
//                                }
//                                else if (bulletSO is HomingBulletAbilityDataSO homingSO && serverData.homingBulletStats != null)
//                                {
//                                    homingSO.stats = serverData.homingBulletStats;
//                                }
//                                else if (bulletSO is PierceBulletAbilityDataSO pierceSO && serverData.pierceBulletStats != null)
//                                {
//                                    pierceSO.stats = serverData.pierceBulletStats;
//                                }
//                            }

//                            // ==============================================================
//                            // [3-B. 패시브 어빌리티 (PlayerAbilityDataSO) 캐스팅 및 적용]
//                            // ==============================================================
//                            else if (targetSO is PlayerAbilityDataSO playerAbilitySO)
//                            {
//                                // 플레이어 패시브는 baseStats(Sprite)가 없으므로 리스트를 통째로 덮어씌워도 안전합니다.
                            
//                                if (playerAbilitySO is PlayerAllBulletBounceCountUpAbilityDataSO bounceSO && serverData.bounceIncreases != null)
//                                {
//                                    bounceSO.increases = serverData.bounceIncreases;
//                                }
//                                else if (playerAbilitySO is PlayerBurstModeAblityDataSO burstSO && serverData.burstModeIncreases != null)
//                                {
//                                    burstSO.levels = serverData.burstModeIncreases;
//                                }
//                                else if (playerAbilitySO is PlayerCriticalAbilityDataSO critSO && serverData.criticalAbilityData != null)
//                                {
//                                    critSO.levels = serverData.criticalAbilityData;
//                                }
//                                else if (playerAbilitySO is PlayerDamageUpAbilityDataSO dmgUpSO && serverData.damageUpAbilityData != null)
//                                {
//                                    dmgUpSO.maxHpIncreases = serverData.damageUpAbilityData;
//                                }
//                                else if (playerAbilitySO is PlayerMaxHpUpAbilityDataSO maxHpSO && serverData.maxHpUpData != null)
//                                {
//                                    maxHpSO.maxHpIncreases = serverData.maxHpUpData;
//                                }
//                                else if (playerAbilitySO is PlayerReloadCountUpAbilityDataSO rCountSO && serverData.reloadCountUpData != null)
//                                {
//                                    rCountSO.reloadCountIncreases = serverData.reloadCountUpData;
//                                }
//                                else if (playerAbilitySO is PlayerReloadTimeDecreaseAbilityDataSO rTimeSO && serverData.reloadTimeDownData != null)
//                                {
//                                    rTimeSO.reloadTimeDecreases = serverData.reloadTimeDownData;
//                                }
//                                else if (playerAbilitySO is PlayerShieldAbilityDataSO shieldSO && serverData.ShieldAbilityData != null)
//                                {
//                                    shieldSO.increases = serverData.ShieldAbilityData;
//                                }
//                                else if (playerAbilitySO is PlayerShotTimeDecreaseAbilityDataSO sTimeSO && serverData.shotTimeDownData != null)
//                                {
//                                    sTimeSO.shotTimeDecreases = serverData.shotTimeDownData;
//                                }
//                                else if (playerAbilitySO is PlayerSpeedUpAbilityDataSO speedSO && serverData.speedUpData != null)
//                                {
//                                    speedSO.speedIncreases = serverData.speedUpData;
//                                }
//                                else if (playerAbilitySO is PlayerHealDataSO healSO && serverData.playerHealData != null)
//                                {
//                                    healSO.values = serverData.playerHealData;
//                                }
//                                else if (playerAbilitySO is PlayerSplitBulletAbilityDataSO splitSO && serverData.splitBulletData != null)
//                                {
//                                    splitSO.splitIncreases = serverData.splitBulletData;
//                                }
//                                else if (playerAbilitySO is MineAbilityDataSO mineSO && serverData.mineStats != null)
//                                {
//                                    mineSO.values = serverData.mineStats;
//                                }
//                                else if (playerAbilitySO is PlayerMagnetAbilityDataSO magnetSO && serverData.magnetData != null)
//                                {
//                                    magnetSO.values = serverData.magnetData;
//                                }
//                                else if (playerAbilitySO is PlayerAttackRangeUpDataSO attackRangeDataSO && serverData.attackRangeUpData != null)
//                                {
//                                    attackRangeDataSO.values = serverData.attackRangeUpData;
//                                }
//                                else if (playerAbilitySO is EnergyFieldAbilityDataSO energySO && serverData.energyFieldStats != null)
//                                {
//                                    energySO.increases = serverData.energyFieldStats;
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//        }
//    }
//    private void UpdateBossStatData()
//    {
//        string bossJson = RemoteConfigService.Instance.appConfig.GetJson("BossStatData");
//        if (!string.IsNullOrEmpty(bossJson))
//        {
//            BossConfigWrapper wrapper = JsonUtility.FromJson<BossConfigWrapper>(bossJson);

//            if (wrapper != null && wrapper.bossList != null)
//            {
//                foreach (BossBalanceData serverData in wrapper.bossList)
//                {
//                    // 1. 보스 타입(키값) 번역
//                    if (Enum.TryParse(serverData.Type, true, out Define.BossType parsedBossType))
//                    {
//                        if (Managers.Data.BossStatDataDict.TryGetValue(parsedBossType, out BossStatDataSO targetSO))
//                        {
//                            // 2. 기본 스탯 덮어씌우기
//                            targetSO.MaxHp = serverData.MaxHp;
//                            targetSO.Speed = serverData.Speed;
//                            targetSO.Damage = serverData.Damage;
//                            targetSO.Score = serverData.Score;

//                            // 3. 아이템 드랍 테이블 처리 (이전 메테오 때 썼던 로직 동일)
//                            if (serverData.dropTable != null)
//                            {
//                                targetSO.dropTable.Clear();
//                                foreach (var dropStr in serverData.dropTable)
//                                {
//                                    if (Enum.TryParse(dropStr.itemType, true, out Define.ItemType parsedItemType))
//                                    {
//                                        targetSO.dropTable.Add(new DropItemRate { itemType = parsedItemType, dropRate = dropStr.dropRate });
//                                    }
//                                }
//                            }

//                            // 4. 대망의 '패턴 SO 리스트' 갈아끼우기!
//                            if (serverData.myPatterns != null)
//                            {
//                                targetSO.myPatterns.Clear(); // 기존에 들고 있던 패턴 목록 싹 비우기

//                                foreach (string patternName in serverData.myPatterns)
//                                {
//                                    if (Enum.TryParse(patternName, true, out Define.BossPatternType parsedType))
//                                    {
//                                        // DataManager의 패턴 보따리에서 서버가 준 이름과 똑같은 SO를 찾습니다.
//                                        if (Managers.Data.BossPatternDict.TryGetValue(parsedType, out BossPatternSO foundPatternSO))
//                                        {
//                                            // 찾았다면 보스의 실제 패턴 리스트에 진짜 SO를 쏙 넣어줍니다!
//                                            targetSO.myPatterns.Add(foundPatternSO);
//                                        }
//                                        else
//                                        {
//                                            Debug.LogWarning($"[보스 패치 경고] {patternName} 이라는 패턴 SO를 찾을 수 없습니다! 오타를 확인하세요.");
//                                        }
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//        }
//    }
//    private void UpdateBossPatternData()
//    {
//        string patternJson = RemoteConfigService.Instance.appConfig.GetJson("BossPatternData");
//        if (!string.IsNullOrEmpty(patternJson))
//        {
//            PatternConfigWrapper wrapper = JsonUtility.FromJson<PatternConfigWrapper>(patternJson);

//            if (wrapper != null && wrapper.patternList != null)
//            {
//                foreach (PatternBalanceData serverData in wrapper.patternList)
//                {
//                    //  1. JSON의 문자열(type)을 Enum으로 안전하게 번역합니다.
//                    if (Enum.TryParse(serverData.type, true, out Define.BossPatternType parsedType))
//                    {
//                        //  2. 번역된 Enum을 키값으로 딕셔너리에서 찾습니다.
//                        if (Managers.Data.BossPatternDict.TryGetValue(parsedType, out BossPatternSO targetSO))
//                        {
//                            // 3. 부모 공통 변수 덮어씌우기
//                            targetSO.nextPatternDelay = serverData.nextPatternDelay;

//                            // patternName도 바꾸고 싶다면 추가 (선택사항)
//                            if (!string.IsNullOrEmpty(serverData.patternName))
//                                targetSO.patternName = serverData.patternName;

//                            // 4. 각 자식 클래스 캐스팅 후 데이터 덮어씌우기
//                            if (targetSO is Pattern_CircleBurstSO circleSO)
//                            {
//                                circleSO.bulletCount = serverData.bulletCount;
//                                circleSO.burstCount = serverData.burstCount;
//                                circleSO.burstDelay = serverData.burstDelay;
//                                circleSO.bulletSpeed = serverData.bulletSpeed;
//                            }
//                            else if (targetSO is Pattern_PinballSO pinballSO)
//                            {
//                                pinballSO.totalBullets = serverData.totalBullets;
//                                pinballSO.fireDelay = serverData.fireDelay;
//                                pinballSO.bulletSpeed = serverData.bulletSpeed;
//                            }
//                            else if (targetSO is Pattern_ShotgunSO shotgunSO)
//                            {
//                                shotgunSO.bulletCount = serverData.bulletCount;
//                                shotgunSO.spreadAngle = serverData.spreadAngle;
//                                shotgunSO.burstCount = serverData.burstCount;
//                                shotgunSO.burstDelay = serverData.burstDelay;
//                                shotgunSO.bulletSpeed = serverData.bulletSpeed;
//                            }
//                            else if (targetSO is Pattern_SniperSO sniperSO)
//                            {
//                                sniperSO.burstCount = serverData.burstCount;
//                                sniperSO.fireDelay = serverData.fireDelay;
//                                sniperSO.repeatCount = serverData.repeatCount;
//                                sniperSO.repeatDelay = serverData.repeatDelay;
//                                sniperSO.bulletSpeed = serverData.bulletSpeed;
//                            }
//                            else if (targetSO is Pattern_SpiralSO spiralSO)
//                            {
//                                spiralSO.bulletCount = serverData.bulletCount;
//                                spiralSO.angleStep = serverData.angleStep;
//                                spiralSO.fireDelay = serverData.fireDelay;
//                                spiralSO.bulletSpeed = serverData.bulletSpeed;
//                            }
//                            else if (targetSO is Pattern_WallGapSO wallGapSO)
//                            {
//                                wallGapSO.totalBullets = serverData.totalBullets;
//                                wallGapSO.spreadAngle = serverData.spreadAngle;
//                                wallGapSO.gapSize = serverData.gapSize;
//                                wallGapSO.waveCount = serverData.waveCount;
//                                wallGapSO.waveDelay = serverData.waveDelay;
//                                wallGapSO.bulletSpeed = serverData.bulletSpeed;
//                            }
//                            else if(targetSO is Pattern_WarpSO warpSO)
//                            {
//                                warpSO.warpRadius = serverData.warpRadius;
//                                warpSO.fadeOutTime = serverData.fadeOutTime;
//                                warpSO.fadeInTime = serverData.fadeInTime;
//                            }
//                            else if(targetSO is Pattern_DashSO dashSO)
//                            {
//                                dashSO.dashSpeed = serverData.dashSpeed;
//                                dashSO.overshoot = serverData.overshoot;
//                                dashSO.warningTime = serverData.warningTime;    
//                            }
//                            else if (targetSO is Pattern_BlackHoleSO blackHoleSO)
//                            {
//                                blackHoleSO.pullForce = serverData.blackHolePullForce;
//                                blackHoleSO.fireDelay = serverData.blackHoleFireDelay;
//                                blackHoleSO.bulletSpeed = serverData.blackHoleBulletSpeed;
//                                blackHoleSO.centerDamage = serverData.blackHoleCenterDamage;
//                                blackHoleSO.travelDistance = serverData.blackHoleTravelDistance;
//                                blackHoleSO.damageInterval = serverData.blackHoleDamageInterval;
//                                blackHoleSO.lifeTime = serverData.blackHoleLifeTime;
//                            }
//                        }
//                        else
//                        {
//                            Debug.LogWarning($"[패턴 패치 경고] {parsedType} 패턴을 딕셔너리에서 찾을 수 없습니다.");
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogWarning($"[패턴 파싱 실패] JSON의 type({serverData.type})을 BossPatternType Enum으로 변환할 수 없습니다.");
//                    }
//                }
//                Debug.Log("모든 보스 패턴 상세 수치 패치 완료! (Enum 기반)");
//            }
//        }
//    }
//    private void UpdateUpgradeData()
//    {
//        // 1. Remote Config에서 데이터 가져오기
//        string upgradeJson = RemoteConfigService.Instance.appConfig.GetJson("UpgradeData");

//        if (!string.IsNullOrEmpty(upgradeJson))
//        {
//            // 2. JSON을 Wrapper 구조체로 파싱
//            UpgradeConfigWrapper wrapper = JsonUtility.FromJson<UpgradeConfigWrapper>(upgradeJson);

//            if (wrapper != null && wrapper.upgradeList != null)
//            {
//                // 3. 리스트를 돌면서 덮어씌우기
//                foreach (UpgradeBalanceData serverData in wrapper.upgradeList)
//                {
//                    // string("HP")을 Define.UpgradeType Enum으로 안전하게 번역
//                    if (Enum.TryParse(serverData.type, true, out Define.UpgradeType parsedType))
//                    {
//                        // 딕셔너리에서 실제 업그레이드 SO 찾기
//                        if (Managers.Data.UpgradeDataDict.TryGetValue(parsedType, out UpgradeDataSO targetSO))
//                        {
//                            // ?? 데이터 덮어씌우기

//                            // 이름 변경 (서버 데이터가 비어있지 않을 때만)
//                            if (!string.IsNullOrEmpty(serverData.upgradeName))
//                            {
//                                targetSO.upgradeName = serverData.upgradeName;
//                            }

//                            // 핵심! 레벨별 수치 배열 통째로 갈아끼우기
//                            if (serverData.levelInfos != null && serverData.levelInfos.Length > 0)
//                            {
//                                targetSO.levelInfos = serverData.levelInfos;
//                            }
//                        }
//                        else
//                        {
//                            Debug.LogWarning($"[RemoteConfig] {parsedType} 영구 강화 SO를 찾을 수 없습니다.");
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogWarning($"[RemoteConfig] 알 수 없는 업그레이드 타입입니다: {serverData.type}");
//                    }
//                }

//                Debug.Log("모든 영구 강화(Upgrade) 데이터 패치 완료!");
//            }
//        }
//    }
//}
