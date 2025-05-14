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
        public bool IsSunken { get; set; }

        //För att visa Drukna pirater/ överlevt pirater
        public int TotalPirateCount { get; set; }
        public int SurvivorsCount { get; set; }

        public int DeadCount { get; set; }
    }
}
