# 개발 상태와 확인된 문제

## 기준과 읽는 순서

- 분석/기록일: **2026-10-26 (Asia/Seoul)**.
- 코드 기준 커밋: **`250996380fbf7a43b5a1ec002d15fdeafc56c7f5`** (`2509963`, 문서 구축 커밋). 이번 재고 작업 시작 시 사용자의 폰트 변경이 이미 있었고 보존했다.
- 현재 작업 트리: 재고·시장·퀘스트 변경에 더해 선택 고용 코드, `Assets/10_Data/Employee` 후보 3개, Addressables 라벨, 메뉴 UI 프리팹이 미커밋 상태다. 기존 사용자 변경을 보존했고 커밋·Push하지 않는다.
- 분석 범위: 프로젝트 게임 스크립트 84개가 있는 `Assets/01_Scripts`의 주요 구현·호출 관계, 주요 씬 3개, 게임/UI 프리팹의 연결 검색, 레시피·해금·성장·가구·퀘스트 데이터, 패키지/빌드 설정, 기존 기획 2개. 외부 에셋 전체·실기기·서버까지 전수 검증한 것은 아니다.
- ChatGPT 권장 읽기: 이 문서 → [GAME_DESIGN](GAME_DESIGN.md) → [ARCHITECTURE](ARCHITECTURE.md) → [DECISIONS](DECISIONS.md) → [BACKLOG](BACKLOG.md). 구현 지시 전 [AGENTS](../AGENTS.md)의 보호 규칙도 확인한다.

**코드 구현 상태와 실제 동작 검증 상태는 독립적이다.** 아래의 코드 구현 완료는 표에 명시한 작은 범위의 구현이 존재한다는 뜻이다. 이번에 Unity 재컴파일과 재고 매니저 격리 실행, Play Mode 시작 스모크 점검은 했지만 손님 주문·직원 자동 제조·플레이어 제조의 전체 흐름은 검증하지 않았다. API가 있고 UI 호출이 없으면 사용자 기능 전체는 부분 구현으로 기록한다.

## 시스템별 현재 상태

