//SceneDoorGroup.cs

using System.Collections.Generic;

namespace ArchDandara
{
    public class SceneDoorGroup
    {
        public string SceneName;
        public List<DoorRecord> Doors = new List<DoorRecord>();
        
        public override string ToString()
        {
            return $"{SceneName} ({Doors.Count} doors)";
        }
    }
}