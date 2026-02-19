using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Examples // change to have your name and the mod name, to be identifiable for crashes
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class AdditiveSBC_FactionType : MySessionComponentBase
    {
        public override void LoadData()
        {
            // This script allows you to add or remove items and grids from FactionType definitions.
            // Example usage below, change the subtype if needed, then call the methods shown inside.
            // Duplicate the entire chunk to edit more subtypes.

            using(var e = new EditFactionType(this, subtype: "Builder"))
            {
                // None of the below function are required, and each one can be called multiple times if needed.

                // the <TypeId> and <SubtypeId> from the item, separated by /
                e.OffersList_Add("ConsumableItem/RadiationKit");
                e.OffersList_Remove("Datapad/Datapad");
                e.OffersList_Remove("Component/ZoneChip");

                //e.OrdersList_Add("...");
                //e.OrdersList_Remove("...");

                // these take prefab SubtypeId from inside the file (not file name)
                e.GridsForSale_Remove("Cargo Shuttle");
                //e.GridsForSale_Add("...");
            }

            //...
        }

        // do not edit below this point unless you absolutely understand what you are doing

        protected override void UnloadData()
        {
            // required for the changes to not persist into other loaded worlds
            foreach(EditFactionType edit in Edits)
            {
                edit.Undo();
            }
        }

        List<EditFactionType> Edits = new List<EditFactionType>();

        class EditFactionType : IDisposable
        {
            readonly MyModContext ModContext;
            readonly MyFactionTypeDefinition Def;

            readonly SerializableDefinitionId[] SellOriginal;
            readonly List<SerializableDefinitionId> Sell;

            readonly SerializableDefinitionId[] BuyOriginal;
            readonly List<SerializableDefinitionId> Buy;

            readonly string[] SellGridsOriginal;
            readonly List<string> SellGrids;

            public EditFactionType(AdditiveSBC_FactionType session, string subtype)
            {
                ModContext = (MyModContext)session.ModContext;

                if(!MyDefinitionManager.Static.TryGetDefinition<MyFactionTypeDefinition>(MyStringHash.GetOrCompute(subtype), out Def))
                {
                    Log($"Couldn't find FactionType definition with subtype: {subtype}", TErrorSeverity.Error);
                    Def = null;
                    return;
                }

                SellOriginal = Def.OffersList;
                Sell = Def.OffersList?.ToList() ?? new List<SerializableDefinitionId>();

                BuyOriginal = Def.OrdersList;
                Buy = Def.OrdersList?.ToList() ?? new List<SerializableDefinitionId>();

                SellGridsOriginal = Def.GridsForSale;
                SellGrids = Def.GridsForSale?.ToList() ?? new List<string>();

                session.Edits.Add(this);
            }

            public void OrdersList_Add(string itemId) => ModifyItems(itemId, buy: true, remove: false);
            public void OffersList_Add(string itemId) => ModifyItems(itemId, buy: false, remove: false);
            public void OrdersList_Remove(string itemId) => ModifyItems(itemId, buy: true, remove: true);
            public void OffersList_Remove(string itemId) => ModifyItems(itemId, buy: false, remove: true);

            void ModifyItems(string itemId, bool buy, bool remove)
            {
                if(Def == null)
                    return;

                MyDefinitionId id;
                if(!MyDefinitionId.TryParse(itemId, out id))
                {
                    Log($"Item's TypeId is invalid: {itemId}", TErrorSeverity.Error);
                    return;
                }

                {
                    MyPhysicalItemDefinition itemDef;
                    if(!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(id, out itemDef))
                    {
                        Log($"Item does not exist in definitions: {id} - {(remove ? "Removing" : "Adding")} anyway.", TErrorSeverity.Error);
                        //return;
                    }
                }

                var list = buy ? Buy : Sell;
                if(remove)
                {
                    if(!list.Remove(id))
                    {
                        Log($"Failed to remove item from the {(buy ? "Orders" : "Offers")}List because it's not in the list: {id}", TErrorSeverity.Warning);
                    }
                }
                else
                {
                    if(list.Contains(id))
                        Log($"Item already exists in the {(buy ? "Orders" : "Offers")}List, added duplicate: {id}", TErrorSeverity.Warning);

                    list.Add(id);
                }
            }

            public void GridsForSale_Add(string prefabId) => ModifyShips(prefabId, remove: false);
            public void GridsForSale_Remove(string prefabId) => ModifyShips(prefabId, remove: true);

            void ModifyShips(string prefabId, bool remove)
            {
                {
                    MyPrefabDefinition prefabDef;
                    if(!MyDefinitionManager.Static.GetPrefabDefinitions().TryGetValue(prefabId, out prefabDef))
                    {
                        Log($"Prefab does not exist in definitions: {prefabId} - {(remove ? "Removing" : "Adding")} anyway.", TErrorSeverity.Error);
                        //return;
                    }
                }

                if(remove)
                {
                    if(!SellGrids.Remove(prefabId))
                        Log($"Failed to remove prefab from the GridsForSale list because it's not in the list: {prefabId}", TErrorSeverity.Warning);
                }
                else
                {
                    if(SellGrids.Contains(prefabId))
                        Log($"Prefab already exists in the GridsForSale list, added duplicate: {prefabId}", TErrorSeverity.Warning);

                    SellGrids.Add(prefabId);
                }
            }

            public void Undo()
            {
                Def.OffersList = SellOriginal;
                Def.OrdersList = BuyOriginal;
                Def.GridsForSale = SellGridsOriginal;
            }

            public void Dispose() // called by the using() statement when it exits its code block.
            {
                if(Def == null)
                    return;

                Def.OffersList = Sell.Count == 0 ? null : Sell.ToArray();
                Def.OrdersList = Buy.Count == 0 ? null : Buy.ToArray();
                Def.GridsForSale = SellGrids.Count == 0 ? null : SellGrids.ToArray();

                Def.Postprocess(); // it's empty right now but might not be in the future

                Log("Finished modifying", TErrorSeverity.Notice);
            }

            void Log(string message, TErrorSeverity severity = TErrorSeverity.Error)
            {
                if(Def != null)
                    message = $"(FactionType definition: {Def.Id.SubtypeName}) {message}";

                if(severity == TErrorSeverity.Notice)
                    MyLog.Default.WriteLine($"{ModContext.ModName} - Info: {message}");
                else
                    MyDefinitionErrors.Add(ModContext, message, severity, writeToLog: true);
            }
        }
    }
}
