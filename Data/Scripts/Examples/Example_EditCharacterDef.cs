using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace Digi.Examples
{
    // A simplistic example of editing character definitions, with the important part of undoing.
    // NOTE: recommended to look at Example_EditDefinition instead which has a more flexible way of editing definitions with easier undo support.
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_EditCharacterDef : MySessionComponentBase
    {
        // need to store original values and reset definitions on unload as you're basically editing the vanilla ones (but only in memory, not permanently).
        readonly Dictionary<MyCharacterDefinition, OriginalData> OriginalCharacterData = new Dictionary<MyCharacterDefinition, OriginalData>();

        struct OriginalData
        {
            public float Jetpack_ForceMagnitude;
        }

        public override void LoadData()
        {
            // specific characters to edit
            // you can also iterate the MyDefinitionManager.Static.Characters to modify all available ones or by more complex filters.
            EditCharacterDef("Default_Astronaut");
            EditCharacterDef("Default_Astronaut_Female");
        }

        void EditCharacterDef(string subtypeId)
        {
            MyCharacterDefinition charDef;
            if(!MyDefinitionManager.Static.Characters.TryGetValue(subtypeId, out charDef))
            {
                // adds errors to F11 menu and SE log too
                MyDefinitionErrors.Add((MyModContext)ModContext, $"Could not find character subtypeId: {subtypeId}", TErrorSeverity.Error);
                return;
            }

            // Checklist to change something new:
            // * Add field to OriginalData.
            // * Set that field in the new OriginalData() below with the existing value.
            // * change the field in the character definition to the modified value.
            // * in UnloadData() change the same character definition field to the original value.

            // store the data we're about to change
            OriginalCharacterData[charDef] = new OriginalData()
            {
                Jetpack_ForceMagnitude = charDef.Jetpack.ThrustProperties.ForceMagnitude,
            };

            // then make the changes
            charDef.Jetpack.ThrustProperties.ForceMagnitude = 1000f;

            // mark it as edited by this mod, not really necessary but nice to inform.
            charDef.Context = (MyModContext)ModContext;
        }

        protected override void UnloadData()
        {
            // always catch errors here because throwing them will NOT crash the game and instead prevent other mods from unloading properly, causing all sorts of hidden issues...
            try
            {
                foreach(var kv in OriginalCharacterData)
                {
                    MyCharacterDefinition charDef = kv.Key;
                    OriginalData orig = kv.Value;

                    // reset to base game regardless if it was from a mod or not, because if it was from a mod then it'll be gone and gets re-created on next load.
                    charDef.Context = MyModContext.BaseGame;

                    // reset all the same fields back to their original values
                    charDef.Jetpack.ThrustProperties.ForceMagnitude = orig.Jetpack_ForceMagnitude;
                }
            }
            catch(Exception e)
            {
                MyLog.Default.Error(e.ToString());
            }
        }
    }
}