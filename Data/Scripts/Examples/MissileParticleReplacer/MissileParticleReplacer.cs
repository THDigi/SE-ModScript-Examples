using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Physics;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using CollisionLayers = Sandbox.Engine.Physics.MyPhysics.CollisionLayers;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MissileParticleReplacer : MySessionComponentBase
    {
        readonly Dictionary<string, string> AssignedParticles = new Dictionary<string, string>()
        {
            // editable, you can also add more lines similar to this:
            //   ["MagazineSubtypeIdHere"] = "ParticleSubtypeIdHere",
            ["Missile200mm"] = "Tree_Drill",
        };

        // this changes the above dictionary to use model path name suffix as key.
        // if you have a unique model path then this is ideal for performance.
        readonly bool DetectBasedOnModelSuffix = false;


        MyPhysicsComponentBase TemporaryPhysics;
        readonly Dictionary<long, MyParticleEffect> MissileEffects = new Dictionary<long, MyParticleEffect>();
        readonly MyObjectBuilderType MissileTypeId = typeof(MyObjectBuilder_Missile);

        public override void LoadData()
        {
            // missile instances are re-used so they'll only be added/removed from scene.
            MyEntities.OnEntityAdd += EntityAddedToScene;
            MyEntities.OnEntityRemove += EntityRemovedFromScene;

            // create a physics object to give to the missile before getting the OB
            var tempEnt = new MyEntity();
            MyPhysicsHelper.InitSpherePhysics(tempEnt, MyMaterialType.MISSILE, Vector3.Zero, 0.01f, 0.01f, 0f, 0f, CollisionLayers.NoCollisionLayer, RigidBodyFlag.RBF_DEBRIS);
            TemporaryPhysics = tempEnt.Physics;
            tempEnt.Physics = null; // no need to keep the entity in memory anymore
        }

        protected override void UnloadData()
        {
            MyEntities.OnEntityAdd -= EntityAddedToScene;
            MyEntities.OnEntityRemove -= EntityRemovedFromScene;
        }

        private void EntityAddedToScene(MyEntity ent)
        {
            try
            {
                if(!ent.DefinitionId.HasValue || ent.DefinitionId.Value.TypeId != MissileTypeId)
                    return;

                if(DetectBasedOnModelSuffix)
                {
                    string modelName = ((IMyModel)ent.Model).AssetName;

                    foreach(var kv in AssignedParticles)
                    {
                        if(modelName.EndsWith(kv.Key))
                        {
                            ReplaceEffectForEntity(ent, kv.Value);
                            break;
                        }
                    }
                }
                else
                {
                    MyObjectBuilder_Missile ob;
                    if(ent.Physics == null)
                    {
                        // HACK: missiles don't have Physics on MP clients and GetObjectBuilder() accesses the Physics field, therefore it needs to have that field valid...
                        ent.Physics = TemporaryPhysics;
                        ob = (MyObjectBuilder_Missile)ent.GetObjectBuilder();
                        ent.Physics = null;
                    }
                    else
                    {
                        ob = (MyObjectBuilder_Missile)ent.GetObjectBuilder();
                    }

                    string particleName;
                    if(AssignedParticles.TryGetValue(ob.AmmoMagazineId.SubtypeName, out particleName))
                    {
                        ReplaceEffectForEntity(ent, particleName);
                    }
                }
            }
            catch(Exception e)
            {
                Error(this, e);
            }
        }

        private void ReplaceEffectForEntity(MyEntity ent, string particleName)
        {
            Vector3D missilePos = ent.WorldMatrix.Translation;

            // HACK: find the right particle by getting closest one to the missile, with the expected name
            MyParticleEffect closestEffect = null;
            double closestDistanceSq = double.MaxValue;

            foreach(var effect in MyParticlesManager.Effects)
            {
                if(effect.GetName() != "Smoke_Missile")
                    continue;

                double distSq = Vector3D.DistanceSquared(effect.WorldMatrix.Translation, missilePos);
                if(distSq < closestDistanceSq)
                {
                    closestDistanceSq = distSq;
                    closestEffect = effect;
                }
            }

            if(closestEffect != null)
            {
                MyParticlesManager.RemoveParticleEffect(closestEffect);
            }

            if(!string.IsNullOrEmpty(particleName))
            {
                // spawn new particle effect
                Vector3D worldPos = ent.WorldMatrix.Translation;
                MyParticleEffect newEffect;
                if(MyParticlesManager.TryCreateParticleEffect(particleName, ref MatrixD.Identity, ref worldPos, ent.Render.GetRenderObjectID(), out newEffect))
                {
                    // track missiles to stop their particle effects when removed from scene
                    MissileEffects.Add(ent.EntityId, newEffect);
                }
                else
                {
                    Error(this, $"Failed to spawn particle effect called: {particleName}");
                }
            }
        }

        private void EntityRemovedFromScene(MyEntity ent)
        {
            try
            {
                if(!ent.DefinitionId.HasValue || ent.DefinitionId.Value.TypeId != MissileTypeId)
                    return;

                MyParticleEffect effect;
                if(MissileEffects.TryGetValue(ent.EntityId, out effect))
                {
                    effect.Stop(true);
                    MissileEffects.Remove(ent.EntityId);
                }
            }
            catch(Exception e)
            {
                Error(this, e);
            }
        }

        public void Error(object caller, Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {caller.GetType().FullName}: {e.Message}\n{e.StackTrace}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ERROR in {ModContext.ModName}, Send SpaceEngineers.Log to mod author", 10000, MyFontEnum.Red);
        }

        public void Error(object caller, string message)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {caller.GetType().FullName}: {message}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ERROR in {ModContext.ModName}, Send SpaceEngineers.Log to mod author", 10000, MyFontEnum.Red);
        }
    }
}