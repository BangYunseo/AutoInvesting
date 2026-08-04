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
    /// 자동 투자 시스템 
    /// 24시간 자동 매매
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                Logger.Initialize();
                Logger.Info("[Program] 자동 투자 API 서버 초기화 중...");

                var builder = WebApplication.CreateBuilder(args);
                builder.Host.UseSerilog();

                // ── 로컬 시크릿 파일 명시적 로드 ──
                builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

                // ── 설정 체계 초기화 ──
                // 환경변수(민감정보) → appsettings.json → PostgreSQL DB 우선순위
                AppConfigManager.Initialize(builder.Configuration);
                NotificationService.Initialize(builder.Configuration);

                // ── 암호화 유틸 초기화 (MASTER_KEY: 시크릿 암복호화 + 토큰 서명) ──
                CryptoUtil.Initialize(builder.Configuration);
                if (!CryptoUtil.IsConfigured)
                    Logger.Warn("[Program] MASTER_KEY 미설정 — 시크릿이 평문으로 저장됩니다. 운영 환경에서는 반드시 설정하세요.");

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
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[Program] 치명적 오류: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Logger.FlushAndClose();
            }
        }
    }
}
