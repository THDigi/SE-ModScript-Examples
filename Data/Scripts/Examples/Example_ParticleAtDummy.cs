using System;
using System.Collections.Generic;
using ObjectBuilders.SafeZone;
using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.ObjectBuilders.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    // a script to allow modders to tie particles to specific dummies in their models.
    // it's a bit complex to use dummies and their properties while also allows itself to be easily expanded.

    // first off you need the dummies to start with particle_ prefix.
    // then their customdata/properties need to have a "particle" with the value being the subtypeId of the particle it spawns.
    // past that, you can program whatever condition you wish.
    // currently there's an example one where the "condition" property with "working" value is going to change the particle to only emit while block is working.

    // now these below are what link blocks to the gamelogic that handles the particle spawning.
    // the typeof(MyObjectBuilder_Reactor) is simply the block's <TypeId> with the MyObjectBuilder_ prefix tacked on (do not use the xsi:type value).
    // then you can optionally declare what subtypes to limit it to like the battery below, or leave it undeclared to not care about subtype, like the reactor below.

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Reactor), false)]
    public class ParticleOnReactor : StandardParticleGamelogic { }

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_BatteryBlock), false, "SpecificSubtype", "MoreIfNeeded", "Etc...")]
    public class ParticleOnBattery : StandardParticleGamelogic { }

    // ^ can be duplicated for multiple block types


    // just a basic particle container that holds it until model changes, nothing to edit here.
    public class ParticleBase
    {
        public MyParticleEffect Effect;

        public ParticleBase(MyParticleEffect effect)
        {
            Effect = effect;
        }

        public virtual void Close()
        {
            MyParticlesManager.RemoveParticleEffect(Effect);
        }
    }

    // example particle with condition
    // you can clone+rename+change to have some custom conditions, just remember to add it to CreateFromDummy(), at the switch(condition).
    public class ParticleOnWorking : ParticleBase, IUpdateable
    {
        public readonly IMyFunctionalBlock Block;

        public ParticleOnWorking(MyParticleEffect effect, IMyCubeBlock block) : base(effect)
        {
            Block = block as IMyFunctionalBlock;
            if(block == null)
                StandardParticleGamelogic.AddToLog(this, $"{GetType().Name}: Unsupported block type, needs on/off");
        }

        // frequency dictated by the gamelogic, currently it's every 10th tick (approx, they're spread out to run as few per tick as possible)
        public void Update()
        {
            if(Block == null)
                return;

            bool isEmitting = !Effect.IsEmittingStopped;

            if(Block.IsWorking != isEmitting)
            {
                if(Block.IsWorking)
                {
                    Effect.Play();
                }
                else
                {
                    Effect.StopEmitting();
                }
            }
        }
    }

    public interface IUpdateable
    {
        void Update();
    }

    public class StandardParticleGamelogic : MyGameLogicComponent
    {
        IMyCubeBlock Block;
        int LastModelId = 0;
        List<ParticleBase> SpawnedParticles = new List<ParticleBase>();
        List<IUpdateable> UpdateableParticles = new List<IUpdateable>();

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
            if(!dummy.Name.StartsWith("particle_", StringComparison.OrdinalIgnoreCase))
                return null;

            object obj;
            if(!dummy.CustomData.TryGetValue("particle", out obj))
            {
                //AddToLog(this, $"Cannot find particle customdata on dummy '{dummy.Name}' (block '{Block.BlockDefinition}')");
                return null;
            }

            string subtypeId = obj as string;

            if(string.IsNullOrEmpty(subtypeId))
            {
                AddToLog(this, $"Particle subtype is empty or wrong type for dummy '{dummy.Name}' (block '{Block.BlockDefinition}')");
                return null;
            }

            MyParticleEffect effect = SpawnParticle(subtypeId, dummy.Matrix);
            if(effect == null)
                return null;

            // example of an additional dummy property making a different object that has a condition
            if(dummy.CustomData.TryGetValue("condition", out obj) && obj is string)
            {
                string condition = (string)obj;
                switch(condition)
                {
                    case "working": return new ParticleOnWorking(effect, Block);

                        // add more here if you make more custom condition classes
                }
            }

            return new ParticleBase(effect);
        }

        MyParticleEffect SpawnParticle(string subtypeId, MatrixD localMatrix)
        {
            MyParticleEffect effect;
            Vector3D worldPos = Entity.GetPosition();
            uint parentId = Entity.Render.GetRenderObjectID();
            if(!MyParticlesManager.TryCreateParticleEffect(subtypeId, ref localMatrix, ref worldPos, parentId, out effect))
                return null;

            return effect;
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
