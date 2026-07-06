//using Newtonsoft.Json;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Unity.Services.CloudCode;
//using Unity.Services.CloudCode.GeneratedBindings;
//using Unity.Services.CloudCode.GeneratedBindings.Project;
//using Unity.Services.CloudSave;
//using Unity.Services.Economy;
//using Unity.Services.Economy.Model;
//using UnityEngine;
//using UnityEngine.Purchasing;
//using UnityEngine.Purchasing.Security;

//public class IAPStoreManager
//{
//    // TODO 
////    실제 서비스에서는 유저의 인터넷이 불안정해서 Connect나 FetchProducts가 실패할 수도 있습니다.

////이런 경우를 대비해 **'재시도 버튼'**을 UI에 만들어두고, 클릭 시 다시 InitializeIAPSync() 를 호출하게 하면 훨씬 안정적인 상점을 만들 수 있습니다.

////지금 유저님의 코드는 try-catch로 에러를 잘 잡고 있으니, 에러 로그 출력 시점에 "네트워크를 확인해 주세요"라는 팝업을 띄우는 로직만 추가하면 상용 수준의 코드가 됩니다!



//    // 최신 Unity IAP v5 컨트롤러 (상품 조회, 구매, 확정을 모두 담당하는 매니저)
//    // v5 Controller (one stop for fetching, purchasing, confirming)
//    private StoreController _storeController;

//    // 서버의 현금 결제(ProcessRealMoneyPurchase) 함수를 부르기 위한 리모컨
//    // 서버 스토어 로직 클래스
//    private StoreServiceBindings _storeServiceBindings;

//    // 구매 진행중 여부 체크 변수
//    private bool _isPurchaseInProgress;

//    // 안드로이드(구글) 로컬 영수증 1차 검증기
//    private CrossPlatformValidator _crossPlatformValidator;

//    public event Action<string> SuccessfullyPurchased;
//    public event Action<string> PurchaseFailed;

//    #region Core
//    public void Init()
//    {
//        // 1. UGS의 상점 데이터 다운로드가 끝나면 -> IAP 초기화 시작
//        Managers.PlayerEconomy.EconomyConfigSynced -= InitializeIAPSync;
//        Managers.PlayerEconomy.EconomyConfigSynced += InitializeIAPSync;

//        // 2. 유니티 서비스 초기화 시 서버 리모컨 조립
//        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
//        Managers.Initialize.OnUnityServiceInit += SetupBindings;
//    }
//    public void SetupBindings()
//    {
//        _storeServiceBindings = new StoreServiceBindings(CloudCodeService.Instance);
//    }
//    private void Clear()
//    {

//        Managers.PlayerEconomy.EconomyConfigSynced -= InitializeIAPSync;
//        Managers.Initialize.OnUnityServiceInit -= SetupBindings;
//        UnsubscribeIAPEvents();
//    }

//    // IAP v5의 모든 이벤트를 구독합니다.
//    private void SubscribeIAPEvents()
//    {
//        if (_storeController == null) return;

//        _storeController.OnProductsFetched += OnProductsFetched;
//        _storeController.OnProductsFetchFailed += OnProductsFetchFailed;

//        _storeController.OnPurchasesFetched += OnPurchasesFetched;
//        _storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

//        _storeController.OnPurchasePending += OnPurchasePending;
//        _storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
//        _storeController.OnPurchaseFailed += OnPurchaseFailed;

//        _storeController.OnPurchaseDeferred += OnPurchaseDeferred;

//        _storeController.OnStoreDisconnected += OnStoreDisconnected;
//    }
//    private void UnsubscribeIAPEvents()
//    {
//        if (_storeController == null) return;

//        _storeController.OnProductsFetched -= OnProductsFetched;
//        _storeController.OnProductsFetchFailed -= OnProductsFetchFailed;

//        _storeController.OnPurchasesFetched -= OnPurchasesFetched;
//        _storeController.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;

//        _storeController.OnPurchasePending -= OnPurchasePending;
//        _storeController.OnPurchaseConfirmed -= OnPurchaseConfirmed;
//        _storeController.OnPurchaseFailed -= OnPurchaseFailed;

