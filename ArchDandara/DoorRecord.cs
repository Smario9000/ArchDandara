//DoorRecord.cs
using Newtonsoft.Json;

namespace ArchDandara
{

    public class DoorRecord
    {
        [JsonIgnore] 
        public string sceneName;   // used at runtime, NOT serialized
        
        public string doorName;
        public string otherSideScene;
        public string fakeSpawnID;
        // Position of the door (for mapping)
        public float posX;
        public float posY;
        public float posZ;


        public override string ToString()
        {
            return $"{doorName} → {otherSideScene}";
        }
    }
}