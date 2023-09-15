using System.IO;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Example_SetInventoryConstraintIcon : MySessionComponentBase
    {
        public override void LoadData()
        {
            SetReactorConstraintIcon("YourReactorSubtype", @"Textures\GUI\Icons\filter_ingot.dds");

            // a vanilla example to try out right away:
            SetReactorConstraintIcon("SmallBlockSmallGenerator", @"Textures\GUI\Icons\Bug.dds");
        }

        void SetReactorConstraintIcon(string subtypeId, string iconPath)
        {
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Reactor), subtypeId)) as MyReactorDefinition;
            if(def == null)
            {
                MyDefinitionErrors.Add((MyModContext)ModContext, $"Couldn't find Reactor with subtype: {subtypeId}", TErrorSeverity.Warning);
                return;
            }

            if(MyAPIGateway.Utilities.FileExistsInModLocation(iconPath, ModContext.ModItem))
            {
                iconPath = Path.Combine(ModContext.ModPath, iconPath); // turn it into full path
            }
            else if(!MyAPIGateway.Utilities.FileExistsInGameContent(iconPath))
            {
                MyDefinitionErrors.Add((MyModContext)ModContext, $"Couldn't find icon in mod folder nor in game folder: {iconPath}", TErrorSeverity.Warning);
                return;
            }

            def.InventoryConstraint.Icon = iconPath;
        }
    }
}
