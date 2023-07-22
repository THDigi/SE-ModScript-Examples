using System;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Examples
{
    // for info on the component, refer to: https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/BasicExample_GameLogicAndSession/GameLogic.cs
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false)]
    public class Example_WriteToDetailedInfo : MyGameLogicComponent
    {
        IMyTerminalBlock Block;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            Block = (IMyTerminalBlock)Entity;

            if(Block.CubeGrid?.Physics == null)
                return; // ignore ghost grids

            // this event gets invoked (from all mods) by calling block.RefreshCustomInfo()
            // which does not automatically refresh the block's detailedinfo, that one depends on the block type.
            // in a recent major we got block.SetDetailedInfoDirty() which does just that!
            Block.AppendingCustomInfo += AppendingCustomInfo;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        void AppendingCustomInfo(IMyTerminalBlock block, StringBuilder sb)
        {
            try // only for non-critical code
            {
                // NOTE: don't Clear() the StringBuilder, it's the same instance given to all mods.

                sb.Append("ETA: ").Append(60 - DateTime.Now.Second).Append("\n");
            }
            catch(Exception e)
            {
                LogError(e);
            }
        }

        public override void UpdateAfterSimulation100()
        {
            try // only for non-critical code
            {
                // ideally you want to refresh this only when necessary but this is a good compromise to only refresh it if player is in the terminal.
                // this check still doesn't mean you're looking at even the same grid's terminal as this block, for that there's other ways to check it if needed.
                if(MyAPIGateway.Gui.GetCurrentScreen == MyTerminalPageEnum.ControlPanel)
                {
                    Block.RefreshCustomInfo();
                    Block.SetDetailedInfoDirty();
                }
            }
            catch(Exception e)
            {
                LogError(e);
            }
        }

        void LogError(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR on {GetType().FullName}: {e}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ERROR on {GetType().FullName}: Send SpaceEngineers.Log to mod author]", 10000, MyFontEnum.Red);
        }
    }
}
