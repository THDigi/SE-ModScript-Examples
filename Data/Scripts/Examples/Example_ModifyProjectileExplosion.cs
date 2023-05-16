using System;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_ModifyProjectileExplosion : MySessionComponentBase
    {
        public override void LoadData()
        {
            MyExplosions.OnExplosion += OnExplosion;
        }

        protected override void UnloadData()
        {
            MyExplosions.OnExplosion -= OnExplosion;
        }

        void OnExplosion(ref MyExplosionInfo info)
        {
            try
            {
                // disclaimer: not thoroughly tested, up to you to do so and feedback with findings :P

                if(info.ExplosionType != MyExplosionTypeEnum.ProjectileExplosion)
                    return;

                IMyEntity originEntity = MyAPIGateway.Entities.GetEntityById(info.OriginEntity);
                if(originEntity == null)
                    return;

                IMyCubeBlock block = originEntity as IMyCubeBlock;
                if(block != null)
                {
                    switch(block.BlockDefinition.SubtypeName)
                    {
                        case "SomeWeaponId":
                        case "MoreIfYouWant":
                            info.ExplosionType = MyExplosionTypeEnum.CUSTOM;
                            info.CustomEffect = "SomeParticleId";
                            break;

                            // as many subtypes as you want
                    }
                    return;
                }

                IMyHandheldGunObject<MyGunBase> handHeld = originEntity as IMyHandheldGunObject<MyGunBase>;
                if(handHeld?.PhysicalItemDefinition != null)
                {
                    switch(handHeld.PhysicalItemDefinition.Id.SubtypeName)
                    {
                        case "SomeItemId":
                        case "Etc...":
                            info.ExplosionType = MyExplosionTypeEnum.CUSTOM;
                            info.CustomEffect = "SomeParticleId";
                            break;

                            // as many subtypes as you want
                    }
                    return;
                }
            }
            catch(Exception e)
            {
                AddToLog(e);
            }
        }

        void AddToLog(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {GetType().FullName}: {e.ToString()}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }
    }
}
