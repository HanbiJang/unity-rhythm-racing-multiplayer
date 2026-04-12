<img width="405" height="719" alt="Image" src="https://github.com/user-attachments/assets/22a06464-ef79-4433-ba2e-d9bfec19ad52" />

## 📋 프로젝트 정보

| 항목 | 내용 |
|------|------|
| 개발 기간 | 2023.01 ~ 2023.02 (인턴십 게임잼, 팀 프로젝트) |
| 개인 개선 | 2023.03 ~ 2026.01 (개인 리팩토링 및 기능 추가) |
| 개발 인원 | 4인 개발 / 1인 유지보수 |
| 사용 기술 | Unity (C#), C++, TCP 소켓 통신, Bezier Curve |

## 📌 프로젝트 개요

음악의 박자에 맞춰 3개의 레인을 이동하며 노드를 획득하는 **리듬 게임**과 **레이싱 장르**를 결합한 멀티플레이어 게임입니다.  
Unity 클라이언트와 **C++로 직접 제작한 게임 서버**를 연동하여 멀티플레이 환경을 구현했습니다.

## ⚙ 핵심 기술 및 구현 내용

### 🕹 게임 플레이
- `A` / `S` / `D` 키 입력으로 좌/중/우 3개의 레인 전환 (`Vector3.Lerp`를 활용한 부드러운 이동 처리)
- 음악 BPM에 동기화된 노드 스폰 시스템 (0.5714초 간격, `InvokeRepeating` 활용)
- **Perfect / Good / Bad / Miss** 4단계 판정 시스템 (`JudgmentSystem`)
- 콤보 트래커를 통한 연속 판정 추적 및 점수 집계
- 게임 시작 → 인게임 → 결과 → 로비 복귀의 완전한 게임 루프 구현

### 🌐 멀티플레이어 네트워크
- C++로 직접 구현한 TCP 기반 게임 서버 (`Server`, `Session`, `Room`, `RoomManager` 구조)
- Unity 클라이언트와 서버 간 바이트 직렬화 통신 (`IClientAction` 인터페이스 기반 Command 패턴)
- 클라이언트 액션 분리: `JoinGame`, `ReadyGame`, `StartGame`, `Judgement`, `ScoreBroadcast`, `EndGame`, `RetryGame`
- 서버에서 XML(`MusicNodeData.xml`)을 파싱하여 노드 데이터를 클라이언트에 브로드캐스트

### 🎨 시각/음향 연출
- 타격 시 파티클 이펙트 및 `HitEffectManager`를 통한 카메라 피드백 효과
- `ScreenFlashManager`를 이용한 화면 플래시 연출
- 노드별 SFX 분기 처리 (`NodeSfxManager`)
- Bezier Curve를 활용한 도로 경로 구성 (`BezierScripts`, `BezierAssets` 적용)
- Synty, Realistic Terrain Collection 등 3D 에셋 연동

### 🖥 UI 및 시스템
- 실시간 점수 표시 및 프로그레스 바 UI
- 게임 결과 화면 및 랭킹 시스템 (`ResultUIController`, `ResultFlow`)
- 리듬 박자 편집 도구 (`CreateModeNoteEditor`) — 커스텀 노트 배치 편집 기능
- 클라이언트 상태 Enum(`ClientState`) 기반 게임 흐름 관리

## 🔑 주요 구현 포인트
- 멀티플레이어 네트워크 : C++로 TCP 소켓 게임 서버를 직접 설계하고, Command 패턴 기반 액션 분리 및 Unity 클라이언트와 바이트 직렬화 통신 연동
- 게임플레이 시스템 : BPM 동기화 노드 스폰, 4단계 판정 시스템, 콤보 트래커, `Vector3.Lerp` 기반 3레인 이동 구현
- `ClientState` Enum으로 완전한 게임 루프를 관리하고, 커스텀 리듬 박자 편집 툴 자체 제작
- Bezier Curve 기반 도로 경로 구성 및 타격 파티클 → 카메라 피드백 → 화면 플래시 → SFX로 이어지는 이펙트 파이프라인 구현
