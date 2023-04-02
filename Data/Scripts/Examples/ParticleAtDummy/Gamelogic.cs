using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Examples
{
    public interface IUpdateable
    {
        void Update();
    }

    public abstract partial class StandardParticleGamelogic : MyGameLogicComponent
    {
        IMyCubeBlock Block;
        int LastModelId = 0;
        List<ParticleBase> SpawnedParticles = new List<ParticleBase>();
        List<IUpdateable> UpdateableParticles = new List<IUpdateable>();

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

        protected void Declare(string dummy, string particle, string condition)
        {
            PropOverrides[dummy] = new Props(particle, condition);
        }

        protected virtual void Setup() { }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if(MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.IsServer)
                return;

            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void MarkForClose()
        {
            ClearParticles();
        }

        public override void UpdateOnceBeforeFrame()
        {
            Block = Entity as IMyCubeBlock;
            if(Block?.CubeGrid?.Physics == null)
                return;

            if(PropOverrides == null)
            {
                PropOverrides = new Dictionary<string, Props>();
                Setup();
            }

            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void UpdateBeforeSimulation10()
        {
            int modelId = Entity.Model.UniqueId;
            if(LastModelId != modelId)
            {
                LastModelId = modelId;
                ModelChanged();
            }

            foreach(IUpdateable obj in UpdateableParticles)
            {
                obj.Update();
            }
        }

        static Dictionary<string, IMyModelDummy> _tempDummies = new Dictionary<string, IMyModelDummy>();
        void ModelChanged()
        {
            try
            {
                ClearParticles();

                _tempDummies.Clear();
                Entity.Model.GetDummies(_tempDummies);
                if(_tempDummies.Count == 0)
                    return;

                foreach(IMyModelDummy dummy in _tempDummies.Values)
                {
                    ParticleBase particleData = CreateFromDummy(dummy);
                    if(particleData != null)
                    {
                        SpawnedParticles.Add(particleData);

                        IUpdateable updateable = (particleData as IUpdateable);
                        if(updateable != null)
                            UpdateableParticles.Add(updateable);
                    }
                }
            }
            catch(Exception e)
            {
                AddToLog(this, e);
            }
            finally
            {
                _tempDummies.Clear();
            }
        }

        ParticleBase CreateFromDummy(IMyModelDummy dummy)
        {
            if(PropOverrides != null && PropOverrides.Count > 0)
            {
                Props dec;
                if(PropOverrides.TryGetValue(dummy.Name, out dec))
                    return CreateParticleHolder(dummy, dec.ParticleSubtypeId, dec.Condition);
            }

            // customdata way, not working with SEUT currently but leaving it here in case anyone wants to experiment

            if(!dummy.Name.StartsWith("particle_", StringComparison.OrdinalIgnoreCase))
                return null;

            object obj;
            if(!dummy.CustomData.TryGetValue("particle", out obj))
            {
                AddToLog(this, $"Cannot find 'particle' customdata on dummy '{dummy.Name}' (block '{Block.BlockDefinition}')");
                return null;
            }

            string subtypeId = obj as string;
            if(string.IsNullOrEmpty(subtypeId))
            {
                AddToLog(this, $"Particle subtype is empty or wrong type for dummy '{dummy.Name}' (block '{Block.BlockDefinition}')");
                return null;
            }

            // example of an additional dummy property making a different object that has a condition
            if(dummy.CustomData.TryGetValue("condition", out obj) && obj is string)
            {
                string condition = (string)obj;
                return CreateParticleHolder(dummy, subtypeId, condition);
            }

            return CreateParticleHolder(dummy, subtypeId);
        }

        void ClearParticles()
        {
            foreach(var data in SpawnedParticles)
            {
                data.Close();
            }

            SpawnedParticles.Clear();
            UpdateableParticles.Clear();
        }

        public static void AddToLog(object source, Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {source?.GetType()?.FullName}: {e.ToString()}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {source?.GetType()?.FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }

        public static void AddToLog(object source, string text)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {source?.GetType()?.FullName}: {text}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {source?.GetType()?.FullName}: {text} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }
    }
}
