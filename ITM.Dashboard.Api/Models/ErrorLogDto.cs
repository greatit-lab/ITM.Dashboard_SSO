// ITM.Dashboard.Api/Models/ErrorLogDto.cs
using System;

namespace ITM.Dashboard.Api.Models
{
    public class ErrorLogDto
    {
        public DateTime TimeStamp { get; set; }
        public string EqpId { get; set; }
        public string ErrorId { get; set; }
        public string ErrorLabel { get; set; }
        public string ErrorDesc { get; set; }
        public string ExtraMessage1 { get; set; }
        public string ExtraMessage2 { get; set; }
    }
}
