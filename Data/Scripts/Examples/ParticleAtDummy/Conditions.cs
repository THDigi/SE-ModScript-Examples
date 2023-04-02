using System;
using System.Text;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using SpaceEngineers.ObjectBuilders.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    // this is where the conditions are registered with the gamelogic
    public abstract partial class StandardParticleGamelogic
    {
        ParticleBase CreateParticleHolder(IMyModelDummy dummy, string particleSubtypeId, string condition = null)
        {
            switch(condition)
            {
                case "working": // only shows particle if block is functional+enabled+powered
                    return new ParticleOnWorking(this, particleSubtypeId, dummy.Matrix);

                case "producing": // only show particle if block is a refinery/assembler/survivalkit/gasgenerator and it's currently producing
                    return new ParticleOnProducing(this, particleSubtypeId, dummy.Matrix);

                    // add more here if you make more custom condition classes
            }

            return new ParticleBase(this, particleSubtypeId, dummy.Matrix);
        }
    }

    // just a basic particle container that holds it until model changes, nothing to edit here.
    public class ParticleBase
    {
        public StandardParticleGamelogic GameLogic;
        public string SubtypeId;
        public MatrixD LocalMatrix;
        public MyParticleEffect Effect;

        public ParticleBase(StandardParticleGamelogic gamelogic, string subtypeId, MatrixD localMatrix)
        {
            GameLogic = gamelogic;
            SubtypeId = subtypeId;
            LocalMatrix = localMatrix;

            Effect = SpawnParticle();
            if(Effect == null)
                throw new Exception($"Couldn't spawn particle: {subtypeId}");
        }

        public virtual void Close()
        {
            if(Effect != null)
                MyParticlesManager.RemoveParticleEffect(Effect);
        }

        protected virtual MyParticleEffect SpawnParticle()
        {
            MyParticleEffect effect;
            Vector3D worldPos = GameLogic.Entity.GetPosition();
            uint parentId = GameLogic.Entity.Render.GetRenderObjectID();
            if(!MyParticlesManager.TryCreateParticleEffect(SubtypeId, ref LocalMatrix, ref worldPos, parentId, out effect))
                return null;

            return effect;
        }
    }

    // example particle with condition
    // you can clone+rename+change to have some custom conditions, just remember to add it to CreateParticleHolder() above.
    public class ParticleOnWorking : ParticleBase, IUpdateable
    {
        public readonly IMyFunctionalBlock Block;

        public ParticleOnWorking(StandardParticleGamelogic gamelogic, string subtypeId, MatrixD localMatrix) : base(gamelogic, subtypeId, localMatrix)
        {
            Block = gamelogic.Entity as IMyFunctionalBlock;
            if(Block == null)
                throw new Exception($"{GetType().Name}: Unsupported block type, needs on/off");
        }

        // frequency dictated by the gamelogic, currently it's every 10th tick (approx, they're spread out to run as few per tick as possible)
        public void Update()
        {
            if(Block == null)
                return;

            bool currentState = Effect != null;
            bool targetState = Block.IsWorking;

            if(targetState != currentState)
            {
                if(targetState)
                {
                    Effect = SpawnParticle();
                }
                else
                {
                    Effect.Stop();
                    Effect = null;
                }
            }
        }
    }

    public class ParticleOnProducing : ParticleBase, IUpdateable
    {
        public readonly IMyProductionBlock Production;
        public readonly IMyGasGenerator GasGenerator;

        public ParticleOnProducing(StandardParticleGamelogic gamelogic, string subtypeId, MatrixD localMatrix) : base(gamelogic, subtypeId, localMatrix)
        {
            Production = gamelogic.Entity as IMyProductionBlock;
            GasGenerator = gamelogic.Entity as IMyGasGenerator;

            if(Production == null && GasGenerator == null)
                throw new Exception($"{GetType().Name}: Unsupported block type, needs to be: assembler, survivalkit, refinery, gasgenerator");
        }

        public void Update()
        {
            bool currentState = Effect != null;
            bool targetState = false;

            if(Production != null)
            {
                targetState = Production.IsProducing;
            }
            else if(GasGenerator != null)
            {
                MyResourceSourceComponent sourceComp = GasGenerator.Components.Get<MyResourceSourceComponent>();
                foreach(MyDefinitionId resourceType in sourceComp.ResourceTypes)
                {
                    if(sourceComp.CurrentOutputByType(resourceType) > 0f)
                    {
                        targetState = true;
                        break;
                    }
                }
            }

            if(targetState != currentState)
            {
                if(targetState)
                {
                    Effect = SpawnParticle();
                }
                else
                {
                    Effect.Stop();
                    Effect = null;
                }
            }
        }
    }
}