//        _storeController.OnPurchaseDeferred -= OnPurchaseDeferred;

//        _storeController.OnStoreDisconnected -= OnStoreDisconnected;
//    }


//    #region Product/Purchase fetch callbacks
//    private void OnProductsFetched(List<Product> products)
//    {
//        // Init 한 후에 불림


//        // 스토어에서 상품 정보를 성공적으로 가져왔습니다!
//        // 여기서 FetchPurchases()를 부르면 유저가 과거에 샀던 '비소모성(광고제거 등)' 아이템 내역을 복구해 옵니다.
//        // 구매한 상품에 대해 과거 구매내역 조회하고 사용가능한 상품을 기록
//        _storeController.FetchPurchases();

//        LogProductsFetched(products);
//    }
//    private void LogProductsFetched(List<Product> products)
//    {
//        Debug.Log($"[IAP] Products fetched :{products.Count}");
//        foreach(var p in products)
//        {
//            Debug.Log($"[IAP] {p.definition.id} | {p.metadata.localizedTitle} | {p.metadata.localizedPriceString}");
//        }
//    }
//    private void OnProductsFetchFailed(ProductFetchFailed failure)
//    {
//        Debug.LogError($"[IAP] Product fetch failed : {failure.FailureReason}");
//    }

//    void OnPurchasesFetched(Orders orders)
//    {
//        // 1. orders.count 대신 orders.ConfirmedOrders.Count 를 사용합니다.
//        Debug.Log($"[IAP] 과거 결제 내역 조회 완료. 확정된 주문: {orders.ConfirmedOrders.Count}건");

//        // 2.  핵심: orders 자체가 아니라 'orders.ConfirmedOrders' 리스트를 순회합니다!
//        foreach (var order in orders.ConfirmedOrders)
//        {
//            var purchasedProduct = order.CartOrdered.Items().FirstOrDefault()?.Product;
//            if (purchasedProduct != null)
//            {
//                string pid = purchasedProduct.definition.id;
//                Debug.Log($"[IAP] 복원된 상품 발견: {pid}");

//                // 3. 영구 상품(광고 제거 등)이라면 혜택을 다시 적용해 줍니다!
//                if (pid.Equals(Define.k_IAP_RemoveAd, StringComparison.OrdinalIgnoreCase))
//                {
//                    // 비동기 함수를 호출 (에러 방지를 위해 Task.Run이나 UniTask 권장, 여기서는 간단히 래핑)
//                    RestorePurchaseToServerAsync(purchasedProduct, order.Info.Receipt);
//                }
//            }
//        }
//    }
//    private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
//    {
//        Debug.LogError($"[IAP] Purchases fetch failed : {failure.FailureReason}");
//    }
//    private void OnStoreDisconnected(StoreConnectionFailureDescription desc)
//    {
//        Debug.LogError($"[IAP] Store disconnected : {desc.Message}");
//    }
//    #endregion

//    private void OnPurchaseDeferred(DeferredOrder deferred)
//    {
//        // 5. '부모 승인 요청' 상태가 되면 일단 유저가 할 일은 끝났으므로 로딩 OFF!
//        Managers.UI.ClosePopupUI();
//        _isPurchaseInProgress = false;
//        Debug.Log($"[IAP] 구매 승인 대기 중 (Ask to Buy)");
//    }

//    //[핵심] 결제 결과 대기 상태(구글/애플에서 결제는 성공했고, 우리가 보상을 줄 차례)
//    private async void OnPurchasePending(PendingOrder pending)
//    {
//        try
//        {
//            Debug.Log($"Full receipt JSON : {pending.Info.Receipt}");

//            // 1. 장바구니에서 유저가 산 상품의 ID를 꺼냅니다.
//            // v5 : products live in the order's cart (usually 1 item, but don't assume)
//            var firstItem = pending.CartOrdered.Items().FirstOrDefault();
//            var pid = firstItem?.Product?.definition?.id;
//            var receipt = pending.Info.Receipt;  // 1. 영수증(Receipt) 추출

