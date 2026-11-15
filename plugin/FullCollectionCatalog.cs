#if FULL_COLLECTION
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using PegasusShared;
using EngineCard = SabberStoneCore.Model.Card;
using EngineCards = SabberStoneCore.Model.Cards;

internal static class FullCollectionCatalog
{
    internal static bool Ready { get; private set; }
    internal static readonly Dictionary<string, EngineCard> Cards = new Dictionary<string, EngineCard>(StringComparer.Ordinal);
    internal static readonly Dictionary<string, int[]> Premiums = new Dictionary<string, int[]>(StringComparer.Ordinal);
    internal static readonly HashSet<string> Standard = new HashSet<string>(StringComparer.Ordinal);
    internal static readonly HashSet<int> Sets = new HashSet<int>();
    internal static bool Contains(string id) => id != null && Cards.ContainsKey(id);
    internal static int Count(string id, int premium) => Contains(id) && Premiums[id].Contains(premium) ? FullCollectionPolicy.Copies((int)Cards[id].Rarity) : 0;
    internal static IEnumerable<LocalCardCount> Owned => Cards.Keys.SelectMany(id => Premiums[id].Select(p => new LocalCardCount { Id=id, Premium=p, Count=Count(id,p), Acquired=Count(id,p) }));
    internal static bool ValidFormat(FormatType format) => format == FormatType.FT_STANDARD || format == FormatType.FT_WILD;
    internal static FormatType DeckFormat(long id) => (FormatType)(LocalLadder.Profile?.GetDraft(id)?.CollectionFormat ?? 2);

