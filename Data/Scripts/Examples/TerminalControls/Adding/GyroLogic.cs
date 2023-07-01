using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples.TerminalControls.Adding
{
    // For more info about the gamelogic comp see https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/BasicExample_GameLogicAndSession/GameLogic.cs
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Gyro), false, "SmallBlockGyro")]
    public class GyroLogic : MyGameLogicComponent
    {
        IMyGyro Gyro;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            GyroTerminalControls.DoOnce(ModContext);

            Gyro = (IMyGyro)Entity;
            if(Gyro.CubeGrid?.Physics == null)
                return; // ignore ghost/projected grids

            // stuff and things
        }

        public bool Terminal_ExampleToggle
        {
            get
            {
                return Gyro?.Enabled ?? false;
            }
            set
            {
                if(Gyro != null)
                    Gyro.Enabled = value;
            }
        }

        public float Terminal_ExampleFloat { get; set; }
    }
}