| 시스템 | 코드 구현 상태 | 씬/UI 연결 상태 | 실제 동작 검증 |
| --- | --- | --- | --- |
| 플레이어 이동·동종 메뉴 적재·잔액 변경 API | 코드 구현 완료(해당 API 범위) | Player 프리팹 및 레벨의 관련 컴포넌트 존재 | 미실시 |
| 공용 트리거 상호작용·대상 예약·진행 UI | 코드 구현 완료(공통 처리 범위) | 시설 하위 클래스 사용 | 다중 작업자/취소 포함 미실시 |
| 손님 생성·대기·좌석·식사·결제·퇴장 | 부분 구현: 재고 부족 손님이 좌석을 점유하고 보충을 기다리도록 연결 | CustomerSceneBinder와 시설 참조 사용 | 전체 순환 미실시; Play Mode 시작 스모크만 확인 |
| 방해 손님·빈손 해결 | 코드 구현 있음: 대기줄/착석 소음, 자리 독점 | CustomerResolve 참조와 콜라이더 필요 | 세 유형 모두 미실시 |
| 레시피 제조·재료 재고 | 코드 구현: 재료 수량 합산·주문 예약·제조 시 일괄 차감·시작 재고 데이터·보충 API | IngredientManager가 Managers에 등록되고 `10_Data` 재료 에셋 세 개를 Addressables 라벨로 로드 | Unity 재컴파일 성공, 우동 차감·부족 거부·중복 합산 격리 실행 확인. Play Mode에서 시작 재고 각 10개 로드 확인. 실제 제조/차감은 미검증 |
| 메뉴 일치·메뉴별 결제액 | 부분 구현: 레시피 보관하지만 type만 전달, Pay 미사용 | 서빙·계산 흐름 연결이 불완전 | 미실시; ISS-003 |
| 직원 고용·업무 배정·자동 실행 | 부분 구현: `EmployeeData` 후보 3개와 선택 고용·후보별 업무/능력치·중복 방지·유료 시 결제 API 존재. 기존 직접 생성 API는 비용 없음. 자동 작업은 별도 미완성 | `UI_MenuPopup`의 사전 제작 고용 패널·세 카드가 `TryHire`에 연결됨. 현재 후보 가격은 모두 0 | Unity 재컴파일 및 프리팹에 세 후보 데이터 적용 격리 검증 성공. 실제 화면·고용·업무 실행은 미검증; ISS-004/008 |
| 좌석 청소 | 코드 구현 완료(Dirty → Empty) | 플레이어/직원 경로 존재 | 미실시 |
| 식기·설거지 | 부분 구현/주 루프 중단 | 청소 후 더러운 잔 생성이 주석 처리됨 | 미실시 |
| 시설 해금 | 부분 구현: 분할 지불·선행 조건·생성/활성화 | 씬 발판 사용, 저장 없음 | 미실시; ISS-005 |
| 플레이어 성장 | 코드 구현 완료(구매·효과·저장 API 범위) | 현재 주요 UI에서 구매 호출 미확인 | 구매/재실행 미실시 |
| 직원별 성장 | 부분 구현: 개별 효과·구매 있음, 영속 저장 없음 | 현재 주요 UI에서 구매 호출 미확인 | 미실시 |
| 리뷰 | 코드 구현 완료(계산·통계 저장·결제 반영 범위) | Counter가 사용, 표시 UI 구독 미확인 | 입력 메뉴 판정 결함과 별개로 계산 테스트도 미실시 |
| 가구 구매·배치·이동·복원 | 부분 구현: 관련 코드 존재 | 주요 씬/프리팹에 컨트롤러 GUID 연결과 외부 호출 미확인 | 미실시; 기존 설계와 차이 있음 |
| 퀘스트 | 부분 구현: 순차 목표·보상·UI 진행바/완료/보상 연출과 대기 상태 존재 | UI_Quest가 Init에서 이벤트 구독; 메뉴 제작 대상 매핑 누락 | Unity 컴파일 및 QuestManager 완료 대기·중복 방지·UI 부재 흐름 격리 확인. 실제 UI 연출/게임 플레이 미검증; ISS-006 |
| UI 프레임워크·잔액·메뉴 팝업 | 코드 구현: 메뉴/시장/고용 패널 전환, 재료 테스트 구매, 직원 후보 정보와 선택 고용 | UI_MenuPopup 프리팹에 시장·고용 카드와 버튼 사전 배치 | 시장의 기존 Play Mode 결과만 확인. 고용 패널은 정적 연결·컴파일 검증, 실제 클릭/표시 미검증 |
| 시작/씬 전환 | 부분 구현 | LoginScene 진입 흐름 누락, Level1_Pocha 기본 빌드 목록에 없음 | 빌드 시작 미실시; ISS-001 |
| 로컬 저장 | 부분 구현: 재화·전역 성장·리뷰·가구 | 시스템별 키로 분산 | 실제 종료/재시작 복구 미실시 |

## 미구현 또는 현재 실행 경로가 없는 기능

- 해금 완료/분할 지불, 고용 직원 목록/직원별 성장, 퀘스트 진행의 영속 저장.
- 통합 세이브의 일관된 버전/마이그레이션, 여러 시스템 구매 결과의 원자적 저장.
- 재료 구매량·가격 밸런스, 대량 구매 UI, 재고 저장/복원. 현재 시장 패널은 세 재료를 1개씩 구매하는 테스트 범위다.
- 기계별 업그레이드 구매/저장. 과거 UI_MachineUpgrade 및 BeerMachine 클래스는 현행 커밋에서 삭제됐다.
- 가구 전용 금지 영역·통로 연결성 검증·범용 철거/판매 기능.
- 서비스 운영 기능: UGS 폴더의 Ads/IAP/Leaderboard/PlayerData/PlayerEconomy/RemoteConfig/VirtualStore는 전체 주석 처리된 코드이며 UGSLoginManager도 실질 구현이 없다. 활성 Core LoginManager/WebServerManager 코드는 별도로 존재하지만 로그인 시작 흐름·서버 응답은 검증되지 않았다.
- 프로젝트가 관리하는 정형 EditMode/PlayMode 회귀 테스트 스위트. `Assets/Editor/PlayModeTestRunner.cs`는 임시 테스트 실행 도구이고 통과한 회귀 테스트 목록의 증거가 아니다.

