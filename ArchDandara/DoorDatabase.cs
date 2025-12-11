using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArchDandara
{

    public class DoorDatabase
    {
        public List<DoorRecord> doors = new List<DoorRecord>();

        public override string ToString()
        {
            return $"Door Database: {string.Join(",", doors.Select(x => x.ToString()).ToArray())}";
        }
    }
}