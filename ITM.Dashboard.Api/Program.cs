// 파일 경로: ITM.Dashboard.Api/Program.cs

using ITM.Dashboard.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = WebApplication.CreateBuilder(args);

// CORS 정책 추가: 특정 주소(Blazor UI)에서의 요청을 허용합니다.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorApp",
        builder => builder.WithOrigins("https://localhost:7263") // Blazor UI의 주소
                           .AllowAnyHeader()
                           .AllowAnyMethod());
});

// Add services to the container.

// API 컨트롤러 기능을 서비스에 등록합니다.
builder.Services.AddControllers();
// Swagger/OpenAPI (API 테스트 UI) 관련 서비스를 등록합니다.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
// 개발 환경일 때만 Swagger UI를 활성화합니다.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS로 리디렉션합니다.
app.UseHttpsRedirection();

// 위에서 정의한 CORS 정책을 사용하도록 설정합니다.
app.UseCors("AllowBlazorApp");

app.UseAuthorization();

// 등록된 컨트롤러들의 경로를 실제로 매핑합니다.
app.MapControllers();

app.Run();
