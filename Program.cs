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
    /// 자동 투자 시스템 진입점 (ASP.NET Core Web API).
    /// Headless 백그라운드 서비스로 24시간 자동 매매를 수행하며,
    /// REST API를 통해 외부에서 상태 조회 및 제어가 가능합니다.
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
                // 환경변수(민감정보) → appsettings.json → SQLite DB 우선순위
                AppConfigManager.Initialize(builder.Configuration);
                NotificationService.Initialize(builder.Configuration);

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
