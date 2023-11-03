using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Digi.Examples
{
    // Keen's session components have their Draw() method called in a thread to be parallel to other things.
    // (See MyGuiScreenGamePlay.Draw(), the very first Parallel.Start() will iterate their session Draw() calls)
    //
    // Mods' are forced to be sync because that would break things for existing mods, but they also didn't offer it as an option...
    //
    // This here is a very hacky way of getting a session component's Draw() to be async like the Keen ones, if you need such a thing.
    // Use with caution!
    //
    // NOTE: This whole thing relies on Assembly.GetTypes() to give these 2 objects in alphanumerical order, otherwise this can fail.
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SessionTest1 : MySessionComponentBase
    {
        public static SessionTest1 Instance;
        public IMyModContext OriginalContext;

        // Each session comp is instanced then their .ModContext is assigned which means any changes here would be lost.
        // However if you can get a second object right after this one (which is what SessionTest2 is intended for),
        //   then you can change ModContext in the very short window available (at MySession.RegisterComponentsFromAssemblies's end).
        public SessionTest1()
        {
            Instance = this;
        }

        public override void Draw()
        {
            if(!MyParticlesManager.Paused) // doing notifications when game is paused will glitch/break them
                MyAPIGateway.Utilities.ShowNotification($"{GetType().Name} :: threadId={Environment.CurrentManagedThreadId}", 16);
        }

        public override void LoadData()
        {
            Instance = null;
            ModContext = OriginalContext; // play nice and set it back ASAP
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SessionTest2 : MySessionComponentBase
    {
        public SessionTest2()
        {
            SessionTest1 st1 = SessionTest1.Instance;
            if(st1 != null)
            {
                st1.OriginalContext = st1.ModContext;
                st1.ModContext = null;
            }
        }
    }
}