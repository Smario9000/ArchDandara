//SceneDoorGroup.cs

using System.Collections.Generic;

namespace ArchDandara
{
    public class SceneDoorGroup
    {
        public string sceneName;
        public List<DoorRecord> doors = new List<DoorRecord>();
        
        public override string ToString()
        {
            return $"{sceneName} ({doors.Count} doors)";
        }
    }
}