//            if (string.IsNullOrEmpty(pid))
//            {
//                Debug.LogError("[IAP] Pending order has no product id.");
//                PurchaseFailed?.Invoke("No product id in pending order");
//                return;
//            }
//            Debug.Log($"[IAP] 처리 대기 중인 주문 발견 (복원 포함): {pid}");
//            var product = _storeController?.GetProductById(pid);

//            if (product == null)
//            {
//                Debug.LogError($"[IAP] Product not found in controller : {pid}");
//                PurchaseFailed?.Invoke($"Product not found : {pid}");
//                return;
//            }
//            // 2. 안드로이드라면 서버에 보내기 전에 클라이언트에서 1차로 가짜 영수증인지 검사합니다.
//            // Optional Google validation (Apple handled internally in v5)
//            if (!ValidateIfGoogle(receipt))
//            {
//                Debug.LogError("[IAP] Google receipt validation failed.");
//                PurchaseFailed?.Invoke("Invalid receipt for " + product.definition.id);
//                return;
//            }
//            Debug.Log($"[IAP] Pending purchase : {product.definition.id}");
//            Debug.Log($"[IAP] 서버 검증 요청 시작 : {product.definition.id}");

//            // 3. 서버(Cloud Code)에 영수증을 보내서 2차 깐깐한 검증을 받고 보상을 지급받습니다!
//            // Cloud Code validation + grant
//            PlayerDataResponse response = await _storeServiceBindings.ProcessRealMoneyPurchase(
//                product.definition.id,
//                receipt,        
//                (double)product.metadata.localizedPrice,
//                product.metadata.isoCurrencyCode);

//            // 4. 보상이 잘 들어왔으니 내 지갑(로컬 데이터)을 최신화합니다.
//            // 무료 보상 때 쓰셨던 최신화 도우미 함수들을 그대로 돌려줍니다.
//            Managers.PlayerData.UpdatedPlayerData(response.PlayerData);
//            Managers.PlayerEconomy.HandleEconomyUpdate(response.PlayerEconomyData);

//            // 인벤토리 티켓 로직 실행
//            ApplyPurchaseBenefit(pid);

//            // 5. 서버 보상까지 완벽히 끝났으니, 스토어에 "결제 확정(Confirm)해 줘!" 라고 알립니다.
//            // (이걸 안 부르면 며칠 뒤에 유저에게 환불 처리됩니다.)
//            _storeController.ConfirmPurchase(pending);
//            Debug.Log($"[IAP] Confirmed purchase : {product.definition.id}");
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"[IAP] Error processing pending order : {ex.Message}");
//            PurchaseFailed?.Invoke("Purchase failed : " + ex.Message);

//            // 서버 검증에서 에러가 나면 여기서 로딩을 닫아줘야 유저가 다시 시도할 수 있습니다.
//            Managers.UI.ClosePopupUI();
//            _isPurchaseInProgress = false;
//        }
//    }
//    private void OnPurchaseConfirmed(Order order)
//    {
//        // 3. 모든 과정(서버 보상 + 스토어 확정)이 끝났을 때 로딩 OFF!
//        Managers.UI.ClosePopupUI();
//        _isPurchaseInProgress = false;

//        if (order is FailedOrder failedOrder)
//        {
//            Debug.LogWarning($"[IAP] Confirmation failed: {failedOrder.FailureReason}");
//            return;
//        }

//        var purchasedProduct = order.CartOrdered.Items().FirstOrDefault()?.Product;

//        Debug.Log($"[IAP] Purchase confirmed : {purchasedProduct?.definition.id} | Tx : {order.Info?.TransactionID}");
//        SuccessfullyPurchased?.Invoke($"Purchase confirmed : {purchasedProduct?.definition.id}");
//    }

//    private void OnPurchaseFailed(FailedOrder failed)
//    {
//        // 결제 도중 취소하거나 잔액이 부족해 실패한 경우 락(Lock)을 풀어줍니다.
//        _isPurchaseInProgress = false;
//        Managers.UI.ClosePopupUI();

