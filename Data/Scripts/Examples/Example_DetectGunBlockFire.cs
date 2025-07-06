using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Examples
{
    // Edit the block type and subtypes to match your custom block.
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_LargeMissileTurret), false, "YourSubtypeHere", "More if needed")]
    public class Example_DetectGunBlockFire : MyGameLogicComponent
    {
        private IMyFunctionalBlock block;
        private IMyGunObject<MyGunBase> gun;
        private long lastShotTime;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            block = (IMyFunctionalBlock)Entity;

            if(block.CubeGrid?.Physics == null)
                return;

            gun = (IMyGunObject<MyGunBase>)Entity;
            lastShotTime = gun.GunBase.LastShootTime.Ticks;

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateBeforeSimulation()
        {
            try
            {
                if(!block.IsFunctional)
                    return;

                var shotTime = gun.GunBase.LastShootTime.Ticks;

                if(shotTime > lastShotTime)
                {
                    lastShotTime = shotTime;

                    // do stuff
                    MyAPIGateway.Utilities.ShowNotification($"{block.CustomName} has fired!", 1000);
                }
            }
            catch(Exception e)
            {
                AddToLog(e);
            }
        }

        private void AddToLog(Exception e)
        {
            MyLog.Default.WriteLineAndConsole(e.ToString());

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }
    }
}
