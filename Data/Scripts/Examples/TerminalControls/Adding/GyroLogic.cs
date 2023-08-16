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

        // these are going to be set or retrieved by the terminal controls (as seen in the terminal control's Getter and Setter).

        // as mentioned in the other .cs file, the terminal stuff are only GUI.
        // if you want the values to persist over world reloads and be sent to clients you'll need to implement that yourself.
        // see: https://github.com/THDigi/SE-ModScript-Examples/wiki/Save-&-Sync-ways

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