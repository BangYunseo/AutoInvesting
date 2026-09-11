# Git 컨벤션

## 커밋 메시지 형식

```
<type>: <subject>
```

- 스코프(scope) 없이 type만 사용
- 커밋 메시지는 **한국어**로 작성
- 제목은 50자 이내
- 기능 단위별로 끊어서 커밋 (한 번에 모든 변경 커밋 금지)

## 커밋 타입

| 타입 | 설명 | 예시 |
|------|------|------|
| `feat` | 새 기능 | `feat: KIS API 토큰 매니저 구현` |
| `fix` | 버그 수정 | `fix: 스케줄러 중복 실행 방지 로직 수정` |
| `refactor` | 리팩토링 | `refactor: SPA 패널 전환 방식 적용` |
| `docs` | 문서 | `docs: README 증권사 정보 업데이트` |
| `style` | 코드 스타일 | `style: 코드 포맷팅 정리` |
| `design` | UI 디자인 변경 | `design: 대시보드 카드 레이아웃 수정` |
| `chore` | 빌드/설정 | `chore: gitignore 시크릿 파일 제외 추가` |
| `test` | 테스트 | `test: PlanPurchases 배분 시나리오 단위 테스트 추가` |
| `perf` | 성능 개선 | `perf: 환율 조회 캐싱 적용` |
| `ci` | CI 설정 | `ci: GitHub Actions 빌드 워크플로우 추가` |
| `rename` | 파일/폴더 이름 변경만 | `rename: DcaEngine → DcaAccumulationEngine 파일명 변경` |
| `remove` | 파일 삭제만 | `remove: 미사용 LegacyBroker.cs 삭제` |
| `build` | 빌드 파일 수정 | `build: .NET 8.0 타겟 프레임워크 명시` |

```
# ✅ 올바른 예
feat: KIS API 토큰 매니저 구현
fix: 보유 잔고 조회 시 빈 응답 예외 처리 추가

# ❌ 금지 — 스코프 사용
feat(core): KIS API 토큰 매니저 구현
```

## 브랜치 전략 (솔로 개발 기준)

이 프로젝트는 **1인 개발**이며 `main`이 곧 Render.com 배포 대상이다.
`main`에 직접 커밋하면 검증 전 코드가 곧바로 실거래 서버로 나가므로, **`dev` 작업 + `main` 배포** 2브랜치 방식을 따른다 (2026-09-11 전환).

| 브랜치 | 용도 |
|--------|------|
| `dev` | **기본 작업 브랜치.** 일상적인 커밋은 전부 여기서 한다. 배포되지 않으며 크론도 돌지 않는다. |
| `main` | **배포 전용 브랜치.** `dev`에서 검증이 끝난 것만 머지한다. 항상 배포 가능 상태를 유지한다. |
| `feature/xxx`, `fix/xxx` | 큰 실험·위험한 변경에만 임시로 사용하고 `dev`로 머지한 뒤 삭제한다. 보통은 `dev`에서 바로 작업한다. |

### `dev`가 안전한 이유

GitHub Actions의 `schedule` 트리거는 **기본 브랜치(`main`)에서만 발화한다.**
`dev`에 무엇을 커밋하든 `daily-run.yml`·`reconcile.yml`이 돌지 않으므로, 실자금 집행 경로와 완전히 분리된다.
Render도 `main`만 배포 대상으로 본다.

### 운영 원칙 (MUST)
- **일상 작업은 `dev`에서 한다.** `main`에 직접 커밋하지 않는다 (핫픽스도 `dev`를 거친다).
- **`dev`를 오래 쌓아두지 않는다.** 검증이 끝난 단위는 즉시 `main`에 머지한다. 쌓아두면 머지 충돌이 커지고 `main`이 오래 낡은 상태로 남는다.
- **`main` 머지는 fast-forward만 쓴다.** `dev`가 `main`을 포함하고 있으면 충돌이 나지 않는다. 갈라졌다면 `dev`에서 `main`을 먼저 따라잡는다.
- 머지 전 `git log --oneline main..dev`로 올릴 커밋을, `git log --oneline dev..main`으로 분기 여부를 확인한다 (후자가 비어야 fast-forward 가능).
- push는 항상 명시적 확인 후 진행한다 — 특히 `main` push는 프로덕션 재배포를 유발한다. `dev` push는 배포와 무관하므로 자유롭게 한다.

### dev → main 라이프사이클 (MUST)

`main`에 반영할 때는 아래를 **순서대로** 모두 거친다. 어느 한 단계라도 건너뛰지 않는다.

1. **dev 작업**: `dev`에서 커밋한다. 커밋 단위·메시지 규칙은 브랜치와 무관하게 동일하다.
2. **검증**: `dotnet build`(오류 0)와 `dotnet test`(전건 통과)를 확인한다. 필요 시 `IS_PAPER_TRADING`(SimBroker) 모드로 동작을 먼저 본다.
3. **사용자 테스트 완료 확인**: **사용자가 직접 테스트해 OK한 것을 확인한 뒤에만** 다음 단계로 진행한다. 에이전트 판단만으로 `main` 머지를 진행하지 않는다 (`main` = 프로덕션 배포 대상).
4. **main 머지**: 사용자 승인 후 `git checkout main` → `git merge --ff-only dev`. 머지 직후 `git push origin main`으로 동기화한다(재배포 트리거 — push 전 명시적 확인).
5. **dev 복귀**: `git checkout dev`. `dev`는 삭제하지 않는다(상시 브랜치).

임시 브랜치(`feature/xxx`)를 썼다면 `dev`로 머지한 뒤 **로컬·원격 모두 삭제**한다 (`git branch -d <name>` + `git push origin --delete <name>`).

> **드리프트 방지 (교훈)**: 브랜치가 오래 뒤처지면(예: 수십 커밋) 머지 시 충돌이 폭증하고 최신 수정이 유실될 위험이 커진다. 1인 개발은 리뷰 대기가 없어 "나중에 한꺼번에"가 되기 쉬우므로, **검증이 끝난 단위는 미루지 말고 바로 `main`에 올린다.**

> 협업 인원이 늘면 PR 리뷰를 필수로 걸고 `main`에 브랜치 보호 규칙을 추가한다.
