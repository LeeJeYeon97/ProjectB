# 현재 아키텍처

기록일: 2026-10-26. [DEVELOPMENT_STATUS](DEVELOPMENT_STATUS.md)의 코드 기준 스냅샷을 설명한다. 의도한 미래 구조는 [GAME_DESIGN](GAME_DESIGN.md)과 구분한다.

## 폴더와 실행 환경

| 위치 | 책임 |
| --- | --- |
| `Assets/01_Scripts/Managers/Core` | 공용 인프라, 초기화, 리소스·풀·씬·UI·저장·퀘스트. 수정 전 사용자 승인 필수 |
| `Assets/01_Scripts/Managers/Content` | 손님·직원·작업·좌석·해금·성장·리뷰·식기·영업 상태 |
| `Assets/01_Scripts/Managers/UGS` | 서비스 관련 과거 코드. 대부분 전체 주석 처리되어 현행 기능으로 사용하지 않음 |
| `Assets/01_Scripts/Content` | 씬 오브젝트의 입력, 이동, 상태, 상호작용, 가구 배치 |
| `Assets/01_Scripts/Data`, `Utils/Quest` | ScriptableObject 설정 및 퀘스트 실행 자료 |
| `Assets/01_Scripts/UI` | UI_Base, UI_Scene, UI_Popup 및 화면·서브 UI |
| `Assets/01_Scripts/Utils` | Define enum, 이동 입력/컨트롤러, Poolable 등 |
| `Assets/02_Scenes`, `03_Prefabs`, `10_Data` | 프로젝트 씬, 게임 프리팹, 레시피·해금·퀘스트 에셋 |
| `Assets/Resources` | GrowthCatalog, FurnitureCatalog, CameraRig 등 Resources 경로 자산 |
| `Assets/AddressableAssetsData` | 사전 로딩 그룹·주소·Preload 라벨 설정 |
| `Assets/09_Plan` | 보존하는 과거 기획 문서. 현재 구현 상태의 기준은 아님 |

Unity 6000.5.10f1, URP 17.5.0, Input System 1.20.0, AI Navigation 2.0.14, Cinemachine 3.1.7, uGUI 2.5.0을 사용한다. Addressables는 직접 manifest 항목이 아니라 lock 파일에 해결된 의존성 2.9.1이다. DOTween도 코드에서 사용한다. 게임 스크립트 전용 asmdef는 확인되지 않으며 주요 타입은 Assembly-CSharp에 있다. 정확한 의존성은 [manifest](../Packages/manifest.json)와 [lock](../Packages/packages-lock.json)을 따른다.

## 매니저와 씬의 생명주기

`Managers`는 MonoBehaviour 싱글턴이며 나머지 주요 매니저를 일반 C# 객체로 생성하는 전역 접근점이다. 명시적 생성자 의존성 주입보다 `Managers.Customer`, `Managers.Task` 같은 정적 접근을 사용한다.

```mermaid
flowchart TD
    Scene[GameScene 및 씬 컴포넌트] --> M[Managers]
    M --> R[ResourceManager / PoolingManager]
    M --> U[UIManager / SceneManagerEx / CameraManager]
    M --> G[GameManager]
    M --> C[CustomerManager]
    M --> S[StationManager]
    M --> E[EmployeeManager]
    M --> T[TaskManager]
    M --> X[UnlockManager / GrowthManager / ReviewManager]
    M --> P[PlayerPrefsManager / QuestManager]
    C --> CC[CustomerController]
    C --> S
    E --> EC[EmployeeController]
    T --> EC
    EC --> I[InteractableBase 하위 시설]
    CC --> I
    I --> T
    X --> P
```

