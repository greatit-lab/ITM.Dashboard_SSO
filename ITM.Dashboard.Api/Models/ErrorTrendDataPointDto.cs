// ITM.Dashboard.Api/Models/ErrorTrendDataPointDto.cs
using System;

namespace ITM.Dashboard.Api.Models
{
    public class ErrorTrendDataPointDto
    {
        public DateTime Date { get; set; }
        public long Count { get; set; }
    }
}
