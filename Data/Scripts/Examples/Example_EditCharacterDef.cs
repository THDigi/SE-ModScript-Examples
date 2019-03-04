using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_EditCharacterDef : MySessionComponentBase
    {
        // need to store and reset definitions on unload as you're basically editing the vanilla ones (but only in memory, not permanently).
        private float original_ForceMagnitude;

        private readonly List<string> CharacterSubtypeIDs = new List<string>()
        {
            "Default_Astronaut",
            "Default_Astronaut_Female"
        };

        public override void LoadData()
        {
            var charDefs = MyDefinitionManager.Static.Characters;

            foreach(var charDef in charDefs)
            {
                charDef.Context = (MyModContext)ModContext;

                if(CharacterSubtypeIDs.Contains(charDef.Id.SubtypeName))
                {
                    original_ForceMagnitude = charDef.Jetpack.ThrustProperties.ForceMagnitude;
                    charDef.Jetpack.ThrustProperties.ForceMagnitude = 1000f;
                }
            }
        }

        protected override void UnloadData()
        {
            var charDefs = MyDefinitionManager.Static.Characters;

            foreach(var charDef in charDefs)
            {
                if(CharacterSubtypeIDs.Contains(charDef.Id.SubtypeName))
                {
                    charDef.Jetpack.ThrustProperties.ForceMagnitude = original_ForceMagnitude;
                }
            }
        }
    }
}