1. 첫 `Managers.Instance` 접근 때 `@Managers`를 찾거나 생성하고, 플레이 중이면 DontDestroyOnLoad로 유지한다. 매니저 필드는 `new`로 다시 구성된다.
2. Core 일부를 초기화하고 `Resource.Init()`가 Preload 비동기 로드를 시작한다. Customer/Employee는 로드 완료 이벤트를 기다릴 수 있다. Growth는 Resources 카탈로그를 읽고 Review는 저장 통계를 읽는다.
3. `Managers.Awake()`는 `Game.OnAwake()`를 호출한다. 이 함수는 이름이 `Player`인 활성 오브젝트를 찾는다. 플레이어 없는 씬의 예외 방어가 없다.
4. `CustomerSceneBinder.Awake()`는 스폰·입구 대기점·퇴장점을 등록한다. `PlayerController.Awake()`는 자신의 Instance, 적재 시스템, 저장 잔액, 성장, 카메라 타깃을 연결한다.
5. `GameScene`은 리소스 로드 후 `Unlock.Init()` → `Customer.RefreshAvailableRecipes()` → 선택적 `Game.OpenShop()` → 게임 UI 생성 순서로 진행한다.
6. `Managers.Update()`가 CustomerManager만 매 프레임 갱신한다. 손님·직원·시설은 각 MonoBehaviour에서 자체 갱신한다.
7. `SceneManagerEx.LoadScene()`은 `Managers.Clear()` 후 enum 이름으로 씬을 비동기 로드한다. Clear가 처리하는 것은 씬·사운드·UI·직원 목록·작업 목록이다. 손님 대기열, 영업 상태, 식기 등 모든 세션 상태를 일괄 초기화하는 구현은 아니다.

`BaseScene.SceneType`은 기본 Unknown이며 파생 씬에서 지정하는 코드가 없다. LoginScene의 Init은 비어 있다. Level1_Pocha는 GameScene 컴포넌트를 사용하지만 Define.Scene과 기본 빌드 목록에는 없다. 생명주기 변경 전에는 직접 레벨 실행과 로그인 경유 실행을 별도로 검증해야 한다.

근거: [Managers](../Assets/01_Scripts/Managers/Core/Managers.cs), [SceneManagerEx](../Assets/01_Scripts/Managers/Core/SceneManagerEx.cs), [GameScene](../Assets/01_Scripts/Scene/GameScene.cs), [GameManager](../Assets/01_Scripts/Managers/Content/GameManager.cs).

## 상속과 인터페이스

```mermaid
classDiagram
    MonoBehaviour <|-- BaseController
    BaseController <|-- PlayerController
    MonoBehaviour <|-- CustomerController
    MonoBehaviour <|-- EmployeeController
    MonoBehaviour <|-- InteractableBase
    InteractableBase <|-- MenuMachine
    InteractableBase <|-- SeatStation
    InteractableBase <|-- Counter
    InteractableBase <|-- Sink
    InteractableBase <|-- UnlockStation
    InteractableBase <|-- CustomerResolve
    MonoBehaviour <|-- UI_Base
    UI_Base <|-- UI_Scene
    UI_Base <|-- UI_Popup
    UI_Scene <|-- UI_GameScene
    UI_Scene <|-- UI_LoginScene
    UI_Popup <|-- UI_MenuPopup
    UI_Base <|-- UI_Bubble
    UI_Base <|-- UI_Quest
    MonoBehaviour <|-- BaseScene
    BaseScene <|-- GameScene
    BaseScene <|-- LoginScene
    MenuMachine --> IIngredientStock : 선택적 연결
```

위 다이어그램의 `IIngredientStock` 연결은 선택 사항이다. 인터페이스는 `HasIngredients(IReadOnlyList<FoodIngredient>)`, `TryUseIngredients(...)`를 정의하지만 현재 구현 클래스와 연결 호출은 없다. 재고를 연결하려면 모든 요구 재료를 한 번에 검사·차감하는 계약을 지켜야 한다.

`IDamageable`, `IStatusTarget`은 선언만 있고 주요 게임 코드에서 구현/사용이 확인되지 않았다. 이를 근거로 전투·상태이상 시스템을 구현 완료로 기록하면 안 된다. `BaseController`도 현재 공통 게임 행동이 없는 골격이다.

## 시스템별 책임과 핵심 API

