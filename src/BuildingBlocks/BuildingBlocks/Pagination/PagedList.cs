using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Pagination
{
    public class PagedList<T> : IPagedList<T> where T : class
    {
        public PagedList() => this.Items = new List<T>();
        public int Count { get; set; }
        public IList<T> Items { get; set; }
    }
}
