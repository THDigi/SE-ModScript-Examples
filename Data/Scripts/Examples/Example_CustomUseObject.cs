using Sandbox.ModAPI;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Digi.Examples
{
    // UseObjects are interactive areas on entities (primarily used on blocks).

    // First you need an empty on the model.

    // You can OVERRIDE game's useobjects too, for example entering "terminal", but use it with caution because it will affect ALL blocks.
    // This can also be used for some quick prototyping without a custom model.

    // For your own model you need empties prefixed with detector_ and the name of the useobject is up until the next _ (if any).
    // Examples: detector_YourUseObjectName_IgnoredStuff    -> [MyUseObject("YourUseObjectName")]
    //           detector_YourUseObjectName_2               -> [MyUseObject("YourUseObjectName")]
    //           detector_SomethingElse                     -> [MyUseObject("SomethingElse")]
    // The names are not case-sensitive.
    // Recommended to use a very unique name, like your mod name.
    // For a different explanation see the useobjects guide: https://steamcommunity.com/sharedfiles/filedetails/?id=2560048279
    [MyUseObject("YourUseObjectName")]
    public class Example_CustomUseObject : MyUseObjectBase
    {
        // Probably determines what actions to show as hints? Experiment!
        public override UseActionEnum SupportedActions => UseActionEnum.Manipulate
                                                        | UseActionEnum.Close
                                                        | UseActionEnum.BuildPlanner
                                                        | UseActionEnum.OpenInventory
                                                        | UseActionEnum.OpenTerminal
                                                        | UseActionEnum.PickUp
                                                        | UseActionEnum.UseFinished; // gets called when releasing manipulate

        // What action gets sent to Use() when interacted with PrimaryAttack or Use binds.
        public override UseActionEnum PrimaryAction => UseActionEnum.Manipulate;

        // What action gets sent to Use() when interacted with SecondaryAttack or Inventory/Terminal binds.
        public override UseActionEnum SecondaryAction => UseActionEnum.OpenTerminal;

        public Example_CustomUseObject(IMyEntity owner, string dummyName, IMyModelDummy dummyData, uint shapeKey) : base(owner, dummyData)
        {
            // This class gets instanced per entity that has this detector useobject on it.
            // NOTE: this exact constructor signature is required, will throw errors mid-loading (and prevent world from loading) otherwise.
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

            MyAPIGateway.Utilities.ShowNotification($"Use() action={actionEnum}; user={user}");

            switch(actionEnum)
            {
                case UseActionEnum.Manipulate:
                    MyAPIGateway.Utilities.ShowNotification("Oh you pressed it!");
                    break;

                    // ...more cases if needed
            }
        }

        // there's a few more things you can optionally override, like OnSelectionLost()
    }
}