using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Experiments
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_ModifyBlockVariants : MySessionComponentBase
    {
        // if you expect blocks or groups to be missing then set this to false to not report them in log.
        const bool ReportMissingDefinitions = true;

        const bool ReportInF11Menu = true;

        void SetupGroups()
        {
            // A few notes about groups:
            // - group has to exist with SBC, C# mods cannot add definitions in general.
            // - has to have at least one valid block or game will remove the group.
            // - blocks can only link to a single group, ideally don't use blocks that are already in a group.


            // usage example:

            using(var group = new GroupChange("CockpitGroup"))
            {
                group.AddBlock("Assembler", "LargeAssembler");
                group.AddBlock("Assembler", "LargeAssemblerIndustrial");
                // etc...

                group.RemoveBlock("Cockpit", "DBSmallBlockFighterCockpit");
                group.RemoveBlock("Cockpit", "SmallBlockStandingCockpit");
                group.RemoveBlock("Cockpit", "LargeBlockStandingCockpit");
                // etc...
            }

            // then repeat the above chunk all groups you want to edit
        }




        static Example_ModifyBlockVariants Instance;
        List<MyCubeBlockDefinition> NewDefs = new List<MyCubeBlockDefinition>(32);
        List<MyDefinitionId> AppendBlocks = new List<MyDefinitionId>(32);
        HashSet<MyDefinitionId> RemoveBlocks = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);
        StringBuilder SB = new StringBuilder(1024);

        public override void LoadData()
        {
            try
            {
                Instance = this;
                SetupGroups();
            }
            finally
            {
                AppendBlocks = null;
                RemoveBlocks = null;
                NewDefs = null;
                Instance = null;
                SB = null;
            }
        }

        struct GroupChange : IDisposable
        {
            readonly string VariantsId;

            public GroupChange(string variantsId)
            {
                VariantsId = variantsId;

                Instance.AppendBlocks?.Clear();
                Instance.RemoveBlocks?.Clear();
            }

            public void AddBlock(string typeName, string subtypeName)
            {
                MyObjectBuilderType type;
                if(ValidateType(typeName, out type))
                    Instance.AppendBlocks.Add(new MyDefinitionId(type, subtypeName));
            }

            public void RemoveBlock(string typeName, string subtypeName)
            {
                MyObjectBuilderType type;
                if(ValidateType(typeName, out type))
                    Instance.RemoveBlocks.Add(new MyDefinitionId(type, subtypeName));
            }

            static bool ValidateType(string typeName, out MyObjectBuilderType type)
            {
                if(!MyObjectBuilderType.TryParse(typeName, out type))
                {
                    // not ignoring this one because mods cannot add block types.
                    LogError($"Type '{typeName}' does not exist, you must use a block type that exists in the game.");
                    return false;
                }

                return true;
            }

            public void Dispose()
            {
                if(Instance.RemoveBlocks == null || Instance.AppendBlocks == null || Instance.NewDefs == null)
                {
                    LogError($"Cannot modify `{VariantsId}`, script is already finished setup!");
                    return;
                }

                MyBlockVariantGroup group;
                if(!MyDefinitionManager.Static.GetBlockVariantGroupDefinitions().TryGetValue(VariantsId, out group))
                {
                    if(ReportMissingDefinitions)
                        LogError($"Cannot find BlockVariantsGroup subtypeId: `{VariantsId}`");

                    return;
                }

                Instance.NewDefs.Clear();

                foreach(MyCubeBlockDefinition blockDef in group.Blocks)
                {
                    if(Instance.RemoveBlocks.Contains(blockDef.Id))
                    {
                        Instance.RemoveBlocks.Remove(blockDef.Id);

                        // detach block from group properly
                        blockDef.BlockStages = null;
                        blockDef.BlockVariantsGroup = null;
                        blockDef.GuiVisible = true; // this was assigned by the group so we have to reset it

                        continue;
                    }

                    Instance.NewDefs.Add(blockDef);
                }

                if(Instance.RemoveBlocks.Count > 0)
                {
                    foreach(MyDefinitionId blockId in Instance.RemoveBlocks)
                    {
                        if(ReportMissingDefinitions)
                            LogError($"Cannot find block id to remove: `{blockId}`");
                    }
                }

                foreach(MyDefinitionId blockId in Instance.AppendBlocks)
                {
                    MyCubeBlockDefinition blockDef = MyDefinitionManager.Static.GetCubeBlockDefinition(blockId);
                    if(blockDef == null)
                    {
                        if(ReportMissingDefinitions)
                            LogError($"Cannot find block id to add: `{blockId}`");

                        continue;
                    }

                    Instance.NewDefs.Add(blockDef);
                }

                group.Context = (MyModContext)Instance.ModContext; // mark it modified by this mod.

                group.Blocks = Instance.NewDefs.ToArray();

                group.DisplayNameEnum = null;
                group.Icons = null;
                group.Postprocess();

                // not handled by Postprocess() but by MyDefinitionManager.InitBlockGroups()
                foreach(MyCubeBlockDefinition blockDef in group.Blocks)
                {
                    blockDef.BlockVariantsGroup = group;
                }

                StringBuilder sb = Instance.SB.Clear().Append("Modified block variants group '").Append(group.Id.SubtypeName).Append("', final blocks:");
                foreach(MyCubeBlockDefinition blockDef in group.Blocks)
                {
                    sb.Append("\n  ").Append(blockDef.Id.ToString());

                    if(blockDef == group.PrimaryGUIBlock)
                        sb.Append("   (Primary GUI block)");
                }
                LogInfo(sb.ToString());

                Instance.AppendBlocks.Clear();
                Instance.RemoveBlocks.Clear();
            }
        }

        static void LogError(string message)
        {
            if(ReportInF11Menu)
                MyDefinitionErrors.Add((MyModContext)Instance.ModContext, message, TErrorSeverity.Error, writeToLog: true);
            else
                MyLog.Default.WriteLineAndConsole($"Mod '{Instance.ModContext.ModName}' Error: {message}");
        }

        static void LogInfo(string message)
        {
            //if(ReportInF11Menu)
            //    MyDefinitionErrors.Add((MyModContext)Instance.ModContext, message, TErrorSeverity.Notice, writeToLog: true);
            //else
            MyLog.Default.WriteLineAndConsole($"Mod '{Instance.ModContext.ModName}': {message}");
        }
    }
}