## 정적 코드와 데이터에서 확인한 문제

아래 문제는 **런타임 재현 완료 목록이 아니다**. 확정 사실은 코드·직렬화 자료에서 확인했으며 실제 영향은 조건과 함께 기술한다. 우선순위는 Codex 제안이고 개발 승인을 뜻하지 않는다.

### ISS-001 — 기본 시작 흐름과 레벨 연결 누락 / P0

기본 빌드 목록은 LoginScene → GameScene이다. LoginScene.Init은 비어 있고 해당 씬에 UI_LoginScene 배치가 없으며, ResourceManager의 로그인 UI 표시 호출은 주석이다. UI_LoginScene.StartGame은 GameScene을 로드한다. Level1_Pocha는 목록/Define.Scene에 없고 `openOnStart = 1`, GameScene은 `0`이다. 따라서 기본 시작 씬에서 버튼까지 도달할 코드 경로가 빠져 있고, 레벨 직접 Play와 빌드 시작 결과를 동일하게 볼 수 없다. 실제 사용할 시작/레벨 정책은 사용자 확인 필요다.

근거: [빌드 목록](../ProjectSettings/EditorBuildSettings.asset), [LoginScene](../Assets/01_Scripts/Scene/LoginScene.cs), [UI_LoginScene](../Assets/01_Scripts/UI/Scene/UI_LoginScene.cs), [리소스 초기화](../Assets/01_Scripts/Managers/Core/ResourceManager.cs).

### ISS-002 — 런타임 코드의 에디터 의존 / P0

[CustomerSceneBinder](../Assets/01_Scripts/Content/CustomerSceneBinder.cs)는 `using UnityEditor;`가 조건부 컴파일 밖에 있다. Handles 사용부만 UNITY_EDITOR로 감싸져 있다. 플레이어 컴파일에서 에디터 네임스페이스 참조 문제가 예상된다. [Managers](../Assets/01_Scripts/Managers/Core/Managers.cs)의 NUnit.Framework.Internal import도 테스트 어셈블리 의존 여부를 확인해야 한다. 에디터 오류 0개는 플레이어 빌드 성공 증거가 아니다.

### ISS-003 — 메뉴 식별과 가격 정보의 전달 단절 / P1

[CustomerController](../Assets/01_Scripts/Content/Controller/CustomerController.cs)는 주문 recipe를 보관하지만 OnFoodServed는 ItemType만 비교한다. 메뉴가 모두 Menu이므로 맥주/우동을 구별하지 못한다. [SeatStation](../Assets/01_Scripts/Content/InteractObject/SeatStation.cs)의 IsMenuItem은 항상 true로, 잔 같은 비메뉴도 통과할 수 있다. 직원 기계 탐색도 ItemType만 받아 주문 recipe와 다를 수 있다. 결제는 `payAmount`를 쓰며 `FoodRecipeData.Pay` 읽기 경로가 없다. 잘못된 메뉴 허용/감점 및 가격 기준은 설계 확정이 필요하다.

### ISS-004 — 빈손 직원이 제조를 건너뜀 / 코드 수정, 플레이 검증 전

기준 커밋에서는 EmployeeController.IsMenuItem이 항상 true여서 빈손이 제조 분기를 건너뛰었다. 이번 변경에서 메뉴 타입만 메뉴로 인식하게 고치고, 손님 예약과 주문 레시피가 같은 기계를 선택하도록 수정했다. 직원은 주문 예약분으로 제조 후 서빙한다. SeatStation도 비메뉴 아이템을 거부한다. Unity 재컴파일은 성공했으며 실제 직원 자동 제조·서빙 검증은 하지 않았다.

### ISS-005 — 해금 지불과 영속 상태 불일치 / P1

[UnlockManager](../Assets/01_Scripts/Managers/Content/UnlockManager.cs)의 LoadSaveData/SaveData는 주석 예시뿐이다. [UnlockStation](../Assets/01_Scripts/Content/InteractObject/UnlockStation.cs)은 재화를 차감하지만 누적 지불은 자체 필드에만 저장하고 Setup에서 초기화한다. 재화는 다른 저장 호출이나 종료 시 기록될 수 있어 재실행 후 잔액만 줄고 해금/중간 지불은 사라질 위험이 있다.

