using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox.Game.Localization;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples.TerminalControls.Adding
{
    // Example of adding terminal controls and actions to a specific gyro subtype.
    // It can be generalized to the entire type by simply not using the visible-filtering methods.


    /*
     * Important notes about controls/actions:
     * 
     * 1. They are global per block type! Not per block instance, not per block type+subtype.
     * Which is why they need to be added once per world and isolated to avoid accidental use of instanced things from a gamelogic.
     * 
     * 2. Should only be retrieved/edited/added after the block type fully spawned because of game bugs.
     * Simplest way is with a gamelogic component via first update (not Init(), that's too early).
     * 
     * 3. They're only UI! They do not save nor sync anything, they only read and call things locally.
     * That means you have to roll your own implementation of saving and synchronizing the data.
     * 
     * Also keep in mind that these can be called by mods and PBs, which also includes being called dedicated-server-side.
     * Make sure your backend code does all the checks, including ensuring limits for sliders and such.
     */

    public static class GyroTerminalControls
    {
        const string IdPrefix = "YourMod_"; // highly recommended to tag your properties/actions like this to avoid colliding with other mods'

        static bool Done = false;

        public static void DoOnce(IMyModContext context) // called by GyroLogic.cs
        {
            if(Done)
                return;
            Done = true;

            CreateActions(context); // the modcontext is for accessing mod's full path, to be used in action icon example.
            CreateControls();
            CreateProperties();
        }

        static void CreateActions(IMyModContext context)
        {
            // yes, there's only one type of action
            {
                var a = MyAPIGateway.TerminalControls.CreateAction<IMyGyro>(IdPrefix + "SampleAction");

                a.Name = new StringBuilder("Sample Action");

                // If the action is visible for grouped blocks (as long as they all have this action).
                a.ValidForGroups = true;

                // The icon shown in the list and top-right of the block icon in toolbar.
                a.Icon = @"Textures\GUI\Icons\Actions\CharacterToggle.dds";
                // For paths inside the mod folder you need to supply an absolute path which can be retrieved from a session or gamelogic comp's ModContext.
                //a.Icon = Path.Combine(context.ModPath, @"Textures\YourIcon.dds");

                // Called when the toolbar slot is triggered
                // Should not be unassigned.
                a.Action = (b) => { };

                // The status of the action, shown in toolbar icon text and can also be read by mods or PBs.
                a.Writer = (b, sb) =>
                {
                    sb.Append("Hi\nthere");
                };

                // What toolbar types to NOT allow this action for.
                // Can be left unassigned to allow all toolbar types.
                // The below are the options used by jumpdrive's Jump action as an example.
                //a.InvalidToolbarTypes = new List<MyToolbarType>()
                //{
                //    MyToolbarType.ButtonPanel,
                //    MyToolbarType.Character,
                //    MyToolbarType.Seat
                //};
                // PB checks if it's valid for ButtonPanel before allowing the action to be invoked.

                // Wether the action is to be visible for the given block instance.
                // Can be left unassigned as it defaults to true.
                // Warning: gets called per tick while in toolbar for each block there, including each block in groups.
                //   It also can be called by mods or PBs.
                a.Enabled = CustomVisibleCondition;

                MyAPIGateway.TerminalControls.AddAction<IMyGyro>(a);
            }
        }

        static void CreateControls()
        {
            // all the control types:
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyGyro>(""); // separators don't store the id
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyGyro>(IdPrefix + "SampleLabel");
                c.Label = MyStringId.GetOrCompute("Sample Label");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyGyro>(IdPrefix + "SampleOnOff");
                c.Title = MyStringId.GetOrCompute("Sample OnOff");
                c.Tooltip = MyStringId.GetOrCompute("This does some stuff!");
                c.SupportsMultipleBlocks = true; // wether this control should be visible when multiple blocks are selected (as long as they all have this control).

                // callbacks to determine if the control should be visible or not-grayed-out(Enabled) depending on whatever custom condition you want, given a block instance.
                // optional, they both default to true.
                c.Visible = CustomVisibleCondition;
                //c.Enabled = CustomVisibleCondition;

                c.OnText = MySpaceTexts.SwitchText_On;
                c.OffText = MyStringId.GetOrCompute("Meh");

                // setters and getters should both be assigned on all controls that have them, to avoid errors in mods or PB scripts getting exceptions from them.
                c.Getter = (b) => b?.GameLogic?.GetAs<GyroLogic>()?.Terminal_ExampleToggle ?? false;
                c.Setter = (b, v) =>
                {
                    var logic = b?.GameLogic?.GetAs<GyroLogic>();
                    if(logic != null)
                        logic.Terminal_ExampleToggle = v;
                };

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyGyro>(IdPrefix + "SampleCheckbox");
                c.Title = MyStringId.GetOrCompute("Sample Checkbox");
                c.Tooltip = MyStringId.GetOrCompute("This does some stuff!");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;
                c.Enabled = (b) => false; // to see how the grayed out ones look

                c.Getter = (b) => true;
                c.Setter = (b, v) => { };

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyGyro>(IdPrefix + "SampleSlider");
                c.Title = MyStringId.GetOrCompute("Sample Slider");
                c.Tooltip = MyStringId.GetOrCompute("This does some stuff!");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.Setter = (b, v) =>
                {
                    var logic = b?.GameLogic?.GetAs<GyroLogic>();
                    if(logic != null)
                        logic.Terminal_ExampleFloat = MathHelper.Clamp(v, 0f, 10f); // just a heads up that the given value here is not clamped by the game, a mod or PB can give lower or higher than the limits!
                };
                c.Getter = (b) => b?.GameLogic?.GetAs<GyroLogic>()?.Terminal_ExampleFloat ?? 0;

                c.SetLimits(0, 10);
                //c.SetLimits((b) => 0, (b) => 10); // overload with callbacks to define limits based on the block instance.
                //c.SetDualLogLimits(0, 10, 2); // all these also have callback overloads
                //c.SetLogLimits(0, 10);

                // called when the value changes so that you can display it next to the label
                c.Writer = (b, sb) =>
                {
                    var logic = b?.GameLogic?.GetAs<GyroLogic>();
                    if(logic != null)
                    {
                        float val = logic.Terminal_ExampleFloat;
                        sb.Append(Math.Round(val, 2)).Append(" quacks");
                    }
                };

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyGyro>(IdPrefix + "SampleButton");
                c.Title = MyStringId.GetOrCompute("Sample Button");
                c.Tooltip = MyStringId.GetOrCompute("This does some stuff!");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.Action = (b) => { };

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, IMyGyro>(IdPrefix + "SampleColor");
                c.Title = MyStringId.GetOrCompute("Sample Color");
                c.Tooltip = MyStringId.GetOrCompute("This does some stuff!");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.Getter = (b) => new Color(255, 0, 255);
                c.Setter = (b, color) => { };

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, IMyGyro>(IdPrefix + "SampleComboBox");
                c.Title = MyStringId.GetOrCompute("Sample ComboBox");
                c.Tooltip = MyStringId.GetOrCompute("This does some stuff!");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.Getter = (b) => 0;
                c.Setter = (b, key) => { };
                c.ComboBoxContent = (list) =>
                {
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 0, Value = MyStringId.GetOrCompute("Value A") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 1, Value = MyStringId.GetOrCompute("Value B") });
                    list.Add(new MyTerminalControlComboBoxItem() { Key = 2, Value = MyStringId.GetOrCompute("Value C") });
                };

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyGyro>(IdPrefix + "SampleListBox");
                c.Title = MyStringId.GetOrCompute("Sample ListBox");
                //c.Tooltip = MyStringId.GetOrCompute("This does some stuff!"); // presenece of this tooltip prevents per-item tooltips
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.VisibleRowsCount = 3;
                c.Multiselect = false; // wether player can select muliple at once (ctrl+click, click&shift+click, etc)
                c.ListContent = (b, content, preSelect) =>
                {
                    // this is the getter, gets called when the list needs to be shown/refreshed.
                    // the 2 lists in the parameters are there for you to fill:
                    //   `content` with the options to show
                    //   `preSelect` with the options to be already selected (only needed if you want to persist selections).
                    // NOTE: `preSelect` requires the same instance(s) as the ones given to `content`, giving it new MyTerminalControlListBoxItem would not work.

                    for(int i = 1; i <= 5; ++i)
                    {
                        var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute($"Item {i}"),
                                                                    tooltip: MyStringId.GetOrCompute($"This is item number {i} alright."),
                                                                    userData: i); // userData can be whatever you wish and it's retrievable in the ItemSelected call.

                        content.Add(item);

                        if(MyUtils.GetRandomInt(1, 6) == i)
                            preSelect.Add(item);
                    }
                };
                c.ItemSelected = (b, selected) =>
                {
                    // the setter, called when local player clicks on one or more things in the list, those are given to you through `selected`.
                };

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyGyro>(IdPrefix + "SampleTextBox");
                c.Title = MyStringId.GetOrCompute("Sample TextBox");
                c.Tooltip = MyStringId.GetOrCompute("This does some stuff!");
                c.SupportsMultipleBlocks = true;
                c.Visible = CustomVisibleCondition;

                c.Setter = (b, v) => { };
                c.Getter = (b) => new StringBuilder("Ney!");

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(c);
            }
        }

        static bool CustomVisibleCondition(IMyTerminalBlock b)
        {
            // only visible for the blocks having this gamelogic comp
            return b?.GameLogic?.GetAs<GyroLogic>() != null;
        }

        static void CreateProperties()
        {
            // terminal controls automatically generate properties like these, but you can also add new ones manually without the GUI counterpart.
            // The main use case is for PB to be able to read them.
            // The type given is only limited by access, can only do SE or .NET types, nothing custom (except methods because the wrapper Func/Action is .NET).
            // For APIs, one cand send a IReadOnlyDictionary<string, Delegate> for a list of callbacks. Just be sure to use a ImmutableDictionary to avoid getting your API hijacked.
            {
                var p = MyAPIGateway.TerminalControls.CreateProperty<Vector3, IMyGyro>(IdPrefix + "SampleProp");
                // SupportsMultipleBlocks, Enabled and Visible don't have a use for this, and Title/Tooltip don't exist.

                p.Getter = (b) =>
                {
                    float interferrence;
                    Vector3 gravity = MyAPIGateway.Physics.CalculateNaturalGravityAt(b.GetPosition(), out interferrence);
                    return gravity;
                };

                p.Setter = (b, v) =>
                {
                };

                MyAPIGateway.TerminalControls.AddControl<IMyGyro>(p);


                // a mod or PB can use it like:
                //Vector3 vec = gyro.GetValue<Vector3>("YourMod_SampleProp");
                // just careful with sending mutable reference types, there's no serialization inbetween so the mod/PB can mutate your reference.
            }
        }
    }
}