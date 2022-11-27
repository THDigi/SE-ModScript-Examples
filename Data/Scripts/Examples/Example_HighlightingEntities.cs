using Sandbox.Game;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Example_HighlightingEntities : MySessionComponentBase
    {
        IMyEntity CurrentlyHighlighted;

        public override void UpdateAfterSimulation()
        {
            // The highlight stuff are in MyVisualScriptLogicProvider (VSLP)
            // There's many highlight methods but they're designed for serverside scenario stuff so they're synchronized.
            // It is recommended to use the Local one unless you know you absolutely need the synchronized one.
            //  MyVisualScriptLogicProvider.SetHighlightLocal()

            // All methods in VSLP (MyVisualScriptLogicProvider) require entity names as input, that one is simply Name prop from [I]MyEntity
            //  which is automatically assigned with entityId.ToString(), but can be custom names in some cases too.

            // Thickness greatly affects how bright it gets, but you can also multiply the color to make it less bright aswell if you want to mix&match.
            // To remove highlight, set thickness to -1 or lower.
            // To disable pulsing, set pulseTimeInFrames to 0 or negative.
            // Leave the playerId as -1 if you use the local method.

            // Now to a practical example, pressing R while holding a welder/grinder and aiming at a block, will highlight it and will remember that.
            // There is unfortunately no way to get an entity's highlighted state.
            if(MyAPIGateway.Input.IsNewKeyPressed(MyKeys.R))
            {
                if(CurrentlyHighlighted != null)
                {
                    MyVisualScriptLogicProvider.SetHighlightLocal(CurrentlyHighlighted.Name, thickness: -1);

                    MyAPIGateway.Utilities.ShowNotification("Highlight unset.");

                    CurrentlyHighlighted = null;
                }
                else
                {
                    IMyCharacter chr = MyAPIGateway.Session?.Player?.Character;
                    IMySlimBlock aimed = chr?.EquippedTool?.Components?.Get<MyCasterComponent>()?.HitBlock as IMySlimBlock;
                    if(aimed?.FatBlock != null)
                    {
                        CurrentlyHighlighted = aimed.FatBlock; // slimblocks aren't entities, so deformable armor won't be highlightable this way.
                                                               // you can instead highlight the entire grid by giving the grid name.

                        MyVisualScriptLogicProvider.SetHighlightLocal(CurrentlyHighlighted.Name, thickness: 2, pulseTimeInFrames: 6, color: Color.SkyBlue);

                        MyAPIGateway.Utilities.ShowNotification("Highlight set. Press again to unset.");
                    }
                }
            }
        }
    }
}