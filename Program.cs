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
                Logger.Info("[서버] 자동 투자 API 서버 초기화 중...");

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
                    Logger.Warn("[서버] MASTER_KEY 미설정 — 시크릿이 평문으로 저장됩니다. 운영 환경에서는 반드시 설정하세요.");

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

                app.UseSwagger();
                app.UseSwaggerUI();

                // ── 정적 파일 제공 (프론트엔드 React) ──
                app.UseDefaultFiles();
                app.UseStaticFiles();

                app.UseAuthorization();
                app.MapControllers();
                app.MapHealthChecks("/api/health");

                // ── SPA Fallback (프론트엔드 라우팅) ──
                app.MapFallbackToFile("index.html");

                Logger.Info("[서버] 자동 투자 API 서버 시작 완료");
                app.Run();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"[서버] 치명적 오류: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                Logger.FlushAndClose();
            }
        }
    }
}
