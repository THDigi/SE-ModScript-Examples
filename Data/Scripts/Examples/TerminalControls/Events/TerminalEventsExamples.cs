using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace Digi.Examples.TerminalControls.Events
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class TerminalEventsExamples : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter += CustomActionGetter;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter -= CustomActionGetter;
        }

        IMyTerminalControlButton SampleButton; // for the add example below

        // gets called every time player has to see controls (or actions), and also gets called per selected block if multiple.
        // the list of controls (or actions) in the parameters is filled with the ones that are going to be shown for this block instance.
        // you can modify the list by moving things around, removing or even adding (see caution below).
        // however, you should NOT modify the instances from the list, they're still the same global controls and doing so will affect any block that uses those controls.
        void CustomControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            // Let's say we want to move CustomData button to be first... for ALL blocks!
            {
                int index = -1;
                for(int i = 0; i < controls.Count; i++)
                {
                    IMyTerminalControl control = controls[i];
                    if(control.Id == "CustomData")
                    {
                        index = i;
                        break;
                    }
                }

                if(index != -1)
                {
                    IMyTerminalControl control = controls[index];
                    controls.RemoveAt(index);
                    controls.Insert(0, control);
                }
            }

            // Or let's say PB just has too many controls, get rid of everything except Edit, but only on smallgrid =)
            // Downside of this is that it's undetectable by other mods, like build vision for example.
            // Ideal way to remove controls is to append a condition to their Visible callback, see the /Hiding/ folder example.
            // Neither of these methods prevent mods or PBs from using them, unless permanently removed... and even then, the block interface likely has a way around.
            if(block is IMyProgrammableBlock && block.CubeGrid.GridSizeEnum == MyCubeSize.Small)
            {
                for(int i = controls.Count - 1; i >= 0; i--)
                {
                    IMyTerminalControl control = controls[i];
                    if(control.Id != "Edit")
                    {
                        controls.RemoveAt(i);
                    }
                }
            }

            // Can also add controls:
            // Downside or upside is that PB cannot see these. Mods can but only if they specifically hook this event and read the list.
            // CAUTION: Don't create new controls every time! Not just because allocation but mods could be storing the references (like buildinfo does for actions), effectively leading to memory leaks.
            if(block is IMyCockpit)
            {
                if(SampleButton == null)
                {
                    SampleButton = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyCockpit>("YourMod_SomeUniqueId");
                    SampleButton.Action = (b) =>
                    {
                        var cockpit = b as IMyCockpit;
                        if(cockpit != null)
                        {
                            cockpit.RemovePilot();
                            // its xmldoc says "call on server" so it wouldn't work outside of singleplayer.
                            // in cases like this you have to synchronize it manually by sending a packet to server with the block entityId and have it call the method.
                        }
                    };
                    SampleButton.Title = MyStringId.GetOrCompute("Clickbait!");

                    // Don't use MyAPIGateway.TerminalControls.AddControl() on controls meant to be added using this event.
                }

                controls.AddOrInsert(SampleButton, 4);
            }
        }

        // similar to the above really, can sort, remove and even add (with caution)
        void CustomActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
        }
    }
}