using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class DataManager
{
    //// 모든 데이터 담고 있는 딕셔너리
    //public Dictionary<Define.BulletType, BulletStatDataSO> BulletDataDict { get; private set; }
    //public Dictionary<Define.AbilityType, AbilityDataSO> AbilityDataDict { get; private set; }

    //public GameDataSO GameData { get; private set; }
    //public PlayerStatDataSO playerStatData { get; private set; }
    //public Dictionary<Define.ItemType, ItemDataSO> ItemDataList { get; private set; }

    //public Dictionary<Define.MeteorType, MeteorStatDataSO> MeteorStatDataDict { get; private set; }
    //public Dictionary<Define.BossType, BossStatDataSO> BossStatDataDict { get; private set; }
    //public Dictionary<Define.BossPatternType, BossPatternSO> BossPatternDict { get; private set; }
    //public SoundDataSO SoundData { get; private set; }
    //public EffectDataSO EffectData { get; private set; }
    //public StageBalanceDataSO StageData { get; private set; }
    //public Dictionary<Define.ShopItemType, ShopItemDataSO> ShopItemDataDict { get; private set; }

    //public Dictionary<Define.UpgradeType, UpgradeDataSO> UpgradeDataDict { get; private set; }

    //public void Init()
    //{
    //    // 딕셔너리 로드 (내부에서 자동으로 복사본 생성)
    //    BulletDataDict = LoadDataToDict<Define.BulletType, BulletStatDataSO>("RemoteConfigDatas/BulletStat", data => data.type);
    //    AbilityDataDict = LoadDataToDict<Define.AbilityType, AbilityDataSO>("RemoteConfigDatas/Abilities", data => data.type);

    //    // [핵심 변경] 새로 만든 안전 로드 함수(LoadAndInstantiate)를 사용하여 코드가 엄청나게 깔끔해졌습니다!
    //    GameData = LoadAndInstantiate<GameDataSO>("Datas/RemoteConfigDatas/GameData");
    //    playerStatData = LoadAndInstantiate<PlayerStatDataSO>("Datas/RemoteConfigDatas/PlayerStatData");
    //    StageData = LoadAndInstantiate<StageBalanceDataSO>("Datas/RemoteConfigDatas/StageBalanceData");
    //    MeteorStatDataDict = LoadDataToDict<Define.MeteorType, MeteorStatDataSO>("RemoteConfigDatas/Meteors", data => data.Type);


    //    EffectData = LoadAndInstantiate<EffectDataSO>("Datas/EffectData");
    //    SoundData = LoadAndInstantiate<SoundDataSO>("Datas/SoundData");
    //    ShopItemDataDict = LoadDataToDict<Define.ShopItemType, ShopItemDataSO>("ShopItems", data => data.type);
    //    ItemDataList = LoadDataToDict<Define.ItemType, ItemDataSO>("Items", data => data.type);

    //    BossStatDataDict = LoadDataToDict<Define.BossType, BossStatDataSO>("RemoteConfigDatas/Boss", data => data.Type);
    //    BossPatternDict = LoadDataToDict<Define.BossPatternType, BossPatternSO>("RemoteConfigDatas/BossPattern", data => data.type);

    //    UpgradeDataDict = LoadDataToDict<Define.UpgradeType, UpgradeDataSO>("Upgrade", data => data.type);
    //}

    ///// <summary>
    /////  단일 ScriptableObject를 안전하게(복사본으로) 불러오는 헬퍼 함수
    ///// 에디터에서 원본 파일이 변조되는 것을 완벽하게 막아줍니다.
    ///// </summary>
    //private T LoadAndInstantiate<T>(string path) where T : ScriptableObject
    //{
    //    T original = Managers.Resource.Load<T>(path);
    //    if (original != null)
    //    {
    //        // 원본 파일은 그대로 두고, 메모리에 똑같이 생긴 복사본(Clone)을 띄워서 반환합니다.
    //        return UnityEngine.Object.Instantiate(original);
    //    }

    //    Debug.LogError($"DataManager Null 에러: {path} 경로에서 데이터를 찾을 수 없습니다.");
    //    return null;
    //}

    ///// <summary>
    ///// 데이터를 로드하여 지정한 키 생성 규칙에 따라 딕셔너리를 생성합니다.
    ///// </summary>
    //private Dictionary<TKey, TValue> LoadDataToDict<TKey, TValue>(string folderName, Func<TValue, TKey> keySelector) where TValue : ScriptableObject
    //{
    //    Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
    //    TValue[] datas = Managers.Resource.LoadAll<TValue>($"Datas/{folderName}");

    //    if (datas == null || datas.Length == 0)
    //    {
    //        Debug.LogWarning($"DataManager: Datas/{folderName} 경로에 데이터가 없습니다.");
    //        return dict;
    //    }

    //    foreach (var data in datas)
    //    {
    //        TKey key = keySelector(data);

    //        if (key == null) continue;

    //        if (!dict.ContainsKey(key))
    //        {
    //            //  딕셔너리에 들어가는 수많은 데이터들도 나중에 RemoteConfig로 수정할 때를 대비해 모두 복사본으로 저장!
    //            dict.Add(key, UnityEngine.Object.Instantiate(data));
    //        }
    //        else
    //        {
    //            Debug.LogWarning($"DataManager: 중복된 키 감지! Type: {typeof(TValue)}, Key: {key}");
    //        }
    //    }

    //    return dict;
    //}

    //private List<T> LoadDataToList<T>(string folderName) where T : ScriptableObject
    //{
    //    List<T> list = new List<T>();
    //    T[] datas = Managers.Resource.LoadAll<T>($"Datas/{folderName}");

    //    if (datas == null || datas.Length == 0)
    //    {
    //        Debug.LogWarning($"DataManager: Datas/{folderName} 경로에 데이터가 없습니다.");
    //        return list; // null 대신 빈 리스트 반환이 널포인터 에러 예방에 좋습니다.
    //    }

    //    // 리스트 역시 안전하게 복사
    //    foreach (var data in datas)
    //    {
    //        list.Add(UnityEngine.Object.Instantiate(data));
    //    }

    //    return list;
    //}

    //// T로 들어온 스크립터블 오브젝트로 만든 모든 데이터들중에 이름으로 찾아주는 함수
    ////public T GetData<T>(string name) where T : ScriptableObject
    ////{
    ////    // 내가 찾고 싶은 데이터 타입
    ////    System.Type type = typeof(T);
    ////
    ////    if(_allDataDict.TryGetValue(type,out object dictObj))
    ////    {
    ////        var dict = dictObj as Dictionary<string, T>;
    ////
    ////        if(dict != null && dict.TryGetValue(name, out T data))
    ////        {
    ////            return data;
    ////        }
    ////    }
    ////    return null;
    ////}

    ////// T로 들어온 스크립터블 오브젝트로 만든 모든 데이터를 리스트로 반환하는 함수
    ////public List<T> GetAllData<T> () where T : ScriptableObject
    ////{
    ////    System.Type type = typeof(T);
    ////
    ////    if (_allDataDict.TryGetValue(type, out object dictObj))
    ////    {
    ////        var dict = dictObj as Dictionary<string, T>;
    ////
    ////        if(dict != null)
    ////        {
    ////            return new List<T>(dict.Values);
    ////        }
    ////    }
    ////    Debug.LogError($"DataManager: {type.Name} 타입의 데이터가 존재하지 않습니다.");
    ////  
    ////    return null;
    ////}

    ////Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    ////{
    ////    TextAsset textAsset = Managers.Resource.Load<TextAsset>($"Data/{path}");
    ////    return JsonUtility.FromJson<Loader>(textAsset.text);
    ////}
}


