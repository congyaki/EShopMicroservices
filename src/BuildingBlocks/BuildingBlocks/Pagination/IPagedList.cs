using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildingBlocks.Pagination
{
    public interface IPagedList<T> where T : class
    {
        int Count { get; set; }
        IList<T> Items { get; set; }
    }
}
