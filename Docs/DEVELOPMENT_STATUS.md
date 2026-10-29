# 개발 상태와 확인된 문제

## 기준과 읽는 순서

- 분석/기록일: **2026-10-27 (Asia/Seoul)**.
- 2026-10-28 해금 발판 편집 기능: 기준 SHA `7cf8ca596a2f6583645133ac383765d2d20e0c0c` + 기존 작업 트리 변경에 `UnlockStation.cs`의 Scene 뷰 메시 미리보기를 추가했다. SpawnPrefab은 데이터의 프리팹을 하늘색으로, ToggleActive는 연결된 씬 오브젝트를 주황색으로 표시한다. Unity MCP로 프로젝트 경로와 해금 프리팹 7개의 MeshFilter를 확인했다. 새 컴파일 완료와 Scene 뷰의 실제 표시·위치는 미검증이다. Unity CLI는 1.0.0-beta.12이며 beta.14 업데이트는 승인 대기 중이다.
- 2026-10-28 생성 위치 수정: 같은 기준 SHA와 작업 트리에서 `UnlockStation`은 축소 연출 전에 생성 위치와 회전을 보관한다. 자식 `targetSpawnPoint`가 축소로 발판 위치에 모여도 원래 위치를 사용한다. 연결된 ProjectB Unity 6000.5.10f1에서 Pipeline `recompile`은 완료, 오류 0건이다. 실제 해금 플레이와 Scene 미리보기 표시는 미검증이다. CLI 1.0.0-beta.12는 업데이트하지 않았다.
- 2026-10-28 해금 데이터 참조 전환: 기준 SHA `7cf8ca596a2f6583645133ac383765d2d20e0c0c` + 기존 작업 트리에서 `UnlockNodeData`, 발판, `UnlockManager`, 사용자 승인된 Core `QuestManager`, 퀘스트 데이터와 `Level1_Pocha` 첫 발판을 변경했다. 데이터별 해금·선행 조건·생성물 참조를 사용하고 첫 구역 개점 설정 및 두 해금 퀘스트 목표를 연결했다. `Level1_Pocha`의 발판 6개는 데이터 중복과 선행 발판 누락이 없었다. CLI 1.0.0-beta.12를 업데이트하지 않았고 Unity Pipeline `recompile` 초기 두 번은 SceneHandle 형 오류, 수정 후 완료·오류 0건이었다. Pipeline 격리 실행으로 3단계 발판 표시, 퀘스트 대상·모든 대상 및 생성물 중복 방지를 확인했다. 퀘스트 에셋 저장 호출은 시간 초과를 반환했지만 디스크의 두 에셋에서 목표 연결과 설정을 확인했다. 실제 지불·생성 연출·개점·퀘스트 플레이 및 빌드는 미검증이다. `GameScene`에는 같은 좌석 데이터를 여러 발판이 공유하고 이전 필드만 가진 레벨 데이터가 남아 있어 새 구조 적용을 검토해야 한다. 열린 씬은 작업 후 dirty 상태로 관찰되어 추가 저장하지 않았다.
- 코드 기준 커밋: **`7cf8ca596a2f6583645133ac383765d2d20e0c0c`** (`7cf8ca5`, 작업 시작 시 깨끗한 main).
- 현재 작업 트리: 기준 SHA + 이전 DEV-003 코드·손님 프리팹 변경, DEV-007 플레이어 성장 UI 코드·프리팹·Docs 변경, 성장 카탈로그의 Addressables 이전, `ThirdPersonController`의 프로젝트 전용 참조 분리, 재고 부족 시 주문 우선 확정 변경, `UI_Bubble` 크기 초기화와 풀 반환 방식 변경. 기존 미커밋 변경은 보존했다. 커밋·Push하지 않았다.
- 분석 범위: 프로젝트 게임 스크립트 84개가 있는 `Assets/01_Scripts`의 주요 구현·호출 관계, 주요 씬 3개, 게임/UI 프리팹의 연결 검색, 레시피·해금·성장·가구·퀘스트 데이터, 패키지/빌드 설정, 기존 기획 2개. 외부 에셋 전체·실기기·서버까지 전수 검증한 것은 아니다.
- ChatGPT 권장 읽기: 이 문서 → [GAME_DESIGN](GAME_DESIGN.md) → [ARCHITECTURE](ARCHITECTURE.md) → [DECISIONS](DECISIONS.md) → [BACKLOG](BACKLOG.md). 구현 지시 전 [AGENTS](../AGENTS.md)의 보호 규칙도 확인한다.

