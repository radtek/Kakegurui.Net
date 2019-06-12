using System;
using System.Collections.Generic;
using System.Security.Claims;
using Kakegurui.Core;
using Kakegurui.Monitor;
using Kakegurui.Web.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Kakegurui.Web
{
    /// <summary>
    /// 系统启动
    /// </summary>
    public abstract class Startup
    {
        /// <summary>
        /// 数据库连接字符串格式
        /// </summary>
        protected static string DbFormat = "server={0};port={1};user={2};password={3};database={4};CharSet=utf8";

        /// <summary>
        /// 配置项
        /// </summary>
        protected IConfiguration _configuration;

        /// <summary>
        /// 服务实例提供者
        /// </summary>
        protected IServiceProvider _serviceProvider;

        /// <summary>
        /// 定时任务线程
        /// </summary>
        protected FixedJobTask _fixedJobTask;

        /// <summary>
        /// 系统监控
        /// </summary>
        private SystemMonitor _systemMonitor;

        /// <summary>
        /// 查询系统状态
        /// </summary>
        public static Func<MonitorStatus> GetStatus;

        /// <summary>
        /// 重启系统
        /// </summary>
        public static Action Restart;

        /// <summary>
        /// 补足遗漏数据
        /// </summary>
        public static Action<DateTime, int> FillEmpty;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration">配置项</param>
        protected Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// 权限
        /// </summary>
        /// <param name="services"></param>
        protected void ConfigureAuthorizations(IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.AddPolicy("admin", policy => policy.RequireClaim(ClaimTypes.Webpage, "00000000"));
            });
        }

        /// <summary>
        /// jwt验证
        /// </summary>
        /// <param name="services"></param>
        protected void ConfigureJWTToken(IServiceCollection services)
        {
            //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            //    .AddJwtBearer(options =>
            //    {
            //        options.TokenValidationParameters = new TokenValidationParameters
            //        {
            //            ValidateIssuer = true,
            //            ValidateAudience = true,
            //            ValidateLifetime = true,
            //            ValidateIssuerSigningKey = true,
            //            ValidAudience = Token.Audience,
            //            ValidIssuer = Token.Issuer,
            //            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Token.Key))
            //        };
            //    });
        }

        /// <summary>
        /// 异常处理
        /// </summary>
        /// <param name="app"></param>
        protected void ConfigureException(IApplicationBuilder app)
        {
            //exception
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";

                    var exceptionHandlerPathFeature =
                        context.Features.Get<IExceptionHandlerPathFeature>();
                    string s = JsonConvert.SerializeObject(new
                    {
                        exceptionHandlerPathFeature.Path,
                        exceptionHandlerPathFeature.Error.Message,
                        exceptionHandlerPathFeature.Error.StackTrace,
                        exceptionHandlerPathFeature.Error.InnerException
                    });
                    await context.Response.WriteAsync(s);
                });
            });

        }

        /// <summary>
        /// websocket
        /// </summary>
        /// <param name="app"></param>
        protected void ConfigureWebSocket(IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.UseTrafficWebSocket();
        }

        /// <summary>
        /// 跨域
        /// </summary>
        /// <param name="app"></param>
        protected void ConfigureCors(IApplicationBuilder app)
        {
            //跨域
            app.UseCors(builder =>
                builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        }

        /// <summary>
        /// 同子类实现的系统配置
        /// </summary>
        /// <param name="app"></param>
        protected abstract void ConfigureCore(IApplicationBuilder app);

        public abstract void ConfigureServices(IServiceCollection services);

        public void Configure(IApplicationBuilder app, IApplicationLifetime appLifetime)
        {
            _serviceProvider = app.ApplicationServices;
            appLifetime.ApplicationStarted.Register(Start);
            appLifetime.ApplicationStopping.Register(Stop);
            GetStatus = GetStatusCore;
            Restart = RestartCore;
            ConfigureCore(app);
        }

        /// <summary>
        /// 供子类实现的系统系统
        /// </summary>
        protected abstract void StartCore();

        /// <summary>
        /// 供子类实现的系统停止
        /// </summary>
        protected abstract void StopCore();

        /// <summary>
        /// 启动系统
        /// </summary>
        private void Start()
        {
            _fixedJobTask = new FixedJobTask();
            _systemMonitor = new SystemMonitor();
            _fixedJobTask.AddFixedJob(_systemMonitor, DateTimeLevel.Minute, TimeSpan.Zero, "cpu");
            StartCore();
            _fixedJobTask.Start();
        }

        /// <summary>
        /// 停止系统
        /// </summary>
        private void Stop()
        {
            _fixedJobTask.Stop();
            StopCore();
        }

        /// <summary>
        /// 供子类实现的重启系统
        /// </summary>
        protected abstract void RestartCore();

        /// <summary>
        /// 供子类实现的填充系统状态
        /// </summary>
        /// <param name="status">系统状态</param>
        protected abstract void FillStatusCore(MonitorStatus status);

        /// <summary>
        /// 获取系统状态
        /// </summary>
        /// <returns>系统状态</returns>
        protected MonitorStatus GetStatusCore()
        {
            MonitorStatus status = new MonitorStatus
            {
                Cpu = _systemMonitor.Cpu,
                Memory = _systemMonitor.Memory,
                ThreadCount = _systemMonitor.ThreadCount,
                WarningLogs = new List<string>(LogPool.Warnings),
                ErrorLogs = new List<string>(LogPool.Errors),
                FixedJobs = new List<string>(),
                Adapters = new List<string>(),
                Branchs = new List<string>(),
                Connections = new List<string>()
            };

            foreach (var pair in _fixedJobTask.FixedJobs)
            {
                status.FixedJobs.Add($"{pair.Value.Name} {pair.Value.Level} {pair.Value.ChangeTime:yyyy-MM-dd HH:mm:ss}");
            }
            FillStatusCore(status);
            return status;
        }
    }
}