### ISS-006 — 메뉴 제조 퀘스트 진행 누락 / P1

[QuestManager](../Assets/01_Scripts/Managers/Core/QuestManager.cs)의 GetItemTargetType은 Money/Beer_Empty/Beer_Dirty만 처리한다. Menu는 Unknown이고 ReportProgress에서 무시된다. [Quest_MakeBeer](../Assets/10_Data/Quest/Quest_MakeBeer.asset)는 Beer 목표를 요구하므로 현행 메뉴 생성 보고로 완료되지 않는다. 이 수정은 Core 승인 대상이다.

### ISS-007 — 퀘스트 UI의 매니저 연결 호출 누락 / 코드 해소, 화면 미검증

[UI_Quest](../Assets/01_Scripts/UI/SubItem/UI_Quest.cs)는 현재 Init/OnEnable에서 QuestManager 이벤트를 직접 구독하고 비활성화·파괴 시 해제한다. 이전의 Bind 호출 누락 설명은 코드 변경으로 유효하지 않다. UI_GameScene 프리팹에 UI_Quest 인스턴스는 있으나 진행바·완료·보상 연출은 실제 Play Mode 화면에서 확인하지 않았다.

### ISS-008 — 성장·고용·가구 API와 사용자 진입점의 공백 / P1

`UI_MenuPopup`에서 후보별 `TryHire` 호출과 사전 제작 고용 패널 연결이 추가됐다. 세 후보는 현재 무료 테스트 값이며 실제 고용 화면·버튼 동작은 Play Mode에서 확인하지 못했다. 기존 `HireEmployee` 생성 API는 여전히 비용을 차감하지 않고, `HireCost` 필드는 선택 고용 가격으로 사용되지 않는다. `TryUpgrade`, `TryUpgradeEmployeeStat`, `BuyFurniture`, `BeginPlacement`의 사용자 UI 호출은 확인되지 않았다. 주요 씬/게임 프리팹에서 가구 컨트롤러 연결도 확인하지 못했다. 성장·가구 진입점과 선택 고용 실동작 검증이 남았다.

### ISS-009 — 삭제 스크립트와 레시피 데이터 잔재 / P1

[GameScene](../Assets/02_Scenes/GameScene.unity)은 삭제된 StreetMapGenerator의 m_Script GUID `53e09270b8242e84ea0ffc36fb4d05c7`를 유지한다. [Recipe_ Kimbap](<../Assets/10_Data/FoodRecipes/Recipe_ Kimbap.asset>)은 파일명·FoodName·기계 종류·프리팹이 서로 맞지 않는다. 모든 외부 에셋 참조를 전수 검사한 것은 아니며 PackageCache에 있는 스크립트를 Assets 검색 실패만으로 Missing Script로 판정하면 안 된다.

### 재고 시스템 구현

`IngredientManager`가 재료별 매장 수량과 손님 주문 예약을 관리한다. 재료 필요량을 종류별로 합산하고 모든 재료가 충분할 때만 차감한다. 플레이어는 주문 예약을 우선 사용하고 직원은 해당 손님의 주문 레시피를 만들어 예약분을 소비한다. 제조물 생성 실패는 재고 차감 전에 처리하며, 차감 실패 시 생성물을 파괴한다. 재고가 충분한 레시피가 없으면 손님은 주문/서빙 작업을 만들지 않고 좌석을 계속 차지한다. `AddIngredients` 보충 이벤트를 받으면 대기 중인 손님이 주문을 다시 시도한다. 직원 제조 작업이 취소되면 좌석과 주문 예약을 보존하고 서빙 작업을 다시 등록한다.

`Assets/10_Data/FoodIngredient`의 재료 에셋 세 개는 물·면·소스 시작 수량을 각 10개로 설정한다. 각 에셋에는 종류·이름·가격·프리팹 필드도 있으며 가격은 현재 구매/판매 계산에 사용되지 않는다. `Recipe_Udon`은 각 1개를 요구하며 맥주 레시피는 재료 목록이 비어 있다. 손님이 재고를 기다리는 동안 자리를 계속 점유하는 규칙은 사용자 확정이다. 시작 수량과 가격 밸런스는 미확정이고 재고 저장 및 보급 UI는 없다.

