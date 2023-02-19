using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Debug_ShowShotPositions : MySessionComponentBase
    {
        List<DrawShot> DrawShots = new List<DrawShot>();

        struct DrawShot
        {
            public readonly Vector3D Position;
            public readonly Vector3D DirectionAndDistance;
            public readonly int ExpiresAt;

            public DrawShot(Vector3D position, Vector3D directionAndDistance)
            {
                Position = position;
                DirectionAndDistance = directionAndDistance;
                ExpiresAt = MyAPIGateway.Session.GameplayFrameCounter + (60 * 10); // 10 seconds
            }
        }

        MyStringId MaterialDot = MyStringId.GetOrCompute("WhiteDot");
        MyStringId MaterialSquare = MyStringId.GetOrCompute("Square");

        public override void BeforeStart()
        {
            MyAPIGateway.Projectiles.OnProjectileAdded += ProjectileAdded;
            MyAPIGateway.Missiles.OnMissileAdded += MissileAdded;
        }

        protected override void UnloadData()
        {
            if(MyAPIGateway.Projectiles != null)
                MyAPIGateway.Projectiles.OnProjectileAdded -= ProjectileAdded;

            if(MyAPIGateway.Missiles != null)
                MyAPIGateway.Missiles.OnMissileAdded -= MissileAdded;
        }

        void ProjectileAdded(ref MyProjectileInfo projectile, int index)
        {
            AddLine(projectile.Position, projectile.Velocity);
        }

        void MissileAdded(IMyMissile missile)
        {
            AddLine(missile.GetPosition(), missile.LinearVelocity);
        }

        void AddLine(Vector3D position, Vector3D directionAndDistance)
        {
            if(Vector3D.IsZero(directionAndDistance, 0.001))
                directionAndDistance = Vector3D.Forward * 10000000;

            DrawShots.Add(new DrawShot(position, directionAndDistance));
        }

        public override void Draw()
        {
            int tick = MyAPIGateway.Session.GameplayFrameCounter;

            for(int i = DrawShots.Count - 1; i >= 0; i--)
            {
                DrawShot shotInfo = DrawShots[i];

                if(tick >= shotInfo.ExpiresAt)
                {
                    DrawShots.RemoveAtFast(i);
                    continue;
                }

                MyTransparentGeometry.AddPointBillboard(MaterialDot, Color.Red, shotInfo.Position, radius: 0.25f, angle: 0,
                    blendType: MyBillboard.BlendTypeEnum.AdditiveTop);

                MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Red, shotInfo.Position, shotInfo.DirectionAndDistance, length: 1f, thickness: 0.1f,
                    blendType: MyBillboard.BlendTypeEnum.AdditiveTop);
            }
        }
    }
}
