using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Utils;

namespace Digi.Examples.TerminalControls.Hiding
{
    // In this example we're hiding the "Detect asteroids" terminal control and terminal action. Also bonus, enforcing it to stay false.
    //  All this only on a specific sensor block to show doing it properly without breaking other mods trying to do the same.
    //
    // This is also compatible with multiple mods doing the same thing on the same type, but for different subtypes.
    //   For example another mod could have the same on largegrid sensor to hide a different control, or even the same control, it would work properly.


    // You will need the internal IDs for the terminal properties and/or actions you wish to edit.
    //
    // For vanilla ones, pick one way:
    // - Use the commented-out code I left in the Edit*() methods below.
    // - Use the MDK wiki that is usually up-to-date: https://github.com/malware-dev/MDK-SE/wiki/List-Of-Terminal-Properties-and-Actions
    // - With a decompiler go to the block's class and find CreateTerminalControls()
    //
    // For ones added by other mods, pick one way:
    // - Use the commented-out code I left in the Edit*() methods below.
    // - Check the mod's workshop page if they happen to list them there
    // - In your mod, use block.GetProperties()/GetActions() on a block and dump'em to log
    // - With your mod code, hook the CustomControlGetter or CustomActionGetter events and dump all the stuff to log, then open terminal or rightclick that block in a toolbar GUI to trigger it
    // - Open the mod's downloaded folder (by searching its workshopid in the steam folder) and dive through its .cs files to find where they're declaring them


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

        static bool AppendedCondition(IMyTerminalBlock block)
        {
            // if block has this gamelogic component then return false to hide the control/action.
            return block?.GameLogic?.GetAs<SensorLogic>() == null;
        }

        static void EditControls()
        {
            List<IMyTerminalControl> controls;

            // mind the IMySensorBlock input
            MyAPIGateway.TerminalControls.GetControls<IMySensorBlock>(out controls);

            foreach(IMyTerminalControl c in controls)
            {
                // a quick way to dump all IDs to log
                //string name = MyTexts.GetString((c as IMyTerminalControlTitleTooltip)?.Title.String ?? "N/A");
                //string valueType = (c as ITerminalProperty)?.TypeName ?? "N/A";
                //MyLog.Default.WriteLine($"[DEV] terminal property: id='{c.Id}'; type='{c.GetType().Name}'; valueType='{valueType}'; displayName='{name}'");

                switch(c.Id)
                {
                    case "Detect Asteroids": // for IDs uncomment above or for alternatives see at the very top of the file
                    {
                        // appends a custom condition after the original condition with an AND.

                        // pick which way you want it to work:
                        c.Enabled = TerminalChainedDelegate.Create(c.Enabled, AppendedCondition); // grays out
                        //c.Visible = TerminalChainedDelegate.Create(c.Visible, AppendedCondition); // hides
                        break;
                    }
                }
            }
        }

        static void EditActions()
        {
            List<IMyTerminalAction> actions;

            // mind the IMySensorBlock input
            MyAPIGateway.TerminalControls.GetActions<IMySensorBlock>(out actions);

            foreach(IMyTerminalAction a in actions)
            {
                // a quick way to dump all IDs to log 
                //MyLog.Default.WriteLine($"[DEV] toolbar action: id='{a.Id}'; displayName='{a.Name}'");

                switch(a.Id)
                {
                    case "Detect Asteroids": // for IDs uncomment above or for alternatives see at the very top of the file
                    case "Detect Asteroids_On":
                    case "Detect Asteroids_Off":
                    {
                        // appends a custom condition after the original condition with an AND.

                        a.Enabled = TerminalChainedDelegate.Create(a.Enabled, AppendedCondition);
                        // action.Enabled hides it, there is no grayed-out for actions.

                        break;
                    }
                }
            }
        }
    }
}