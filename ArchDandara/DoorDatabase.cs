//DoorDatabase.cs

using System.Collections.Generic;

namespace ArchDandara
{

    public class DoorDatabase
    {
        public List<SceneDoorGroup> Scenes = new List<SceneDoorGroup>();

        public override string ToString()
        {
            return $"Door Database: {Scenes.Count} scenes";
        }
    }
}