# 개발 백로그

기록일: 2026-10-26. 코드 기준/문제 상세는 [DEVELOPMENT_STATUS](DEVELOPMENT_STATUS.md), 게임 규칙은 [GAME_DESIGN](GAME_DESIGN.md)를 따른다.

## 상태와 우선순위

- `TODO`: 계획 또는 제안, 미착수. `IN_PROGRESS`: 승인된 범위로 실제 작업 중. `DONE`: 명시한 완료 조건 충족. `BLOCKED`: 구체적인 외부 조건/결정 때문에 진행 불가.
- P0: 시작·빌드 경로를 막을 수 있는 문제. P1: 플레이 루프·재화/저장·사용자 진입 문제. P2: 안정성·구조·확장 준비. P3: 후속 콘텐츠 후보.
- **사용자가 확정한 작업:** DOC-001 문서 시스템 구축, DEV-016 레시피 재료 재고 연결, DEV-017 시장 테스트 구매 UI, DEV-018 퀘스트 완료 연출 대기, DEV-019 선택 직원 고용 테스트 UI. 아래 나머지 개선 항목은 Codex 제안이며 구현 승인이 아니다. 기존 기획은 출처를 따로 표시한다.
- Core 수정이 필요한 작업은 AGENTS 규칙에 따라 구체적 변경 범위를 준비하고 사용자 승인 후 진행한다. 승인된 현재 문서 작업을 그 이유로 멈추지 않는다.

## 사용자 확정 작업

| ID/기능 | 목적 | 우선순위 | 선행 조건 | 예상 수정 시스템 | 완료 조건 | 상태 |
| --- | --- | --- | --- | --- | --- | --- |
| DOC-001 AI 협업 문서 구축 | ChatGPT가 저장소 문서만으로 현행 구조·공백·다음 작업을 이해 | P0 | 실제 코드 및 기존 기획 분석 | Docs 5종, AGENTS | 기존 규칙/기획 보존, 구현·검증 구분, 근거 링크, 문서 일치 검토, 문서만 변경, 결과 보고 | DONE |
| DEV-017 시장 테스트 구매 UI | 메뉴 팝업에서 재료 정보와 임시 구매를 사용 가능하게 함 | P1 | 재료 데이터·재고 매니저, UI_MenuPopup 프리팹 | UI_MenuPopup, Define, IngredientManager, UI 프리팹 | 시장 버튼 전환, 세 재료 이름/가격/수량 표시, 사전 제작 버튼으로 1개 구매, 돌아가기, Play Mode에서 현재 가격 0의 구매 확인 | DONE — 현재 무료 테스트 범위 검증, 유료 구매/실제 입력은 후속 검증 |
| DEV-018 퀘스트 완료 연출 대기 | 진행바·완료·보상 표시를 마친 뒤 다음 목표로 전환 | P1 | QuestManager Core 수정 사용자 승인 | QuestManager, UI_Quest | 설정 시간에 맞춘 순차 연출, 종료 후 한 번만 다음 목표 시작, UI 부재/숨김 시 진행, 실제 화면 확인 | IN_PROGRESS — 코드/격리 검증 완료, 실제 화면 확인 대기 |
| DEV-019 선택 직원 고용 테스트 UI | 후보를 골라 같은 프리팹에 능력치·업무 데이터를 적용 | P1 | 기존 직원 프리팹·UI_MenuPopup·EmployeeManager | EmployeeData, EmployeeManager/Controller, UI_MenuPopup 프리팹, Addressables | 사전 제작 카드 3개 표시, 후보별 고용, 중복 고용 차단, 업무/속도 반영과 버튼 동작을 Play Mode에서 확인 | IN_PROGRESS — 코드·프리팹·데이터 및 격리 적용/컴파일 확인, 실제 화면·고용 미검증 |

## 코드에서 발견한 개선 과제 — 미승인 제안

