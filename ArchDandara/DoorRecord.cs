using System;
using UnityEngine;

namespace ArchDandara
{

    public class DoorRecord
    {
        public string sceneName;
        public string doorName;
        public string otherSideScene;
        public string fakeSpawnID;
        // Position of the door (for mapping)
        public float posX;
        public float posY;
        public float posZ;


        public override string ToString()
        {
            return $"SceneName: {sceneName} DoorName: {doorName} " +
                   $"OtherSpawnSide: {otherSideScene} Pos: ({posX}, {posY}, {posZ})";
        }
    }
}