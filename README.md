# 🎮 Unity 리듬 레이싱 게임 (멀티플레이어)

> 음악 박자에 맞춰 레인을 이동하며 노드를 획득하는 리듬 + 레이싱 장르 결합 멀티플레이어 게임

<br>

## 📋 프로젝트 정보

| 항목 | 내용 |
|------|------|
| 개발 기간 | 2023.01 ~ 2023.02 (인턴십 게임잼, 팀 프로젝트) |
| 개인 개선 | 2023.03 ~ 2026.01 (개인 리팩토링 및 기능 추가) |
| 개발 인원 | 4인 개발 / 1인 유지보수 |
| 사용 기술 | Unity (C#), C++, TCP 소켓 통신, Bezier Curve |

<br>

## 🗂 목차
- [프로젝트 개요](#-프로젝트-개요)
- [핵심 기능](#-핵심-기능)
- [아키텍처 및 설계 특징](#-아키텍처-및-설계-특징)

<br>

## 📌 프로젝트 개요

음악의 박자에 맞춰 3개의 레인을 이동하며 노드를 획득하는 **리듬 게임**과 **레이싱 장르**를 결합한 멀티플레이어 게임입니다.  
Unity 클라이언트와 **C++로 직접 제작한 게임 서버**를 연동하여 멀티플레이 환경을 구현했습니다.

<br>

## ⚙ 핵심 기능

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

<br>

## 🏗 아키텍처 및 설계 특징

| 패턴 / 원칙 | 내용 |
|-------------|------|
| **Command 패턴** | 서버로 전송되는 모든 클라이언트 액션을 `IClientAction` 인터페이스로 추상화, `ClientActionFactory`와 `ActionSelector`로 분리하여 확장성과 유지보수성 확보 |
| **Singleton 패턴** | `GameModeManager`, `SoundManager` 등 전역 관리 시스템에 싱글톤 적용 |
| **데이터 모델 분리** | `NoteData`, `ServerData`, `GameState` 등 데이터 클래스를 별도 네임스페이스로 분리하여 관심사 분리 원칙 적용 |
| **서버-클라이언트 이중 구조** | Unity(C#) 클라이언트 + C++ 네이티브 서버를 별도 프로젝트로 구성, 각각 독립적으로 빌드 및 실행 가능 |