**코드 구현 상태와 실제 동작 검증 상태는 독립적이다.** 아래의 코드 구현 완료는 표에 명시한 작은 범위의 구현이 존재한다는 뜻이다. 이전 작업에서 Unity 재컴파일과 재고 매니저 격리 실행, Play Mode 시작 스모크 점검은 했지만 손님 주문·직원 자동 제조·플레이어 제조의 전체 흐름은 검증하지 않았다. DEV-003의 새 컴파일 결과는 아래에 따로 기록한다. API가 있고 UI 호출이 없으면 사용자 기능 전체는 부분 구현으로 기록한다.

## 시스템별 현재 상태

2026-10-29 DEV-024 배송 대기열: 사용자 선택에 따라 배송 영역에 최대 12개만 표시하고 나머지는 비활성 물건으로 대기시킨다. 기존 표시 위치를 유지하며 빈자리에 순서대로 표시한다. 재컴파일 성공. Play Mode에서 프레임 진행 후 16개 배송 시 12개 표시, 한 개를 집은 뒤 다음 물건 표시·기존 위치 유지, 만재 반환 물품의 대기 및 재표시를 확인했다. 시장 팝업 버튼 이벤트 호출로 배송 수량 증가·재고 불변과 만재 시 구매 버튼 비활성을 확인했다. 임시 가격 7 및 임시 잔액 100에서 버튼 호출 시 7 차감과 배송 생성, 입고 전 재고 0을 확인하고 가격과 잔액을 원복했다. 음식과 재료 양방향 혼합 차단, 예약된 재료 중복 소비 거부와 예약 차감도 확인했다. 실제 마우스/터치와 플레이어 이동 입고는 미검증이다. 기존 카드의 글자 높이가 넘쳐 세 줄로 수정했고 수정 전 임시 문구의 선호 높이 167.56이 카드 높이 205 이내임을 확인했다. 재화·리뷰·성장 PlayerPrefs는 검증 전 백업 후 복원하고 일치함을 확인했다. 기존 사용자 씬의 미저장 상태는 유지했다.

2026-10-29 배송·창고 DEV-024: 기준 SHA `7cf8ca596a2f6583645133ac383765d2d20e0c0c` + 기존 작업 트리에서 IngredientManager, Storage, DeliveryZone, 재료 아이템·프리팹, 스택, 시장 UI, 직원 업무와 씬 연결을 변경했다. 활성 창고가 있을 때 기본 합산 용량 15, 세 재료 시작 수량 0, 시장 구매 후 즉시 배송, 혼합 재료 운반, 플레이어·직원 입고와 가득 찰 때 반환을 구현했다. 추가 창고 용량 기본값은 사용자 결정 전 0이며 직원 후보 셋에 운반 업무를 연결했다. ProjectB Unity 6000.5.10f1 Pipeline 재컴파일 완료·오류 0. Play Mode에서 프레임 진행을 확인하고 배송 직후 재고 0, 물·면 혼합 및 직접 입고, 용량 15와 주문/입고 거부, 직접 반환을 확인했다. 무료 직원 한 명의 배송 지점→창고 실제 이동과 입고, 이동 중 만재 시 배송 지점까지 돌아가 물품을 반환한 것을 확인했다. 트리거 후보가 겹쳐 반환 물품 재회수가 막히던 문제를 수정하고 재회수를 확인했다. 재화·리뷰·성장 PlayerPrefs는 검증 전 백업 후 복원했다. 시장 실제 버튼·유료 결제, 실제 마우스 입력, 플레이어 직접 이동 입고, 여러 직원 동시성, 모든 취소/씬 전환, 기존 주문 예약 및 재시작은 미검증이다. Pipeline eval 및 재진입에서 일시 시간 초과/HTTP 오류 후 연결에 성공했다. CLI 1.0.0-beta.12는 업데이트하지 않았다. 열린 씬의 미저장 변경은 추가 저장하지 않았다.

