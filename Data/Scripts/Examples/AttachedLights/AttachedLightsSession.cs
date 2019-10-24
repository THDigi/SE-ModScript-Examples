using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Lights;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Digi.AttachedLights
{
    public delegate void LightConfigurator(string dummyName, MyLight light, BlockLogic blockLogic);

    public class BlockHandling
    {
        public LightConfigurator ConfiguratorForAll = null;
        public Dictionary<string, LightConfigurator> ConfiguratorPerSubtype = null;
    }

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public partial class AttachedLightsSession : MySessionComponentBase
    {
        public const string DUMMY_PREFIX = "customlight_";
        public const int CHECK_VIEW_DIST_EVERY_TICKS = 15;

        public static AttachedLightsSession Instance;

        public List<BlockLogic> UpdateOnce;
        public Dictionary<long, Action<Vector3D>> ViewDistanceChecks;
        public Dictionary<string, IMyModelDummy> TempDummies;

        Dictionary<MyObjectBuilderType, BlockHandling> monitorTypes;
        Dictionary<long, BlockLogic> blockLogics;
        int tick;

        public override void LoadData()
        {
            try
            {
                if(MyAPIGateway.Utilities.IsDedicated) // DS side doesn't need lights
                    return;

                Instance = this;
                UpdateOnce = new List<BlockLogic>();
                monitorTypes = new Dictionary<MyObjectBuilderType, BlockHandling>();
                blockLogics = new Dictionary<long, BlockLogic>();
                ViewDistanceChecks = new Dictionary<long, Action<Vector3D>>();

                TempDummies = new Dictionary<string, IMyModelDummy>();

                MyAPIGateway.Entities.OnEntityAdd += EntitySpawned;

                SetUpdateOrder(MyUpdateOrder.AfterSimulation);

                Setup();
            }
            catch(Exception e)
            {
                SimpleLog.Error(this, e);
                UnloadData();
                throw;
            }
        }

        protected override void UnloadData()
        {
            Instance = null;

            if(MyAPIGateway.Utilities.IsDedicated)
                return;

            MyAPIGateway.Entities.OnEntityAdd -= EntitySpawned;

            monitorTypes?.Clear();
            monitorTypes = null;

            blockLogics?.Clear();
            blockLogics = null;

            ViewDistanceChecks?.Clear();
            ViewDistanceChecks = null;

            TempDummies?.Clear();
            TempDummies = null;
        }

        void Add(LightConfigurator settings, MyObjectBuilderType blockType, params string[] subtypes)
        {
            BlockHandling blockHandling;

            if(!monitorTypes.TryGetValue(blockType, out blockHandling))
            {
                blockHandling = new BlockHandling();
                monitorTypes.Add(blockType, blockHandling);
            }

            if(subtypes == null || subtypes.Length == 0)
            {
                blockHandling.ConfiguratorForAll = settings;
            }
            else
            {
                if(blockHandling.ConfiguratorPerSubtype == null)
                {
                    blockHandling.ConfiguratorPerSubtype = new Dictionary<string, LightConfigurator>();
                }

                foreach(var subtype in subtypes)
                {
                    if(blockHandling.ConfiguratorPerSubtype.ContainsKey(subtype))
                    {
                        SimpleLog.Error(this, $"Subtype '{subtype}' for type {blockType.ToString()} was already previously registered!");
                        continue;
                    }

                    blockHandling.ConfiguratorPerSubtype.Add(subtype, settings);
                }
            }
        }

        void EntitySpawned(IMyEntity ent)
        {
            try
            {
                var grid = ent as MyCubeGrid;

                if(grid == null || grid.Physics == null || grid.IsPreview)
                    return;

                grid.OnBlockAdded += BlockAdded;
                grid.OnClose += GridClosed;

                foreach(IMySlimBlock slim in grid.GetBlocks())
                {
                    BlockAdded(slim);
                }
            }
            catch(Exception e)
            {
                SimpleLog.Error(this, e);
            }
        }

        void GridClosed(IMyEntity ent)
        {
            try
            {
                var grid = (IMyCubeGrid)ent;
                grid.OnBlockAdded -= BlockAdded;
                grid.OnClose -= GridClosed;
            }
            catch(Exception e)
            {
                SimpleLog.Error(this, e);
            }
        }

        void BlockAdded(IMySlimBlock slimBlock)
        {
            try
            {
                var defId = slimBlock.BlockDefinition.Id;
                BlockHandling blockHandling;

                if(monitorTypes.TryGetValue(defId.TypeId, out blockHandling))
                {
                    LightConfigurator settings;

                    if(blockHandling.ConfiguratorPerSubtype != null && blockHandling.ConfiguratorPerSubtype.TryGetValue(defId.SubtypeName, out settings))
                    {
                        CreateLogicFor(slimBlock, settings);
                    }
                    else if(blockHandling.ConfiguratorForAll != null)
                    {
                        CreateLogicFor(slimBlock, blockHandling.ConfiguratorForAll);
                    }
                }
            }
            catch(Exception e)
            {
                SimpleLog.Error(this, e);
            }
        }

        void CreateLogicFor(IMySlimBlock slimBlock, LightConfigurator settings)
        {
            var def = (MyCubeBlockDefinition)slimBlock.BlockDefinition;

            if(def.BlockTopology == MyBlockTopology.Cube && def.Model == null)
            {
                // deformable armor not supported.
                return;
            }

            var block = slimBlock.FatBlock;

            if(block == null)
            {
                SimpleLog.Error(this, $"{slimBlock.BlockDefinition.Id.ToString()} has no fatblock?! buildRatio={slimBlock.BuildLevelRatio.ToString()}; damageRatio={slimBlock.DamageRatio.ToString()}");
                return;
            }

            if(blockLogics.ContainsKey(block.EntityId))
            {
                BlockMarkedForClose(block);
            }

            var logic = new BlockLogic(this, block, settings);
            block.OnMarkForClose += BlockMarkedForClose;
            blockLogics[block.EntityId] = logic;
        }

        void BlockMarkedForClose(IMyEntity ent)
        {
            try
            {
                var block = (IMyCubeBlock)ent;
                block.OnMarkForClose -= BlockMarkedForClose;

                blockLogics.GetValueOrDefault(block.EntityId, null)?.Close();
                blockLogics.Remove(block.EntityId);
                ViewDistanceChecks.Remove(block.EntityId);
            }
            catch(Exception e)
            {
                SimpleLog.Error(this, e);
            }
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if(ViewDistanceChecks.Count > 0 && ++tick % CHECK_VIEW_DIST_EVERY_TICKS == 0)
                {
                    var cameraPos = MyAPIGateway.Session.Camera.WorldMatrix.Translation;

                    foreach(var action in ViewDistanceChecks.Values)
                    {
                        action(cameraPos);
                    }
                }

                if(UpdateOnce.Count > 0)
                {
                    foreach(var logic in UpdateOnce)
                    {
                        if(logic.Block.MarkedForClose)
                            continue;

                        logic.UpdateOnce();
                    }

                    UpdateOnce.Clear();
                }
            }
            catch(Exception e)
            {
                SimpleLog.Error(this, e);
            }
        }
    }
}
