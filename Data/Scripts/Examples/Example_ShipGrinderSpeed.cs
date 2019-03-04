using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_ShipGrinderSpeed : MySessionComponentBase
    {
        private readonly Dictionary<MyDefinitionId, float> perToolAdjust = new Dictionary<MyDefinitionId, float>(MyDefinitionId.Comparer)
        {
            [new MyDefinitionId(typeof(MyObjectBuilder_ShipGrinder), "LargeShipGrinder")] = 3f,
            [new MyDefinitionId(typeof(MyObjectBuilder_ShipGrinder), "SmallShipGrinder")] = 0.1f,
        };

        public override void BeforeStart()
        {
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, BeforeDamageApplied);
        }

        protected override void UnloadData()
        {
            // damage system doesn't have an unregister method
        }

        private void BeforeDamageApplied(object target, ref MyDamageInformation info)
        {
            if(info.IsDeformation || info.AttackerId == 0 || !(target is IMySlimBlock))
                return; // fastest checks first to exit ASAP as this method is quite frequently called

            var shipTool = MyEntities.GetEntityById(info.AttackerId) as IMyShipToolBase;

            if(shipTool == null)
                return;

            float adjust;

            if(!perToolAdjust.TryGetValue(shipTool.BlockDefinition, out adjust))
                return;

            info.Amount *= adjust;
        }
    }
}
