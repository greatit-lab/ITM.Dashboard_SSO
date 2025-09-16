// ITM.Dashboard.Api/Models/PreAlignDataDto.cs
using System;

namespace ITM.Dashboard.Api.Models
{
    public class PreAlignDataDto
    {
        public DateTime Timestamp { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Notch { get; set; }
    }
}
