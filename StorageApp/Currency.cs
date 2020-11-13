using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StorageApp
{
    class Currency
    {
        public int Id { get; set; }
        public String name { get; set; }
        public Double Exchange { get; set; }
        public DateTime updateTime { get; set; }
    }
}
