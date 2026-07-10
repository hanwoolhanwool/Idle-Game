# Git 커밋 컨벤션

이 프로젝트는 [Conventional Commits 1.0.0](https://www.conventionalcommits.org/) 을 기반으로 하며,
Unity 프로젝트 특성에 맞춰 일부 규칙을 확장한다.

## 1. 기본 형식

```
<type>(<scope>): <subject>

<body>

<footer>
```

- **한 커밋 = 하나의 논리적 변경.** 여러 기능/수정이 섞이면 커밋을 분리한다.
- `type`, `subject` 는 필수, `scope`/`body`/`footer` 는 선택.
- **언어 규칙**: `type`/`scope` 는 항상 영어 소문자, 콜론(`:`) 뒤의 `subject` 와 `body` 는 **한국어**로 작성한다.
- 제목(`subject`)은 무엇을 했는지 간결한 한국어 서술로 쓰고, 끝에 마침표를 찍지 않으며 50자 이내를 권장한다.
- 본문(`body`)은 제목과 한 줄 띄우고, **무엇을·왜** 바꿨는지 설명한다(어떻게는 코드가 설명).

## 2. Type

| type       | 의미                              | 예시                                              |
| ---------- | --------------------------------- | ------------------------------------------------- |
| `feat`     | 기능 추가                          | `feat(combat): add critical hit system`           |
| `fix`      | 버그 수정                          | `fix(camera): prevent clipping through walls`     |
| `refactor` | 동작 변화 없는 구조 개선            | `refactor(player): split movement controller`     |
| `perf`     | 성능 개선                          | `perf(rendering): reduce draw calls in lobby`     |
| `chore`    | 빌드·설정·패키지·기타 작업          | `chore(packages): update addressables`            |
| `docs`     | 문서 수정                          | `docs(readme): add build instructions`            |
| `test`     | 테스트 추가/수정                   | `test(inventory): add item stacking tests`        |
| `style`    | 포맷팅·네이밍 등 로직 변화 없음     | `style(ui): rename HUD prefab labels`             |
| `build`    | 빌드 시스템 관련                   | `build(android): update keystore config`          |
| `ci`       | CI/CD 관련                        | `ci: add Unity test runner workflow`              |

## 3. Scope

변경이 속한 도메인/모듈을 나타낸다. 필요 시 새 scope 를 추가하되, 아래 목록을 우선 사용한다.

```
player      enemy-ai    combat      inventory
ui          hud         camera      animation
audio       scene       save        network
addressables shader     vfx         input
physics     build       android     ios
skill       stats       state       docs
packages
```

- scope 가 여러 도메인에 걸치면, 커밋을 분리하는 것을 먼저 고려한다.
- 분리가 불가능한 광범위 변경만 scope 를 생략한다.

## 4. Unity 프로젝트 전용 규칙 (표준 대비 확장)

- **`.cs` 와 `.cs.meta` 는 항상 같은 커밋에 포함**한다. `.meta` 누락은 다른 팀원에게서 GUID 깨짐/참조 유실을 유발한다.
- 폴더 신설 시 생성되는 **`<folder>.meta` 도 함께** 커밋한다.
- `.unity`(씬), `.prefab`, `.asset` 바이너리성 변경은 로직 커밋과 섞지 말고
  `scene`/`prefab` scope 로 **분리**한다. (충돌·리뷰 난이도 때문)
- 패키지 변경은 `Packages/manifest.json` 과 `Packages/packages-lock.json` 을
  **함께** `chore(packages)` 로 커밋한다.

## 5. Breaking Change

동작·API 가 하위 호환을 깨면 다음 중 하나로 표기한다.

- 제목의 type 뒤에 `!`: `refactor(input)!: rename IJoystickInputReader to IMoveInputSource`
- 또는 footer: `BREAKING CHANGE: <설명>`

## 6. 커밋 템플릿 사용

레포 루트의 `.gitmessage` 를 커밋 템플릿으로 등록하면 형식을 자동으로 안내받는다.

```bash
git config commit.template .gitmessage
```

이후 `git commit` (에디터 열림) 실행 시 템플릿이 채워진다.

## 7. 예시

```
feat(skill): 쿨다운 기반 스킬 시전 파이프라인 추가

ISkillEffect/ICastGate 계약과 SkillCooldownTracker 를 도입해
쿨다운 기반 스킬 시전 흐름을 구성한다. 상태머신이 시전 가능
여부를 게이팅하도록 PlayerStateMachineCastGate 로 연동한다.
```

```
refactor(input)!: IJoystickInputReader 를 IMoveInputSource 로 변경

이동 입력 소스를 조이스틱 외(자동전투 등)로 확장하기 위해
계약 이름을 입력 수단 중립적으로 변경한다.

BREAKING CHANGE: IJoystickInputReader 를 구현하던 타입은
IMoveInputSource 로 갱신해야 한다.
```
