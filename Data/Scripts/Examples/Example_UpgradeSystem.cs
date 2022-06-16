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
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Collector), false, "YourSubtypeHere", "More if needed")]
    public class Example_UpgradeSystem_Receiver : MyGameLogicComponent
    {
        const string SomeUpgradeKey = "SomeUpgrade";

        IMyCubeBlock Block;
        int LastRunTick = -1;
        const float ItemPerSecond = 5f;
        static readonly MyObjectBuilder_PhysicalObject OreOB = new MyObjectBuilder_Ore()
        {
            SubtypeName = "Ice",
        };

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            Block = (IMyCubeBlock)Entity;

            // can do this Add() multiple times if you need multiple upgrade values

            // the 0 is the starting value, depends on how you want the upgrade math to work.
            // for example you could default to 1 and modules can multiply by 1.2 which will get way stronger with more modules
            Block.UpgradeValues.Add(SomeUpgradeKey, 0f);

            // the key name is what you give to the upgrade module's SBC to affect
            /*
            <Upgrades>
                <MyUpgradeModuleInfo>
                    <UpgradeType>SomeUpgrade</UpgradeType>
                    <ModifierType>Additive</ModifierType>
                    <Modifier>0.25</Modifier>
                </MyUpgradeModuleInfo>
            </Upgrades>
            */

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if(Block?.CubeGrid?.Physics == null)
                return; // ignore ghost grids

            LastRunTick = MyAPIGateway.Session.GameplayFrameCounter;

            // this is only if you want to cache the value, not really necessary
            //Block.OnUpgradeValuesChanged += UpgradesChanged;
            //UpgradesChanged();

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        //void UpgradesChanged()
        //{
        //    // read Block.UpgradeValues[SomeUpgradeKey]
        //}

        public override void UpdateAfterSimulation100()
        {
            // the Block.UpgradeValues[SomeUpgradeKey] has the final value modified by all connected upgrade modules.


            int tick = MyAPIGateway.Session.GameplayFrameCounter;
            float deltaTime = (tick - LastRunTick) / MyEngineConstants.UPDATE_STEPS_PER_SECOND;
            LastRunTick = tick;

            float addAmount = ItemPerSecond * (1f + Block.UpgradeValues[SomeUpgradeKey]) * deltaTime;
            Block.GetInventory().AddItems((MyFixedPoint)addAmount, OreOB);
        }
    }
}