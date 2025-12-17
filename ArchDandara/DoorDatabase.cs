//DoorDatabase.cs

using System.Collections.Generic;

namespace ArchDandara
{
    // =============================================================================================
    //  DoorDatabase
    // ---------------------------------------------------------------------------------------------
    //  This is the TOP-LEVEL structure that gets serialized into JSON.
    //  It contains *every* scene + the doors inside those scenes.
    //
    //  Structure:
    //     {
    //        "Scenes": [
    //            { SceneDoorGroup },
    //            { SceneDoorGroup },
    //            ...
    //        ]
    //     }
    //
    //  DoorJsonManager builds this, saves it, reloads it,
    //  and updates entries as you explore the world.
    // =============================================================================================

    public class DoorDatabase
    {
        // List of all grouped scenes containing door data.
        public List<SceneDoorGroup> Scenes = new List<SceneDoorGroup>();

        // Quick readable summary.
        public override string ToString()
        {
            return $"Door Database: {Scenes.Count} scenes";
        }
    }
}