2026-10-29 배송 지점 범위 표시: 기준 SHA `7cf8ca596a2f6583645133ac383765d2d20e0c0c` + 기존 작업 트리의 `Level1_Pocha`에서 기존 DeliveryZone BoxCollider의 가로·세로 2.5×1.5에 맞춰 반투명 바닥과 테두리를 추가했다. 기존 휴지통 표시 재질을 재사용하고 시각 요소에는 콜라이더를 추가하지 않았다. ProjectB Unity 6000.5.10f1 Pipeline eval로 표시·기존 콜라이더 크기를 확인했다. 기존 씬은 수정 전 저장 상태였고 변경 후 씬을 저장했다. 실제 게임 화면과 플레이 범위는 당시 미검증이었다. 이후 DEV-024에서 배송과 창고 기능을 연결했다.

2026-10-29 휴지통 바닥 표시: 기준 SHA `7cf8ca596a2f6583645133ac383765d2d20e0c0c` + 기존 작업 트리의 `TrashCan.prefab`에 반투명 바닥·테두리·휴지통 표식을 미리 배치하고 전용 재질 두 개를 추가했다. ProjectB Unity 6000.5.10f1 Pipeline `eval`로 프리팹을 편집했으며 CLI 1.0.0-beta.12는 업데이트하지 않았다. 프리팹의 표시 크기와 기존 트리거 가로·세로 3.4×3.8, 중심, 표시용 콜라이더 0개, 기존 TrashPoint 연결을 확인했고 격리 카메라 렌더에서 표시와 표식을 보았다. 실제 씬 바닥과의 겹침 및 Play Mode 진입 범위는 미검증이다. 트리거를 나중에 수정하면 바닥 표시도 함께 조정해야 한다.

2026-10-29 휴지통 DEV-023: 기준 SHA `7cf8ca596a2f6583645133ac383765d2d20e0c0c` + 기존 작업 트리에서 `TrashCan.cs`, `StackingSystem.cs`, `TrashCan.prefab`을 변경했다. 플레이어만 스택의 메뉴를 0.2초 간격으로 한 개씩 분리해 휴지통까지 날린 뒤 제거하며 재료는 반환하지 않는다. 직원과 식기는 거부한다. ProjectB Unity 6000.5.10f1 Pipeline 재컴파일 완료·오류 0건, 프리팹 스크립트·트리거·도착점·0.2초 설정 확인, 에디터 격리 실행에서 음식/식기·직원 조건과 맨 위 분리 확인. 최초 Pipeline eval 연결 실패 후 editor_status 및 eval 연결 성공; 셸 Unity CLI 1.0.0-beta.12 status는 STATUS_NO_INSTANCES였고 beta.14 업데이트는 승인 대기다. 실제 Play Mode에서 트리거 진입·연속 간격·연출·풀 반환·범위 이탈은 미검증이다. 주문 예약 재료로 만든 메뉴를 플레이어가 버리면 기존 제조 코드가 제거한 직원 서빙 작업과 소진한 예약이 자동 복구되지 않을 가능성이 있어 별도 검토가 필요하다. 열린 씬의 미저장 상태는 보존했다.

2026-10-28 플레이어 상호작용 중복 방지: 기준 SHA `7cf8ca596a2f6583645133ac383765d2d20e0c0c` + 기존 작업 트리에서 `PlayerController`, `InteractableBase`, `Sink`를 변경했다. 한 플레이어는 한 대상만 사용하고 종료·취소·비활성화 시 해제한다. 싱크대의 자동 잔 회수도 작업 중이면 대기한다. CLI 1.0.0-beta.12는 업데이트하지 않았고 ProjectB Unity 6000.5.10f1 Pipeline 재컴파일은 완료·오류 0건이었다. 에디터 격리 실행에서 대상 선점·다른 대상 거부·잘못된 해제 거부·해제 후 재시작 및 자동 회수 조건을 확인했다. 트리거·진행 시간·직원 인계와 실제 두 좌석 청소의 Play Mode 검증은 미실시다.