| ID/기능 | 목적/근거 | 우선순위 | 선행 조건 | 예상 수정 시스템 | 구현 완료 조건 | 상태 |
| --- | --- | --- | --- | --- | --- | --- |
| DEV-001 시작 씬·레벨 연결 | 정상 시작부터 영업까지 도달, ISS-001 | P0 | 실제 시작 씬과 Level1_Pocha 연결 정책 확정. Core 변경 시 승인 | LoginScene, GameScene, SceneManagerEx, Define, 빌드 설정/UI | 승인된 시작 씬에서 UI와 플레이어·시설·영업이 준비되고 빌드에서도 동일 경로 통과 | TODO |
| DEV-002 플레이어 빌드 의존성 점검 | 에디터 전용 import 분리, ISS-002 | P0 | 목표 플랫폼 지정. Managers 수정 시 승인 | CustomerSceneBinder, Managers, 빌드 의존성 | 에디터/플레이어 컴파일을 별도로 통과하고 결과·경고 기록 | TODO |
| DEV-003 레시피 기준 주문·서빙·가격 | 다른 메뉴와 비메뉴를 정확히 처리, ISS-003 | P1 | 오주문 허용/감점·가격 기준 결정 | CustomerController, SeatStation, MenuMachine, ItemController, Counter, ReviewManager, 레시피 | 두 레시피의 정상/오주문/비메뉴 시나리오와 메뉴별 금액이 확정 규칙대로 동작 | TODO |
| DEV-004 직원 자동 제조·서빙 복구 | 빈손의 제조 단계 진입과 올바른 기계 선택, ISS-004 | P1 | DEV-003의 레시피 전달 계약 확정 | EmployeeController, TaskManager, MenuMachine, SeatStation | 빈손→제조→이동→서빙 완료, 기계 점유 시 대기, 플레이어 개입·취소 후 예약 해제 및 다음 작업 통과 | TODO |
| DEV-005 해금/분할 지불 저장 | 지불한 재화와 시설 상태의 복구 일치, ISS-005 | P1 | 중간 지불 복구/환불 정책·매장별 ID 범위 확정 | UnlockStation, UnlockManager, 저장 데이터; Core 변경 시 승인 | 일부 지불/완료 후 재실행, 반복 진입, 이전 저장에서 잔액·시설·발판 일치 및 중복 생성 없음 | TODO |
| DEV-006 퀘스트 보고·표시 연결 | 메뉴 제조 목표와 화면 진행 복구, ISS-006/007 | P1 | DEV-003의 레시피 식별 계약, QuestManager 수정 승인 | QuestManager, QuestData, MenuMachine, SeatStation, UI_GameScene/UI_Quest | 맥주 제작이 해당 목표만 진행, UI 이벤트 구독이 한 번 연결, 완료 보상 한 번, 재진입 구독 정리 확인 | TODO — UI 구독 코드는 연결됐으나 메뉴 제작 목표는 남음 |
| DEV-007 성장·고용·가구 진입 UI | 존재하는 API를 사용 가능한 기능으로 연결, ISS-008 | P1 | 첫 UI 범위·고용 비용 책임·가구 지불 정책 결정 | UI_MenuPopup 등 UI/프리팹, EmployeeManager, GrowthManager, FurniturePlacementController | 프리팹에서 UI 확인 가능, enum Bind 사용, 버튼→업무→잔액/표시 흐름 검증, 부족 시 변화 없음, 고용 비용 누락/중복 없음 | IN_PROGRESS — DEV-019의 고용 화면 코드 연결, 성장·가구 UI와 실제 고용 검증은 남음 |
| DEV-008 저장 정책 통합 및 직원/퀘스트 복원 | 성장/보상의 재접속 일관성, RISK-008 | P1 | 직원별 성장과 퀘스트 재시작/반복 보상 정책 확정, DEV-005와 저장 계약 조율 | EmployeeManager/Controller, GrowthManager, QuestManager, 저장 계층 | 고용/능력치/진행/보상 복원, 버전 호환, 부분 저장 실패 대응. Core 변경 승인 및 복원 테스트 필요 | TODO |
| DEV-009 씬 생명주기 안정화 | 영속 매니저와 씬 참조의 수명 정리, RISK-001 | P1 | DEV-001, Core 변경 승인 | Managers, GameManager, CustomerManager, SceneManagerEx, UI 이벤트 | 플레이어 없는 씬과 레벨 재진입에서 예외·중복 매니저·유령 대기열 없이 초기화/정리 | TODO |
| DEV-010 삭제 참조·데이터 정리 | 누락 스크립트와 메뉴 데이터 혼선 제거, ISS-009 | P1 | 사용/보존할 씬·콘텐츠 확정 | GameScene, 레시피/프리팹/Addressables 데이터 | 선택 범위의 Missing Script·잘못된 역참조 검사, 미사용/미완성 콘텐츠 명시, 관련 씬 진입 통과 | TODO |
| DEV-011 가구 수명·저장 식별자 보강 | 미리보기/이동이 영업 상태를 건드리지 않게 함, RISK-003/004 | P2 | DEV-007 및 기존 배치 설계 유지 범위 확정 | FurniturePlacementController, FurnitureMarker, 좌석 등록/저장 | 미리보기의 일시 등록 없음, 이동 중 신규 예약 방지, 취소 복구, 계층 변경 후 기존 가구 복구 검증 | TODO |
| DEV-012 리소스 로드 실패·중복·해제 계약 | 로드 콜백과 상태의 일관성, RISK-002 | P2 | Core 수정 승인 | ResourceManager, Customer/Employee 로딩 구독 | 동일 키 동시 요청, 없는 키/라벨, 실패 후 재시도, Clear 후 재로딩 검증. 성공/실패 구분 명확 | TODO |
| DEV-013 최소 회귀 검증 체계 | 변경으로 깨지는 핵심 순환을 재현 가능하게 확인 | P1 | 테스트 씬·저장 격리·목표 플랫폼 결정 | 테스트 코드/전용 에셋, 검증 문서 | 시작, 주문/서빙, 직원 예약, 재화, 저장 복구에 유효한 검증 시나리오와 실행 결과. 문서-only 작업에서는 미구현 | TODO |
| DEV-014 규칙 정합성과 책임 정리 | 확장 시 복잡도와 규칙 불일치 축소, RISK-005/006 | P2 | 대상 범위 승인, 영향 없는 분리 단위 결정 | 가구 UI/컨트롤러, Define, 관련 UI/직원 코드 | UI 요소 enum은 해당 UI 스크립트에 배치, UI를 사전 제작/Bind로 연결, 한국어 주석/배치/region 준수, 기존 동작 회귀 확인 | TODO |
| DEV-015 측정 후 성능 개선 | 객체 탐색·할당·저장 비용 평가, RISK-007 | P2 | 목표 기기·동시 NPC 수·예산 확정, 플레이 루프 안정화 | Customer/MenuMachine/Task/Station/가구/저장 | 동일 부하의 전후 프로파일로 개선 확인. 추정만으로 전역 캐시나 새 Manager를 추가하지 않음 | TODO |
| DEV-016 레시피 재료 재고 | 우동 재료를 실제 차감하고 주문 간 중복 소비를 방지 | P1 | 사용자 승인 완료, 시작 수량을 데이터로 조정 | IngredientManager, Managers, IngredientCatalog, MenuMachine, CustomerManager/Controller, EmployeeController, SeatStation | 재료 합산·예약·부족 차감 거부, 제조물과 재고 일치, 직원/플레이어 제조, 재고 부족 주문 처리, 인터페이스 참조 정리 | IN_PROGRESS — 코드 구현 및 격리 검증 완료, 씬 플레이 검증 대기 |