//        Debug.LogError($"[IAP] Purchase failed : {failed.FailureReason.ToString()}");
//        PurchaseFailed?.Invoke($"Purchase failed : {failed.FailureReason.ToString()}");

//    }
//    private async void InitializeIAPSync()
//    {
//        // Get Controller
//        _storeController = UnityIAPServices.StoreController();

//        // Subscribe to events
//        SubscribeIAPEvents();

//        try
//        {
//            // When this call completes, you may assume that IAP has connected to your current app store.
//            await _storeController.Connect();
//            Debug.Log("[IAP] Connected to store.");

//            // Build and fetch products from economy config
//            // UGS 대시보드에 등록해둔 현금 상품들을 유니티 IAP 시스템에 맞게 포장합니다.
//            var productDefs = BuildProductDefinitionsFromEconomy();

//            if (productDefs.Count == 0)
//            {
//                Debug.Log("[IAP] No real-money products found in Economy config.");
//                return;
//            }

//            // 스토어(구글/애플)에 상품 목록을 던져주며 "이것들 팔 준비해!" 라고 요청합니다.
//            _storeController.FetchProducts(productDefs);
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"[IAP] Connect failed : {ex.Message}");
//        }

//        // 필요한 경우 영수증 유효성 검사기 초기화
//        InitializeReceiptValidatorsIfNeeded();
//    }
//    private List<ProductDefinition> BuildProductDefinitionsFromEconomy()
//    {
//        // 1. 주문서(빈 리스트)를 하나 준비합니다.
//        var productDefinitions = new List<ProductDefinition>();

//        // 2.UGS Economy 서버에서 다운로드해 두었던 '현금 상품 목록'을 전부 가져옵니다.
//        // (이 데이터는 게임 켤 때 Managers.PlayerEconomy.Init() 등에서 이미 다운로드된 상태입니다)
//        // 실제 현금 구매 정의 목록 가져오기
//        var realMoneny = EconomyService.Instance.Configuration.GetRealMoneyPurchases();

//        // 3. UGS에서 가져온 상품들을 하나씩 꺼내봅니다.
//        foreach (var purchase in realMoneny)
//        {
//            // 가져온 현금 구매 정의를 IAP 제품 정의 객체로 변환
//            // 소모성 아이템으로 정의 -> 광고제거 같은 거는 코드나 IAP카탈로그에서 진행 해야함?
//            // 주의: 현재 모든 상품을 '소모성(Consumable)'으로 박아두었습니다. (아래 개선점 참고)
//            // 4. UGS 데이터를 Unity IAP가 알아먹을 수 있는 규격(ProductDefinition)으로 포장합니다.
//            // "상품 ID는 이거고, 소모성(Consumable) 아이템이야!" 라고 이름표를 붙이는 과정입니다.
//            // 1. 유니티 내부용 대문자 아이디 (예: IAP_REMOVE_AD)
//            string baseId = purchase.Id;

//            // 2. 구글 플레이용 소문자 아이디를 담을 변수 (일단 기본 아이디로 초기화)
//            string storeSpecificId = baseId;

//            //  [핵심] 대시보드에 적어둔 "Store identifiers"를 여기서 꺼내옵니다!
//            // 안드로이드 기기일 경우, "GooglePlay" 키값에 해당하는 소문자 아이디를 찾아옵니다.
//            if (purchase.StoreIdentifiers != null)
//            {
//                if (Application.platform == RuntimePlatform.Android)
//                {
//                    // 구글 플레이용 아이디가 비어있지 않다면 적용
//                    if (!string.IsNullOrEmpty(purchase.StoreIdentifiers.GooglePlayStore))
//                    {
//                        storeSpecificId = purchase.StoreIdentifiers.GooglePlayStore;
//                    }
//                }
//                else if (Application.platform == RuntimePlatform.IPhonePlayer)
//                {
//                    // 애플 앱스토어용 아이디가 비어있지 않다면 적용
//                    if (!string.IsNullOrEmpty(purchase.StoreIdentifiers.AppleAppStore))
//                    {
//                        storeSpecificId = purchase.StoreIdentifiers.AppleAppStore;
//                    }
//                }
//            }

