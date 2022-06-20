using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    // This example shows how to read upgrade module info by making a reactor generate ice if it has a certain upgrade attached.
    //
    // The script is only required on the host block, the upgrade module detection and math is built-in to the game.
    //
    // You do however need to have an upgrade module SBC with your required values.
    // Also the model of both this block and the module requires upgrade empties to allow the connection to happen on keen's code.

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false, "YourSubtypeHere", "More if needed")]
    public class Example_UpgradeModuleSupport : MyGameLogicComponent
    {
        const string IcePerSecondKey = "IcePerSecond";

        IMyTerminalBlock Block;
        int LastRunTick = -1;

        static readonly MyObjectBuilder_PhysicalObject ItemOB = new MyObjectBuilder_Ore()
        {
            SubtypeName = "Ice",
        };

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Block = (IMyTerminalBlock)Entity;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME; // enables UpdateOnceBeforeFrame() to be called once in the next update

            // can do this Add() multiple times if you need multiple upgrade values

            // the 0 is the starting value, depends on how you want the upgrade math to work.
            // for example you could default to 1 and modules can multiply by 1.2 which will get way stronger with more modules
            Block.UpgradeValues.Add(IcePerSecondKey, 0f);

            // the key name is what you give to the upgrade module's SBC to affect
            /*
            <Upgrades>
                <MyUpgradeModuleInfo>
                    <UpgradeType>IcePerSecond</UpgradeType>
                    <ModifierType>Additive</ModifierType>
                    <Modifier>0.2</Modifier>
                </MyUpgradeModuleInfo>
            </Upgrades>
            */
            // adding this tag to upgrade module and using it with this script would result in 0.2 ice per second, per upgrade module attached.
        }

        public override void UpdateOnceBeforeFrame()
        {
            if(Block?.CubeGrid?.Physics == null)
                return; // ignore ghost grids like projections or in-paste ones

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME; // enables UpdateAfterSimulation100() to get called almost every 100 ticks, not guaranteed 100.

            LastRunTick = MyAPIGateway.Session.GameplayFrameCounter; // used by the item adding, can remove if you removed that part

            // this is only if you want to cache the value, not really necessary.
            /*
            Block.OnUpgradeValuesChanged += UpgradesChanged;
            UpgradesChanged(); // required to read the value on spawn as it likely doesn't trigger the event this late.
            */
        }

        /*
        void UpgradesChanged()
        {
            // read Block.UpgradeValues[IcePerSecondKey]
        }
        */

        public override void UpdateAfterSimulation100()
        {
            // this has the final value already modified by upgrade modules, you just read it 
            float icePerSecond = Block.UpgradeValues[IcePerSecondKey];

            MyAPIGateway.Utilities.ShowNotification($"{Block.CustomName}: icePerSecond={icePerSecond.ToString("N2")}", 16 * 100, "Debug");

            // and this is just some random usage example, by adding an item to inventory
            // inventory stuff should only be done serverside (and can be optimized by choosing NeedsUpdate to be set only serverside too)
            if(MyAPIGateway.Session.IsServer && icePerSecond > 0)
            {
                int tick = MyAPIGateway.Session.GameplayFrameCounter;
                float deltaTime = (tick - LastRunTick) / MyEngineConstants.UPDATE_STEPS_PER_SECOND;
                LastRunTick = tick;

                float addAmount = icePerSecond * deltaTime;
                Block.GetInventory().AddItems((MyFixedPoint)addAmount, ItemOB);
            }
        }
    }
}