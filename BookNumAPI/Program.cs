using BookNumAPI.Match.Events;
using BookNumAPI.Match.Events.Handlers;
using BookNumAPI.Match.Hubs;
using BookNumAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BookNumAPI");

// 1. 基本服務註冊
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddDbContext<BookNumApiContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddScoped<IEventPublisher, InMemoryEventPublisher>();
builder.Services.AddScoped<IEventHandler<MatchChatRoomCreatedEvent>, ChatRoomCreatedHandler>();
builder.Services.AddSignalR();
builder.Services.AddHttpClient();

// 2. CORS 設定 (整合版：保留允許憑證，這對 SignalR 來說是必需的)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // 允許傳遞憑證 (SignalR 連線必備)
    });
});

// 3. JWT 身分驗證設定
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings.GetValue<string>("SecretKey");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
            ValidateAudience = true,
            ValidAudience = jwtSettings.GetValue<string>("Audience"),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
        };

        // 讓 SignalR 也能讀取到 JWT Token (因為 WebSocket 無法把 Token 放進 Header)
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chatHub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// 4. Swagger 設定 (整合版：加入 JWT 授權 UI)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BookNum API",
        Version = "v1"
    });

    // 讓 Swagger 介面支援輸入 JWT Token (右上角 Authorize)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "請直接輸入你取得的 Token (不需輸入 Bearer)。"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ----- Middleware Pipeline -----

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BookNum API v1");
    c.RoutePrefix = "swagger";
});

// 自動打開 Swagger 瀏覽器
var url = "https://localhost:7091/swagger/index.html";
try
{
    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    {
        FileName = url,
        UseShellExecute = true
    });
}
catch
{
    Console.WriteLine($"請手動打開瀏覽器並輸入 {url}");
}

// 補回第一份的 Https 導向與靜態檔案支援
app.UseHttpsRedirection();
app.UseStaticFiles();

// CORS 必須放在 Auth 之前
app.UseCors("AllowFrontend");

// 身分驗證 (Authentication) 必須在 授權 (Authorization) 之前
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();