//            // 3. 상품 타입 지정 (광고 제거는 1번만 사는 거니까 비소모성(NonConsumable)이어야 합니다!)
//            ProductType pType = (baseId == Define.k_IAP_RemoveAd) ? ProductType.NonConsumable : ProductType.Consumable;

//            // 4. [핵심] 생성자에 baseId와 storeSpecificId를 둘 다 넣어줍니다!
//            // 이렇게 해야 IAP가 "내부에서는 IAP_REMOVE_AD로 부르고, 구글한테는 iap_remove_ad로 물어봐야지!" 라고 똑똑하게 작동합니다.
//            var def = new ProductDefinition(id: baseId, storeSpecificId: storeSpecificId, type: pType);

//            productDefinitions.Add(def);
//        }

//        Debug.Log($"[IAP] Prepared {productDefinitions.Count} ProductDefinitions for fetch.");
//        return productDefinitions;
//    }
//    private void BuildAndFetchProductsWithCatalog()
//    {
//        // Load Catalog from Assets/Resources/IAPProcutCatalog.json
//        var catalog = ProductCatalog.LoadDefaultCatalog();
//        if (catalog == null || catalog.allProducts == null || catalog.allProducts.Count == 0)
//        {
//            Debug.LogWarning("[IAP] No products in IAPProductCatalog.json");
//            return;
//        }

//        var productDefinitions = new List<ProductDefinition>();
//        foreach (var item in catalog.allProducts)
//        {
//            // Convert each catalog item into a  ProductDefinition
//            productDefinitions.Add(new ProductDefinition(item.id, item.type));
//        }

//        // Fetch products from store using Unity IAP Services
//        _storeController.FetchProducts(productDefinitions);
//    }

//    private void LogRealPurchasesFromConfig(List<RealMoneyPurchaseDefinition> realMoneyPurchases)
//    {
//        Debug.Log($"Real purchases from economy config :\n{JsonConvert.SerializeObject(realMoneyPurchases, Formatting.Indented)}");
//    }

//    // Alternative : Using CatalogProvider instead of Economy configuration
    
//    private void InitializeReceiptValidatorsIfNeeded()
//    {
//#if !UNITY_EDITOR
//        // In v5, Apple receipts are handled by StoreKit2. Keep validator for Googld only.
//        if(Application.platform == RuntimePlatform.Android)
//        {
//            try
//            {
//                _crossPlatformValidator = new CrossPlatformValidator(GooglePlayTangle.Data(), Application.identifier);
//                Debug.Log("[IAP] Google receipt validator initialized.");
//            }
//            catch (Exception e)
//            {
//                Debug.LogWarning($"[IAP] Validator init skipped/failed : {e.Message}");
//            }
//        }
//#endif
//    }

//    private bool ValidateIfGoogle(string receipt)
//    {
//        if (_crossPlatformValidator == null) return true;

//        try
//        {
//            var result = _crossPlatformValidator.Validate(receipt);
            
//            foreach(var r in result)
//            {
//                Debug.Log($"[IAP] Receipt OK : {r.productID} @ {r.purchaseDate} | Tx : {r.transactionID}");
//            }
//            return true;
//        }
//        catch(IAPSecurityException e)
//        {
//            Debug.LogError($"[IAP] Receipt invalid : {e.Message}");
//            return false;
//        }
//    }
//    #endregion

//    public void ForceResetPurchaseState()
//    {
//        _isPurchaseInProgress = false;
//        Debug.LogWarning("[IAP] 결제 진행 상태가 강제로 초기화 되었습니다.");
//    }

//    #region Content
//    public void PurchaseRealMoneyProduct(string productId)
//    {
//        if (_isPurchaseInProgress)
//        {
//            Debug.LogWarning("[IAP] 이미 결제가 진행 중입니다.");
//            return;
//        }

//        // 상품이 진짜 존재하는지 먼저 검사
//        var product = _storeController?.GetProductById(productId);
//        if (product == null)
//        {
//            Debug.LogWarning($"[IAP] 상품을 찾을 수 없습니다: {productId}");
//            return;
//        }