`IIngredientStock`과 `MenuMachine.SetIngredientStock`을 제거했다. 프로젝트 검색에서 남은 사용처는 없었다. `Managers`에는 재고 매니저 등록·접근·초기화만 추가했다.

## 구조적 위험과 기술 부채

| ID | 코드 근거와 영향 | 확인 수준 |
| --- | --- | --- |
| RISK-001 | Managers는 영속인데 Game.Player는 이름 탐색으로 연결하고 Clear는 모든 콘텐츠 상태를 정리하지 않음. CustomerManager 대기열 앞에 파괴된 참조가 있으면 매칭이 return함. 씬 재진입/플레이어 없는 씬에서 수명 충돌 가능 | 정적 위험, 재현 미실시 |
| RISK-002 | ResourceManager는 assetLoaded 호출 후 IsLoaded=true, Clear 후 로드 플래그 재설정 없음, 동시 동일 키 로드 시 Dictionary.Add 중복 가능, 리소스 위치 조회 실패 방어 부족 | 정적 위험, 로드 실패/중복 요청 시험 미실시 |
| RISK-003 | FurniturePlacementController가 활성 프리팹을 복제한 뒤 컴포넌트를 꺼서 미리보기의 OnEnable 등록이 먼저 발생 가능. 기존 좌석 이동은 렌더러만 숨겨 이동 중 새 손님 예약을 막지 않음 | 정적 위험, 컨트롤러 연결 후 재현 필요 |
| RISK-004 | 가구 기존 오브젝트 식별이 형제 순번 경로라 씬 계층 변경에 취약. 바닥 네 모서리 검사로 전체 바닥 연속성과 통로를 보장하지 못함. 명시적인 금지 영역 없음 | 코드 제한 확인 |
| RISK-005 | 가구 컨트롤러 1,271줄이 입력·구매·UI 생성·저장을 함께 수행. 손님 771줄, 직원 840줄. 전역 Managers 접근과 public 변경 가능 필드로 상태 변경 지점이 넓음 | 책임 집중 확인; 줄 수는 기준 커밋 값 |
| RISK-006 | 런타임 가구 UI 생성, 변수/Unity 함수/사용자 함수 순서와 region의 불일치, 일부 읽을 수 없는 주석 존재 | 기존 코딩 규칙과 차이. 이번 작업에서 수정하지 않음. UI 요소 enum은 해당 UI 스크립트에 두는 현행 규칙 적용 |
| RISK-007 | 메뉴 기계 전체 탐색, 소음 손님의 전체 손님 탐색, 좌석 Count의 FindAll 할당, 가구 RaycastAll/OverlapBox 반복 | 최적화 후보. 성능 문제로 측정된 것은 아님 |
| RISK-008 | 재화/성장/리뷰/가구 저장 시점과 버전이 분산. 퀘스트는 재시작 시 초기화되고 보상 잔액은 저장되므로 반복 보상 정책 확인 필요 | 저장 구조 확인, 정책 미확정 |

## 최근 주요 변경

`git diff-tree`로 확인한 b4fd92b와 직전 커밋의 파일 차이를 기준으로 한다. 커밋 메시지가 포괄적이어서 각 변경의 승인 배경은 알 수 없다.

- BeerMachine/Fridge/UnlockAnchor/StreetMapGenerator 및 여러 과거 UI 스크립트 삭제.
- FoodRecipeData·MenuMachine·ItemController·StackingSystem·주문/퀘스트 연결 변경, IIngredientStock 추가.
- ScreenMoveInput, UI_MenuPopup, UI_ClosePopup 추가 및 이동/UI 변경.
- GameScene/Level1_Pocha, 해금 데이터와 게임 프리팹 변경.
- 이번 문서 구축은 기존 설계 자료를 보존하고 현행 구현/연결/검증 차이를 처음으로 통합 기록했다.
- 이후 사용자 요청에 따라 매장 재료 재고, 주문 예약과 제조 차감, 재고 부족 손님의 좌석 점유/보충 대기를 추가했다. 상세 변경은 아래 검증 기록과 관련 문서 항목을 따른다.

