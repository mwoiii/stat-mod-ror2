using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatsMod.CustomStats {
    internal class NonScrapPrinted : BaseCustomStat {
        private static Dictionary<string, uint> nonScrapPrintedDict = [];  // The amount of items used in printers and soups that were not scrap

        public override void Init() {
            base.Init();
            IL.RoR2.PurchaseInteraction.OnInteractionBegin += NonScrapTrack;
        }

        private static void NonScrapTrack(ILContext il) {
            ILCursor c = new ILCursor(il);
            int itemIndexLoc = 20;
            if (c.TryGotoNext(
                x => x.MatchLdloc(out itemIndexLoc),
                x => x.MatchLdfld<Inventory.ItemAndStackValues>(nameof(Inventory.ItemAndStackValues.itemIndex)),
                x => x.MatchLdloc(out _),
                x => x.MatchBeq(out var _)
                )) {

                c.Emit(OpCodes.Ldloc_0);
                c.Emit(OpCodes.Ldloc, itemIndexLoc);
                c.EmitDelegate<Action<CharacterBody, Inventory.ItemAndStackValues>>((interactorBody, item) => {
                    ItemDef itemDef = ItemCatalog.GetItemDef(item.itemIndex);
                    ItemDef[] itemBlacklist = {
                    RoR2Content.Items.ScrapWhite,
                    RoR2Content.Items.ScrapGreen,
                    RoR2Content.Items.ScrapRed,
                    RoR2Content.Items.ScrapYellow,
                    DLC1Content.Items.RegeneratingScrap
                };
                    if (!itemBlacklist.Contains(itemDef)) {
                        var player = interactorBody.master.playerCharacterMasterController;
                        string playerName = RecordHandler.masterControllerToName[player];
                        if (nonScrapPrintedDict.ContainsKey(playerName)) { nonScrapPrintedDict[playerName]++; } else { nonScrapPrintedDict.Add(playerName, 1); }
                    }
                });
            } else {
                Log.Error("NonScrapTrack IL hook borked.");
            }
            /*
            CreateItemTakenOrb(component.corePosition, base.gameObject, item);
            if (item.index != itemIndex) <-- matching
            {
                < delegate emitted here >
                PurchaseInteraction.onItemSpentOnPurchase?.Invoke(this, activator);
            }
            */
        }

        public override void ConfigureStatsTable() {
            CustomStatTracker.statsTable.Add("nonScrapPrinted", nonScrapPrintedDict);
        }

        public override void Deserialize(Dictionary<string, JToken> restored) {
            if (restored.CanDeserialize("nonScrapPrinted")) {
                nonScrapPrintedDict = restored["nonScrapPrinted"].ToObject<Dictionary<string, uint>>();
            }
        }
    }
}