| 시스템 | 소유하는 상태/책임 | 설계에 필요한 API와 계약 |
| --- | --- | --- |
| [PlayerController](../Assets/01_Scripts/Content/Player/PlayerController.cs) | 잔액, 현재 플레이어, 성장 효과 전달 | `TrySpendMoney(amount)`는 양수·잔액 충족 시 차감. `UpdateMoney`, `SetMoney`는 저장값·이벤트를 갱신. 공개 money 직접 수정은 이 경로를 우회 |
| [StackingSystem](../Assets/01_Scripts/Content/Player/StackingSystem.cs) | 적재 목록·용량·이동 연출 | `CheckListItem`, `CanHoldMenu(recipe)`로 사전 검사 후 `PickUpItem`. `PopItem()`은 맨 위 물건을 제거·반환/파괴. `GetFirstListItemType`은 이름과 달리 맨 위 종류를 읽음 |
| [CustomerManager](../Assets/01_Scripts/Managers/Content/CustomerManager.cs) | 입구 대기열, 생성 타이머, 주문 가능한 레시피 | `SetScenePositions`, `RefreshAvailableRecipes`, `TryGetRandomRecipe`, `OnUpdate`. 씬 참조 준비와 리소스 로딩 필요 |
| [CustomerController](../Assets/01_Scripts/Content/Controller/CustomerController.cs) | 손님 한 명의 이동·주문·식사·방해·결제 상태 | `Init`, `AssignSeat`, `OnFoodServed(ItemType)`, `ResolveDisturbance`, `CompletePayment`. 현재 주문 레시피는 private이고 서빙 API에는 전달되지 않음 |
| [StationManager](../Assets/01_Scripts/Managers/Content/StationManager.cs) | 좌석 등록부, 빈 좌석 탐색 | `RegisterSeat`, `UnregisterSeat`, `FindAvailableSeat`, `RegisterDirtyTable`. AvailableSeatCount는 빈 좌석 수가 아닌 활성 좌석 수 |
| [EmployeeManager](../Assets/01_Scripts/Managers/Content/EmployeeManager.cs) | 직원 생성과 목록 | `HireEmployee(Vector3?)`는 생성 API이며 비용 지불을 하지 않음. `Employees`는 읽기 전용 목록. `Clear`는 목록만 비움 |
| [TaskManager](../Assets/01_Scripts/Managers/Content/TaskManager.cs) | 작업 목록, 중복 방지, 배정·예약 해제 | `CreateTask(type,target)`, `AssignWaitingTasks`, `FinishTask`, `TakeOverTask` 두 오버로드, `RemoveTasksFor`, `RegisterDishWashTask` |
| [EmployeeController](../Assets/01_Scripts/Content/Controller/EmployeeController.cs) | 직원의 NavMesh 이동·실제 작업·개별 성장 | `AssignTask`, `CancelTask`, `StopTaskForPlayer`, `SetMoveUpgrade`, `SetInteractionUpgrade`. 게임 틱은 자체 Update |
| [MenuMachine](../Assets/01_Scripts/Content/InteractObject/MenuMachine.cs) | 레시피 검증·기계 작업자·생산 | `Recipe`, `CanOfferRecipe`, `TryStartMachineWorkForEmployee`, `TryProduceMenu`, `SetIngredientStock`. 공통 예약과 별도 machineWorkingActor가 모두 존재 |
| [SeatStation](../Assets/01_Scripts/Content/InteractObject/SeatStation.cs) | 좌석 상태·손님 참조·서빙·청소 | `SetState`, `IsWaitingToServe`, `ServeForEmployee`, `CleanSeat`, `CleanSeatForEmployee`. OnEnable/OnDisable에서 등록/해제 |
| [Counter](../Assets/01_Scripts/Content/InteractObject/Counter.cs) | 결제 대기열·돈 오브젝트 배치 | `AddWaitingCustomer`, `CollectForEmployee`, `PayPoint`. OnEnable에서 단일 `Managers.Game.Counter`로 등록 |
| [UnlockManager](../Assets/01_Scripts/Managers/Content/UnlockManager.cs) | 씬 발판 사전·해금 ID 집합 | `Init`, `RefreshMapState`, `UnlockNode`. 노드 ID는 `그룹_index`. 영속 저장 함수는 비어 있음 |
| [GrowthManager](../Assets/01_Scripts/Managers/Content/GrowthManager.cs) | 전역 성장 단계·카탈로그·구매 | `TryUpgrade`, `TryUpgradeEmployeeStat`, `GetLevel`, `GetNextPrice`, `GetTotalValue`, `ApplyStartingEmployeeStats`. 직원별 구매는 별도 저장하지 않음 |
| [ReviewManager](../Assets/01_Scripts/Managers/Content/ReviewManager.cs) | 누적 리뷰 수·별점 합계·결제 배율 | `CreateReview`가 통계 저장과 결제 결과 생성까지 수행. 단순 조회 함수가 아님 |
| [FurniturePlacementController](../Assets/01_Scripts/Content/Furniture/FurniturePlacementController.cs) | 구매 재고·입력·검사·미리보기·UI 생성·배치 저장 | `BuyFurniture`, `GetInventoryCount`, `BeginPlacement`, `CancelPlacement`. BeginPlacement의 ID 인수는 현재 즉시 선택에 사용하지 않고 보유 목록을 엶 |