//        //  1. 결제 시작 시 로딩 팝업 ON!
//        // (구글/애플 결제창이 뜨기 전까지의 짧은 공백을 메워줍니다)
//        Managers.UI.ShowPopupUI<UI_LoadingPopup>();

//        _isPurchaseInProgress = true;
//        _storeController.PurchaseProduct(productId);
//    }
//    #endregion

//    public string GetLocalizedPrice(string productId)
//    {

//        Debug.Log("광고 제거 상품 가져오기");
//        Debug.Log($"{productId}");
//        var product = _storeController?.GetProductById(productId);
//        if(product == null)
//        {
//            Debug.Log("Product null!");
//        }
//        if (product != null)
//        {
//            Debug.Log("Product not null");
//            // 스토어가 주는 "1,500" 같은 문자열을 그대로 반환
//            return product.metadata.localizedPriceString;
//        }
//        return "N/A"; // 아직 로드가 안 됐을 경우
//    }

//    // 결제/복원 성공 시 Economy 인벤토리에 아이템을 넣어주는 공통 함수
//    private void ApplyPurchaseBenefit(string productId)
//    {
//        if (productId == Define.k_IAP_RemoveAd)
//        {
//            // 1. [핵심] 게임 내 광고 송출 시스템 강제 종료 (대표님의 광고 매니저 호출)
//            // 예시: Managers.Ads.SetRemoveAdState(true);
//            Debug.Log("[IAP] 광고 제거 혜택이 게임에 적용되었습니다!");

//            Managers.AD.IsAdsRemoved = true;

//            // UI 보유중으로 바꾸기

//            //// =======================================================
//            ////  4. [서버 동기화] 새로운 익명 계정일 경우를 대비해 서버에 덮어씌우기
//            //// =======================================================
//            //try
//            //{
//            //    //  내 메모리에 들고 있는 플레이어 데이터 원본의 값을 먼저 true로 바꿔줍니다.
//            //    // (※ Managers.PlayerData.PlayerDataLocal 부분은 대표님이 실제로 데이터를 담아두신 변수명으로 맞춰주세요!)
//            //    if (Managers.PlayerData.PlayerDataLocal != null)
//            //    {
//            //        Managers.PlayerData.PlayerDataLocal.IsAdsRemoved = true;

//            //        //  그 덩어리 전체를 "PLAYER_DATA"라는 키 값으로 다시 포장합니다.
//            //        var data = new Dictionary<string, object>
//            //    {
//            //        { "PLAYER_DATA", Managers.PlayerData.PlayerDataLocal }
//            //    };

//            //        // 통째로 서버에 덮어씌웁니다!
//            //        await CloudSaveService.Instance.Data.Player.SaveAsync(data);

//            //        Debug.Log("[UGS] PLAYER_DATA 내부의 광고 제거 상태가 성공적으로 갱신되었습니다!");
//            //    }
//            //}
//            //catch (System.Exception e)
//            //{
//            //    // 네트워크가 끊겨서 저장을 실패해도 괜찮습니다.
//            //    // 어차피 구글 영수증은 폰에 남아있어서 다음 번에 게임을 켤 때 또 복구(ApplyPurchaseBenefit)를 시도하기 때문입니다!
//            //    Debug.LogWarning($"[UGS] 서버 동기화 실패 (다음 접속 시 재시도): {e.Message}");
//            //}
//        }
//    }


//    //  [추가] 서버에 복원을 요청하는 통신 함수
//    private async void RestorePurchaseToServerAsync(Product product, string receipt)
//    {
//        try
//        {
//            Debug.Log($"[IAP] 서버에 {product.definition.id} 복원 및 검증을 요청합니다...");

//            // 1. 서버의 "RestoreRealMoneyPurchase" 함수 호출!
//            PlayerDataResponse response = await _storeServiceBindings.RestoreRealMoneyPurchase(
//                product.definition.id,
//                receipt,
//                (double)product.metadata.localizedPrice,
//                product.metadata.isoCurrencyCode
//            );