    internal static void Initialize(string reports, BepInEx.Logging.ManualLogSource log)
    {
        if (Ready) return;
        var definitions = DefLoader.Get().GetAllEntityDefs();
        var inventory = new List<string> { "id\tdbfId\tname\tsetId\tset\tstandard\tversions" };
        var excluded = new List<string> { "id\tset\ttype\tcollectible" };
        foreach (var def in definitions.Values.OrderBy(d => d.GetCardId(), StringComparer.Ordinal))
        {
            string id = def.GetCardId();
            var tags = new Dictionary<int,int>(def.GetTags().GetMap());
            // Client 37 resolves series from CardSetTiming; CARD_SET is absent on native definitions.
            // A clone avoids SoloStone's canonical-definition preview patches.
            int set = FullCollectionPolicy.ResolveSet(tags.TryGetValue(183, out var s) ? s : 0,
                (int)def.Clone().GetCardSet());
            tags[183] = set;
            int type = tags.TryGetValue(202, out var t) ? t : 0;
            bool collectible = tags.TryGetValue(321, out var c) && c == 1;
            if (!FullCollectionPolicy.Eligible(set,type,collectible)) {
                if (collectible) excluded.Add(id+"\t"+set+"\t"+type+"\t1");
                continue;
            }
            int dbid = GameUtils.TranslateCardIdToDbId(id);
            if (dbid <= 0) throw new InvalidOperationException("Collectible has no native DBF identity: " + id);
            var card = EngineCards.RegisterCollectionDefinition(id, dbid, def.GetName(), tags);
            Cards.Add(id,card); Sets.Add(set);
            Premiums.Add(id,FullCollectionPolicy.Versions(tags.TryGetValue(1932,out var diamond) && diamond > 0,
                tags.TryGetValue(2589,out var signature) && signature > 0));
            bool standard = FullCollectionPolicy.StandardSet(set) || set == 1941 && new[]{"EDR_","TLC_","TIME_","CATA_","JAIL_","BE_"}.Any(prefix=>id.StartsWith(prefix,StringComparison.Ordinal));
            if (standard) Standard.Add(id);
            inventory.Add(id+"\t"+dbid+"\t"+def.GetName().Replace('\t',' ')+"\t"+set+"\t"+(TAG_CARD_SET)set+"\t"+standard+"\t"+string.Join(",",Premiums[id]));
        }
        Directory.CreateDirectory(reports);
        File.WriteAllLines(Path.Combine(reports,"full-collection-inventory.tsv"),inventory);
        File.WriteAllLines(Path.Combine(reports,"full-collection-excluded.tsv"),excluded);
        log.LogInfo("FULL_COLLECTION scanned definitions="+definitions.Count+" admitted="+Cards.Count+" excludedCollectibles="+(excluded.Count-1));
        if (Cards.Count < 4000 || !Cards.ContainsKey("FP1_030") || !Cards.ContainsKey("GVG_110") || !Cards.ContainsKey("BE_036"))
            throw new InvalidOperationException("Full collection inventory is incomplete: " + Cards.Count);
        Ready = true;
        var harmony = new Harmony("solostone.fullcollection.formats");
        Patch(harmony,"IsStandardCardSet",new[]{typeof(TAG_CARD_SET)},nameof(StandardSet));
        Patch(harmony,"IsWildCardSet",new[]{typeof(TAG_CARD_SET)},nameof(WildSet));
        Patch(harmony,"GetStandardSets",Type.EmptyTypes,nameof(StandardSets));
        Patch(harmony,"GetAllWildPlayableSets",Type.EmptyTypes,nameof(WildSets));
        Patch(harmony,"GetWildSets",Type.EmptyTypes,nameof(WildOnlySets));
        Patch(harmony,"IsSetRotated",new[]{typeof(TAG_CARD_SET)},nameof(Rotated));
        Patch(harmony,"IsCardValidForFormat",new[]{typeof(FormatType),typeof(EntityDef)},nameof(ValidCard));
        Patch(harmony,"IsStandardCard",new[]{typeof(EntityDef)},nameof(StandardCard));
        Patch(harmony,"IsCardSetFilterEventActive",new[]{typeof(string)},nameof(FilterActive));
        Patch(harmony, nameof(GameUtils.IsCardGameplayEventActive), new[] { typeof(string) }, nameof(CollectionCardReleased));
        Patch(harmony, nameof(GameUtils.IsCardGameplayEventActive), new[] { typeof(EntityDef) }, nameof(CollectionDefinitionReleased));
        int released = Cards.Keys.Count(GameUtils.IsCardGameplayEventActive);
        log.LogInfo("FULL_COLLECTION release check " + released + "/" + Cards.Count);
        File.WriteAllLines(Path.Combine(reports,"full-collection-summary.txt"),new[]{
            "client="+TelemetryManager.ProgramVersion,"cards="+Cards.Count,"standard="+Standard.Count,
            "wildIncludingStandard="+Cards.Count,"versions="+Premiums.Values.Sum(v=>v.Length),
            "diamond="+Premiums.Values.Count(v=>v.Contains(2)),"signature="+Premiums.Values.Count(v=>v.Contains(3))
        }.Concat(Cards.Values.GroupBy(c=>(int)c.Set).OrderBy(g=>g.Key).Select(g=>((TAG_CARD_SET)g.Key)+"="+g.Count())));
        log.LogInfo("FULL_COLLECTION ready cards="+Cards.Count+" standard="+Standard.Count+" versions="+Premiums.Values.Sum(v=>v.Length)+" client="+TelemetryManager.ProgramVersion);
    }
    static void Patch(Harmony h,string name,Type[] args,string prefix) => h.Patch(AccessTools.Method(typeof(GameUtils),name,args) ?? throw new MissingMethodException(name),prefix:new HarmonyMethod(typeof(FullCollectionCatalog),prefix));
    static bool StandardSet(TAG_CARD_SET __0,ref bool __result) { __result=FullCollectionPolicy.StandardSet((int)__0)|| (int)__0==1941; return false; }
    static bool WildSet(TAG_CARD_SET __0,ref bool __result) { __result=Sets.Contains((int)__0)&&!FullCollectionPolicy.StandardSet((int)__0); return false; }
    static bool StandardSets(ref TAG_CARD_SET[] __result) { __result=Sets.Where(s=>FullCollectionPolicy.StandardSet(s)||s==1941).Select(s=>(TAG_CARD_SET)s).ToArray(); return false; }
    static bool WildSets(ref TAG_CARD_SET[] __result) { __result=Sets.Select(s=>(TAG_CARD_SET)s).ToArray(); return false; }
    static bool WildOnlySets(ref TAG_CARD_SET[] __result) { __result=Sets.Where(s=>!FullCollectionPolicy.StandardSet(s)).Select(s=>(TAG_CARD_SET)s).ToArray(); return false; }
    static bool Rotated(TAG_CARD_SET __0,ref bool __result) { __result=Sets.Contains((int)__0)&&!FullCollectionPolicy.StandardSet((int)__0)&&(int)__0!=1941; return false; }
    static bool StandardCard(EntityDef __0,ref bool __result) { if(__0==null||!Contains(__0.GetCardId()))return true; __result=Standard.Contains(__0.GetCardId());return false; }
    static bool ValidCard(FormatType __0,EntityDef __1,ref bool __result) { if(__1==null||!Contains(__1.GetCardId())||!ValidFormat(__0))return true; __result=__0==FormatType.FT_WILD||Standard.Contains(__1.GetCardId());return false; }
    static bool FilterActive(string __0,ref bool __result) { if(!Contains(__0))return true;__result=true;return false; }

    // Offline has no Battle.net launch event schedule. Only the imported collection is granted release status.
    private static bool CollectionCardReleased(string __0, ref bool __result)
    {
        if (!Ready || !Contains(__0)) return true;
        __result = true;
        return false;
    }

    private static bool CollectionDefinitionReleased(EntityDef __0, ref bool __result) =>
        CollectionCardReleased(__0?.GetCardId(), ref __result);
}
#endif
