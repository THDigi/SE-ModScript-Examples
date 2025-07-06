using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using static VRageRender.MyBillboard;

namespace Digi.Experiments
{
    // NOTE: The new <Pivots> has Z flipped

    // for development/debugging purposes only!
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Debug_DrawSolarPanelRays : MySessionComponentBase
    {
        static readonly MyStringId MaterialDot = MyStringId.GetOrCompute("WhiteDot");
        static readonly MyStringId MaterialSquare = MyStringId.GetOrCompute("Square");

        List<IMyTerminalBlock> Blocks = new List<IMyTerminalBlock>();
        List<Vector3> TempPivots = new List<Vector3>(8);

        public override void LoadData()
        {
            // using this instead of gamelogic because solar panels and oxy farms override the gamelogic breaking any mod trying to add to that.
            MyEntities.OnEntityCreate += EntityCreated;
        }

        protected override void UnloadData()
        {
            MyEntities.OnEntityCreate -= EntityCreated;
        }

        void EntityCreated(MyEntity ent)
        {
            if(ent is IMySolarPanel || ent is IMyOxygenFarm)
            {
                var tb = (IMyTerminalBlock)ent;
                if(!Blocks.Contains(tb))
                    Blocks.Add(tb);
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                for(int i = (Blocks.Count - 1); i >= 0; i--)
                {
                    IMyTerminalBlock block = Blocks[i];

                    if(block.MarkedForClose)
                    {
                        Blocks.RemoveAtFast(i);
                        continue;
                    }

                    {
                        var def = block.SlimBlock.BlockDefinition as MySolarPanelDefinition;
                        if(def != null)
                            DrawRays(block, def.Size, def.IsTwoSided, def.PanelOrientation, def.PanelOffset, def.Pivots);
                    }

                    {
                        var def = block.SlimBlock.BlockDefinition as MyOxygenFarmDefinition;
                        if(def != null)
                            DrawRays(block, def.Size, def.IsTwoSided, def.PanelOrientation, def.PanelOffset);
                    }
                }
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLineAndConsole(e.ToString());

                if(MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} ]", 10000, MyFontEnum.Red);
            }
        }

