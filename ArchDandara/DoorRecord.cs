//DoorRecord.cs
using Newtonsoft.Json;

namespace ArchDandara
{

    public class DoorRecord
    {
        [JsonIgnore] 
        public string SceneName;   // used at runtime, NOT serialized
        
        public string DoorName;
        public string OtherSideScene;
        public string FakeSpawnID;
        // Position of the door (for mapping)
        public float PosX;
        public float PosY;
        public float PosZ;


        public override string ToString()
        {
            return $"{DoorName} → {OtherSideScene}";
        }
    }
}