| 시스템 | 코드 구현 상태 | 씬/UI 연결 상태 | 실제 동작 검증 |
| --- | --- | --- | --- |
| 플레이어 이동·동종 메뉴 적재·잔액 변경 API | 이동 코드의 성장·적재·화면 드래그 직접 참조를 PlayerController로 분리하고 제어 모드 enum은 이동 코드 내부로 이전. 나머지 API는 기존 구현 | Player 프리팹 및 레벨의 관련 컴포넌트 존재 | 이번 분리 후 컴파일·플레이 검증은 사용자 진행 예정 |
| 공용 트리거 상호작용·대상 예약·진행 UI | 코드 구현 완료(공통 처리 범위) | 시설 하위 클래스 사용 | 다중 작업자/취소 포함 미실시 |
| 손님 생성·대기·좌석·식사·결제·퇴장 | 부분 구현: 재고가 없어도 주문과 말풍선을 확정하고 좌석에서 같은 주문의 재료를 기다리도록 연결 | CustomerSceneBinder와 시설 참조 사용 | 이번 주문 규칙 변경 후 컴파일·실제 플레이는 사용자 검증 예정 |
| 방해 손님·빈손 해결 | 코드 구현 있음: 대기줄/착석 소음, 자리 독점 | CustomerResolve 참조와 콜라이더 필요 | 세 유형 모두 미실시 |
| 레시피 제조·재료 재고 | 재료 예약·제조 차감 및 공용 용량·배송/창고 입고 구현, 시작 수량 각 0 | 세 임시 재료 프리팹, Level1_Pocha 창고·배송 지점, 시장 버튼과 직원 업무 연결 | DEV-024 배송·입고·직원 왕복 일부 Play Mode 확인. 실제 시장 입력·전체 주문/제조·동시성은 미검증 |
| 메뉴 일치·메뉴별 결제액 | 코드 연결: 주문/제공 레시피 비교, 다른 메뉴 수락·리뷰 감점, 주문 `Pay` 전액/절반 기본 결제 | 플레이어·직원 서빙과 Counter 연결, 손님 프리팹의 이전 고정 가격 필드 제거 | 이후 Unity 재컴파일 성공; 전체 씬 플레이 미실시, ISS-003 |
| 직원 고용·업무 배정·자동 실행 | 부분 구현: `EmployeeData` 후보 3개와 선택 고용·후보별 업무/능력치·중복 방지·유료 시 결제 API 존재. 기존 직접 생성 API는 비용 없음. 자동 작업은 별도 미완성 | `UI_MenuPopup`의 사전 제작 고용 패널·세 카드가 `TryHire`에 연결됨. 현재 후보 가격은 모두 0 | Unity 재컴파일 및 프리팹에 세 후보 데이터 적용 격리 검증 성공. 실제 화면·고용·업무 실행은 미검증; ISS-004/008 |
| 좌석 청소 | 코드 구현 완료(Dirty → Empty) | 플레이어/직원 경로 존재 | 미실시 |
| 식기·설거지 | 부분 구현/주 루프 중단 | 청소 후 더러운 잔 생성이 주석 처리됨 | 미실시 |
| 시설 해금 | 부분 구현: 데이터 참조 기반 분할 지불·선행 조건·생성/활성화 | Level1_Pocha 발판 연결, GameScene 구 데이터 검토 필요, 저장 없음 | 격리 확인, 실제 플레이 미실시; ISS-005 |
| 플레이어 성장 | 기존 구매·효과·저장 API를 UI_MenuPopup의 세 성장 카드에 연결. 카탈로그를 `10_Data`의 Addressables 에셋으로 이전 | 메인 메뉴의 성장 버튼 → 사전 제작 패널 → GrowthManager.TryUpgrade | UI 변경 당시 Level1_Pocha Play Mode에서 세 항목 구매·효과 확인. 카탈로그 이전 후 실행 검증은 사용자 진행 예정 |
| 직원별 성장 | 부분 구현: 개별 효과·구매 있음, 영속 저장 없음 | 현재 주요 UI에서 구매 호출 미확인 | 미실시 |
| 리뷰 | 코드 구현 완료(계산·통계 저장·결제 반영 범위) | Counter가 사용, 표시 UI 구독 미확인 | 입력 메뉴 판정 결함과 별개로 계산 테스트도 미실시 |
| 가구 구매·배치·이동·복원 | 부분 구현: 관련 코드 존재 | 주요 씬/프리팹에 컨트롤러 GUID 연결과 외부 호출 미확인 | 미실시; 기존 설계와 차이 있음 |
| 퀘스트 | 부분 구현: 순차 목표·보상·UI 진행바/완료/보상 연출과 대기 상태 존재 | UI_Quest가 Init에서 이벤트 구독; 메뉴 제작 대상 매핑 누락 | Unity 컴파일 및 QuestManager 완료 대기·중복 방지·UI 부재 흐름 격리 확인. 실제 UI 연출/게임 플레이 미검증; ISS-006 |
| UI 프레임워크·잔액·메뉴 팝업 | 코드 구현: 메뉴/시장/고용/플레이어 성장 패널 전환과 구매 표시 | UI_MenuPopup 프리팹에 시장·고용·성장 카드와 버튼 사전 배치 | 성장 패널은 Play Mode의 버튼 이벤트 호출과 화면 캡처로 확인. 실제 마우스/터치 입력, 고용 패널 실동작은 미검증 |
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

