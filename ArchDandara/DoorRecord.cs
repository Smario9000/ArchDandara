//DoorRecord.cs

using Newtonsoft.Json;

namespace ArchDandara
{
    // =============================================================================================
    //  DoorRecord
    // ---------------------------------------------------------------------------------------------
    //  This class describes a *single* door inside the game world.
    //
    //  Every door contains:
    //    • DoorName       — Unity object name (ex: "LeftExit")
    //    • OtherSideScene — The target Unity scene the door connects to
    //    • FakeSpawnID    — Reserved for future Archipelago warp logic
    //    • PosX/Y/Z       — The exact world position where the door exists
    //
    //  This structure is serialized to JSON by DoorJsonManager.
    //  The JSON file becomes your editable door routing table.
    // =============================================================================================

    public class DoorRecord
    {
        // SceneName is intentionally NOT serialized.
        // It is part of SceneDoorGroup's organization instead.
        [JsonIgnore]
        public string SceneName;

        // Name of the GameObject containing the Door component.
        public string DoorName;

        // The destination scene this door leads to.
        public string OtherSideScene;

        // Placeholder for future custom warp logic.
        public string FakeSpawnID;

        // Raw 3D position of the door in Unity space.
        public float PosX;
        public float PosY;
        public float PosZ;

        // Human-readable summary.
        public override string ToString()
        {
            return $"{DoorName} → {OtherSideScene}";
        }
    }
}