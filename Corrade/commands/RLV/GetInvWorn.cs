using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class RLVBehaviours
        {
            public static Action<string, RLVRule, UUID> getinvworn = (message, rule, senderUUID) =>
            {
                int channel;
                if (!int.TryParse(rule.Param, out channel) || channel < 1)
                {
                    return;
                }
                InventoryNode RLVFolder =
                    FindInventory<InventoryNode>(Client.Inventory.Store.RootNode,
                        RLV_CONSTANTS.SHARED_FOLDER_NAME)
                        .AsParallel()
                        .FirstOrDefault(o => o.Data is InventoryFolder);
                if (RLVFolder == null)
                {
                    Client.Self.Chat(string.Empty, channel, ChatType.Normal);
                    return;
                }
                KeyValuePair<InventoryNode, LinkedList<string>> folderPath = FindInventoryPath<InventoryNode>(
                    RLVFolder,
                    CORRADE_CONSTANTS.OneOrMoRegex,
                    new LinkedList<string>())
                    .AsParallel().Where(o => o.Key.Data is InventoryFolder)
                    .FirstOrDefault(
                        o =>
                            string.Join(RLV_CONSTANTS.PATH_SEPARATOR, o.Value.Skip(1).ToArray())
                                .Equals(rule.Option, StringComparison.InvariantCultureIgnoreCase));
                switch (!folderPath.Equals(default(KeyValuePair<InventoryNode, LinkedList<string>>)))
                {
                    case false:
                        Client.Self.Chat(string.Empty, channel, ChatType.Normal);
                        return;
                }
                Func<InventoryNode, string> GetWornIndicator = node =>
                {
                    Dictionary<AppearanceManager.WearableData, WearableType> currentWearables =
                        GetWearables(Client.Inventory.Store.RootNode).ToDictionary(o => o.Key, o => o.Value);
                    Dictionary<Primitive, AttachmentPoint> currentAttachments =
                        GetAttachments(corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout)
                            .ToDictionary(o => o.Key, p => p.Value);

                    int myItemsCount = 0;
                    int myItemsWornCount = 0;

                    Parallel.ForEach(
                        node.Nodes.Values.AsParallel().Where(
                            n =>
                                !n.Data.Name.StartsWith(RLV_CONSTANTS.DOT_MARKER) &&
                                n.Data is InventoryItem && CanBeWorn(n.Data)
                            ), n =>
                            {
                                Interlocked.Increment(ref myItemsCount);
                                if ((n.Data is InventoryWearable &&
                                     currentWearables.Keys.AsParallel().Any(
                                         o => o.ItemID.Equals(ResolveItemLink(n.Data as InventoryItem).UUID))) ||
                                    currentAttachments.AsParallel().Any(
                                        o =>
                                            o.Key.Properties.ItemID.Equals(
                                                ResolveItemLink(n.Data as InventoryItem).UUID)))
                                {
                                    Interlocked.Increment(ref myItemsWornCount);
                                }
                            });


                    int allItemsCount = 0;
                    int allItemsWornCount = 0;

                    Parallel.ForEach(
                        node.Nodes.Values.AsParallel().Where(
                            n =>
                                !n.Data.Name.StartsWith(RLV_CONSTANTS.DOT_MARKER) &&
                                n.Data is InventoryFolder
                            ),
                        n => Parallel.ForEach(n.Nodes.Values
                            .AsParallel().Where(o => !o.Data.Name.StartsWith(RLV_CONSTANTS.DOT_MARKER))
                            .Where(
                                o =>
                                    o.Data is InventoryItem && CanBeWorn(o.Data) &&
                                    !o.Data.Name.StartsWith(RLV_CONSTANTS.DOT_MARKER)), p =>
                                    {
                                        Interlocked.Increment(ref allItemsCount);
                                        if ((p.Data is InventoryWearable &&
                                             currentWearables.Keys.AsParallel().Any(
                                                 o =>
                                                     o.ItemID.Equals(
                                                         ResolveItemLink(p.Data as InventoryItem).UUID))) ||
                                            currentAttachments.AsParallel().Any(
                                                o =>
                                                    o.Key.Properties.ItemID.Equals(
                                                        ResolveItemLink(p.Data as InventoryItem).UUID)))
                                        {
                                            Interlocked.Increment(ref allItemsWornCount);
                                        }
                                    }));


                    Func<int, int, string> WornIndicator =
                        (all, one) => all > 0 ? (all.Equals(one) ? "3" : (one > 0 ? "2" : "1")) : "0";

                    return WornIndicator(myItemsCount, myItemsWornCount) +
                           WornIndicator(allItemsCount, allItemsWornCount);
                };
                List<string> response = new List<string>
                {
                    string.Format("{0}{1}", RLV_CONSTANTS.PROPORTION_SEPARATOR,
                        GetWornIndicator(folderPath.Key))
                };
                response.AddRange(
                    folderPath.Key.Nodes.Values.AsParallel().Where(node => node.Data is InventoryFolder)
                        .Select(
                            node =>
                                string.Format("{0}{1}{2}", node.Data.Name,
                                    RLV_CONSTANTS.PROPORTION_SEPARATOR, GetWornIndicator(node))));

                Client.Self.Chat(string.Join(RLV_CONSTANTS.CSV_DELIMITER, response.ToArray()),
                    channel,
                    ChatType.Normal);
            };
        }
    }
}