### ISS-003 — 메뉴 식별과 가격 정보의 전달 단절 / 코드 수정, 플레이 검증 전

[SeatStation](../Assets/01_Scripts/Content/InteractObject/SeatStation.cs)는 플레이어·직원 스택의 `ItemController.MenuRecipe`를 읽어 [CustomerController](../Assets/01_Scripts/Content/Controller/CustomerController.cs)의 주문 레시피와 비교한다. 비메뉴와 레시피 없는 메뉴는 소비하지 않는다. 다른 메뉴는 수락하고 기존 리뷰 감점 2점을 적용한다. [Counter](../Assets/01_Scripts/Content/InteractObject/Counter.cs)는 주문 레시피 `Pay`를 기본액으로 사용하며 다른 메뉴 제공 시 절반으로 줄인다(DEC-010). 직원 기계 선택은 이미 주문 레시피를 검사한다. 실제 씬에서 두 메뉴/비메뉴/결제/예약 해제의 전체 경로는 아직 검증하지 않았다.

### ISS-004 — 빈손 직원이 제조를 건너뜀 / 코드 수정, 플레이 검증 전

기준 커밋에서는 EmployeeController.IsMenuItem이 항상 true여서 빈손이 제조 분기를 건너뛰었다. 이번 변경에서 메뉴 타입만 메뉴로 인식하게 고치고, 손님 예약과 주문 레시피가 같은 기계를 선택하도록 수정했다. 직원은 주문 예약분으로 제조 후 서빙한다. SeatStation도 비메뉴 아이템을 거부한다. Unity 재컴파일은 성공했으며 실제 직원 자동 제조·서빙 검증은 하지 않았다.

### ISS-005 — 해금 지불과 영속 상태 불일치 / P1

[UnlockManager](../Assets/01_Scripts/Managers/Content/UnlockManager.cs)는 해금 상태를 메모리에만 보관한다. [UnlockStation](../Assets/01_Scripts/Content/InteractObject/UnlockStation.cs)은 재화를 차감하지만 누적 지불은 자체 필드에만 저장하고 Setup에서 초기화한다. 재화는 다른 저장 호출이나 종료 시 기록될 수 있어 재실행 후 잔액만 줄고 해금/중간 지불은 사라질 위험이 있다.

### ISS-006 — 메뉴 제조 퀘스트 진행 누락 / P1

[QuestManager](../Assets/01_Scripts/Managers/Core/QuestManager.cs)의 GetItemTargetType은 Money/Beer_Empty/Beer_Dirty만 처리한다. Menu는 Unknown이고 ReportProgress에서 무시된다. [Quest_MakeBeer](../Assets/10_Data/Quest/Quest_MakeBeer.asset)는 Beer 목표를 요구하므로 현행 메뉴 생성 보고로 완료되지 않는다. 이 수정은 Core 승인 대상이다.

### ISS-007 — 퀘스트 UI의 매니저 연결 호출 누락 / 코드 해소, 화면 미검증

[UI_Quest](../Assets/01_Scripts/UI/SubItem/UI_Quest.cs)는 현재 Init/OnEnable에서 QuestManager 이벤트를 직접 구독하고 비활성화·파괴 시 해제한다. 이전의 Bind 호출 누락 설명은 코드 변경으로 유효하지 않다. UI_GameScene 프리팹에 UI_Quest 인스턴스는 있으나 진행바·완료·보상 연출은 실제 Play Mode 화면에서 확인하지 않았다.

### ISS-008 — 성장·고용·가구 API와 사용자 진입점의 공백 / P1

`UI_MenuPopup`에서 플레이어 성장 3종의 `TryUpgrade` 호출과 사전 제작 성장 패널이 연결됐다. Level1_Pocha Play Mode에서 구매·효과·버튼 상태를 확인했다. 후보별 `TryHire` 호출과 고용 패널은 코드 연결 상태지만 실제 고용 화면·버튼 동작은 아직 검증하지 못했다. 기존 `HireEmployee` 생성 API는 여전히 비용을 차감하지 않고, `HireCost` 필드는 선택 고용 가격으로 사용되지 않는다. 직원별 `TryUpgradeEmployeeStat`, `BuyFurniture`, `BeginPlacement`의 사용자 UI 호출은 확인되지 않았다. 주요 씬/게임 프리팹에서 가구 컨트롤러 연결도 확인하지 못했다. 직원 성장·가구 진입점과 선택 고용 실동작 검증이 남았다.