## 이번 검증 결과

2026-10-26 약 19:11~19:14 KST에 다음을 수행했다. 셸 CLI와 연결된 Pipeline 도구의 결과는 다르게 나왔다.

| 수행 항목 | 실제 결과 | 해석 |
| --- | --- | --- |
| Git HEAD/status, 파일·호출부·YAML·기획 문서 읽기 | 완료 | 위 SHA의 정적 분석 근거 |
| `unity --version` | 1.0.0-beta.12 | 설치된 버전 |
| `unity self-update --check --format json` | public-cdn.cloud.unity3d.com DNS 실패. beta.14 업데이트 알림 및 승인 필요 표시 | 최신 버전 확인 요청 실패. 업데이트 실행 안 함 |
| `unity status --project-path <ProjectB 절대 경로> --format json` | STATUS_NO_INSTANCES, 실행 중 에디터가 보고서에 없을 수 있다는 경고 | 셸의 탐색 한계. 에디터 미실행/컴파일 오류로 단정하지 않음 |
| 연결 도구 `mcp__unity__editor_status` | ProjectB 경로, Unity 6000.5.10f1, ready, compiling=false, playMode=stopped | 해당 에디터 연결 성공 |
| `mcp__unity_mcp__Unity_Recompile` | 스크립트 재컴파일 완료, failed=false, errors=[] | 재고 시스템 변경 후 에디터 스크립트 컴파일 성공 |
| 재고 매니저 격리 실행 | 우동 재료 3종 차감, 부족 재료 시 전량 차감 거부, 중복 재료 합산 확인 | 코드 경로 실행 확인. 씬·UI·직원 행동 전체 검증은 아님 |
| `mcp__unity_mcp__Unity_Console` | 현재 콘솔 오류 0, 경고 11 | 수집 시점 오류 없음 |

경고에는 FindObjectsSortMode/기존 FindObjectsByType 오버로드의 CS0618, LoginManager의 CS4014, 기존 Input Manager 사용 경고가 있었다. 도구 연결 관련 서명 경고는 게임 로직 오류와 구분한다.

**미실시:** 플레이어 빌드, EditMode/PlayMode 자동 테스트, 정상 손님 전체 루프, 직원 자동 제조·서빙, 잘못된 메뉴 처리, 씬 재진입, 저장·복원, 모바일 입력, 성능 프로파일링, 서버/광고/IAP 실연동. 짧은 Play Mode 시작 확인은 전체 게임 플레이 검증에 해당하지 않는다.

**선택 고용 변경 확인 (2026-10-26):** Unity 재컴파일은 `failed=false`, `errors=[]`, `compilationFailed=false`였다. 열린 씬의 미저장 상태를 유지한 채 직원 프리팹을 격리 로드하고 세 후보 데이터 각각의 ID, 업무 목록, 작업 시간 배율 적용을 확인했다. 세 후보 에셋의 Addressables `EmployeeData` 라벨과 고용 패널의 버튼·텍스트 구성도 확인했다. 실제 Play Mode에서 카드 표시, 클릭 고용, 결제, 업무 실행, 재실행 시 상태는 확인하지 않았다.

**재고 변경 후 확인 (2026-10-26):** 이전 구조에서 Unity 에디터 재컴파일은 `failed=false`, `errors=[]`로 완료됐다. `IngredientManager` 격리 실행에서 우동 재료 정상 차감, 한 재료 부족 시 나머지도 차감하지 않는 동작, 중복 재료 합산, 재고 보충 이벤트를 확인했다. Play Mode를 잠시 시작해 게임 화면과 콘솔 오류 0건을 확인하고 종료했다. 이후 사용자가 재료 데이터 구조를 바꾸었고, 재료 세 에셋을 `10_Data` 및 Addressables `IngredientData` 라벨로 연결했다. 이 변경의 별도 검증 결과는 아래에 기록한다. 재고 부족 손님의 자리 점유, 보충 후 주문, 직원/플레이어 제조는 직접 확인하지 않았다.

