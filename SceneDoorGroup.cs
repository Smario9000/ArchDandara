//SceneDoorGroup.cs

using System.Collections.Generic;

namespace ArchDandara
{
    // =============================================================================================
    //  SceneDoorGroup
    // ---------------------------------------------------------------------------------------------
    //  This class represents the idea of "All doors inside a single scene."
    //  It holds:
    //    • SceneName — the name of the Unity scene (ex: "Valley", "TutorialRoom1")
    //    • Doors     — a list containing EVERY DoorRecord found in that scene
    //
    //  Why group by scene?
    //    • JSON becomes MUCH easier to read
    //    • We avoid dozens of flat entries with no organization
    //    • Modders can open the JSON and visually understand room layout
    //
    //  DoorJsonManager builds and maintains these groups automatically.
    // =============================================================================================

    public class SceneDoorGroup
    {
        // Name of the Unity scene this group represents.
        public string SceneName;

        // A collection of all doors found in this scene.
        public List<DoorRecord> Doors = new List<DoorRecord>();

        // Developer-level summary format for debugging/logging.
        public override string ToString()
        {
            return $"{SceneName} ({Doors.Count} doors)";
        }
    }
}