매니저는 목록/전역 규칙을, 컨트롤러는 개별 오브젝트의 실제 동작을 주로 맡는다. 다만 가구 컨트롤러는 UI·구매·저장까지 맡고, CustomerController는 리뷰 입력과 연출까지 가진다. 책임 분리가 완전한 구조로 간주하지 않는다. 한 시스템을 여러 Manager로 무조건 분할하지 말라는 기존 규칙도 유지한다.

## 상호작용과 작업 흐름

`InteractableBase`는 Player 레이어의 플레이어 또는 EmployeeController를 식별한다. 대상의 `CanInteract`가 허용하면 예약 → 시작 → 진행 → 완료를 수행하고 범위 이탈·조건 변경·비활성화 시 정리한다. 플레이어 작업 시간은 `Duration × InteractionTimeScale`이다. 직원 자동 작업은 이 트리거 시간 루프에 전적으로 의존하지 않고 EmployeeController가 대상의 별도 작업 API를 호출한다.

```mermaid
sequenceDiagram
    participant Seat as 좌석/손님/계산대
    participant Task as TaskManager
    participant Worker as EmployeeController
    participant Target as 작업 대상
    Seat->>Task: CreateTask(type,target)
    Task->>Task: 같은 종류와 대상 중복 검사
    Task->>Worker: Idle 및 업무 가능 확인
    Task->>Target: TryReserve(worker)
    Task->>Worker: AssignTask(task)
    Worker->>Target: 이동 후 업무별 API 호출
    Worker->>Task: FinishTask 또는 CancelTask 경유 FinishTask
    Task->>Target: ReleaseReservation
    Task->>Task: 다음 작업 배정
```

플레이어가 가능한 작업에 접근하면 `TakeOverTask`가 직원 작업을 대기로 돌린다. 기계에는 별도의 제조 예약도 있으므로 취소·종료 시 두 예약을 모두 해제해야 한다. 작업 목록 변경 중 재배정 호출이 발생할 수 있다. 대상 비활성화·직원 취소·플레이어 개입을 각각 검증하지 않고 새 업무를 추가하지 않는다.

설거지는 `DishManager.DirtyDishCount > 0`뿐 아니라 직원이 실제 더러운 잔을 들고 있어야 배정된다. 현재 `SeatStation.CleanSeatInternal`의 잔 생성/공용 재고 추가가 블록 주석 안에 있어 주 루프에서 이 조건을 만들지 않는다.

## 주요 상태 전이

```mermaid
stateDiagram-v2
    [*] --> WalkEntrance
    WalkEntrance --> WaitingAtEntrance : 입구 도착
    WaitingAtEntrance --> MovingToSeat : 방해 해결 및 Empty 좌석 예약
    MovingToSeat --> WaitingForFood : 일반 착석
    MovingToSeat --> WaitingToOrder : 착석 소음 손님
    WaitingToOrder --> WaitingForFood : 방해 해결
    WaitingForFood --> Eating : 서빙
    Eating --> OccupyingSeat : 자리 독점 손님
    OccupyingSeat --> MovingToCounter : 방해 해결
    Eating --> MovingToCounter : 일반 식사 종료
    MovingToCounter --> WaitingToPay : 계산대 실제 도착
    WaitingToPay --> Leaving : 결제 완료
    Leaving --> [*] : 퇴장 및 오브젝트 정리
```

- 입구 소음 손님은 WaitingAtEntrance에 남고 `CanLeaveQueue`가 false다.
- 좌석: Empty → Reserved → Occupied → Dirty → Empty. 좌석 해제는 계산대 이동 시작과 연결된다.
- 직원: Idle → MovingToWork → Working → Idle. 서빙 내부 enum `ServingStep`은 None/MakeBeer/DeliverBeer이며 아직 Define.cs 밖에 있다.
- 실패 복구는 상태별로 다르다. 계산대 이동은 경로 재시도가 있지만 입구/좌석/퇴장 이동에는 같은 수준의 제한시간 처리가 없다.

