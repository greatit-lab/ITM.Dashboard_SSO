// ITM.Dashboard.Api/Models/PagedResult.cs
using System.Collections.Generic;

namespace ITM.Dashboard.Api.Models
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public long TotalItems { get; set; }
    }
}
