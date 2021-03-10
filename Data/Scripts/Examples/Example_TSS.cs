using System;
using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    // Text Surface Scripts (TSS) can be selected in any LCD's scripts list.
    // These are meant as fast no-sync (sprites are not sent over network) display scripts, and the Run() method only executes player-side (no DS).
    // You can still use a session comp and access it through this to use for caches/shared data/etc.
    //
    // The display name has localization support aswell, same as a block's DisplayName in SBC.
    [MyTextSurfaceScript("InternalNameHere", "Display Name Here")]
    public class SomeClassName : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10; // frequency that Run() is called.

        private readonly IMyTerminalBlock TerminalBlock;

        public SomeClassName(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            TerminalBlock = (IMyTerminalBlock)block; // internal stored m_block is the ingame interface which has no events, so can't unhook later on, therefore this field is required.
            TerminalBlock.OnMarkForClose += BlockMarkedForClose; // required if you're gonna make use of Dispose() as it won't get called when block is removed or grid is cut/unloaded.

            // Called when script is created.
            // This class is instanced per LCD that uses it, which means the same block can have multiple instances of this script aswell (e.g. a cockpit with all its screens set to use this script).
        }

        public override void Dispose()
        {
            base.Dispose(); // do not remove
            TerminalBlock.OnMarkForClose -= BlockMarkedForClose;

            // Called when script is removed for any reason, so that you can clean up stuff if you need to.
        }

        void BlockMarkedForClose(IMyEntity ent)
        {
            Dispose();
        }

        // gets called at the rate specified by NeedsUpdate
        // it can't run every tick because the LCD is capped at 6fps anyway.
        public override void Run()
        {
            try
            {
                base.Run(); // do not remove

                // hold L key to see how the error is shown, remove this after you've played around with it =)
                if(MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.L))
                    throw new Exception("Oh noes an error :}");

                Draw();
            }
            catch(Exception e) // no reason to crash the entire game just for an LCD script, but do NOT ignore them either, nag user so they report it :}
            {
                DrawError(e);
            }
        }

        void Draw() // this is a custom method which is called in Run().
        {
            Vector2 screenSize = Surface.SurfaceSize;
            Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

            var frame = Surface.DrawFrame();

            // Drawing sprites works exactly like in PB API.
            // Therefore this guide applies: https://github.com/malware-dev/MDK-SE/wiki/Text-Panels-and-Drawing-Sprites

            // there are also some helper methods from the MyTSSCommon that this extends.
            // like: AddBackground(frame, Surface.ScriptBackgroundColor); - a grid-textured background

            // the colors in the terminal are Surface.ScriptBackgroundColor and Surface.ScriptForegroundColor, the other ones without Script in name are for text/image mode.

            var text = MySprite.CreateText("Hi!", "Monospace", Surface.ScriptForegroundColor, 1f, TextAlignment.LEFT);
            text.Position = screenCorner + new Vector2(16, 16); // 16px from topleft corner of the visible surface
            frame.Add(text);

            // add more sprites and stuff

            frame.Dispose(); // send sprites to the screen
        }

        void DrawError(Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

            try // first try printing the error on the LCD
            {
                Vector2 screenSize = Surface.SurfaceSize;
                Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

                var frame = Surface.DrawFrame();

                var bg = new MySprite(SpriteType.TEXTURE, "SquareSimple", null, null, Color.Black);
                frame.Add(bg);

                var text = MySprite.CreateText($"ERROR: {e.Message}\n{e.StackTrace}\n\nPlease send screenshot of this to mod author.\n{MyAPIGateway.Utilities.GamePaths.ModScopeName}", "White", Color.Red, 0.7f, TextAlignment.LEFT);
                text.Position = screenCorner + new Vector2(16, 16);
                frame.Add(text);

                frame.Dispose();
            }
            catch(Exception e2)
            {
                MyLog.Default.WriteLineAndConsole($"Also failed to draw error on screen: {e2.Message}\n{e2.StackTrace}");

                if(MyAPIGateway.Session?.Player != null)
                    MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
            }
        }
    }
}