//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using Unity.Services.CloudCode;
//using Unity.Services.CloudCode.GeneratedBindings;
//using Unity.Services.Economy;
//using UnityEngine;

//public class VirtualStoreManager
//{
//    private StoreServiceBindings _storeServiceBindings;


//    private Dictionary<string, int> _itemCosts = new Dictionary<string, int>();

//    private bool _isPurchasing = false; // 구매 진행 중 플래그

//    public void Init()
//    {
//        // 매니저가 "상점 물가표(Config) 다운로드 끝났어!" 라고 방송하면 상점 세팅을 시작합니다.
//        Managers.PlayerEconomy.EconomyConfigSynced -= InitializeVirtualStore;
//        Managers.PlayerEconomy.EconomyConfigSynced += InitializeVirtualStore;
//        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
//        Managers.Initialize.OnUnityServiceInit += SetupBindings;
//    }

//    private void SetupBindings()
//    {
//        if (_storeServiceBindings == null)
//        {
//            _storeServiceBindings = new StoreServiceBindings(CloudCodeService.Instance);
//        }
//    }
//    private void InitializeVirtualStore()
//    {
//        try
//        {
//            LogVirtualPurchasesFromConfig();// 디버그용: 어떤 상품이 있는지 확인
//            InitializePurchaseCosts();// 실제 플레이를 위한 가격표 세팅
//        }
//        catch(Exception ex)
//        {
//            Debug.LogError($"Failed to sync Economy configuration : {ex.Message}");
//        }
//    }
//    // 디버그용: UGS에 등록된 가상 상품 목록을 예쁘게 콘솔에 찍어줌
//    private void LogVirtualPurchasesFromConfig()
//    {
//        var virtualPurchases = EconomyService.Instance.Configuration.GetVirtualPurchases();
//        string virtualPurchasesJson = JsonConvert.SerializeObject(virtualPurchases, Formatting.Indented);
//        Debug.Log($"Virtual purchases from economy config : {virtualPurchasesJson}");
//    }

//    // [4] 서버 데이터를 바탕으로 내 상점의 가격표를 만듭니다.
//    private void InitializePurchaseCosts()
//    {
//        try
//        { 
//            // 서버 카탈로그에서 '물약 구매'라는 상품 정보를 쏙 빼옵니다.
//            var allpurchasesDefinition = EconomyService.Instance.Configuration.GetVirtualPurchases();

//            if (allpurchasesDefinition == null)
//            {
//                Debug.LogWarning($"Virtual purchase not found.");
//                return;
//            }

//            // 이 상품을 사기 위해 내야 하는 비용(Costs) 목록을 뒤집니다. (보통 골드나 다이아)
//            foreach (var purchase in allpurchasesDefinition)
//            {
//                var goldCost = purchase.Costs.FirstOrDefault(c => c.Item.GetReferencedConfigurationItem().Id == Define.k_GoldCurrencyKey);

//                if (goldCost != null)
//                {
//                    // 딕셔너리에 [상품ID : 가격] 형태로 저장합니다.
//                    _itemCosts[purchase.Id] = goldCost.Amount;
//                    Debug.Log($"[상점 캐싱] {purchase.Id} : {goldCost.Amount} 골드");
//                }
//                // 그 비용이 우리가 쓰는 '골드'라면?
//                //if (cost.Item.GetReferencedConfigurationItem().Id == Define.k_GoldCurrencyKey)
//                //{
//                //    // 서버가 정해준 가격을 로컬 변수에 덮어씌웁니다! (라이브옵스 핵심)
//                //    m_CurrentPotionCost = cost.Amount;
//                //    Debug.Log($"Health Potion cost set to {m_CurrentPotionCost} gold");
//                //    return;
//                //}
//            }
//            //Debug.LogWarning($"Could not find gold cost for purchase {Define.k_HealthPotionPurchaseId}. Using default value.");
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"Error initializing purchase costs : {ex.Message}. Using default values.");
//        }
//    }
//    // [5] 실제 물약 구매 버튼을 눌렀을 때 실행되는 함수
//    //public async void PurchaseHealthPotion()
//    //{
//    //    // 1차 클라이언트 방어막: 내 지갑에 돈이 부족하면 서버에 물어볼 필요도 없이 컷!
//    //    if (!CanAffordVirtualPurchase(m_CurrentPotionCost))
//    //    {
//    //        Debug.LogWarning($"Not enough gold! Need {m_CurrentPotionCost}, have {Managers.PlayerEconomy.Gold}");
//    //        return;
//    //    }
//    //    try
//    //    {
//    //        // 2차 서버 통신: Cloud Code 서버 함수를 호출하여 안전하게 결제 진행
//    //        // Process purchase through Cloude Code
//    //        var economyData = await _storeServiceBindings.VirtualPurchaseHealthPotion();
//    //
//    //        Debug.Log($"Successfully Purchased - Product :{Define.k_HealthPotionPurchaseId}");
//    //
//    //        // 결제가 완료되어 서버가 최신 잔액을 보내주면, 경제 매니저에게 업데이트를 지시합니다.
//    //        // (이러면 경제 매니저가 방송을 터뜨려서 상점 UI의 골드 표시가 알아서 깎입니다!)
//    //        Managers.PlayerEconomy.HandleEconomyUpdate(economyData);
//    //    }
//    //    catch(CloudCodeException ex)
//    //    {
//    //        // 만약 서버에서 "너 돈 없어!"라거나 "아이템 지급 에러!"가 나면 여기서 잡힙니다.
//    //        Debug.LogException(ex);
//    //    }
//    //}
//    public async void PurchaseVirtualItem(string purchaseId)
//    {
//        if (_isPurchasing) return; // 이미 진행 중이면 무시