## 이벤트와 데이터 전달

| 발행자/이벤트 | 내용 | 현재 소비 경로 |
| --- | --- | --- |
| ResourceManager.assetLoaded | Preload 처리가 끝남 | GameScene, CustomerManager, EmployeeManager. 콜백 호출 뒤 IsLoaded=true로 설정하는 현재 순서에 주의 |
| PlayerController.OnMoneyChanged | 새 잔액 int | UI_GameScene 구독 및 Clear 해제 |
| QuestManager.OnQuestChanged | 제목·설명·진행·목표·완료 | UI_Quest.Bind(manager)가 구독하도록 구현. 현재 Bind 호출 경로가 확인되지 않음 |
| StationManager.OnSeatCountChanged | 활성 좌석 수 | 주요 게임 코드의 구독처 미확인 |
| GrowthManager.OnChanged | 성장 구매 후 변경 알림 | 주요 게임 코드의 구독처 미확인 |
| ReviewManager.OnReviewChanged | 평균 별점과 리뷰 수 | 주요 게임 코드의 구독처 미확인 |
| DishManager.OnDishCountChanged | 더러운/깨끗한 식기 수 | 주요 게임 코드의 구독처 미확인 |
| FurniturePlacementController.OnInventoryChanged | 가구 재고 변경 | 외부 UI 구독처 미확인 |

중앙 EventManager의 이벤트 버스 구현은 주석 처리되어 있다. 실제 전달은 직접 메서드 호출과 각 객체의 C# event를 사용한다. 이벤트 이름이 존재한다고 화면까지 연결됐다고 판단하지 않는다.

주문은 `FoodRecipeData`를 손님 private 필드에 저장하지만 제조 선택·서빙 결과·퀘스트 보고는 `ItemType.Menu`만 전달한다. 반면 스택은 레시피 참조까지 비교한다. 새 메뉴 설계는 이 불일치를 먼저 고려해야 한다.

재화 흐름은 `Counter → ReviewManager.CreateReview → 돈 ItemController.Value → StackingSystem 획득 연출 → PlayerController.UpdateMoney → PlayerPrefs/OnMoneyChanged`다. 시설 해금은 `UnlockStation 분할 지불 → UnlockManager.UnlockNode → 생성/활성화 → 주문 후보 갱신`이며 완료 시 퀘스트에도 보고한다.

## 데이터와 저장 계약

| 데이터 | 키/식별 방식 | 현재 보장과 제한 |
| --- | --- | --- |
| 플레이어 재화 | `ProjectB.Player.Money` | SetInt는 메모리 값 갱신. 플레이어 Pause/Quit에서 Save 호출 |
| 전역 성장 | `ProjectB.Growth.Save` | JSON version 3, 버전 1~3 읽기. 과거 employee_clean/make를 employee_interact로 옮기고 큰 단계 유지 |
| 리뷰 | `ProjectB.Review.Count`, `ProjectB.Review.StarTotal` | 리뷰 처리마다 명시적 Save |
| 가구 재고 | `ProjectB.Furniture.Inventory` | 공통 JSON, 현재 버전 필드 없음 |
| 가구 배치 | GameScene: `ProjectB.Furniture.Save`; 다른 씬: `ProjectB.Furniture.Save.{sceneName}` | version 1. ID·위치·Quaternion·씬 기존 가구 여부·형제 순번 경로 저장 |
| 해금 및 분할 지불 | unlockedIDs / UnlockStation 내부 필드 | 메모리에만 유지. 영속 키 없음 |
| 고용 목록·직원별 성장·퀘스트 진행·식기 재고·진행 중 손님/작업 | 런타임 객체 | 해당 시스템의 저장/복원 구현 없음 |

[PlayerPrefsManager](../Assets/01_Scripts/Managers/Core/PlayerPrefsManager.cs)의 SetInt/Float/String은 디스크 Save를 직접 호출하지 않는다. SetJson은 Save를 호출한다. 주석보다 구현을 기준으로 판단한다. 여러 시스템 값을 함께 쓰는 원자적 저장·통합 세이브 버전·클라우드 동기화는 없다.

