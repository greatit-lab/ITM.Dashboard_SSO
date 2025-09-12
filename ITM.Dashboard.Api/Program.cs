// ITM.Dashboard.Api/Program.cs

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp",
        builder => builder.WithOrigins("https://localhost:7263") // Blazor 앱의 주소 (포트 번호는 다를 수 있음)
                           .AllowAnyHeader()
                           .AllowAnyMethod());
});

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddHttpClient();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS 정책 사용 (이전 단계에서 추가함)
app.UseCors("AllowBlazorApp");

app.UseAuthorization();

app.MapControllers();

app.Run();
