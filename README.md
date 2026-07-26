# Idle Game

> Unity 기반 **방치형(Idle) + 능동 전투 하이브리드**.
> 자동으로 도는 성장 루프 위에, 플레이어가 직접 개입하는 전투를 얹었다.

`Unity` · `C#` · 1인 개발 · 2026.03 ~ 2026.07

---

## 이 저장소가 보여주는 것 — 설계를 문서로 남기는 방식

기능을 구현할 때마다 **왜 그렇게 설계했는지**를 명세로 남겼다.
Player 도메인 하나에만 기술 명세 10편이 있고, 문서 폴더는 코드의 `Features/<도메인>` 구조를 그대로 미러링한다.

| | |
|---|---|
| 커밋 | 46 |
| 스크립트 | 145개 |
| 기술 명세 | Player 10편 · Enemy 1편 |
| 설계 원칙 | OOP · SOLID · 확장 가능성 · 테스트 가능성 |

## Player 시스템 명세

플레이어 하나를 10개 시스템으로 쪼개고, 각각의 구조·설계 근거·다이어그램을 문서로 고정했다.

- [상태 머신](docs/specs/player/state-machine.md) · [스탯](docs/specs/player/stats.md) · [스킬](docs/specs/player/skills.md) · [전투](docs/specs/player/combat.md) · [입력](docs/specs/player/input.md)
- [이동](docs/specs/player/movement.md) · [성장](docs/specs/player/progression.md) · [장비](docs/specs/player/equipment.md) · [버프](docs/specs/player/buffs.md) · [표현](docs/specs/player/presentation.md)

읽는 순서는 [`specs/player/README.md`](docs/specs/player/README.md) §0에 정리해 두었다.

## 문서

| 분류 | 내용 |
|---|---|
| [**문서 허브**](docs/README.md) | 전체 아키텍처 개요 + 명세 인덱스 |
| [`specs/`](docs/specs) | 완성된 시스템의 구조와 설계 근거 (as-built) |
| [`design/`](docs/design) | 구현 전·중의 방향과 계획서 |
| [`reports/`](docs/reports) | 진단 리포트 · 리팩터링 기록 · 작업 로그 |
| [`conventions/`](docs/conventions) | 명세 작성 규칙 · 커밋 컨벤션 |

`design/`의 계획서가 구현을 거쳐 `specs/`의 as-built 명세로 이어지고,
그 과정에서 발견한 문제는 `reports/`에 진단으로 남긴다.

## 실행

Unity Hub에서 열면 된다. 코드 규칙은 [CLAUDE.md](CLAUDE.md) 참조.
