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
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Digi.Experiments
{
    // for development/debugging purposes only
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Debug_DrawSolarBlocks : MySessionComponentBase
    {
        MyStringId MaterialLaser = MyStringId.GetOrCompute("WeaponLaser");

        List<IMyTerminalBlock> SolarBlocks = new List<IMyTerminalBlock>();

        public override void LoadData()
        {
            // using this instead of gamelogic because solars panels and oxy farms override the gamelogic breaking any mod trying to add to that.
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
                SolarBlocks.Add((IMyTerminalBlock)ent);
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                foreach(IMyTerminalBlock block in SolarBlocks)
                {
                    MyCubeBlockDefinition def = (MyCubeBlockDefinition)block.SlimBlock.BlockDefinition;

                    {
                        MySolarPanelDefinition solarPanelDef = def as MySolarPanelDefinition;
                        if(solarPanelDef != null)
                        {
                            DrawSolarBlock(block, def, solarPanelDef.PanelOrientation, solarPanelDef.IsTwoSided, solarPanelDef.PanelOffset);
                            continue;
                        }
                    }
                    {
                        MyOxygenFarmDefinition oxyFarmDef = def as MyOxygenFarmDefinition;
                        if(oxyFarmDef != null)
                        {
                            DrawSolarBlock(block, def, oxyFarmDef.PanelOrientation, oxyFarmDef.IsTwoSided, oxyFarmDef.PanelOffset);
                            continue;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

                if(MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} ]", 10000, MyFontEnum.Red);
            }
        }

        // cloned from MySolarGameLogicComponent.ComputeSunAngle()
        // if MySolarGameLogicComponent were whitelisted we could just read DebugIsPivotInSun instead.
        void DrawSolarBlock(IMyTerminalBlock block, MyCubeBlockDefinition def, Vector3 panelOrientation, bool isTwoSided, float panelOffset)
        {
            Vector3 directionToSun = MyVisualScriptLogicProvider.GetSunDirection();

            const float LineThick = 0.05f;
            const MyBillboard.BlendTypeEnum LineBlend = MyBillboard.BlendTypeEnum.SDR;
            Color color = Color.White;

            //float angleToSun = Vector3.Dot(Vector3.Transform(panelOrientation, block.WorldMatrix.GetOrientation()), directionToSun);
            //if((angleToSun < 0f && !isTwoSided) || !block.IsFunctional)
            //{
            //    color = Color.Red;
            //}
            //else if(IsOnDarkSide(block.WorldMatrix.Translation))
            //{
            //    color = Color.Red;
            //}
            //else
            //{
            //    color = Color.Lime;
            //}

            MatrixD orientation = block.WorldMatrix.GetOrientation();
            float scale = (float)block.WorldMatrix.Forward.Dot(Vector3.Transform(panelOrientation, orientation));
            float unit = block.CubeGrid.GridSize;

            for(int idx = 0; idx < 8; idx++)
            {
                Vector3D pos = block.WorldMatrix.Translation;
                pos += ((idx % 4) - 1.5f) * unit * scale * (def.Size.X / 4f) * block.WorldMatrix.Left;
                pos += ((idx / 4) - 0.5f) * unit * scale * (def.Size.Y / 2f) * block.WorldMatrix.Up;
                pos += unit * scale * (def.Size.Z / 2f) * Vector3.Transform(panelOrientation, orientation) * panelOffset;

                Vector3D from = pos + directionToSun * 100f;
                Vector3D to = pos + directionToSun * unit / 4f;

                MyTransparentGeometry.AddLineBillboard(MaterialLaser, color, from, (to - from), 1f, LineThick, LineBlend);
            }
        }

        // from MySectorWeatherComponent
        //public static bool IsOnDarkSide(Vector3D point)
        //{
        //    MyPlanet closestPlanet = MyGamePruningStructure.GetClosestPlanet(point);
        //    if(closestPlanet == null)
        //        return false;
        //
        //    return IsThereNight(closestPlanet, ref point);
        //}
        //
        //public static bool IsThereNight(MyPlanet planet, ref Vector3D position)
        //{
        //    Vector3D value = position - planet.PositionComp.GetPosition();
        //    if((float)value.Length() > planet.MaximumRadius * 1.1f)
        //        return false;
        //
        //    Vector3 vector = Vector3.Normalize(value);
        //    return Vector3.Dot(MyVisualScriptLogicProvider.GetSunDirection(), vector) < -0.1f;
        //}
    }
}
