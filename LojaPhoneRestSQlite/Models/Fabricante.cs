using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LojaPhoneRestSQlite.Models
{
    class Fabricante
    {
        [SQLite.PrimaryKey]
        public int id { get; set; }
        public string descricao { get; set; }
    }
}
