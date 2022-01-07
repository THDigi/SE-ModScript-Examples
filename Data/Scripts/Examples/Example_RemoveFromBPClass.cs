using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_RemoveFromBPClass : MySessionComponentBase
    {
        List<MyBlueprintDefinitionBase> NewBlueprints = new List<MyBlueprintDefinitionBase>();

        public override void LoadData()
        {
            RemoveBlueprintsFromBlueprintClass("bpClassSubtype", new HashSet<string>()
            {
                "blueprintSubtype",
                // more like the above line if needed
            });

            // more like the above code chunk if needed for removing from multiple classes



            // these must always be last
            PostProcessProductionBlocks(); // required to make production blocks aware of the blueprint changes, to adjust their inventory constraints and whatever else
            NewBlueprints = null;
        }

        void RemoveBlueprintsFromBlueprintClass(string bpClassName, HashSet<string> removeBlueprintIds)
        {
            MyBlueprintClassDefinition bpClass = MyDefinitionManager.Static.GetBlueprintClass(bpClassName);
            if(bpClass == null)
                throw new Exception($"{ModContext.ModName} :: ERROR: Cannot find blueprint class '{bpClassName}'");

            NewBlueprints.Clear();

            int bpCount = 0;

            foreach(MyBlueprintDefinitionBase bp in bpClass)
            {
                bpCount++;

                if(removeBlueprintIds.Contains(bp.Id.SubtypeName))
                    MyLog.Default.WriteLine($"{ModContext.ModName} :: Removed {bp.Id.SubtypeName} from blueprint class '{bpClassName}'");
                else
                    NewBlueprints.Add(bp);
            }

            if(NewBlueprints.Count == bpCount)
            {
                MyLog.Default.WriteLine($"{ModContext.ModName} :: WARNING: Blueprint class '{bpClassName}' does not contain any of these blueprints: {string.Join(", ", removeBlueprintIds)}");
                return;
            }

            bpClass.ClearBlueprints();

            foreach(MyBlueprintDefinitionBase bp in NewBlueprints)
            {
                bpClass.AddBlueprint(bp);
            }
        }

        void PostProcessProductionBlocks()
        {
            foreach(MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                MyProductionBlockDefinition productionDef = def as MyProductionBlockDefinition;
                if(productionDef != null)
                {
                    productionDef.LoadPostProcess();
                }
            }
        }
    }
}