### ISS-009 — 삭제 스크립트와 레시피 데이터 잔재 / P1

[GameScene](../Assets/02_Scenes/GameScene.unity)은 삭제된 StreetMapGenerator의 m_Script GUID `53e09270b8242e84ea0ffc36fb4d05c7`를 유지한다. [Recipe_ Kimbap](<../Assets/10_Data/FoodRecipes/Recipe_ Kimbap.asset>)은 파일명·FoodName·기계 종류·프리팹이 서로 맞지 않는다. 모든 외부 에셋 참조를 전수 검사한 것은 아니며 PackageCache에 있는 스크립트를 Assets 검색 실패만으로 Missing Script로 판정하면 안 된다.

### 재고 시스템 구현

`IngredientManager`가 재료별 매장 수량과 손님 주문 예약을 관리한다. 재료 필요량을 종류별로 합산하고 모든 재료가 충분할 때만 차감한다. 플레이어는 주문 예약을 우선 사용하고 직원은 해당 손님의 주문 레시피를 만들어 예약분을 소비한다. 제조물 생성 실패는 재고 차감 전에 처리하며, 차감 실패 시 생성물을 파괴한다. 손님은 재고와 무관하게 활성 기계의 레시피를 골라 주문과 말풍선을 확정한다. 재료가 부족하면 같은 주문을 유지하며 좌석을 차지하고 서빙 작업을 만들지 않는다. `AddIngredients` 보충이나 기존 예약 해제 알림을 받으면 같은 주문의 재료 예약을 재시도하고 성공 시 직원 작업을 만든다. 직원 제조 작업이 취소되면 좌석과 주문 예약을 보존하고 서빙 작업을 다시 등록한다.

`Assets/10_Data/FoodIngredient`의 재료 에셋 세 개는 물·면·소스 시작 수량을 각 0개로 설정한다. 각 에셋에는 종류·이름·시장 구매 가격·임시 재료 프리팹이 있다. `Recipe_Udon`은 각 1개를 요구하며 맥주 레시피는 재료 목록이 비어 있다. 손님이 재고를 기다리는 동안 자리를 계속 점유하는 규칙은 사용자 확정이다. 가격 밸런스와 재고 저장은 미확정이다.

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

**DEV-003 변경 확인 (2026-10-27):** 기준 SHA `7cf8ca5`에서 작업했다. 플레이어·직원 서빙 호출부가 모두 실제 `MenuRecipe`를 전달하는지, `Counter`가 `BasePay`를 사용하는지 코드 검색과 diff로 확인했다. 맥주/우동 완성품 프리팹의 `menuRecipe` GUID가 각 레시피 에셋과 일치하고 현재 `Pay`가 각각 0/20인 것을 YAML로 확인했다. Unity CLI 1.0.0-beta.12의 `unity status --project-path`는 `STATUS_NO_INSTANCES`였으나 Unity Pipeline `editor_status`는 같은 프로젝트의 Unity 6000.5.10f1 에디터 연결을 확인했다. 최초 Pipeline `recompile`은 컴파일 중 시간 초과됐지만 이후 `recompile_status`가 `completed`, `failed=false`, `errors=[]`, `compilationFailed=false`로 확인됐다. 실제 씬에서의 플레이어/직원 서빙·리뷰·결제·예약 해제는 미확인이다.

**DEV-007 플레이어 성장 UI 확인 (2026-10-27):** `UI_MenuPopup` 프리팹의 기존 `Button_Upgrade`를 연결하고, 플레이어 이동·적재·작업 성장 카드와 잔액·돌아가기 요소를 프리팹에 미리 배치했다. Unity Pipeline 재컴파일은 `completed`, `failed=false`, `errors=[]`였고 대상 프로젝트는 `C:\Users\dlwpd\01_UnityProjects\ProjectB`, 에디터는 6000.5.10f1이다. `Level1_Pocha` Play Mode에서 프레임 증가를 확인한 뒤 버튼 이벤트를 호출해 메인→성장→메인 전환, 보유 금액·현재/다음 단계 표시를 확인했다. 기존 저장 잔액 959,185에서 이동 세 단계 구매 시 총 850 차감·이동 보너스 1.5·최대 단계 버튼 비활성, 적재 1단계 구매 시 150 차감·최대 적재량 3, 작업 1단계 구매 시 150 차감·시간 배율 0.92를 확인했다. 잔액 0에서는 버튼 비활성 및 단계 불변을 확인했다. 화면 캡처에서 세로 카드와 한글 표시를 확인했고 검증 전 `ProjectB.Player.Money`와 `ProjectB.Growth.Save` 값을 복원해 동일함을 확인했다. 버튼 이벤트를 코드로 호출했으므로 실제 마우스/터치 입력과 앱 재실행 후 저장 복원은 미검증이다.