//        // 권장 (키가 없을 경우 실시간 조회로 백업)
//        int cost = _itemCosts.ContainsKey(purchaseId) ? _itemCosts[purchaseId] : GetItemCostFromConfig(purchaseId);

//        if (!CanAffordVirtualPurchase(cost))
//        {
//            Debug.LogWarning($"Not enough gold! Need {cost}, have {Managers.PlayerEconomy.Gold}");
//            return;
//        }
//        try
//        {
//            _isPurchasing = true; // 플래그 켜기
//            Managers.UI.ShowPopupUI<UI_LoadingPopup>();
//            // 서버의 VirtualPurchaseItem 함수 호출
//            var economyData = await _storeServiceBindings.PurchaseVirtualItem(purchaseId);

//            Debug.Log($"Successfully Purchased - Product :{purchaseId}");

//            // 결제가 완료되어 서버가 최신 잔액을 보내주면, 경제 매니저에게 업데이트를 지시합니다.
//            // (이러면 경제 매니저가 방송을 터뜨려서 상점 UI의 골드 표시가 알아서 깎입니다!)
//            Managers.PlayerEconomy.HandleEconomyUpdate(economyData);
//        }
//        catch (Exception ex)
//        {
//            Debug.LogException(ex);
//        }
//        finally
//        {
//            _isPurchasing = false; // 플래그 끄기
//            Managers.UI.ClosePopupUI();
//        }
//    }

//    // 상품 ID를 넣으면 현재 서버 카탈로그에서 '골드' 가격을 찾아주는 도우미 함수
//    public int GetItemCostFromConfig(string purchaseId)
//    {
//        try
//        {
//            // 1. 서버 카탈로그에서 해당 상품(purchaseId)의 기획서를 가져옵니다.
//            var purchaseDefinition = EconomyService.Instance.Configuration.GetVirtualPurchase(purchaseId);

//            if (purchaseDefinition == null)
//            {
//                Debug.LogWarning($"[상점] 상품 정보를 찾을 수 없습니다: {purchaseId}");
//                return 0; // 에러 방지용 0 반환
//            }

//            // 2. 이 상품을 사기 위해 필요한 비용(Costs) 목록을 뒤집니다.
//            foreach (var cost in purchaseDefinition.Costs)
//            {
//                // 3. 그 비용의 종류가 우리가 쓰는 '골드'라면?
//                if (cost.Item.GetReferencedConfigurationItem().Id == Define.k_GoldCurrencyKey)
//                {
//                    //  그 골드의 양(Amount)을 반환합니다.
//                    return cost.Amount;
//                }
//            }

//            Debug.LogWarning($"[상점] 이 상품은 골드로 살 수 없습니다: {purchaseId}");
//            return 0;
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"[상점] 가격 조회 중 에러 발생: {ex.Message}");
//            return 0;
//        }
//    }
//    // 내 골드가 상품 가격보다 많은지 체크하는 유틸 함수
//    public bool CanAffordVirtualPurchase(int cost)
//    {
//        var gold = Managers.PlayerEconomy.Gold;

//        return gold >= cost;
//    }

//    public void Clear()
//    {
//        Managers.PlayerEconomy.EconomyConfigSynced -= InitializeVirtualStore;
//        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
//    }
//}
