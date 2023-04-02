using System;
using System.Collections.Generic;
using ObjectBuilders.SafeZone;
using Sandbox.Game.EntityComponents;
using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.ObjectBuilders.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    // a generic script to attach particles to dummies in models.

    // first you need a class like the example one below, one per block type so if you need more types just duplicate the entire code chunk.

    // the MyEntityComponentDescriptor line is what block type and subtype(s), for type just use the <TypeId> and add MyObjectBuilder_ prefix.
    // the subtypes can be removed entirely if you want it to affect all blocks of that type.

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "SpecificSubtype", "MoreIfNeeded", "Etc...")]
    public class ParticleOnReactor : StandardParticleGamelogic
    {
        protected override void Setup()
        {
            // a dummy gets assigned a particle id
            // the condition part is what determines the behavior, see all options in Conditions.cs at the CreateParticleHolder()'s switch.

            Declare(dummy: "some_empty_name", particle: "SomeParticleSubtypeId", condition: "working");

            // only one particle per dummy but you can have as many dummies as you want (by copy-pasting the line above multiple times)
        }
    }

    // a few more examples below:

    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false)]
    //public class ParticleOnBattery : StandardParticleGamelogic
    //{
    //    then have the Setup() stuff here
    //}

    // the subtypes don't have to be in one line, after every comma you can add a new line, like so:

    //[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Warhead), false,
    //    "too",
    //    "many",
    //    "subtypes",
    //    "for",
    //    "one",
    //    "line")]
}
