using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Digi.Example_ProtectionBlock
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class ProtectionSession : MySessionComponentBase
    {
        public static ProtectionSession Instance; // NOTE: this is the only acceptable static if you nullify it afterwards.

        public List<IMyCubeBlock> ProtectionBlocks = new List<IMyCubeBlock>();

        public override void LoadData()
        {
            Instance = this;
        }

        public override void BeforeStart()
        {
            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, BeforeDamage);
        }

        protected override void UnloadData()
        {
            Instance = null; // important to avoid this object instance from remaining in memory on world unload/reload
        }

        private void BeforeDamage(object target, ref MyDamageInformation info)
        {
            if(info.Amount == 0)
                return;

            var slim = target as IMySlimBlock;

            if(slim == null)
                return;

            // if any of the protection blocks are on this grid then protect it
            foreach(var block in ProtectionBlocks)
            {
                // checks for same grid-group to extend protection to piston/rotors/wheels but no connectors (change link type to Physical to include those)
                // same grid only check: block.CubeGrid == slim.CubeGrid
                if(MyAPIGateway.GridGroups.HasConnection(block.CubeGrid, slim.CubeGrid, GridLinkTypeEnum.Mechanical))
                {
                    info.Amount = 0;
                    return;
                }
            }
        }
    }
}