        // logic from MySolarGameLogicComponent
        void DrawRays(IMyTerminalBlock block, Vector3I size, bool isTwoSided, Vector3 panelOrientation, float panelOffset, Vector3[] positions = null)
        {
            Vector3 directionToSun = MyVisualScriptLogicProvider.GetSunDirection();
            Color color;

            MyCubeGrid grid = (MyCubeGrid)block.CubeGrid;

            MatrixD worldMatrix = block.WorldMatrix;
            Vector3 panelOrientationWorld = Vector3.TransformNormal(panelOrientation, worldMatrix);

            // HACK: oxygen farm doesn't implement IMySolarOccludable (also this interface is not whitelisteD)
            if(block is IMySolarPanel && grid.IsSolarOccluded)
            {
                color = Color.Yellow;
            }
            else
            {
                float angleToSun = Vector3.Dot(panelOrientationWorld, directionToSun);
                if((angleToSun < 0f && !isTwoSided) || !block.IsFunctional)
                {
                    color = Color.Red;
                }
                else if(IsOnDarkSide(worldMatrix.Translation))
                {
                    color = Color.Gray;
                }
                else
                {
                    color = Color.Lime;
                }
            }

            float scale = (float)worldMatrix.Forward.Dot(panelOrientationWorld);
            float unit = block.CubeGrid.GridSize;

            if(positions != null) // from MyNewSolarGameLogicComponent
            {
                TempPivots.Clear();
                for(int i = 0; i < positions.Length; i++)
                {
                    TempPivots.Add(positions[i] * unit);
                }

                int pivotsCount = isTwoSided ? TempPivots.Count / 2 : TempPivots.Count;

                for(int i = 0; i < pivotsCount; i++)
                {
                    int idx = i * (isTwoSided ? 2 : 1);
                    Vector3D point = worldMatrix.Translation;
                    point += worldMatrix.Right * TempPivots[idx].X;
                    point += worldMatrix.Up * TempPivots[idx].Y;
                    point += worldMatrix.Forward * TempPivots[idx].Z;

                    MyTransparentGeometry.AddPointBillboard(MaterialDot, Color.Blue, point, 0.1f, 0, blendType: BlendTypeEnum.AdditiveTop);

                    if(isTwoSided)
                    {
                        Vector3D point2 = worldMatrix.Translation;
                        point2 += worldMatrix.Right * TempPivots[idx + 1].X;
                        point2 += worldMatrix.Up * TempPivots[idx + 1].Y;
                        point2 += worldMatrix.Forward * TempPivots[idx + 1].Z;

                        MyTransparentGeometry.AddPointBillboard(MaterialDot, Color.Red, point2, 0.075f, 0, blendType: BlendTypeEnum.AdditiveTop);

                        Vector3D test = worldMatrix.Translation + directionToSun * 100f;

                        if((point2 - test).LengthSquared() < (point - test).LengthSquared())
                        {
                            point = point2;
                        }
                    }

                    // HACK: game flips them when inputting to raycast, I'm flipping them early to be less confusing down the line
                    Vector3D from = point;
                    Vector3D to = point + directionToSun * 100f;

                    CastRay(from, to, color, block);
                }
            }
            else
            {
                for(int idx = 0; idx < 8; idx++)
                {
                    Vector3D pos = block.WorldMatrix.Translation;
                    pos += ((idx % 4) - 1.5f) * unit * scale * (size.X / 4f) * block.WorldMatrix.Left;
                    pos += ((idx / 4) - 0.5f) * unit * scale * (size.Y / 2f) * block.WorldMatrix.Up;
                    pos += unit * scale * (size.Z / 2f) * panelOrientationWorld * panelOffset;

                    // HACK: game flips them when inputting to raycast, I'm flipping them early to be less confusing down the line
                    Vector3D from = pos + directionToSun * unit / 4f;
                    Vector3D to = pos + directionToSun * 100f;

                    MyTransparentGeometry.AddPointBillboard(MaterialDot, Color.Yellow, from, 0.1f, 0, blendType: BlendTypeEnum.AdditiveTop);

                    CastRay(from, to, color, block);
                }
            }
        }

        void CastRay(Vector3D from, Vector3D to, Color color, IMyTerminalBlock block)
        {
            MyTransparentGeometry.AddLineBillboard(MaterialSquare, color, from, (to - from), 1f, 0.03f, BlendTypeEnum.AdditiveTop);

            var hits = new List<IHitInfo>(16);
            MyAPIGateway.Physics.CastRayParallel(ref from, ref to, hits, 15, (list) => OnRayCastCompleted(hits, from, to, color, block));
        }

        void OnRayCastCompleted(List<IHitInfo> hits, Vector3D from, Vector3D to, Color color, IMyTerminalBlock block)
        {
            bool inSun = true;

            foreach(IHitInfo hit in hits)
            {
                IMyEntity hitEntity = hit.HitEntity;
                if(hitEntity != block.CubeGrid)
                {
                    to = hit.Position;
                    inSun = false;
                    break;
                }

                MyCubeGrid grid = hitEntity as MyCubeGrid;
                Vector3I? gridPos = grid.RayCastBlocks(from, to);
                if(gridPos.HasValue && grid.GetCubeBlock(gridPos.Value) != block.SlimBlock)
                {
                    to = hit.Position;
                    inSun = false;
                    break;
                }
            }

            if(!inSun)
                MyTransparentGeometry.AddLineBillboard(MaterialSquare, Color.Red, from, (to - from), 1f, 0.06f, BlendTypeEnum.AdditiveTop);
        }

        // from MySectorWeatherComponent
        public static bool IsOnDarkSide(Vector3D point)
        {
            MyPlanet closestPlanet = MyGamePruningStructure.GetClosestPlanet(point);
            if(closestPlanet == null)
                return false;

            return MyVisualScriptLogicProvider.IsOnDarkSide(closestPlanet, point);
        }
    }
}
