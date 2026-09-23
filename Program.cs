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

                builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

                AppConfigManager.Initialize(builder.Configuration);
                NotificationService.Initialize(builder.Configuration);

                // ── 암호화 유틸 초기화 (MASTER_KEY: 세션 토큰 서명 키의 파생 원본) ──
                // 키가 없으면 경고만 남기고 뜨던 것을 기동 중단으로 바꿨다. 토큰 서명 키가 없으면
                // 사람 로그인이 전부 500이 되어, 화면은 열리는데 아무도 들어갈 수 없는 반쪽 상태로
                // 뜬다. 그 상태로 떠 있느니 기동을 거부하고 원인을 로그 한 줄로 남기는 편이 낫다.
                // 로컬 개발은 appsettings.local.json에 MASTER_KEY 한 줄로 해결된다(.gitignore 대상).
                //
                // 참고: AUTH_TOKEN_SECRET만 있어도 토큰 서명은 가능하므로(CryptoUtil.GetTokenKey)
                // 이 검사는 실제 필요보다 한 칸 엄격하다. 배포 환경에 두 키가 모두 있어 지금은
                // 무해하며, 느슨하게 푸는 변경은 별도 판단 대상으로 남긴다.
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

                // PriceController의 현재가 캐시(표시·검증 전용). 주문가 경로는 캐시를 타지 않는다.
                builder.Services.AddMemoryCache();

                // ── 의존성 주입 ──
                builder.Services.AddSingleton(DBManager.Instance);

                // ── 로그 DB 영구 적재 연결 (DBManager 초기화 완료 후) ──
                // Logger는 Data를 참조하지 않으므로 여기서 SystemLogDAO.Insert를 훅으로 주입한다.
                AutoInvest.Utils.Logger.DbSink = AutoInvest.Data.DAO.SystemLogDAO.Insert;
                AutoInvest.Data.DAO.SystemLogDAO.PruneOlderThan(90); // 오래된 로그 정리(무한 증가 방지)

                // ── 로컬 개발용 관리자 계정 자동 생성 (Development 전용) ──
                if (builder.Environment.IsDevelopment())
                {
                    SeedLocalAdmin(builder.Configuration);
                }
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

        /// <summary>
        /// 로컬 개발 환경에서 관리자 계정이 아직 없을 때, <c>appsettings.local.json</c>의
        /// <c>Admin:Username</c>·<c>Admin:Password</c>로 계정을 한 번 생성합니다.
        ///
        /// 최초 설정(<c>POST /api/auth/setup</c>)은 전역 인증 필터를 타므로 <c>x-api-key</c>를 붙여
        /// 직접 호출해야 하는데, 로컬 DB를 새로 만들 때마다 반복하기엔 번거롭습니다. 그 한 번을
        /// 대신합니다. 운영의 최초 설정 경로는 그대로 두며, 이 메서드는 아래 셋을 모두 만족할 때만
        /// 동작합니다 — 하나라도 어긋나면 아무 일도 하지 않습니다.
        ///
        /// <list type="number">
        /// <item><description><c>Development</c> 환경일 것 (호출부에서 검사).</description></item>
        /// <item><description>설정에 아이디·비밀번호가 둘 다 있을 것. 값은 <c>appsettings.local.json</c>에만
        /// 두며, 이 파일은 <c>.gitignore</c>·<c>.dockerignore</c> 대상이라 저장소(공개)와 배포 이미지
        /// 어느 쪽에도 들어가지 않는다 — 그래서 소스에 자격증명을 박지 않는다.</description></item>
        /// <item><description>DB에 관리자 해시가 아직 없을 것. 조회에 실패하면 "없음"으로 오판하지 않고
        /// 건너뛴다(fail-closed) — 기존 계정을 덮어쓰지 않기 위함이다.</description></item>
        /// </list>
        /// </summary>
        /// <param name="config">아이디·비밀번호를 읽을 설정 소스</param>
        private static void SeedLocalAdmin(IConfiguration config)
        {
            string username = (config["Admin:Username"] ?? string.Empty).Trim();
            string password = config["Admin:Password"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return;
            }

            if (!AppConfigManager.TryReadDb("ADMIN_PASSWORD_HASH", out string? existingHash))
            {
                Logger.Warn("[Seed] 관리자 설정 여부를 확인할 수 없어 로컬 계정 생성을 건너뜁니다 (DB 조회 실패).");
                return;
            }

            if (!string.IsNullOrWhiteSpace(existingHash))
            {
                return; // 이미 있는 계정은 덮어쓰지 않는다
            }

            // 사용자명 → 해시 순서를 지킨다(AuthController.Setup과 동일). 뒤집으면 도중 실패 시
            // 해시만 남아 setup은 409, 로그인은 사용자명 공백으로 거부되는 잠김이 된다.
            bool userSaved = AppConfigManager.Set("ADMIN_USERNAME", username);
            bool hashSaved = AppConfigManager.Set("ADMIN_PASSWORD_HASH", CryptoUtil.HashPassword(password));

            if (!userSaved || !hashSaved)
            {
                Logger.Error("[Seed] 로컬 관리자 계정 저장 실패 — 설정이 반영되지 않았습니다.");
                return;
            }

            // 아이디·비밀번호는 로그에 남기지 않는다. TB_SYSTEM_LOG는 영구 저장소다.
            Logger.Warn("[Seed] 로컬 개발용 관리자 계정을 생성했습니다. 이 경로는 Development에서만 동작합니다.");
        }
    }
}
