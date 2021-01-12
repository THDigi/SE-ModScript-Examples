using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_EditCharacterDef : MySessionComponentBase
    {
        private const float SetJetpackForce = 1000f;
        private readonly List<string> CharacterSubtypeIDs = new List<string>()
        {
            "Default_Astronaut",
            "Default_Astronaut_Female",
        };

        // need to store original values and reset definitions on unload as you're basically editing the vanilla ones (but only in memory, not permanently).
        private readonly Dictionary<string, float> OriginalForceMagnitude = new Dictionary<string, float>();

        public override void LoadData()
        {
            var charDefs = MyDefinitionManager.Static.Characters;

            foreach(var charDef in charDefs)
            {
                if(CharacterSubtypeIDs.Contains(charDef.Id.SubtypeName))
                {
                    charDef.Context = (MyModContext)ModContext; // mark it as edited by this mod, not really necessary but nice to inform.

                    OriginalForceMagnitude[charDef.Id.SubtypeName] = charDef.Jetpack.ThrustProperties.ForceMagnitude;

                    charDef.Jetpack.ThrustProperties.ForceMagnitude = SetJetpackForce;
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
                    charDef.Context = MyModContext.BaseGame; // reset to base game regardless, if it was from a mod then it gets reprocessed on next load anyway.

                    charDef.Jetpack.ThrustProperties.ForceMagnitude = OriginalForceMagnitude[charDef.Id.SubtypeName];
                }
            }
        }
    }
}