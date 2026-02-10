using System;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame.Utilities; // this ingame namespace is safe to use in mods as it has nothing to collide with
using VRage.Utils;

namespace Digi.Examples
{
    // This example is minimal code required for a server-side per-world config to work.

    // The gist of it is: ini file is loaded/created (in <WorldFolder>/Storage/<ModId>.sbm_<ScriptsFolderName>/Config.ini specifically) that admin can edit,
    //   then SetVariable is used to store that data in sandbox.sbc which gets automatically sent to joining clients.
    // The reason for SetVariable() is that clients will reliably get the config data as they join which will be available during LoadData() for you to use.
    // Users should be editing that Config.ini, not the sandbox.sbc!

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

        /// <summary>
        /// Only for informing humans!
        /// </summary>
        string ConfigRelativePath = $@"<World>\Storage\{MyAPIGateway.Utilities.GamePaths.ModScopeName}\{FileName}";

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
            #region HACK: Fix for files created in game's CustomWorlds folder when world is created with this mod present.
            // HACK fix for bug: https://support.keenswh.com/spaceengineers/pc/topic/47762-modapi-write-to-world-storage-can-write-to-game-folder
            string savePath = MyAPIGateway.Session?.CurrentPath;
            string gamePath = MyAPIGateway.Utilities?.GamePaths?.ContentPath;
            if(savePath == null || gamePath == null || savePath.StartsWith(MyAPIGateway.Utilities.GamePaths.ContentPath))
            {
                Log("Delaying world config loading/creating because of world creation bugs...");
                MyAPIGateway.Utilities.InvokeOnGameThread(LoadOnHost);
                return;
            }
            #endregion

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
            string configText = iniParser.ToString();
            string configTextForSBC = ";This is not supposed to be edited here in sandbox.sbc! Edit " + ConfigRelativePath + " instead.\n" + configText;

            // sanity tests, can comment them out after ensuring they're fine
            CheckIni(iniParser, configText, nameof(configText));
            CheckIni(iniParser, configTextForSBC, nameof(configTextForSBC));

            MyAPIGateway.Utilities.SetVariable<string>(VariableId, configTextForSBC);

            using(TextWriter file = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(ExampleSettings)))
            {
                file.Write(configText);
            }

            // as seen inside *InWorldStorage() methods.
            Log("World config created/updated: " + ConfigRelativePath);
        }

        void CheckIni(MyIni iniParser, string ini, string nameForLog)
        {
            iniParser.Clear();

            MyIniParseResult result;
            if(!iniParser.TryParse(ini, out result))
                throw new Exception($"Failed to parse {nameForLog}! result: {result}; ini data:\n{ini}");

            Log($"Checking {nameForLog} ini data... all good!");
        }

        void LoadOnClient()
        {
            // Note: if you add this on an existing mod, it will throw this error for players that join servers which haven't been restarted yet,
            //       because players will have the latest mod while server does not, therefore the received sandbox data will not have the config data.
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