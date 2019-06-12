using System.Collections.Generic;

namespace Kakegurui.Web.Models
{
    public class PageModel<T>
    {
        public List<T> Datas { get; set; }
        public int Total { get; set; }
    }
}
