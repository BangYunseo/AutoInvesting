---
trigger: always_on
---

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
| `test` | 테스트 | `test: QuantFilter 단위 테스트 추가` |
| `perf` | 성능 개선 | `perf: 환율 조회 캐싱 적용` |
| `ci` | CI 설정 | `ci: GitHub Actions 빌드 워크플로우 추가` |
| `rename` | 파일/폴더 이름 변경만 | `rename: SmartOrder → SmartOrderEngine 파일명 변경` |
| `remove` | 파일 삭제만 | `remove: 미사용 LegacyBroker.cs 삭제` |
| `build` | 빌드 파일 수정 | `build: .NET 8.0 타겟 프레임워크 명시` |

```
# ✅ 올바른 예
feat: KIS API 토큰 매니저 구현
fix: 보유 잔고 조회 시 빈 응답 예외 처리 추가

# ❌ 금지 — 스코프 사용
feat(core): KIS API 토큰 매니저 구현
```

## 브랜치 전략

| 브랜치 | 용도 |
|--------|------|
| `main` | 안정 버전 (배포 가능) |
| `dev` | 개발 통합 브랜치 |
| `feature/phase3-kis-api` | Phase 3 KIS 실거래 연동 |
| `feature/phase4-ai-engine` | Phase 4 AI 시장분석 엔진 |
| `fix/xxx` | 버그 수정 |
| `docs/xxx` | 문서 작업 |
