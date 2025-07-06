using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Weapons;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum; // HACK bypass non-whitelisted MyBillboard to get to the whitelisted BlendTypeEnum

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class RealisticHoloSight : MySessionComponentBase
    {
        // This class is be used to simulate a holographic sight for handheld rifles.
        //
        // Usage:
        //   Rifle model needs to have either holosight_rectangle or holosight_circle named dummy that defines the sight viewport.
        //   Image reference: https://i.imgur.com/4NS5w2K.png
        //
        //   Edit SetupGuns() below to add your weapons' physical item subtype IDs and optionally you can customize the reticle.
        //   By default it uses WhiteDot material for the reticle or you can define a custom one via TransparentMaterials.sbc, copy game's WhiteDot for reference.

        private void SetupGuns()
        {
            // physical item's subtypeId
            AddGun("YourRifleItemSubtypeId", new DrawSettings()
            {
                // uncomment/add things you wanna change only for this gun, otherwise it'll use the defaults from DrawSettings.

                //ReticleMaterial = MyStringId.GetOrCompute("WhiteDot"),
                //ReticleColor = new Vector4(2, 0, 0, 1);,
                //ReticleSize = 0.001f,
                //FadeStartRatio = 0.8,
                //ReplaceModel = @"Models\YourModel.mwm",
            });

            // multiple AddGun() statements are supported
        }

        private class DrawSettings
        {
            // editing these affects defaults
            public string ReticleMaterial = "WhiteDot"; // material from TransparentMaterials SBC.
            public Vector4 ReticleColor = new Vector4(2, 0, 0, 1); // color R,G,B,A; values from 0 to 1 for regular color, higher than 1 increases intensity.
            public float ReticleSize = 0.001f; // size in meters, needs to be pretty tiny.
            public double FadeStartRatio = 0.8; // angle % at which reticle starts to fade. 0 means it starts fading as soon as it's not centered; 1 is no fading.
            public string ReplaceModel = @""; // if not empty replaces the rifle model (model must be in current mod). useful for replacing vanilla or other mods' models without overwriting their entire definition.

            // caching stuff, not for editing
            internal bool Processed = false;
            internal MyStringId Material;
            internal SightType Type;
            internal Matrix DummyMatrix;
            internal double MaxAngleH;
            internal double MaxAngleV;
        }

        // not for editing below this point

        private const float MAX_VIEW_DIST_SQ = 5 * 5;
        private const double RETICLE_FRONT_OFFSET = 0.25;
        private const double PROJECTED_DISTANCE = 400; // if this is too large it will cause errors on the angle calculations
        private const BlendTypeEnum RETICLE_BLEND_TYPE = BlendTypeEnum.SDR;

        private const string DUMMY_PREFIX = "holosight";
        private const string DUMMY_RECTANGLE_SUFFIX = "_rectangle";
        private const string DUMMY_CIRCLE_SUFFIX = "_circle";

        private List<DrawData> drawInfo;
        private Dictionary<string, IMyModelDummy> dummies;
        private Dictionary<MyDefinitionId, DrawSettings> drawSettings;

        private enum SightType
        {
            Unknown = 0,
            Rectangle,
            Circle,
        }

        private class DrawData
        {
            public readonly IMyEntity Entity;
            public readonly DrawSettings Settings;

            public DrawData(IMyEntity ent, DrawSettings settings)
            {
                Entity = ent;
                Settings = settings;
            }
        }

        public override void LoadData()
        {
            if(MyAPIGateway.Utilities.IsDedicated)
                return;

            drawSettings = new Dictionary<MyDefinitionId, DrawSettings>(MyDefinitionId.Comparer);
            drawInfo = new List<DrawData>();
            dummies = new Dictionary<string, IMyModelDummy>();

            SetupGuns();
            ReplaceModels();

            MyAPIGateway.Entities.OnEntityAdd += EntityAdded;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Entities.OnEntityAdd -= EntityAdded;
        }

        private void AddGun(string subType, DrawSettings settings)
        {
            drawSettings.Add(new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), subType), settings);
        }

        private void ReplaceModels()
        {
            foreach(var kv in drawSettings)
            {
                if(string.IsNullOrWhiteSpace(kv.Value.ReplaceModel))
                    continue;

                MyPhysicalItemDefinition def;

                if(MyDefinitionManager.Static.TryGetPhysicalItemDefinition(kv.Key, out def))
                {
                    def.Model = Path.Combine(ModContext.ModPath, kv.Value.ReplaceModel);
                }
            }
        }

        private void EntityAdded(IMyEntity ent)
        {
            try
            {
                var floatingObject = ent as MyFloatingObject;

                if(floatingObject != null)
                {
                    AddSupportedGun(ent, floatingObject.ItemDefinition.Id);
                    return;
                }

                var handHeldItem = ent as IMyAutomaticRifleGun;

                if(handHeldItem != null)
                {
                    AddSupportedGun(ent, handHeldItem.PhysicalItemId);
                    return;
                }
            }
            catch(Exception e)
            {
                LogError(e);
            }
        }

        private void AddSupportedGun(IMyEntity ent, MyDefinitionId physItemId)
        {
            DrawSettings settings;

            if(!drawSettings.TryGetValue(physItemId, out settings))
                return;

            for(int i = 0; i < drawInfo.Count; ++i)
            {
                if(drawInfo[i].Entity == ent)
                    return;
            }

            if(!settings.Processed) // parse dummies only once per gun subtype
            {
                settings.Processed = true;
                settings.Material = MyStringId.GetOrCompute(settings.ReticleMaterial);

                dummies.Clear();
                ent.Model.GetDummies(dummies);

                foreach(var dummy in dummies.Values)
                {
                    if(dummy.Name.StartsWith(DUMMY_PREFIX))
                    {
                        var dummyMatrix = dummy.Matrix;
                        var gunMatrix = ent.WorldMatrix;

                        var reticleProjectedPosition = Vector3D.Transform(dummyMatrix.Translation, gunMatrix) + gunMatrix.Forward * PROJECTED_DISTANCE;
                        var sightPositionLocal = dummyMatrix.Translation;

                        if(dummy.Name.EndsWith(DUMMY_RECTANGLE_SUFFIX))
                        {
                            settings.Type = SightType.Rectangle;

                            var edgePosH = Vector3D.Transform(sightPositionLocal + dummyMatrix.Left * 0.5f, gunMatrix);
                            var reticleToEdgePosH = Vector3D.Normalize(reticleProjectedPosition - edgePosH);
                            settings.MaxAngleH = Math.Acos(Vector3D.Dot(gunMatrix.Forward, reticleToEdgePosH));

                            var edgePosV = Vector3D.Transform(sightPositionLocal + dummyMatrix.Up * 0.5f, gunMatrix);
                            var reticleToEdgePosV = Vector3D.Normalize(reticleProjectedPosition - edgePosV);
                            settings.MaxAngleV = Math.Acos(Vector3D.Dot(gunMatrix.Forward, reticleToEdgePosV));
                        }
                        else if(dummy.Name.EndsWith(DUMMY_CIRCLE_SUFFIX))
                        {
                            settings.Type = SightType.Circle;

                            var edgePos = Vector3D.Transform(sightPositionLocal + dummyMatrix.Left * 0.5f, gunMatrix);
                            var reticleToEdgePos = Vector3D.Normalize(reticleProjectedPosition - edgePos);
                            settings.MaxAngleH = Math.Acos(Vector3D.Dot(gunMatrix.Forward, reticleToEdgePos));
                        }
                        else
                        {
                            throw new Exception($"{physItemId.SubtypeName} has unsupported dummy suffix: {dummy.Name}");
                        }

                        settings.DummyMatrix = dummy.Matrix;
                        break;
                    }
                }

                dummies.Clear();
            }

            drawInfo.Add(new DrawData(ent, settings));
        }

        public override void Draw()
        {
            try
            {
                int count = drawInfo.Count;

                if(count == 0)
                    return;

                var camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;

                for(int i = count - 1; i >= 0; i--)
                {
                    var data = drawInfo[i];
                    var ent = data.Entity;
                    var settings = data.Settings;

                    if(ent.MarkedForClose)
                    {
                        drawInfo.RemoveAtFast(i);
                        continue;
                    }

                    var gunMatrix = ent.WorldMatrix;

                    var fwDot = gunMatrix.Forward.Dot(camMatrix.Forward);
                    if(fwDot <= 0)
                        continue; // looking more than 90deg away from the direction of the gun.

                    if(Vector3D.DistanceSquared(camMatrix.Translation, gunMatrix.Translation) > MAX_VIEW_DIST_SQ)
                        continue; // too far away to be seen

                    var dummyMatrix = settings.DummyMatrix; // scaled exactly like the dummy from the model

                    var reticleProjectedPosition = Vector3D.Transform(dummyMatrix.Translation, gunMatrix) + gunMatrix.Forward * PROJECTED_DISTANCE;
                    var sightPosition = Vector3D.Transform(dummyMatrix.Translation, gunMatrix);

                    var fwOffsetDot = gunMatrix.Forward.Dot(sightPosition - camMatrix.Translation);
                    if(fwOffsetDot < 0)
                        continue; // camera is ahead of sight, don't draw reticle

                    if(settings.Type == SightType.Rectangle)
                    {
                        var camToReticleDir = Vector3D.Normalize(reticleProjectedPosition - camMatrix.Translation);
                        double angleH = Math.Acos(Vector3D.Dot(gunMatrix.Left, camToReticleDir)) - (Math.PI / 2); // subtracting 90deg
                        double angleV = Math.Acos(Vector3D.Dot(gunMatrix.Up, camToReticleDir)) - (Math.PI / 2);

                        // simplifies math later on
                        angleH = Math.Abs(angleH);
                        angleV = Math.Abs(angleV);

                        if(angleH < settings.MaxAngleH && angleV < settings.MaxAngleV)
                        {
                            var alphaH = GetAlphaForAngle(settings.FadeStartRatio, angleH, settings.MaxAngleH);
                            var alphaV = GetAlphaForAngle(settings.FadeStartRatio, angleV, settings.MaxAngleV);

                            var camToSightDistance = Vector3D.Distance(sightPosition, camMatrix.Translation) + RETICLE_FRONT_OFFSET;
                            var reticlePosition = camMatrix.Translation + (camToReticleDir * camToSightDistance);

                            MyTransparentGeometry.AddBillboardOriented(settings.Material, settings.ReticleColor * (alphaH * alphaV), reticlePosition, gunMatrix.Left, gunMatrix.Up, settings.ReticleSize, blendType: RETICLE_BLEND_TYPE);
                        }
                    }
                    else if(settings.Type == SightType.Circle)
                    {
                        var camToReticleDir = Vector3D.Normalize(reticleProjectedPosition - camMatrix.Translation);
                        double angle = Math.Acos(Vector3D.Dot(gunMatrix.Forward, camToReticleDir));

                        if(angle < settings.MaxAngleH)
                        {
                            var alpha = GetAlphaForAngle(settings.FadeStartRatio, angle, settings.MaxAngleH);

                            var camToSightDistance = Vector3D.Distance(sightPosition, camMatrix.Translation) + RETICLE_FRONT_OFFSET;
                            var reticlePosition = camMatrix.Translation + (camToReticleDir * camToSightDistance);

                            MyTransparentGeometry.AddBillboardOriented(settings.Material, settings.ReticleColor * alpha, reticlePosition, gunMatrix.Left, gunMatrix.Up, settings.ReticleSize, blendType: RETICLE_BLEND_TYPE);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                LogError(e);
            }
        }

        private float GetAlphaForAngle(double fadeRatio, double angle, double boundaryAngle)
        {
            var fadeOutStartAngle = (boundaryAngle * fadeRatio);

            if(angle > fadeOutStartAngle)
            {
                var amount = (angle - fadeOutStartAngle) / (boundaryAngle - fadeOutStartAngle);
                return 1f - (float)amount;
            }

            return 1f;
        }

        private void LogError(Exception e)
        {
            MyLog.Default.WriteLineAndConsole(e.ToString());

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }
    }
}