**성장 카탈로그 로드 변경 (2026-10-27):** 사용자 지시에 따라 `GrowthCatalog.asset`을 GUID를 유지하며 `Assets/10_Data`로 옮기고 Addressables `Data` 그룹에 `GrowthCatalog` 주소로 등록했다. `GrowthManager.Init`은 `LoadAsync`를 시작하고, 성공 콜백에서 기존 저장 단계를 읽어 플레이어 효과를 적용한다. 로드 실패 시 기본 수치로 구매를 열지 않고 오류를 남긴다. 이 변경 후 Unity 컴파일·Play Mode·빌드 검증은 사용자가 진행하기로 하여 수행하지 않았다. 앞 문단의 검증은 카탈로그 이전 전 결과다.

**이동 코드 재사용성 변경 (2026-10-27):** `ThirdPersonController`의 성장 전용 보너스와 `Define.MovementControlMode`, `StackingSystem`, `ScreenMoveInput` 직접 참조를 제거했다. 이동 컴포넌트는 범용 `MoveSpeed`, 외부 방향 함수 `ExtraMoveDirection`, 입력 정리 알림 `MovementDisabled`를 제공한다. `PlayerController`가 카탈로그 효과를 기본 속도에 더하고, 화면 드래그와 운반 애니메이션을 연결한다. 기존 씬·프리팹의 `controlMode` 숫자값은 같은 순서의 클래스 내부 enum으로 해석한다. 컴파일·Play Mode·실제 입력 검증은 사용자 진행 예정이며 이전 성장 UI 검증 결과는 이 변경의 결과가 아니다.

**DEV-016 주문 우선 변경 (2026-10-27):** DEC-013에 따라 `CustomerManager.TryGetRandomRecipe`에서 재고 필터를 제거했다. `CustomerController`는 선택 레시피와 말풍선을 먼저 확정하고, 재료 예약에 실패하면 같은 주문을 유지한 채 기다린다. 재고 보충과 기존 예약 해제 이벤트로 예약을 다시 시도하며 성공한 뒤에만 직원 서빙 작업을 만든다. 이번 변경 후 Unity 컴파일·Play Mode·재고 보충 시나리오 검증은 사용자 진행 예정이며 이전 재고 격리 검증은 이 변경의 결과가 아니다.

**말풍선 보관 방식 변경 (2026-10-27):** `UI_Bubble`은 처음 표시할 때 UI 풀에서 가져오지만, 숨기거나 소유 오브젝트가 비활성화될 때 풀에 반환하지 않는다. 소유 앵커 아래에 둔 채 `ResetForReuse`로 표시 상태를 초기화하고 비활성화하며, 손님/상호작용 오브젝트가 다시 활성화되면 보관한 참조를 통해 같은 오브젝트를 켠다. 이에 따라 비활성화 중 `SetParent`를 호출하던 경로를 제거했다. 컴파일 및 Level1_Pocha Play Mode 재현 검증은 아직 수행하지 않았다.

**재고 변경 후 확인 (2026-10-26):** 이전 구조에서 Unity 에디터 재컴파일은 `failed=false`, `errors=[]`로 완료됐다. `IngredientManager` 격리 실행에서 우동 재료 정상 차감, 한 재료 부족 시 나머지도 차감하지 않는 동작, 중복 재료 합산, 재고 보충 이벤트를 확인했다. Play Mode를 잠시 시작해 게임 화면과 콘솔 오류 0건을 확인하고 종료했다. 이후 사용자가 재료 데이터 구조를 바꾸었고, 재료 세 에셋을 `10_Data` 및 Addressables `IngredientData` 라벨로 연결했다. 이 변경의 별도 검증 결과는 아래에 기록한다. 재고 부족 손님의 자리 점유, 보충 후 주문, 직원/플레이어 제조는 직접 확인하지 않았다.

