//DoorDatabase.cs

using System.Collections.Generic;
using System.Linq;

namespace ArchDandara
{

    public class DoorDatabase
    {
        public List<SceneDoorGroup> scenes = new List<SceneDoorGroup>();

        public override string ToString()
        {
            return $"Door Database: {scenes.Count} scenes";
        }
    }
}