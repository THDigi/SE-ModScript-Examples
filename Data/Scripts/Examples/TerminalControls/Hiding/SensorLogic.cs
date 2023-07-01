using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples.TerminalControls.Hiding
{
    // For more info about the gamelogic comp see https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/BasicExample_GameLogicAndSession/GameLogic.cs
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SensorBlock), false, "SmallBlockSensor")]
    public class SensorLogic : MyGameLogicComponent
    {
        IMySensorBlock Sensor;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            HideControlsExample.DoOnce();

            Sensor = (IMySensorBlock)Entity;

            if(Sensor.CubeGrid?.Physics == null)
                return; // ignore ghost/projected grids

            // the bonus part, enforcing it to stay a specific value.
            if(MyAPIGateway.Multiplayer.IsServer) // serverside only to avoid network spam
            {
                NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
            }
        }

        public override void UpdateAfterSimulation()
        {
            if(Sensor.DetectAsteroids)
            {
                Sensor.DetectAsteroids = false;
            }
        }
    }
}