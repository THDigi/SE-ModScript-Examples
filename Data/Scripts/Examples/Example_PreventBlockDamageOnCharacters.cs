using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_PreventBlockDamageOnCharacters : MySessionComponentBase
    {
        HashSet<MyObjectBuilderType> BlockTypes = new HashSet<MyObjectBuilderType>()
        {
            // prevent an entire block type from doing damage to characters.
            //typeof(MyObjectBuilder_ShipWelder),
            //typeof(MyObjectBuilder_ShipGrinder),
            //typeof(MyObjectBuilder_Drill),
        };

        HashSet<MyDefinitionId> BlockIDs = new HashSet<MyDefinitionId>()
        {
            // add your block's <Id> tag formatted as "TypeId/SubtypeId", like shown below.
            // the list can also be empty, especially if you want to use the above BlockTypes instead.
            MyDefinitionId.Parse("ShipGrinder/SomeSafeGrinder"),
            MyDefinitionId.Parse("ShipWelder/Idunno"),
            MyDefinitionId.Parse("ShipWelder/Whatever"),
        };

        public override void BeforeStart()
        {
            // damage is done only serverside, this also means this script can work for DS that allow console players to join.
            if(MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(100, DamageHandler);
            }
        }

        protected override void UnloadData()
        {
            // damage system does not have unregister
        }

        void DamageHandler(object victim, ref MyDamageInformation info)
        {
            try
            {
                if(info.IsDeformation || info.Amount <= 0 || info.AttackerId == 0)
                    return;

                IMyCharacter chr = victim as IMyCharacter;
                if(chr == null)
                    return;

                IMyCubeBlock block = MyEntities.GetEntityById(info.AttackerId) as IMyCubeBlock;
                if(block == null)
                    return;

                if(BlockTypes.Contains(block.BlockDefinition.TypeId) || BlockIDs.Contains(block.BlockDefinition))
                {
                    info.Amount = 0f;

                    //MyLog.Default.WriteLine($"{ModContext?.ModName ?? GetType().FullName}: Prevented damage from {block.BlockDefinition} to {chr.DisplayName}");
                }
            }
            catch(Exception e)
            {
                AddToLog(e);
            }
        }

        void AddToLog(Exception e)
        {
            string modName = ModContext?.ModName ?? GetType().FullName;
            MyLog.Default.WriteLineAndConsole($"{modName} ERROR: {e.ToString()}");
            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ {modName} ERROR: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }
    }
}