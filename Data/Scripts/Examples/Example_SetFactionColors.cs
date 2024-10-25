using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_SetFactionColors : MySessionComponentBase
    {
        public override void BeforeStart()
        {
            // example usage, first is faction tag, second is faction color in RGB, third is icon color in RGB
            // SetFactionColor("SPRT", new Color(255,0,255), new Color(0,0,255));
        }

        void SetFactionColor(string tag, Color factionColor, Color iconColor)
        {
            var faction = MyAPIGateway.Session.Factions.TryGetFactionByTag(tag);
            if(faction == null)
            {
                MyLog.Default.WriteLine($"ERROR: Can't find faction by tag '{tag}' to change colors.");
                return;
            }

            MyAPIGateway.Session.Factions.EditFaction(faction.FactionId, faction.Tag, faction.Name, faction.Description, faction.PrivateInfo,
                faction.FactionIcon.ToString(), MyColorPickerConstants.HSVToHSVOffset(factionColor.ColorToHSV()), MyColorPickerConstants.HSVToHSVOffset(iconColor.ColorToHSV()));

            MyLog.Default.WriteLine($"Edited faction {faction.Name}'s faction color to {factionColor} and icon color to {iconColor}.");
        }
    }
}