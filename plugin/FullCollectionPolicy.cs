using System.Collections.Generic;

// Versioned to client 37 / 2026 rotation. Tokens and nontraditional modes are never collection entitlements.
internal static class FullCollectionPolicy
{
    internal static readonly HashSet<int> TraditionalSets = new HashSet<int> {
        2,3,4,11,12,13,14,15,20,21,23,25,27,1001,1004,1125,1127,1129,1130,1158,1347,1403,
        1414,1463,1443,1466,1525,1635,1637,1578,1626,1658,1691,1776,1869,1809,1858,1898,
        1892,1897,1905,1935,1946,1952,1957,1980,1988,1994,1941
    };
    internal static bool Eligible(int set, int type, bool collectible) {
        return collectible && TraditionalSets.Contains(set) && (type == 3 || type == 4 || type == 5 || type == 7 || type == 39);
    }
    internal static int ResolveSet(int rawSet, int nativeSet) { return rawSet != 0 ? rawSet : nativeSet; }
    internal static bool StandardSet(int set) { return set == 1637 || set == 1946 || set == 1952 || set == 1957 || set == 1980 || set == 1988 || set == 1994; }
    internal static int Copies(int rarity) { return rarity == 5 ? 1 : 2; }
    internal static int[] Versions(bool diamond, bool signature) {
        var result = new List<int> {0,1}; if (diamond) result.Add(2); if (signature) result.Add(3); return result.ToArray();
    }
}
