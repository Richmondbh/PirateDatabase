using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PirateDatabase.Models
{
    class Ship
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int ShipTypeId { get; set; }
    }
}
