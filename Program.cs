using AutoInvest.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace AutoInvest
{
    /// <summary>
    /// 자동 투자 시스템의 차세대 진입점 (ASP.NET Core Web API).
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                // 콘솔 출력을 지원하는 Logger로 변경하거나 파일 로그 유지
                Logger.Info("자동 투자 API 서버 초기화 중...");

                var builder = WebApplication.CreateBuilder(args);

                // 서비스 등록
                builder.Services.AddControllers();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();

                // 의존성 주입 및 백그라운드 서비스 등록
                builder.Services.AddSingleton<AutoInvest.Core.SessionManager>();
                builder.Services.AddHostedService<AutoInvest.Core.BackgroundServices.TradingBackgroundService>();

                var app = builder.Build();

                // 미들웨어 파이프라인 구성
                if (app.Environment.IsDevelopment() || true) // 임시로 항상 Swagger 열기
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseAuthorization();
                app.MapControllers();

                Logger.Info("자동 투자 API 서버가 포트에서 수신 대기 중입니다.");
                app.Run();
            }
            catch (Exception ex)
            {
                Logger.Fatal($"서버 실행 중 치명적 오류 발생: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
