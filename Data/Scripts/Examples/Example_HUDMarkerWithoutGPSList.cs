using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    /// <summary>
    /// A way to have HUD markers that don't have a presence in the GPS list, clientside only.
    /// 
    /// WARNING: this is a hacky solution that abuses the exact behavior of these methods, also this was not tested in multiplayer.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Example_HUDMarkerWithoutGPSList : MySessionComponentBase
    {
        IMyGps ExampleMarker;

        public override void UpdateAfterSimulation()
        {
            Vector3D pos = MyAPIGateway.Session.Camera.Position + MyAPIGateway.Session.Camera.WorldMatrix.Forward * 3;
            int tick = MyAPIGateway.Session.GameplayFrameCounter;

            // add/remove toggle hotkey for trying it in-game
            if(MyAPIGateway.Input.IsNewKeyPressed(MyKeys.N))
            {
                if(ExampleMarker == null)
                {
                    ExampleMarker = CreateHudMarker();
                    ExampleMarker.Coords = pos;
                    ExampleMarker.Name = "OVER HERE!";
                }
                else
                {
                    RemoveHudMarker(ExampleMarker);
                    ExampleMarker = null;
                }
            }

            // to show that it can be updated in realtime without anything special needed
            if(ExampleMarker != null && tick % 60 == 0)
            {
                // this works too, it's just easier to see if it didn't move in-game
                //TestGPS.Coords = pos;

                ExampleMarker.GPSColor = Color.Lerp(Color.Red, Color.Blue, MyUtils.GetRandomFloat());

                if(tick % 120 == 0)
                {
                    ExampleMarker.Name = "Peeka";
                    ExampleMarker.ContainerRemainingTime = "";
                }
                else
                {
                    ExampleMarker.Name = "";
                    ExampleMarker.ContainerRemainingTime = "Boo";
                }
            }
        }

        static IMyGps CreateHudMarker()
        {
            IMyGps marker = MyAPIGateway.Session.GPS.Create(string.Empty, string.Empty, Vector3D.Zero, true, false);

            MyAPIGateway.Session.GPS.AddLocalGps(marker);

            // HACK: trick the game to not remove the HUD marker by making it think it's not visible
            marker.ShowOnHud = false;
            MyAPIGateway.Session.GPS.RemoveLocalGps(marker);

            return marker;
        }

        static void RemoveHudMarker(IMyGps marker)
        {
            MyAPIGateway.Session.GPS.AddLocalGps(marker);
            marker.ShowOnHud = true; // required for it to be removed for real
            MyAPIGateway.Session.GPS.RemoveLocalGps(marker);
        }
    }
}