**재료 데이터 위치 변경 확인 (2026-10-26):** `IngredientCatalog`의 사용자 추가 필드 `Type`, `Name`, `Price`, `Prefab`을 유지하고 시작 재고를 `StartingAmount`로 각 재료 에셋에 저장했다. 세 에셋 모두 `Assets/10_Data/FoodIngredient`에 있으며 Addressables `Data` 그룹의 `IngredientData` 라벨과 주소를 에디터에서 확인했다. 재컴파일 결과는 `failed=false`, `errors=[]`였다. Play Mode에서 `IngredientManager.GetAmount`가 물·우동 면·우동 소스 각 10개를 반환했고, 종료 후 현재 콘솔 오류 0건이었다. 실제 주문·제조는 아직 확인하지 않았다.

**시장 UI 확인 (2026-10-26):** `UI_MenuPopup` 프리팹의 메인 패널은 켜짐, 시장 패널은 꺼짐으로 저장됐다. 시장 패널에는 물·우동 면·우동 소스의 정보 글자와 구매 버튼, 돌아가기 버튼이 미리 배치돼 있다. Unity 스크립트 재컴파일 상태는 `up_to_date`, `failed=false`, `errors=[]`이며 현재 콘솔 오류는 0건이었다. Play Mode에서 팝업 버튼 이벤트를 호출해 메인→시장, 물 가격 0의 무료 구매로 수량 10→11, 시장→메인 전환을 확인했다. 이 검증은 프로그램에서 버튼 이벤트를 호출한 것으로 실제 마우스/터치 조작, 양수 가격의 잔액 차감, 저장 후 재실행은 검증하지 않았다.

**UI enum 위치 수정 (2026-10-26):** 시장 UI의 GameObjects/Buttons/Texts enum을 Define.cs에서 UI_MenuPopup.cs로 옮겼다. 이전 문단의 Play Mode 검증은 이동 전 결과다. 이동 후 참조 검색과 `git diff --check`는 통과했으며, 이후 퀘스트 연출 변경 시점의 Unity 재컴파일도 완료·오류 0건이었다. 이동 후 시장 UI의 Play Mode 재검증은 하지 않았다.

**퀘스트 연출 대기 (2026-10-26):** QuestManager는 목표 달성 시 완료·보상을 확정하고 UI_Quest가 진행바 채우기(기본 0.5초), 완료 문구(0.8초), 실제 지급 보상 문구(1초)를 마칠 때까지 다음 퀘스트 시작을 보류한다. UI가 없거나 연출 도중 숨겨지면 바로 진행한다. Unity 재컴파일 `completed`, `failed=false`, `errors=[]`; 현재 콘솔 오류 0건. Unity 에디터 격리 실행에서 완료 대기, 중복 보고 무시, 연출 종료 후 한 번만 전환, UI 부재 시 즉시 전환을 확인했다. 실제 화면 연출 및 재실행 후 상태 복원은 미검증이다.

최초 문서 구축 시 문서 로컬 링크 92개, 이슈/작업/결정 ID, 코드 블록·문자 깨짐·행 끝 공백을 검사했고 이상이 없었다. 기존 AGENTS의 코딩 규칙 14개는 원문과 일치했다. 이번 재고 변경 뒤에는 문서 내용과 작업 ID를 다시 대조했으며, `git diff --check`에서 공백 오류는 없었다. Mermaid는 소스 구조만 검토했으며 별도 렌더러로 시각 검증하지 않았다. 이는 게임 코드 검증과 별개의 문서 품질 검사다.

## 아직 확인하지 못한 정보

- 최종 시작 씬, 플랫폼별 실제 빌드 프로파일, 출시 대상과 목표 성능.
- 음식 가격 수치의 밸런스, 직원 성장/비용, 식기 재사용, 가구 지불 시점의 최종 기획 승인.
- 사용자 PC에 저장된 실제 PlayerPrefs 내용 및 과거 버전 저장 호환성.
- 에디터의 미저장 씬 상태와 저장된 YAML의 차이, 외부 에셋 전체 참조 건전성.
- 기존 기획 문서의 작성/승인 날짜, 코드와 달라진 결정의 이유.
- 과거 테스트 도구 실행의 유효한 결과와 실제 기기 플레이 결과.

다음 작업은 [BACKLOG](BACKLOG.md)의 제안 우선순위를 참고해 사용자 범위 승인 후 진행한다.
