using System;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_ModifyWarheadExplosion : MySessionComponentBase
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

                switch(info.ExplosionType)
                {
                    case MyExplosionTypeEnum.WARHEAD_EXPLOSION_02:
                    case MyExplosionTypeEnum.WARHEAD_EXPLOSION_15:
                    case MyExplosionTypeEnum.WARHEAD_EXPLOSION_30:
                    case MyExplosionTypeEnum.WARHEAD_EXPLOSION_50:
                        break; // continue onward

                    default:
                        return; // end function for any other type
                }

                IMyWarhead warhead = info.HitEntity as IMyWarhead;
                IMyCubeGrid grid = info.OwnerEntity as IMyCubeGrid;
                if(warhead == null || grid == null || warhead.CubeGrid != grid)
                    return;

                switch(warhead.BlockDefinition.SubtypeId)
                {
                    case "WarheadA":
                    case "WarheadB":
                        info.ExplosionType = MyExplosionTypeEnum.CUSTOM;
                        info.CustomEffect = "FancyParticle";
                        break;

                    case "WarheadC":
                        info.ExplosionType = MyExplosionTypeEnum.CUSTOM;
                        info.CustomEffect = "SomeLessFancyParticlexD";
                        break;

                        // as many subtypes as you want
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
