using AutoInvest.Data;
using AutoInvest.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;

namespace AutoInvest
{
    /// <summary>
    /// 적립식 매수 시스템
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 프로세스 진입점
        /// 0 정상 종료, 1 기동 거부·치명적 오류
        /// </summary>
        public static int Main(string[] args)
        {
            try
            {
                Logger.Initialize();
                Logger.Info("[Init] API 서버 초기화...");

                var builder = WebApplication.CreateBuilder(args);
                builder.Host.UseSerilog();

                // ── 로컬 시크릿 파일 명시적 로드 ──
                // reloadOnChange: false — 감시 대상마다 리눅스 inotify instance를 1개 쓴다.
                // Render 무료는 호스트 한도(128)를 이웃 컨테이너와 공유하므로, 자리를 못 얻으면
                // CreateBuilder 단계에서 기동이 실패한다(2026-09-02 21:09 장애: 기동 1~2초 만에
                // [FTL], 지수 백오프로 재시작 반복). 이 앱의 설정 변경 경로는 Render 환경변수 수정
                // + 재배포뿐이라 재로드는 애초에 쓸 수 없는 기능이다 — true 로 되돌리지 말 것.
                // CreateBuilder 가 자동 등록하는 appsettings.json·appsettings.Production.json 은
                // 여기서 못 건드리므로 환경변수 DOTNET_hostBuilder__reloadConfigOnChange=false 로 끈다.
                builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false);

                // ── 설정 체계 초기화 ──
                // 환경변수(민감정보) → appsettings.json → PostgreSQL DB 우선순위
                AppConfigManager.Initialize(builder.Configuration);
                NotificationService.Initialize(builder.Configuration);

                // ── 암호화 유틸 초기화 (MASTER_KEY: 시크릿 암복호화 + 토큰 서명) ──
                // 키가 없으면 경고만 남기고 뜨던 것을 기동 중단으로 바꿨다. 이 상태로 떠 있는 편이
                // 더 위험하기 때문이다: 저장된 암호문을 복호화하지 못해 빈 값이 되고
                // (CryptoUtil.DecryptSecret), SessionManager가 "앱키 없음"으로 판단해 조용히
                // SimBrokerClient로 폴백한다 — 화면에는 체결이 찍히지만 실제로는 아무것도 사지 않는다.
                // 사람 로그인도 토큰 서명 키가 없어 이미 500이 되므로, 반쪽으로 떠 있을 이유가 없다.
                // 로컬 개발은 appsettings.local.json에 MASTER_KEY 한 줄로 해결된다(.gitignore 대상).
                CryptoUtil.Initialize(builder.Configuration);
                if (!CryptoUtil.IsConfigured)
                {
                    Logger.Fatal("[Program] MASTER_KEY 미설정(또는 base64 32바이트 아님) — 기동을 중단합니다. "
                        + "환경변수 또는 appsettings.local.json에 MASTER_KEY를 설정하세요.");
                    return 1;
                }

                // ── 서비스 등록 ──
                builder.Services.AddControllers(options =>
                {
                    options.Filters.Add<AutoInvest.Utils.ApiKeyAuthAttribute>(); // 전역 API Key 보안 적용
                });
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                builder.Services.AddHealthChecks();

                // ── 의존성 주입 ──
                builder.Services.AddSingleton(DBManager.Instance);

                // ── 로그 DB 영구 적재 연결 (DBManager 초기화 완료 후) ──
                // Logger는 Data를 참조하지 않으므로 여기서 SystemLogDAO.Insert를 훅으로 주입한다.
                AutoInvest.Utils.Logger.DbSink = AutoInvest.Data.DAO.SystemLogDAO.Insert;
                AutoInvest.Data.DAO.SystemLogDAO.PruneOlderThan(90); // 오래된 로그 정리(무한 증가 방지)
                builder.Services.AddSingleton<AutoInvest.Core.SessionManager>();
                builder.Services.AddScoped<AutoInvest.Core.DailyExecutionService>();

                var app = builder.Build();

                // Swagger는 개발 환경에서만 노출한다.
                // 전역 인증 필터(ApiKeyAuthAttribute)는 MVC 액션 필터라 미들웨어인 Swagger에는 걸리지 않는다.
                // 프로덕션에 켜두면 인증 없이 전체 API 표면(경로·파라미터·DTO 스키마)이 읽힌다 —
                // 실행은 401로 막히지만, ManualOrderRequest의 acknowledgeTax나 dca-run의 force처럼
                // UI가 쓰지 않는 우회 수단까지 명세로 광고하게 된다. 배포 서버의 API 명세는
                // Documents/reference/API_REFERENCE.md를 본다.
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                // ── 정적 파일 제공 (프론트엔드 React) ──
                app.UseDefaultFiles();
                app.UseStaticFiles();

                app.UseAuthorization();
                app.MapControllers();
                app.MapHealthChecks("/api/health");

                // ── SPA Fallback (프론트엔드 라우팅) ──
                app.MapFallbackToFile("index.html");

                Logger.Info("[Program] 자동 투자 API 서버 시작 완료");
                app.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[Program] 치명적 오류: {ex.Message}\n{ex.StackTrace}");
                return 1;
            }
            finally
            {
                Logger.FlushAndClose();
            }
        }
    }
}
