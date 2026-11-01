# GameScript

Unity / C#으로 혼자 개발해 Google Play에 출시한 모바일 게임 2종의 스크립트입니다.

- **개발자**: 한성현 (알베도)
- **Google Play 개발자 페이지**: https://play.google.com/store/apps/dev?id=5968224314421325252
- **YouTube**: https://www.youtube.com/@Production_Alvedo

| 폴더 | 게임 | 장르 |
|---|---|---|
| [`Shapefit`](Shapefit) | ShapeFit | 테트로미노 퍼즐 |
| [`SlimeCollector`](SlimeCollector) | 슬라임 컬렉터 | 머지형 방치 / 도감 수집 |

---

## ShapeFit

테트로미노 조각을 회전·배치해 무작위로 생성되는 실루엣을 채우는 모바일 퍼즐 게임입니다.
일반 / 타임어택 / 빅보드 / 컬러매치 / 커스텀 5개 모드를 지원하며, 이미지·사운드 에셋 없이 그래픽과 효과음을 전부 코드로 생성했습니다.

- **Google Play**: https://play.google.com/store/apps/details?id=com.alvedo.shapefitpuzzle
- **플레이 영상**: https://youtube.com/shorts/u56VNVsBEa4
- **개발**: 1인 개발 (기획, 구현, 출시, 업데이트)
- **엔진**: Unity 6.3 LTS / C#

### 주요 기능과 구현

| 기능 | 구현 내용 |
|---|---|
| 항상 풀리는 퍼즐 생성 | 실루엣을 먼저 만들지 않고, 조각을 하나씩 이어붙여 실루엣을 역으로 생성. 붙일 때마다 겹침과 보드 범위를 검사 |
| 힌트 | 생성 과정의 정답 위치와 회전을 기록해 재사용 |
| 중간 저장 / 이어하기 | 보드, 배치된 조각, 남은 조각의 회전 상태를 JSON으로 저장하고 복원. 데이터가 손상되면 같은 스테이지를 새로 생성 |
| 성능 최적화 | 이펙트 오브젝트 풀링, 이펙트 코루틴 통합, HUD 갱신 주기 제한, 60fps 고정 |
| 에셋 없는 그래픽·사운드 | 블록, 9-slice 패널 스프라이트와 사인파 효과음을 런타임에 생성 |
| 모바일 조작감 | 멀티터치 드래그, 터치 판정 영역 확장, 주변 1칸 자동 스냅 |

### 먼저 보면 좋은 코드

| 스크립트 | 설명 |
|---|---|
| [`StageGenerator`](Shapefit/StageGenerator.cs) | 조각을 이어붙여 항상 풀리는 실루엣을 만드는 생성 로직 |
| [`FxPool`](Shapefit/FxPool.cs) | 이펙트 스프라이트를 생성·파괴하지 않고 재사용하는 오브젝트 풀 |
| [`GameManager`](Shapefit/GameManager.cs) | 스테이지 진행, 저장/불러오기 |

### 해결한 문제

- **카메라 2개 → 1개로 재설계**: 드래그 중인 조각이 카메라 경계를 넘을 때 겹쳐 그려지고 터치 좌표가 어긋나는 문제를, 카메라를 하나로 합치고 조각 배치 범위에 맞춰 크기를 자동 계산하는 구조로 해결
- **이펙트 잔상 버그**: 조각 오브젝트에서 실행한 코루틴이 조각 파괴와 함께 멈춰 정리 코드에 도달하지 못하던 문제를, 항상 살아 있는 매니저에서 실행하도록 바꿔 해결
- **조각 수 증가 시 생성 실패**: 조각을 붙일 때마다 범위를 검사하고, 실패 시 이전 난이도 밑으로 내려가지 않는 선에서 재시도

---

## 슬라임 컬렉터 (Slime Collector)

같은 슬라임끼리 끌어서 합치면 다음 단계 슬라임이 태어나고, 27종 도감을 채우는 머지형 방치 게임입니다.
보유한 슬라임 전체가 터치 골드와 초당 수익을 만들고, 도감이 채워질수록 상점에서 구매되는 슬라임 단계가 자동으로 올라갑니다.

- **Google Play**: https://play.google.com/store/apps/details?id=com.albedo.slimecollector
- **플레이 영상**: https://www.youtube.com/shorts/tMVZ8v41oz4
- **개발**: 1인 개발 (기획, 구현, 밸런싱, 출시, 광고 연동)
- **엔진**: Unity 6.3 LTS / C#

### 주요 기능과 구현

| 기능 | 구현 내용 |
|---|---|
| 드래그 합치기 | EventSystem 포인터 이벤트로 드래그, 놓는 순간 겹친 같은 종류 슬라임과 합치기 |
| 넓은 맵 이동 | 슬라임을 들고 화면 가장자리에 대면 카메라가 따라 이동. 카메라는 Update, 슬라임 위치는 LateUpdate로 나눠 손가락에서 밀리지 않게 처리 |
| 자동 합치기 | 0.3초 주기로 합칠 수 있는 짝을 찾고, 화면 밖이면 카메라가 따라감 |
| 저장 | JSON 저장, 임시 파일에 먼저 쓰고 교체, 백그라운드 전환 시와 5초마다 자동 저장 |
| 오프라인 보상 | 마지막 저장 시각으로 계산, 기기 시계를 과거로 돌리면 무시, 최대 시간 제한 |
| 손맛 | 히트스톱(unscaled 시간으로 연출 유지), 카메라 흔들림, Android VibrationEffect 진동 |
| 광고 | Google Mobile Ads 보상형 광고 (오프라인 보상 2배, 골드 2배, 자동 터치 등) |

### 먼저 보면 좋은 코드

| 스크립트 | 설명 |
|---|---|
| [`SaveManager`](SlimeCollector/SaveManager.cs) | 원자적 저장(임시 파일 → 교체), 백그라운드 전환 시 저장 |
| [`AutoMerge`](SlimeCollector/AutoMerge.cs) | 가장 낮은 단계에서 가장 가까운 같은 종류 짝을 찾는 자동 합치기 |
| [`SlimeDrag`](SlimeCollector/SlimeDrag.cs) / [`SlimeMerge`](SlimeCollector/SlimeMerge.cs) | 드래그 입력과 합치기 판정 |
| [`CameraController`](SlimeCollector/CameraController.cs) | 가장자리 자동 스크롤, 화면 흔들림, 대상 따라가기 |
| [`SlimeEconomy`](SlimeCollector/SlimeEconomy.cs) | 초당 수익 계산 (슬라임 수가 바뀔 때만 재계산) |

### 해결한 문제

- **실기기 렉**: 매 프레임 슬라임 전체를 서로 비교하고(GetComponent 수천 회), 골드 글자를 매 프레임 새 문자열로 만들던 것이 원인. 짝 확인을 0.3초 주기로, 글자는 값이 바뀔 때만 갱신하도록 바꿔 해결
- **완주가 불가능한 성장 구조**: 마지막 27단계를 만들려면 약 6,700만 마리가 필요하다는 것을 계산으로 발견. 최고 발견 단계에 맞춰 구매 단계가 오르도록 바꾸고, 약 3주 만에 도감을 완성하도록 가격과 수익 배율을 조정
- **스토어 정책 거절**: 설치된 앱 이름·아이콘이 스토어 정보와 다르다는 사유로 거절 → Product Name과 적응형 아이콘을 다시 만들어 재출시
