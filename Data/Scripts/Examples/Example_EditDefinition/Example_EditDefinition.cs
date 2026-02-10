using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace Digi.Examples
{
    /*
     * General example for editing definitions which also undoes them on unload with the help of UndoableEditToolset.
     * 
     * Definitions generally need undoing because the changes can leak onto other worlds that don't have the mod anymore.
     * It also can go really badly if you mutate, for example multiplying an existing value would propagate to be quite large after a few reloads.
     *
     * This is mostly for advanced changes that cannot be easily done with SBC or mod adjuster (https://steamcommunity.com/sharedfiles/filedetails/?id=3017795356).
     */
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_EditDefinition : MySessionComponentBase
    {
        UndoableEditToolset Edits = new UndoableEditToolset();

        public override void LoadData()
        {
            // Must first edit definitions here before world spawns anything, to avoid problems.
            ExampleEditSearchlight();
            ExampleAddWeatherToPlanets();
        }

        protected override void UnloadData()
        {
            // always catch errors here because throwing them will NOT crash the game and instead prevent other mods from unloading properly, causing all sorts of hidden issues...
            try
            {
                Edits.UndoAll(); // undo all the changes to ensure they don't persist into other worlds
            }
            catch(Exception e)
            {
                MyLog.Default.Error(e.ToString());
            }
        }

        void ExampleEditSearchlight()
        {
            var def = (MySearchlightDefinition)MyDefinitionManager.Static.GetCubeBlockDefinition(MyDefinitionId.Parse("Searchlight/LargeSearchlight"));

            // now instead of changing values directly, we use the UndoableEditToolset like so:
            Edits.MakeEdit(def,
                setter: (d, v) => d.ReflectorConeDegrees = v,
                originalValue: def.ReflectorConeDegrees,
                newValue: 30);

            // let's break it down by parameter:
            // 1. the reference to the definition object, this one is straightforward
            // 2. a delegate to assign the field we'd be changing, this will be used to both set the new value AND to undo the value later!
            //    Therefore ensure it's always assigned to 'v', not to external variables!
            // 3. The current value, this will be used when undoing.
            // 4. The new value, the method will immediately call the arg2 callback with this value.

            // the method doesn't need to be spread into 4 lines like that, it can be more compact after you're familiar with it:
            //   Edits.MakeEdit(def, (d, v) => d.ReflectorConeDegrees = v, def.ReflectorConeDegrees, newValue: 30);
        }

        void ExampleAddWeatherToPlanets()
        {
            // Note that GetAllDefinitions<T>() and GetAllDefinitions() have different internal lists, one might have stuff that the other doesn't and vice-versa.
            foreach(MyPlanetGeneratorDefinition planetDef in MyDefinitionManager.Static.GetAllDefinitions<MyPlanetGeneratorDefinition>())
            {
                // You can find the SBC-side documentation at https://spaceengineers.wiki.gg/wiki/Modding/Reference/SBC/PlanetGenerator_Definition#WeatherGenerators
                // Note that the SBC data is deserialized into a MyObjectBuilder_PlanetGeneratorDefinition object which is then processed into this MyPlanetGeneratorDefinition,
                //   therefore the data you find on the SBC documentation might not be the same.
                // To check if the game does any alterations to the data before putting it into a definition object you can edit here,
                //   find said object and see how its fields are assigned. https://spaceengineers.wiki.gg/wiki/Modding/Tutorials/Exploring_Game_Code

                var oldValue = planetDef.WeatherGenerators; // first store the *reference* to the old value

                // ToList()'s purpose is to do a shallow copy.
                // WARNING: if you modify any existing data in the other entries and they're classes as opposed to structs, you may need to do a deeper copy!
                var newValue = planetDef.WeatherGenerators.ToList();

                // do whatever changes you want to newValue, mind the warning above
                newValue.Add(new MyWeatherGeneratorSettings()
                {
                    Voxel = "Grass",
                    Weathers = new List<MyWeatherGeneratorVoxelSettings>()
                    {
                        new MyWeatherGeneratorVoxelSettings()
                        {
                            Name= "...",
                            //...
                        }
                    },
                });

                // finally use the UndoableEditToolset, more details on how this works in the other example method.
                Edits.MakeEdit(planetDef, (d, v) => d.WeatherGenerators = v, oldValue, newValue);
            }
        }
    }
}