//            // 2. 서버가 깐깐하게 검증하고 돌려준 최신 데이터를 내 로컬 메모리에 덮어씌웁니다.
//            Managers.PlayerData.UpdatedPlayerData(response.PlayerData);
//            Managers.PlayerEconomy.HandleEconomyUpdate(response.PlayerEconomyData);

//            // 3. 서버가 IsAdsRemoved를 true로 만들어줬는지 확인하고 클라이언트 광고 시스템을 통제합니다.
//            if (Managers.PlayerData.PlayerDataLocal != null && Managers.PlayerData.PlayerDataLocal.IsAdsRemoved)
//            {
//                Managers.AD.IsAdsRemoved = true;
//                Debug.Log("[IAP] 서버 검증을 통한 광고 제거 복구가 완벽하게 완료되었습니다!");
//                Debug.Log("[IAP] TestRemoved OK");

//                // 상점 UI 새로고침 (필요 시)
//                // Managers.UI.FindPopup<UI_ShopPanel>()?.RefreshUI();
//            }
//            else
//            {
//                Debug.Log("[IAP] TestRemoved Failed");
//            }
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"[IAP] 서버 복원 요청 중 에러 발생 (위조 또는 네트워크 오류): {ex.Message}");
//        }
//    }

//    // 구매 복원 버튼 눌렀을 시
//    public void RestorePurchases()
//    {
//        if (_storeController == null)
//        {
//            Debug.LogError("[IAP] 스토어가 초기화되지 않았습니다.");
//            return;
//        }

//        Debug.Log("[IAP] 구매 복원 시작...");

//        // [핵심] v5 방식: 애플, 안드로이드 구분할 필요 없이 이 한 줄이면 끝납니다!
//        // 이 함수가 실행되면 스토어에서 과거 영수증을 찾아 OnPurchasePending 또는 OnPurchasesFetched 로 던져줍니다.
//        _storeController.FetchPurchases();

//        Debug.Log("[IAP] 스토어에 과거 결제 내역 조회를 요청했습니다.");
//    }

//    // 매니저에 추가할 무료 보상 수령 함수 (비동기)
//    public async Task<bool> ClaimDailyFreeRewardAsync(int amount)
//    {
//        try
//        {
//            // 1. 서버의 ClaimDailyFreeReward 함수로 보낼 매개변수
//            var arguments = new Dictionary<string, object> { { "amount", amount } };

//            // 2. Cloud Code 엔드포인트 호출
//            // 호출 후 서버에서 최종 업데이트된 PlayerEconomyData를 반환받습니다.
//            // 바인딩해서 쓰지않고 바로 호출하기
//            //var updatedEconomy = await CloudCodeService.Instance.CallEndpointAsync<PlayerEconomyData>("ClaimDailyFreeReward", arguments);

//            var response = await _storeServiceBindings.ClaimDailyFreeReward(amount);

//            // 3. 서버에서 받은 최신 지갑 데이터로 로컬 데이터 갱신
//            //  서버가 주는 최신 데이터로 클라이언트 메모리를 완벽하게 동기화!
//            Managers.PlayerData.UpdatedPlayerData(response.PlayerData);
//            Managers.PlayerEconomy.HandleEconomyUpdate(response.PlayerEconomyData);

//            Debug.Log($"[IAP] 일일 무료 보상 {amount} 골드 수령 완료!");
//            return true; // 성공
//        }
//        catch (CloudCodeException e)
//        {
//            // 서버에서 throw new InvalidOperationException("ALREADY_CLAIMED_TODAY")를 던지면 
//            // CloudCodeException으로 잡힙니다.
//            if (e.Message.Contains("ALREADY_CLAIMED_TODAY"))
//            {
//                Debug.LogWarning("[IAP] 오늘은 이미 무료 골드를 받았습니다.");
//            }
//            else
//            {
//                Debug.LogError($"[IAP] 무료 보상 수령 실패: {e.Message}");
//            }
//            return false;
//        }
//        catch (Exception ex)
//        {
//            Debug.LogError($"[IAP] 알 수 없는 에러 발생: {ex.Message}");
//            return false;
//        }
//    }

//}
