using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class MissileCoordinator
        {
            #region Properties
            public int ID { get; private set; }
            public Dictionary<long, MissileInfo> ActiveMissiles { get; private set; }
            public HashSet<long> ActiveMissileIDs => ActiveMissiles.Keys.ToHashSet();
            #endregion

            #region Components
            public List<MissileBay> MissileBays { get; private set; }
            #endregion

            private Program _program;
            private CommunicationHandler _communicationHandler;
            private IMyCubeBlock _referenceBlock;
            private long _selfID;

            public MissileCoordinator(Program program, int id, int numberOfMissileBays, IMyCubeBlock referenceBlock, long selfID, CommunicationHandler communicationHandler)
            {
                _program = program;
                ID = id;
                _referenceBlock = referenceBlock;
                _selfID = selfID;
                _communicationHandler = communicationHandler;

                MissileBays = new List<MissileBay>();
                for (int i = 0; i < numberOfMissileBays; i++)
                {
                    MissileBays.Add(new MissileBay(_program, i, _selfID, _communicationHandler.SelfAddress));
                }
            }
        }
    }
}
