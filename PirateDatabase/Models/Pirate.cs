using Accessibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PirateDatabase.Models
{
    class Pirate
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int RankId { get; set; }
        public int Reputation { get; set; }
        public int ShipId { get; set; }

        //För att visa sökresultat
        public string ShipName{ get; set; }
        public int CrewNumber { get; set; }
        public string RankName { get; set; }
    }
}
