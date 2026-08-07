---
title: AutoInvesting GitLab CI 이전 검토
date: 2026-08-07
company: [개인]
tags: [gitlab-ci, github-actions, 스케줄러, 크론, 자동매매, 인프라이전]
status: rejected
---

# AutoInvesting GitLab CI 이전 검토

## 개요
> GitHub Actions에서 발생한 hosted runner 배정 장애를 계기로 AutoInvesting의 일일 스케줄 실행을 GitLab CI/CD Scheduled Pipelines로 이전할지 검토했고, **2026-08-07에 이전하지 않기로 결론**했다. 이 문서는 그 판단의 근거 기록이다.

## 배경 / 목적
2026-08-06 GitHub Actions 인시던트(`qcvjkzcs7j74`)로 `daily-run` 워크플로우가 job 배정 단계에서 실패했다. 발생한 오류는 두 가지였다.

```text
The job was not acquired by Runner of type hosted even after multiple attempts
Internal server error. Correlation ID: 9360bbb1-716a-437e-91d1-0949ef1ac4c1
```

두 오류 모두 워크플로우 정의나 애플리케이션 코드가 아닌 GitHub 인프라 측 원인이며, 해당 job은 **실행이 시작조차 되지 않았다**.

## 본문

### 이전 판단의 전제

이번 장애는 GitHub Actions 고유의 결함이 아니라, 무료 티어 관리형 스케줄러 전반이 공유하는 특성이 드러난 사례다. 따라서 플랫폼 이전 자체는 다음 문제를 해결하지 못한다.

- 관리형 러너 풀 장애로 인한 실행 누락
- 스케줄 트리거의 지연 및 스킵
- 실패를 사용자가 인지하지 못하는 무성실패(silent failure)

GitLab 역시 자체 인시던트 이력을 가지며 무료 티어에는 SLA가 적용되지 않는다. 이전 여부와 무관하게 아래 두 가지가 선행되어야 실질적인 안정성이 확보된다.

- 실행 여부 외부 감시 장치(heartbeat)
- 실행 로직의 중복 방지 가드

이 두 가지가 없는 상태의 플랫폼 이전은 실패 지점을 옮기는 작업에 그친다.

### 판단 — 이전하지 않는다 (2026-08-07 기각)

GitHub Actions와 GitLab CI/CD 모두 무료 티어 관리형 스케줄러이므로 실행 누락·무성실패라는 성질이 같고, 이전은 실패 지점을 옮기는 데 그친다. 또한 이 저장소에서 CI가 하는 일은 타이머 하나뿐이다 — `daily-run.yml`·`reconcile.yml`은 `actions/checkout`조차 없이 `curl`로 엔드포인트를 때리는 워크플로라 러너 이미지·의존성 관리 이점이 성립하지 않는다. GitLab은 스케줄이 저장소 이력에 남지 않아 cron 변경 추적이 오히려 나빠진다.

대안으로 검토했던 것도 함께 기각한다 — 인앱 타이머(`BackgroundService`)는 Render 무료 인스턴스가 잠들면 타이머가 오류 없이 멈추므로 금지(`.agents/rules/architecture.md`), cron-job.org 이중화는 사용자 판단으로 제외.

### 남은 항목

- **중복 실행 가드: 해결됨.** 월 마커를 DB 전용으로 읽고 조회 실패 시 매수하지 않으며(fail-closed), 장 마감 후 체결 대사가 전량 미체결만 되돌린다.
- **heartbeat: 미결.** 정해진 시간 안에 신호가 없으면 알림이 뜨는 외부 감시가 필요하고, job이 시작조차 못 한 이번 인시던트를 잡을 수 있는 유일한 장치다. 다만 워크플로 마지막에 `curl`을 한 줄 더 두는 방식으로는 부족하다 — `dca-run`은 작업 전에 202를 반환하므로 그 신호는 **러너가 떴다는 사실만** 증명하고 매수 여부는 증명하지 않는다. 집행 결과 신호는 애플리케이션이 보내야 한다.
- cron 시각은 정각을 피해 이미 분산돼 있다(`10 15 1-31 * *`).

## 정리 / 결론

- 이번 장애의 원인은 GitHub 인프라이며 워크플로우 정의나 코드 문제가 아니다
- **GitLab 이전은 하지 않는다(2026-08-07 결정).** CI가 기여하는 것은 타이머뿐이고, 옮겨도 무료 티어 관리형 스케줄러라는 성질은 같아 실행 누락은 줄지 않는다 — 이득이 없다
- **인앱 타이머(BackgroundService)도 하지 않는다.** 만들었다가 전량 되돌렸다 — Render 무료 인스턴스는 잠들어 타이머가 오류 없이 멈추고, 비수면 인스턴스 비용은 지불하지 않기로 했다. 금지 규칙만 `architecture.md`에 남겼다
- **cron-job.org 등 2차 크론 소스도 채택하지 않는다**(사용자 기각). 트리거는 GitHub Actions 하나로 유지하고 `daily-run.yml`의 `'10 15 1-31 * *'`는 그대로 둔다. 언젠가 2차 소스를 붙이더라도 1차와 **최소 2시간**은 띄워야 한다 — `RunDcaCycleAsync`에 락이 없고 `dca-run`이 작업 전에 202를 반환하므로, 겹치면 양쪽이 빈 마커를 읽고 둘 다 산다
- 이 문서가 "확인해야 한다"고 적은 중복 실행 가드는 **이미 있었지만 조회 실패 시 열리는(fail-open) 상태**였고, 2026-08-07에 `TryReadDb` fail-closed로 굳혔다. 남은 실제 과제는 heartbeat(미실행 통보) 하나다

## 참고

- GitHub Status 인시던트: https://www.githubstatus.com/incidents/qcvjkzcs7j74
- GitHub Status 이력: https://www.githubstatus.com/history
- GitLab CI/CD 공식 문서: https://docs.gitlab.com/ee/ci/
- `.agents/rules/architecture.md` — 인앱 스케줄러 금지
