using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders;
using VRage.Game.ObjectBuilders.ComponentSystem;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_AppendToCharacterStatComp : MySessionComponentBase
    {
        public override void LoadData()
        {
            AddStatToComp("Default_Astronaut", "whateverstat");
        }

        void AddStatToComp(string compSubtype, string statSubtype)
        {
            MyEntityStatComponentDefinition def = MyDefinitionManager.Static.GetEntityComponentDefinition(new MyDefinitionId(typeof(MyObjectBuilder_CharacterStatComponent), compSubtype)) as MyEntityStatComponentDefinition;

            if(def != null)
            {
                def.Stats.Add(new MyDefinitionId(typeof(MyObjectBuilder_EntityStat), statSubtype));
            }
        }
    }
}