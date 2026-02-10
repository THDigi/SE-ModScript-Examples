using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Digi.Examples
{
    // UseObjects are interactive areas on entities (primarily used on blocks).

    // First you need an empty/dummy on the model.
    //
    // You can OVERRIDE game's useobjects too, for example entering "terminal" will affect all block's terminal interactions in the game, including the ones from mods!
    // This "terminal" one can be temporarily used for prototyping before making a custom model.

    // For custom dummies, the dummy name must start with "detector_" followed by a UNIQUE name without any "_".
    // Optionally, if you need multiple of them, suffix with _1, _2, or even _001, _002, etc but the "_" is important.
    // It's recommended to enter a unique name (like the mod name or author name) because you risk other mods overriding yours or vice-versa.

    // Then, for MyUseObject below you must enter the unique name only:
    //  First, strip away the "detector_" prefix, then the very next "_" you find, remove it and everything after, now you have the useobject name to enter in MyUseObject below.
    //  Another way to look at it: dummyName.Split("_")[1] - this is what they're looking for when processing the dummies in a model.
    // Examples: detector_YourUniqueName_IgnoredStuff    -> [MyUseObject("YourUniqueName")]
    //           detector_YourUniqueName_2               -> [MyUseObject("YourUniqueName")]
    //           detector_SomethingELSE                  -> [MyUseObject("somethingelse")]
    // The name is not case-sensitive.

    // For a different explanation see the useobjects guide: https://steamcommunity.com/sharedfiles/filedetails/?id=2560048279

    [MyUseObject("YourUniqueName")]
    public class Example_CustomUseObject : MyUseObjectBase
    {
        // What action gets sent to Use() when interacted with PrimaryAttack or Use binds.
        public override UseActionEnum PrimaryAction { get; } = UseActionEnum.Manipulate;

        // Same as PrimaryAction but only used if that is left as None, making this pretty useless.
        public override UseActionEnum SecondaryAction { get; }

        public override UseActionEnum SupportedActions { get; }

        public Example_CustomUseObject(IMyEntity owner, string dummyName, IMyModelDummy dummyData, uint shapeKey) : base(owner, dummyData)
        {
            // This class gets instanced per entity and also per detector dummy.
            // WARNING: this exact constructor signature is required, otherwise it throws errors mid-loading which prevents the world from loading.

            // affects how this useobject behaves, see comments:
            SupportedActions |= UseActionEnum.Manipulate;
            SupportedActions |= UseActionEnum.OpenInventory; // overrides the inventory hotkey to invoke Use() instead. remove flag to allow the game to open inventory as normal.
            SupportedActions |= UseActionEnum.OpenTerminal; // overrides the terminal hotkey to invoke Use() instead. remove flag to allow the game to open terminal as normal.
            SupportedActions |= UseActionEnum.BuildPlanner; // if present, the game allows build planner bind (MMB) on this useobject. neither option will trigger Use()!
            SupportedActions |= UseActionEnum.Deposit; // same as above but for deposit (alt+MMB).
            SupportedActions |= UseActionEnum.PickUp; // 
            SupportedActions |= UseActionEnum.Close; // calls Use() when aiming away from the useobject, not recommended because there's a dedicated method for that
            SupportedActions |= UseActionEnum.UseFinished; // makes Use() get called on releasing input
        }

        public override MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            // Called when aiming at this useobject to get what to print on screen

            MyAPIGateway.Utilities.ShowNotification($"GetActionInfo() action={actionEnum}", 1000);

            switch(actionEnum)
            {
                default:
                    return default(MyActionDescription);

                case UseActionEnum.Manipulate:
                    return new MyActionDescription()
                    {
                        Text = MyStringId.GetOrCompute("You could do something with this with your kb/m..."),
                        IsTextControlHint = true,
                        JoystickText = MyStringId.GetOrCompute("You could do something with this with your gamepad..."),
                        ShowForGamepad = true
                    };

                    // ...more cases if needed
            }
        }

        public override void Use(UseActionEnum actionEnum, IMyEntity user)
        {
            // Called when a supported input is used while aiming at this useobject
            // NOTE: it can be called multiple times in the same tick, for example if ContinuousUsage is true and click+F key are held, this will trigger twice per tick.

            MyAPIGateway.Utilities.ShowNotification($"Use() action={actionEnum}; user={user}");

            switch(actionEnum)
            {
                case UseActionEnum.Manipulate:
                    MyAPIGateway.Utilities.ShowNotification("Oh you pressed it!");
                    break;

                    // ...more cases if needed
            }
        }

        // there's a few more things you can optionally override, like HandleInput(), OnSelectionLost(), ContinuousUsage, PlayIndicatorSound, etc.

        //public override bool HandleInput()
        //{
        //    if(!MyParticlesManager.Paused)
        //        MyAPIGateway.Utilities.ShowNotification("HandleInput()", 17);
        //
        //    // careful returning true as it will block A LOT of things including equipped tool click, helmet, flashlight, etc - but not everything, so test throughly.
        //    // this is probably intended to always be returning false and only return true when you have your control detected so that it doesn't do multiple actions.
        //    return false;
        //}
    }
}