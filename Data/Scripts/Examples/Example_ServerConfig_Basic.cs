using System;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities; // this ingame namespace is safe to use in mods as it has nothing to collide with
using VRage.Utils;

namespace Digi.Examples
{
    // This example is minimal code required for it to work and with comments so you can better understand what is going on.

    // The gist of it is: ini file is loaded/created that admin can edit, SetVariable is used to store that data in sandbox.sbc which gets automatically sent to joining clients.
    // Benefit of this is clients will be getting this data before they join, very good if you need it during LoadData()
    // This example does not support reloading config while server runs, you can however implement that by sending a packet to all online players with the ini data for them to parse.

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_ServerConfig_Basic : MySessionComponentBase
    {
        ExampleSettings Settings = new ExampleSettings();

        public override void LoadData()
        {
            Settings.Load(ModContext);

            // example usage/debug
            MyLog.Default.WriteLineAndConsole($"### DEBUG {ModContext.ModName} :: SomeNumber value={Settings.SomeNumber}");
            MyLog.Default.WriteLineAndConsole($"### DEBUG {ModContext.ModName} :: ToggleThings value={Settings.ToggleThings}");
        }
    }

    public class ExampleSettings
    {
        const string VariableId = nameof(Example_ServerConfig_Basic); // IMPORTANT: must be unique as it gets written in a shared space (sandbox.sbc)
        const string FileName = "Config.ini"; // the file that gets saved to world storage under your mod's folder
        const string IniSection = "Config";

        // settings you'd be reading, and their defaults.
        public float SomeNumber = 1f;
        public bool ToggleThings = true;

        IMyModContext Mod;

        void LoadConfig(MyIni iniParser)
        {
            // repeat for each setting field
            SomeNumber = iniParser.Get(IniSection, nameof(SomeNumber)).ToSingle(SomeNumber);

            ToggleThings = iniParser.Get(IniSection, nameof(ToggleThings)).ToBoolean(ToggleThings);
        }

        void SaveConfig(MyIni iniParser)
        {
            // repeat for each setting field
            iniParser.Set(IniSection, nameof(SomeNumber), SomeNumber);
            iniParser.SetComment(IniSection, nameof(SomeNumber), "This number does something for sure"); // optional

            iniParser.Set(IniSection, nameof(ToggleThings), ToggleThings);
        }

        // nothing to edit below this point

        public ExampleSettings()
        {
        }

        public void Load(IMyModContext mod)
        {
            Mod = mod;

            if(MyAPIGateway.Session.IsServer)
                LoadOnHost();
            else
                LoadOnClient();
        }

        void LoadOnHost()
        {
            // HACK: Fix for files created in game's CustomWorlds folder when world is created with this mod present.
            string savePath = MyAPIGateway.Session?.CurrentPath;
            string gamePath = MyAPIGateway.Utilities?.GamePaths?.ContentPath;
            if(savePath == null || gamePath == null || savePath.StartsWith(MyAPIGateway.Utilities.GamePaths.ContentPath))
            {
                Log("Delaying world config loading/creating because of world creation bugs...");
                MyAPIGateway.Utilities.InvokeOnGameThread(LoadOnHost);
                return;
            }

            MyIni iniParser = new MyIni();

            // load file if exists then save it regardless so that it can be sanitized and updated

            if(MyAPIGateway.Utilities.FileExistsInWorldStorage(FileName, typeof(ExampleSettings)))
            {
                using(TextReader file = MyAPIGateway.Utilities.ReadFileInWorldStorage(FileName, typeof(ExampleSettings)))
                {
                    string text = file.ReadToEnd();

                    MyIniParseResult result;
                    if(!iniParser.TryParse(text, out result))
                        throw new Exception($"Config error: {result.ToString()}");

                    LoadConfig(iniParser);
                    Log("World config loaded!");
                }
            }

            iniParser.Clear(); // remove any existing settings that might no longer exist

            SaveConfig(iniParser);

            string saveText = iniParser.ToString();

            MyAPIGateway.Utilities.SetVariable<string>(VariableId, saveText);

            using(TextWriter file = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(ExampleSettings)))
            {
                file.Write(saveText);
            }

            Log("World config created/updated.");
        }

        void LoadOnClient()
        {
            string text;
            if(!MyAPIGateway.Utilities.GetVariable<string>(VariableId, out text))
                throw new Exception("No config found in sandbox.sbc!");

            MyIni iniParser = new MyIni();
            MyIniParseResult result;
            if(!iniParser.TryParse(text, out result))
                throw new Exception($"Config error: {result.ToString()}");

            LoadConfig(iniParser);
            Log("World config loaded!");
        }

        void Log(string msg)
        {
            MyLog.Default.WriteLineAndConsole($"Mod {Mod.ModName}: {msg}");
        }
    }
}