## 기존 기획 또는 미확정 게임 확장 — 승인 이력 확인 필요

| ID/기능 | 목적/출처 | 우선순위 | 선행 조건 | 예상 수정 시스템 | 구현 완료 조건 | 상태 |
| --- | --- | --- | --- | --- | --- | --- |
| PLAN-001 가구 허용·금지 구역과 통로 보호 | 기존 가구 최종 설계의 배치 안전성 | P2 | 매장 구역/통로 규칙 결정, DEV-011 | 가구 검사·카탈로그·맵 설정/NavMesh 연계 | 영역 전체 포함과 금지/통로 차단을 검출하고 좌석/직원 경로 유지. 기존 네 모서리 검사와 구분 | TODO |
| PLAN-002 기계 성장 | 과거 성장 계획의 제조 효율 향상 | P3 | 현재 MenuMachine 구조 기준으로 효과·공통/개별 범위 재설계, DEV-003/007 | Growth 데이터, MenuMachine, UI, 저장 | 승인된 기계 효과·비용·단계·저장/복원 검증. 삭제된 BeerMachine 기준 계획을 그대로 구현하지 않음 | TODO |
| PLAN-003 가구 종류 확대 | 기존 가구 설계의 테이블·장식·영업 설비 확장 | P3 | 의자 배치 검증 및 기능별 제약 결정 | FurnitureCatalog, 가구 프리팹, 기능 등록 | 가구별 배치·구매·저장·기능 등록 검증, 좌석 수를 별도 구매 숫자로 중복 관리하지 않음 | TODO |
| PLAN-004 식기/재료 순환 정책 | 현재 중단된 설거지와 구현된 재고의 향후 운영 범위 결정; Codex 검토 제안 | P3 | 식기 재사용, 재료 구매·저장 정책 확정 | SeatStation, Sink, DishManager, IngredientManager, MenuMachine, 직원 작업 | 확정 정책에 맞는 공급→소비→회수/세척 흐름과 수량 보존 검증. 손님이 보충을 기다리며 좌석을 점유하는 규칙은 사용자 확정. 현행 1개씩 구매하는 테스트 UI를 넘어서는 구매 정책과 재고 저장은 미확정 | TODO |

## 제안하는 착수 순서

1. 사용자와 시작 씬·메뉴 판정/가격 정책을 확정하고 DEV-001/002로 실행 기반을 점검한다.
2. DEV-003/004/006을 같은 레시피 전달 계약으로 설계한다. DEV-013으로 수정 전후를 재현할 검증을 준비한다.
3. DEV-005/008/009와 DEV-007의 구매/저장 책임을 맞춘다. UI 연결만으로 기능 완료를 선언하지 않는다.
4. 가구 안정화, 리소스 안정화, 측정 및 후속 기획을 진행한다.

자동 구현 순서가 아니다. 새 사용자 요청이 들어오면 승인된 범위와 선행 조건에 맞춰 상태/순위를 갱신한다. 이번 문서 작업에서 게임 개선 항목을 IN_PROGRESS나 DONE으로 바꾸지 않는다.
