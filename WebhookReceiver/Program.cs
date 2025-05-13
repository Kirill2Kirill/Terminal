using System;
using System.Text;
using System.Text.Json;
using Serilog;
using Serilog.Extensions.Hosting;


namespace WebhookReceiver
{
    public class Program
    {
        public static WebhookCommandHandler WebhookCommandHandler { get; set; }
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Настройка Serilog
            builder.Host.UseSerilog((context, loggerConfig) =>
            {
                loggerConfig.
                    MinimumLevel.Error()
                    .WriteTo.Console() // Вывод в консоль
                    //.WriteTo.Sink(new MemoryLogSink()) // Логируем в стек
                    .WriteTo.File("logs/ASPlog-.txt", rollingInterval: RollingInterval.Day); // Запись в файл
            });

            var app = builder.Build();

            // Создаем и читаем юзеров
            UserStorage.Initialize();

            var users = UserStorage.ReadUsersAsync().Result;
            WebhookCommandHandler = new WebhookCommandHandler(users);

            Logger.Warning("test warning");
            Logger.Information("test info");
            Logger.Error("test error");

            app.Map("/", OpenIndexPageAsync);
            app.Map("/index", OpenIndexPageAsync);
            app.Map("/all_logs", OpenAllLogsPageAsync);
            app.Map("/users", OpenUsersPageAsync);

            app.MapGet("/api/logs", GetLogs);
            app.MapPost("/api/updateUsers", UpdateUsersAsync);
            app.MapGet("/api/users", async () =>
            {
                var json = await UserStorage.ReadUsersAsync();
                return Results.Json(json);
            });

            app.MapPost("/api/webhook", ProcessWebHookAsync);

            app.Run();
        }

        public static async Task ProcessWebHookAsync(HttpContext context)
        {
            try
            {
                using var reader = new StreamReader(context.Request.Body);
                var body = await reader.ReadToEndAsync();

                var signal = JsonSerializer.Deserialize<TradingViewSignal>(body);

                Logger.Information("Получен веб-хук: " + body);

                if (signal == null)
                {
                    context.Response.StatusCode = 400;
                    Logger.Error("Некорректный JSON: " + body);
                    await context.Response.WriteAsync("❌ Ошибка: Некорректный JSON");
                    return;
                }

                await WebhookCommandHandler.HandleAsync(signal);
                await context.Response.WriteAsync("✅ Webhook обработан");
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка: {ex.Message}", ex);
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("❌ Внутренняя ошибка сервера");
            }
        }

        public static async Task<IResult> UpdateUsersAsync(HttpContext context)
        {
            var users = await context.Request.ReadFromJsonAsync<List<User>>();
            if (users != null)
            {
                UserStorage.UpdateUsers(users);
                await WebhookCommandHandler.UpdateUsersAsync();
                Logger.Information("Пользователи обновлены: " + JsonSerializer.Serialize(users));
                return Results.Ok("Пользователи обновлены!");
            }
            Logger.Error("Ошибка обновления пользователей: " + JsonSerializer.Serialize(users));
            return Results.BadRequest("Ошибка обновления пользователей.");
        }

        public static IResult GetLogs()
        {
            return Results.Json(Logger.GetLogs()); // Передаем JSON с логами из нового статического логгера
        }

        private static async Task OpenPageAsync(HttpContext context, String name)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.SendFileAsync("html/" + name);
        }

        public static async Task OpenIndexPageAsync(HttpContext context)
        {
            await OpenPageAsync(context, "index.html");
        }

        public static async Task OpenAllLogsPageAsync(HttpContext context)
        {
            await OpenPageAsync(context, "all_logs.html");
        }

        public static async Task OpenUsersPageAsync(HttpContext context)
        {
            await OpenPageAsync(context, "users.html");
        }
    }
}
