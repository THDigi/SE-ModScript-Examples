using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Digi.Examples.TerminalControls.Hiding
{
    // In this example we're hiding the "Detect asteroids" terminal control and terminal action. Also bonus, enforcing it to stay false.
    //  All this only on smallgrid sensor to show to to properly do that.
    //
    // This is also compatible with multiple mods doing the same thing on the same type, but for different subtypes.
    //   For example another mod could have the same on largegrid sensor to hide a different control, or even the same control, it would work properly.


    // For important notes about terminal controls see: https://github.com/THDigi/SE-ModScript-Examples/blob/master/Data/Scripts/Examples/TerminalControls/Adding/GyroTerminalControls.cs#L21-L35
    public static class HideControlsExample
    {
        static bool Done = false;

        public static void DoOnce() // called by SensorLogic.cs
        {
            if(Done)
                return;

            Done = true;

            EditControls();
            EditActions();
        }

        static bool CustomVisibleCheck(IMyTerminalBlock block)
        {
            // if block has this component then return false to hide the control/action.
            return block?.GameLogic?.GetAs<SensorLogic>() == null;
        }

        static void EditControls()
        {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMySensorBlock>(out controls);

            foreach(IMyTerminalControl c in controls)
            {
                switch(c.Id)
                {
                    case "Detect Asteroids":
                    {
                        // appends a custom condition after the original condition with an AND.

                        // pick which way you want it to work:
                        c.Enabled = TerminalChainedDelegate.Create(c.Enabled, CustomVisibleCheck); // grays out
                        //c.Visible = TerminalChainedDelegate.Create(c.Visible, CustomVisibleCheck); // hides
                        break;
                    }
                }
            }
        }

        static void EditActions()
        {
            List<IMyTerminalAction> actions;
            MyAPIGateway.TerminalControls.GetActions<IMySensorBlock>(out actions);

            foreach(IMyTerminalAction a in actions)
            {
                switch(a.Id)
                {
                    case "Detect Asteroids":
                    case "Detect Asteroids_On":
                    case "Detect Asteroids_Off":
                    {
                        // appends a custom condition after the original condition with an AND.

                        a.Enabled = TerminalChainedDelegate.Create(a.Enabled, CustomVisibleCheck);
                        // action.Enabled hides it, there is no grayed-out for actions.

                        break;
                    }
                }
            }
        }
    }
}