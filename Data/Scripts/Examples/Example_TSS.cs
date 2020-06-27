using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace Digi.Examples
{
    [MyTextSurfaceScript("InternalNameHere", "Public Name Here")]
    public class SomeClassName : MyTSSCommon
    {
        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10; // frequency that Run() is called.

        public SomeClassName(IMyTextSurface surface, IMyCubeBlock block, Vector2 size) : base(surface, block, size)
        {
            // initialization
        }

        public override void Dispose()
        {
            base.Dispose(); // do not remove

            // when script is removed, clean up stuff
        }

        public override void Run()
        {
            base.Run(); // do not remove

            using(var frame = Surface.DrawFrame())
            {
                Vector2 screenSize = Surface.SurfaceSize;
                Vector2 screenCorner = (Surface.TextureSize - screenSize) * 0.5f;

                // Drawing sprites works exactly like in PB API.
                // Therefore this guide applies: https://github.com/malware-dev/MDK-SE/wiki/Text-Panels-and-Drawing-Sprites

                var text = MySprite.CreateText("Hi!", "Monospace", Surface.ScriptForegroundColor, 1f, TextAlignment.LEFT);
                text.Position = screenCorner + new Vector2(16, 16); // 16px from topleft corner of the visible surface
                frame.Add(text);

                // add more sprites and stuff
            }
        }
    }
}