`FoodRecipeData`, `UnlockNodeData`, `GrowthCatalog`, `FurnitureCatalog`, `QuestData/QuestCatalog`는 설정 에셋이다. 런타임 보유 단계나 퀘스트 진행은 별도 객체에 보관한다. 새 ID와 enum 숫자를 변경할 때는 저장 데이터 및 Unity 직렬화 호환성을 먼저 검토해야 한다.

## UI와 리소스

- UI_Base는 enum 이름에 해당하는 자식을 찾아 타입별 배열에 Bind한다. 프리팹 이름과 enum 이름이 계약이다. UI_GameScene은 잔액과 메뉴 버튼, UI_MenuPopup은 Panel_MainMenu 연결까지만 구현되어 있다.
- UIManager는 씬 UI 하나와 팝업 스택을 보관하고 ScreenSpaceCamera Canvas 및 정렬값을 설정한다. 서브 UI는 프리팹을 생성/풀링한다. 프리팹 인스턴스 생성과 프리팹 내부 요소를 코드로 만드는 것은 구분한다.
- UI_Bubble은 주문·진행·상태 표현, 화면 위치 추적, 풀 반환 초기화를 맡는다. UI_Quest는 프리팹 요소와 이벤트 수신을 제공하나 매니저 연결 호출은 빠져 있다.
- 가구 컨트롤러는 UI를 직접 new GameObject로 구성한다. 현재 UI 프리팹 제작 규칙과 충돌하며 씬/주요 프리팹 연결도 확인되지 않았다.
- ResourceManager는 Preload 캐시에서 동기 Load를 제공한다. Resources 카탈로그는 `LoadResource`로 읽는다. 로드 실패를 성공과 구분한 복구 절차와 동시 동일 키 로딩 처리가 부족하다.
- Poolable이 있으면 일반 생성/파괴를 풀 Get/Release로 연결한다. 가구는 Unpooled API를 사용한다. 풀 키가 원본/인스턴스 이름이므로 풀링 객체 이름 변경, OnEnable 이전 상태 복구, 반환 시 이벤트·Tween 정리를 확인해야 한다.

근거: [UI_Base](../Assets/01_Scripts/UI/Base/UI_Base.cs), [UIManager](../Assets/01_Scripts/Managers/Core/UIManager.cs), [UI_Bubble](../Assets/01_Scripts/UI/SubItem/UI_Bubble.cs), [ResourceManager](../Assets/01_Scripts/Managers/Core/ResourceManager.cs), [PoolingManager](../Assets/01_Scripts/Managers/Core/PoolingManager.cs).

## 새 기능을 연결할 위치

| 추가/변경 | 연결 지점 | 반드시 함께 확인할 것 |
| --- | --- | --- |
| 메뉴 | FoodRecipeData, ItemController.MenuRecipe, MenuMachine | 기계 종류·결과 프리팹 역참조, 주문 후보 갱신, 직원 선택, 서빙 레시피, 가격, 퀘스트 매핑 |
| 시설 상호작용 | InteractableBase 하위 클래스와 프리팹 | Player 레이어, Trigger, 예약, Duration, 취소·비활성화, BubbleAnchor |
| 직원 업무 | Define.EmployeeTaskType, TaskManager, EmployeeController | 작업 생성 조건, CanWork, 선점/해제, 경로 실패, 플레이어 개입, 완료 후 재배정 |
| 성장 항목 | GrowthCatalog + GrowthManager + 실제 대상 | 단계 저장 호환성, 누적 효과, 최소 시간, 구매 UI. 새 Core 수정 필요 시 승인 |
| 가구 | FurnitureCatalog + FurniturePlacementController + SeatStation 등 | 미리보기 생명주기, 예약 좌석 이동, 바닥/통로, 안정적인 저장 ID, 현재 UI 진입 연결 |
| UI | UI_Base/Scene/Popup + Define enum + 프리팹 | Bind 이름, 이벤트 구독/해제, 중복 Init, 사전 로딩 주소 |
| 퀘스트 | QuestData/Catalog + 행동 보고 | Menu/노드/가구 매핑, 저장 정책, UI_Quest.Bind. QuestManager는 Core 승인 대상 |

구체적인 결함과 미검증 위험은 [DEVELOPMENT_STATUS](DEVELOPMENT_STATUS.md), 처리 순서는 [BACKLOG](BACKLOG.md)에서 관리한다. 여기서 새 구조로 변경하기로 결정한 것은 아니다.
