using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PirateDatabase.Models
{
    class Bounty
    {
        public int Id { get; set; }
        public int PirateId { get; set; }
        public int KingdomId { get; set; }
        public int Reward { get; set; }
        public string Crime { get; set; }

    }
}
