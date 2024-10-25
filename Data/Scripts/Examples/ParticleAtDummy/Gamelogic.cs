using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Examples
{
    public interface IUpdateable
    {
        MyEntityUpdateEnum Frequency { get; }

        void Update();
    }

    public abstract partial class StandardParticleGamelogic : MyGameLogicComponent
    {
        protected bool DebugMode = false;
        IMyCubeBlock Block;
        List<ParticleBase> SpawnedParticles;
        List<IUpdateable> UpdateParticles;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if(MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void MarkForClose()
        {
            RemoveAllParticles();
        }

        public override void UpdateOnceBeforeFrame()
        {
            Block = Entity as IMyCubeBlock;
            if(Block?.CubeGrid?.Physics == null)
            {
                if(DebugMode)
                    MyAPIGateway.Utilities.ShowMessage(GetType().Name, "Ghost grid, skipped");

                return;
            }

            if(PropOverrides == null)
            {
                PropOverrides = new Dictionary<string, Props>();
                Setup();
            }

            var ent = (MyEntity)Block;
            ent.OnModelRefresh += BlockModelChanged;
            BlockModelChanged(ent);

            if(DebugMode)
                MyAPIGateway.Utilities.ShowMessage(GetType().Name, "Logic initialized");
        }

        #region For user setup
        static Dictionary<string, Props> PropOverrides;

        class Props
        {
            public readonly string ParticleSubtypeId;
            public readonly string Condition;

            public Props(string particleSubtypeId, string condition = null)
            {
                ParticleSubtypeId = particleSubtypeId;
                Condition = condition;
            }
        }

        protected void Declare(string dummy, string particle, string condition = null)
        {
            if(PropOverrides.ContainsKey(dummy))
                LogError(this, $"Dummy '{dummy}' is declared multiple times, the older ones are overwritten!");

            PropOverrides[dummy] = new Props(particle, condition);
        }

        protected virtual void Setup() { }
        #endregion

        public override void UpdateAfterSimulation()
        {
            if(UpdateParticles == null)
                return;

            foreach(var obj in UpdateParticles)
            {
                if(obj.Frequency == MyEntityUpdateEnum.EACH_FRAME)
                    obj.Update();
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            if(UpdateParticles == null)
                return;

            foreach(var obj in UpdateParticles)
            {
                if(obj.Frequency == MyEntityUpdateEnum.EACH_10TH_FRAME)
                    obj.Update();
            }
        }

        public override void UpdateAfterSimulation100()
        {
            if(UpdateParticles == null)
                return;

            foreach(var obj in UpdateParticles)
            {
                if(obj.Frequency == MyEntityUpdateEnum.EACH_100TH_FRAME)
                    obj.Update();
            }
        }

        static readonly Dictionary<string, IMyModelDummy> TempDummies = new Dictionary<string, IMyModelDummy>();

        void BlockModelChanged(MyEntity ent)
        {
            try
            {
                RemoveAllParticles();
                FindDummiesRecursive(ent);

                var updates = MyEntityUpdateEnum.NONE;
                if(UpdateParticles != null)
                {
                    foreach(var obj in UpdateParticles)
                    {
                        updates |= obj.Frequency;
                    }
                }
                NeedsUpdate = updates;
            }
            catch(Exception e)
            {
                LogError(this, e);
            }
        }

        void FindDummiesRecursive(MyEntity ent)
        {
            GetDummies(ent);

            foreach(MyEntitySubpart subpart in ent.Subparts.Values)
            {
                FindDummiesRecursive(subpart);
            }
        }

        void GetDummies(IMyEntity parent)
        {
            try
            {
                TempDummies.Clear();
                parent.Model.GetDummies(TempDummies);
                if(TempDummies.Count == 0)
                    return;

                foreach(IMyModelDummy dummy in TempDummies.Values)
                {
                    ParticleBase particleData = null;
                    try
                    {
                        particleData = CreateFromDummy(parent, dummy);
                        if(particleData != null)
                        {
                            if(DebugMode)
                                MyAPIGateway.Utilities.ShowMessage(GetType().Name, $"Spawned {particleData.GetType().Name} for {dummy.Name}");

                            if(SpawnedParticles == null)
                                SpawnedParticles = new List<ParticleBase>();

                            SpawnedParticles.Add(particleData);

                            var updateable = particleData as IUpdateable;
                            if(updateable != null)
                            {
                                if(UpdateParticles == null)
                                    UpdateParticles = new List<IUpdateable>();

                                UpdateParticles.Add(updateable);
                            }
                        }
                        else
                        {
                            if(DebugMode)
                                MyAPIGateway.Utilities.ShowMessage(GetType().Name, $"Skipped {dummy.Name}");
                        }
                    }
                    catch(Exception e)
                    {
                        LogError(this, e);
                        particleData?.Close();
                    }
                }
            }
            finally
            {
                TempDummies.Clear();
            }
        }

        ParticleBase CreateFromDummy(IMyEntity parent, IMyModelDummy dummy)
        {
            if(PropOverrides != null && PropOverrides.Count > 0)
            {
                Props dec;
                if(PropOverrides.TryGetValue(dummy.Name, out dec))
                    return CreateParticleHolder(parent, dummy, dec.ParticleSubtypeId, dec.Condition);
            }

            // customdata way, not working with SEUT currently but leaving it here in case anyone wants to experiment

            if(!dummy.Name.StartsWith("particle_", StringComparison.OrdinalIgnoreCase))
                return null;

            object obj;
            if(!dummy.CustomData.TryGetValue("particle", out obj))
            {
                LogError(this, $"Cannot find 'particle' customdata on dummy '{dummy.Name}' (block '{Block.BlockDefinition}')");
                return null;
            }

            string subtypeId = obj as string;
            if(string.IsNullOrEmpty(subtypeId))
            {
                LogError(this, $"Particle subtype is empty or wrong type for dummy '{dummy.Name}' (block '{Block.BlockDefinition}')");
                return null;
            }

            // example of an additional dummy property making a different object that has a condition
            if(dummy.CustomData.TryGetValue("condition", out obj) && obj is string)
            {
                string condition = (string)obj;
                return CreateParticleHolder(parent, dummy, subtypeId, condition);
            }

            return CreateParticleHolder(parent, dummy, subtypeId);
        }

        void RemoveAllParticles()
        {
            try
            {
                if(SpawnedParticles != null)
                {
                    foreach(ParticleBase obj in SpawnedParticles)
                    {
                        obj.Close();
                    }
                }
            }
            finally
            {
                SpawnedParticles?.Clear();
                UpdateParticles?.Clear();
            }
        }

        public static void LogError(object source, Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {source?.GetType()?.FullName}: {e.ToString()}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {source?.GetType()?.FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }

        public static void LogError(object source, string text)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {source?.GetType()?.FullName}: {text}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {source?.GetType()?.FullName}: {text} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }
    }
}
