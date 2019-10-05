using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Utils;

namespace Digi.AttachedLights
{
    public static class SimpleLog
    {
        public static void Error(object caller, Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"{e.Message}\n{e.StackTrace}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {caller.GetType().FullName}: {e.Message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }

        public static void Error(object caller, string message)
        {
            MyLog.Default.WriteLineAndConsole($"ERROR {caller.GetType().FullName}: {message}");

            if(MyAPIGateway.Session?.Player != null)
                MyAPIGateway.Utilities.ShowNotification($"[ ERROR: {caller.GetType().FullName}: {message} | Send SpaceEngineers.Log to mod author ]", 10000, MyFontEnum.Red);
        }
    }
}
