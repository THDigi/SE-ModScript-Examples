using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.ObjectBuilders;

// avoid including ingame namespaces because they cause ambiguity errors, instead, do aliases like this:
using MyAssemblerMode = Sandbox.ModAPI.Ingame.MyAssemblerMode;

namespace Digi.Examples
{
    // Edit the block subtypes to match your custom block(s).
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Assembler), false, "YourSubtypeHere", "More if needed", "etc...")]
    public class Example_AssemblerForceMode : MyGameLogicComponent
    {
        const MyAssemblerMode ForceModeTo = MyAssemblerMode.Assembly;

        IMyAssembler Assembler;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if(MyAPIGateway.Session.IsServer)
            {
                NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            }
        }

        public override void UpdateOnceBeforeFrame()
        {
            Assembler = Entity as IMyAssembler;

            if(Assembler?.CubeGrid?.Physics == null)
                return; // ignore non-assemblers, physicsless grids and whatever other cases would cause any of those things to be null

            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateBeforeSimulation()
        {
            if(Assembler.Mode != ForceModeTo)
            {
                Assembler.Mode = ForceModeTo;
            }
        }
    }
}
