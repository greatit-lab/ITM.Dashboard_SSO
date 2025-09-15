// ITM.Dashboard.Web.Client/Models/PreAlignDataDto.cs
using System;

namespace ITM.Dashboard.Web.Client.Models
{
    public class PreAlignDataDto
    {
        public DateTime Timestamp { get; set; }
        public double Xmm { get; set; }
        public double Ymm { get; set; }
        public double Notch { get; set; }
    }
}