**재료 데이터 위치 변경 확인 (2026-10-26):** `IngredientCatalog`의 사용자 추가 필드 `Type`, `Name`, `Price`, `Prefab`을 유지하고 시작 재고를 `StartingAmount`로 각 재료 에셋에 저장했다. 세 에셋 모두 `Assets/10_Data/FoodIngredient`에 있으며 Addressables `Data` 그룹의 `IngredientData` 라벨과 주소를 에디터에서 확인했다. 재컴파일 결과는 `failed=false`, `errors=[]`였다. Play Mode에서 `IngredientManager.GetAmount`가 물·우동 면·우동 소스 각 10개를 반환했고, 종료 후 현재 콘솔 오류 0건이었다. 실제 주문·제조는 아직 확인하지 않았다.

**시장 UI 확인 (2026-10-26):** `UI_MenuPopup` 프리팹의 메인 패널은 켜짐, 시장 패널은 꺼짐으로 저장됐다. 시장 패널에는 물·우동 면·우동 소스의 정보 글자와 구매 버튼, 돌아가기 버튼이 미리 배치돼 있다. Unity 스크립트 재컴파일 상태는 `up_to_date`, `failed=false`, `errors=[]`이며 현재 콘솔 오류는 0건이었다. Play Mode에서 팝업 버튼 이벤트를 호출해 메인→시장, 물 가격 0의 무료 구매로 수량 10→11, 시장→메인 전환을 확인했다. 이 검증은 프로그램에서 버튼 이벤트를 호출한 것으로 실제 마우스/터치 조작, 양수 가격의 잔액 차감, 저장 후 재실행은 검증하지 않았다.

**UI enum 위치 수정 (2026-10-26):** 시장 UI의 GameObjects/Buttons/Texts enum을 Define.cs에서 UI_MenuPopup.cs로 옮겼다. 이전 문단의 Play Mode 검증은 이동 전 결과다. 이동 후 참조 검색과 `git diff --check`는 통과했으며, 이후 퀘스트 연출 변경 시점의 Unity 재컴파일도 완료·오류 0건이었다. 이동 후 시장 UI의 Play Mode 재검증은 하지 않았다.

**퀘스트 연출 대기 (2026-10-26):** QuestManager는 목표 달성 시 완료·보상을 확정하고 UI_Quest가 진행바 채우기(기본 0.5초), 완료 문구(0.8초), 실제 지급 보상 문구(1초)를 마칠 때까지 다음 퀘스트 시작을 보류한다. UI가 없거나 연출 도중 숨겨지면 바로 진행한다. Unity 재컴파일 `completed`, `failed=false`, `errors=[]`; 현재 콘솔 오류 0건. Unity 에디터 격리 실행에서 완료 대기, 중복 보고 무시, 연출 종료 후 한 번만 전환, UI 부재 시 즉시 전환을 확인했다. 실제 화면 연출 및 재실행 후 상태 복원은 미검증이다.

최초 문서 구축 시 문서 로컬 링크 92개, 이슈/작업/결정 ID, 코드 블록·문자 깨짐·행 끝 공백을 검사했고 이상이 없었다. 기존 AGENTS의 코딩 규칙 14개는 원문과 일치했다. 이번 재고 변경 뒤에는 문서 내용과 작업 ID를 다시 대조했으며, `git diff --check`에서 공백 오류는 없었다. Mermaid는 소스 구조만 검토했으며 별도 렌더러로 시각 검증하지 않았다. 이는 게임 코드 검증과 별개의 문서 품질 검사다.

## 아직 확인하지 못한 정보

- 최종 시작 씬, 플랫폼별 실제 빌드 프로파일, 출시 대상과 목표 성능.
- 잘못된 메뉴, 음식 가격, 직원 성장/비용, 식기 재사용, 가구 지불 시점의 최종 기획 승인.
- 사용자 PC에 저장된 실제 PlayerPrefs 내용 및 과거 버전 저장 호환성.
- 에디터의 미저장 씬 상태와 저장된 YAML의 차이, 외부 에셋 전체 참조 건전성.
- 기존 기획 문서의 작성/승인 날짜, 코드와 달라진 결정의 이유.
- 과거 테스트 도구 실행의 유효한 결과와 실제 기기 플레이 결과.

다음 작업은 [BACKLOG](BACKLOG.md)의 제안 우선순위를 참고해 사용자 범위 승인 후 진행한다.
