using System;
using System.Linq;
class Program
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Main()
    {
        foreach (var set in new[]{2,3,4,11,12,13,14,15,20,21,23,25,27,1001,1004,1125,1127,1129,1130,1158,1347,1403,1414,1463,1443,1466,1525,1635,1637,1578,1626,1658,1691,1776,1869,1809,1858,1898,1892,1897,1905,1935,1946,1952,1957,1980,1988,1994,1941})
            Check(FullCollectionPolicy.Eligible(set, 4, true), "Missing traditional set " + set);
        foreach (var set in new[]{1,5,17,18,1453,1586,1646,1705,1904,1961})
            Check(!FullCollectionPolicy.Eligible(set, 4, true), "Nontraditional set admitted " + set);
        Check(!FullCollectionPolicy.Eligible(1994,4,false), "Token admitted");
        Check(!FullCollectionPolicy.Eligible(1994,10,true), "Hero power admitted");
        Check(FullCollectionPolicy.Copies(5)==1 && FullCollectionPolicy.Copies(4)==2,"Playset counts");
        Check(FullCollectionPolicy.Versions(false,false).SequenceEqual(new[]{0,1}),"Invented premium");
        Check(FullCollectionPolicy.Versions(true,true).SequenceEqual(new[]{0,1,2,3}),"Missing premium");
        Check(FullCollectionPolicy.StandardSet(1994) && FullCollectionPolicy.StandardSet(1637),"Current standard missing");
        Check(!FullCollectionPolicy.StandardSet(12) && !FullCollectionPolicy.StandardSet(1897),"Wild in standard");
        Check(FullCollectionPolicy.ResolveSet(0, 12) == 12, "Missing raw CARD_SET must use native timing-table resolution");
        Check(FullCollectionPolicy.ResolveSet(1994, 0) == 1994, "Explicit raw CARD_SET must survive");
        Check(FullCollectionPolicy.Eligible(FullCollectionPolicy.ResolveSet(0, 1994), 7, true), "BE_000-style native collectible was filtered out");
        var tags = new System.Collections.Generic.Dictionary<int,int> { {202,4}, {183,12}, {321,1}, {203,5}, {199,4}, {48,3} };
        var imported = SabberStoneCore.Model.Cards.RegisterCollectionDefinition("COLLECTION_TEST_001", 999999, "Collection Test", tags);
        Check(SabberStoneCore.Model.Cards.FromId("COLLECTION_TEST_001") == imported && imported.Cost == 3 && imported.Collectible, "Native metadata import lost");
        Check(!imported.Implemented, "Collection-only card advertised as battle implemented");
        var existing = SabberStoneCore.Model.Cards.FromId("FP1_030");
        Check(existing != null && existing.Implemented, "Expected an implemented legacy card fixture");
        var metadata = SabberStoneCore.Model.Cards.RegisterCollectionDefinition(existing.Id, existing.AssetId, "Native metadata", tags);
        Check(metadata.Name == "Native metadata", "Native collection metadata not returned");
        Check(ReferenceEquals(SabberStoneCore.Model.Cards.FromId(existing.Id), existing), "Existing implemented card was replaced, invalidating old drafts and recipes");
        Console.WriteLine("PASS: all traditional sets, nontraditional exclusions, tokens, playsets, variants, rotation");
    }
}
