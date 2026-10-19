using YojigenShift.YiXueLab.API.Common;
using YojigenShift.YiXueLab.API.Setup;
using YojigenShift.YiXueLab.BLL;
using YojigenShift.YiXueLab.DAL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// 1. 注册三层架构 (DAL -> BLL -> API)
string connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("未找到数据库连接字符串 DefaultConnection");

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDataAccessLayer(connStr);
builder.Services.AddBusinessLogicLayer();
builder.Services.AddApiPresentationLayer(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// 2. 自动初始化首个群主管理员账号（解决邀请码冷启动问题）
await DbSeeder.EnsureInitialAdminSeededAsync(app.Services, app.Configuration);

// 3. 配置中间件管道
app.UseCors("VueFrontendPolicy");
app.UseHttpsRedirection();
app.UseResponseCaching();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
