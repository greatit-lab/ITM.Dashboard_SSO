// ITM.Dashboard.Web.Client/Models/PagedResult.cs
using System.Collections.Generic;

namespace ITM.Dashboard.Web.Client.Models
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public long TotalItems { get; set; }
    }
}
