using Sandbox.Definitions;
using VRage.Game.Components;

namespace Digi.Experiments
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_RemoveCategory : MySessionComponentBase
    {
        public override void LoadData()
        {
            RemoveCategory("LargeBlocks");
            // can repeat the above line to remove more
        }

        void RemoveCategory(string categoryName)
        {
            MyGuiBlockCategoryDefinition categoryDef;
            if(MyDefinitionManager.Static.GetCategories().TryGetValue(categoryName, out categoryDef))
            {
                categoryDef.ItemIds.Clear();

                categoryDef.IsBlockCategory = true;
                categoryDef.IsAnimationCategory = false;
                categoryDef.IsShipCategory = false;
                categoryDef.IsToolCategory = false;
            }
        }

        protected override void UnloadData()
        {
        }
    }
}
