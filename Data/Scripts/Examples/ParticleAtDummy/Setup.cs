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
    // A generic script to attach particles to dummies in models.

    // First you need a class like the example one below, one per block type.
    // If you need more types just duplicate the entire code chunk (or more advanced way is to make classes with attributes inheriting a common one)

    // The MyEntityComponentDescriptor line is what block type and subtype(s).
    //  - for type use the <TypeId> and add MyObjectBuilder_ prefix.
    //  - the subtypes can be removed entirely if you want it to affect all blocks of that type.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "SpecificSubtype", "MoreIfNeeded", "etc...")]
    public class ParticleOnReactor : StandardParticleGamelogic
    {
        protected override void Setup()
        {
            Declare(dummy: "some_dummy_name", // The exact name of an in-model dummy, can be in subparts as well.
                                              // NOTE: do not declare the same dummy name multiple times, it will override.
                    particle: "ExhaustFire", // Particle effect subtypeId, must exist and must be a loop.
                                             // The particle will be parented to the block or subpart at the dummy location and orientation.
                    condition: "working" // Logic that determines behavior, see all options in Conditions.cs at the CreateParticleHolder()'s switch. 
            );

            // can repeat the above declaration for more particles on other dummies


            DebugMode = false; // turn on to enable debug chat messages for this block
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
