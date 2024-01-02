using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ModifyTimerLimits : MySessionComponentBase
    {
        public override void LoadData()
        {
            //SetForAll(500, 6000);

            // change only specific timers, use the TypeId and SubtypeId from <Id> tag!
            SetFor("TimerBlock/TimerBlockLarge", 500, 6000);
            SetFor("TimerBlock/TimerBlockSmall", 500, 6000);
        }

        protected override void UnloadData()
        {
        }

        void SetForAll(int minMs, int maxMs)
        {
            foreach(var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var timerDef = def as MyTimerBlockDefinition;
                if(timerDef != null)
                {
                    timerDef.MinDelay = minMs;
                    timerDef.MaxDelay = maxMs;

                    //MyLog.Default.WriteLine($"{defId} modified delay limits to {minMs} to {maxMs} ms.");
                }
            }
        }

        void SetFor(string id, int minMs, int maxMs)
        {
            MyDefinitionId defId;
            if(!MyDefinitionId.TryParse(id, out defId))
            {
                MyDefinitionErrors.Add((MyModContext)ModContext, $"Invalid definition typeId: '{id}' (subtype isn't checked here)", TErrorSeverity.Warning);
                return;
            }

            var timerDef = MyDefinitionManager.Static.GetCubeBlockDefinition(defId) as MyTimerBlockDefinition;
            if(timerDef == null)
            {
                MyDefinitionErrors.Add((MyModContext)ModContext, $"Cannot find definition with id: '{id}' (or it exists but it's not a timer block definition)", TErrorSeverity.Warning);
                return;
            }

            timerDef.MinDelay = minMs;
            timerDef.MaxDelay = maxMs;
            timerDef.Context = (MyModContext)ModContext;

            //MyLog.Default.WriteLine($"{defId} modified delay limits to {minMs} to {maxMs} ms.");
        }
    }

    // the vanilla Delay slider ignores the Min/MaxDelay tags but everything else seems to not
    // so this piece of code changes its limits to respect those tags.
    static class TerminalControls
    {
        static bool ControlsModified = false;

        public static void Setup()
        {
            if(ControlsModified)
                return;

            ControlsModified = true;

            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyTimerBlock>(out controls);

            foreach(var c in controls)
            {
                var cs = c as IMyTerminalControlSlider;
                if(cs != null && c.Id == "TriggerDelay")
                {
                    cs.SetLimits(TimerDelayMin, TimerDelayMax);
                    cs.Writer = TimerDelayWriter;
                }
            }
        }

        static void TimerDelayWriter(IMyTerminalBlock block, StringBuilder sb)
        {
            IMyTimerBlock timer = block as IMyTimerBlock;
            if(timer == null)
                return;

            TimeSpan span = TimeSpan.FromSeconds(timer.TriggerDelay);

            if(span.Days >= 1)
                sb.Append(span.Days).Append("d ");

            sb.Append(span.Hours.ToString("00")).Append(":");
            sb.Append(span.Minutes.ToString("00")).Append(":");
            sb.Append(span.Seconds.ToString("00")).Append(".");
            sb.Append(span.Milliseconds.ToString("000")).Append("");
        }

        static float TimerDelayMin(IMyTerminalBlock block)
        {
            var timerDef = block?.SlimBlock?.BlockDefinition as MyTimerBlockDefinition;

            if(timerDef != null)
                return timerDef.MinDelay / 1000f;
            else
                return 1f; // default from MyTimerBlock.CreateTerminalControls()
        }

        static float TimerDelayMax(IMyTerminalBlock block)
        {
            var timerDef = block?.SlimBlock?.BlockDefinition as MyTimerBlockDefinition;

            if(timerDef != null)
                return timerDef.MaxDelay / 1000f;
            else
                return 3600f; // default from MyTimerBlock.CreateTerminalControls()
        }
    }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TimerBlock), false)]
    public class TimerBlock : MyGameLogicComponent
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            TerminalControls.Setup(); // HACK: because terminal controls are weird
        }
    }
}
