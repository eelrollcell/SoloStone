# Full collection and deck building

This optional build uses the installed Hearthstone client's card definitions to provide a local Standard and Wild collection. It includes Normal and Golden copies and the Diamond and Signature variants flagged by the client. It preserves card upgrades and per-deck format choices in local deck drafts. The feature was exercised on client 37.0.0.255790.

The full collection mode is for browsing cards and building decks. Matches are disabled in this mode because many cards in the collection do not have a battle implementation in SabberStone.

Follow [Building SoloStone](BUILDING.md) to set up the client and loader references. From the repository root, build the optional plugin and engine with:

```text
dotnet build plugin/OfflineProbe.csproj -c Debug -p:EngineSource=true -p:Installer=true -p:FullCollection=true
dotnet run --project tests/full-collection
```

This branch contains source code. The stock release installer does not include this optional build. Use your own Hearthstone installation for client assets and libraries.
