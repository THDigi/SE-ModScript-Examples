using System;
using System.Text;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_InputReading : MySessionComponentBase
    {
        public override void HandleInput()
        {
            // you can read inputs in update methods too, or pretty much anywhere.
            // this method however only runs on players (not DS-side) and runs even if game is paused.


            if(MyParticlesManager.Paused)
                return; // stop here if game is paused, optional, depends on what you're doing.


            // the majority of input reading is in: MyAPIGateway.Input

            // dividing it up into methods for different levels/usecases of examples:

            BasicExamples();

            GamepadInclusiveExamples();
        }




        void BasicExamples()
        {
            // these input methods will work regardless of GUI focus, this is the simplest way to ignore if player is in any menu or chat.
            if(!MyAPIGateway.Gui.IsCursorVisible && !MyAPIGateway.Gui.ChatEntryVisible)
            {
                // example of detecting when current player presses the USE bind on keyboard or mouse, does not react to gamepad binds.
                if(MyAPIGateway.Input.IsNewGameControlPressed(MyControlsSpace.USE))
                {
                    MyAPIGateway.Utilities.ShowNotification("You pressed USE [:o]");
                }
                // also the "New" in the method names means it will only return true in the frame that it started to be pressed (or released).
                //   useful for a single action instead of it repeating while player holds it (which they will even as they tap it, it is inevitable).

                if(MyAPIGateway.Input.IsGameControlPressed(MyControlsSpace.JUMP))
                {
                    MyAPIGateway.Utilities.ShowNotification("You're holding jump...", 17);
                }
            }
        }





        // copied from MyControllerHelper because it is not whitelisted
        // because gamepad is limited in buttons it has to be split up into contexts, these are those.
        MyStringId CX_BASE = MyStringId.GetOrCompute("BASE");
        MyStringId CX_GUI = MyStringId.GetOrCompute("GUI");
        MyStringId CX_CHARACTER = MyStringId.GetOrCompute("CHARACTER");
        MyStringId CX_SPACESHIP = MyStringId.GetOrCompute("SPACESHIP");
        MyStringId CX_JETPACK = MyStringId.GetOrCompute("JETPACK");

        void GamepadInclusiveExamples()
        {
            if(MyAPIGateway.Input.IsControl(CX_CHARACTER, MyControlsSpace.USE, MyControlStateType.NEW_PRESSED))
            {
                MyAPIGateway.Utilities.ShowNotification("You pressed USE (+gamepad support)");
            }
        }
    }
}