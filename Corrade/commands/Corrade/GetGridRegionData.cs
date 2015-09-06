using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> getgridregiondata =
                (commandGroup, message, result) =>
                {
                    if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Land))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    string region =
                        wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.REGION)),
                            message));
                    if (string.IsNullOrEmpty(region))
                    {
                        region = Client.Network.CurrentSim.Name;
                    }
                    ManualResetEvent GridRegionEvent = new ManualResetEvent(false);
                    GridRegion gridRegion = new GridRegion();
                    EventHandler<GridRegionEventArgs> GridRegionEventHandler = (sender, args) =>
                    {
                        if (!args.Region.Name.Equals(region, StringComparison.InvariantCultureIgnoreCase))
                            return;
                        gridRegion = args.Region;
                        GridRegionEvent.Set();
                    };
                    lock (ClientInstanceGridLock)
                    {
                        Client.Grid.GridRegion += GridRegionEventHandler;
                        Client.Grid.RequestMapRegion(region, GridLayerType.Objects);
                        if (!GridRegionEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                        {
                            Client.Grid.GridRegion -= GridRegionEventHandler;
                            throw new ScriptException(ScriptError.TIMEOUT_GETTING_REGION);
                        }
                        Client.Grid.GridRegion -= GridRegionEventHandler;
                    }
                    switch (!gridRegion.Equals(default(GridRegion)))
                    {
                        case false:
                            throw new ScriptException(ScriptError.REGION_NOT_FOUND);
                    }
                    List<string> data = new List<string>(GetStructuredData(gridRegion,
                        wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.DATA)),
                            message))));
                    if (data.Any())
                    {
                        result.Add(wasGetDescriptionFromEnumValue(ResultKeys.DATA),
                            wasEnumerableToCSV(data));
                    }
                };
        }
    }
}