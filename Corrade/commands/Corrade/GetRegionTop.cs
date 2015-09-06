using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> getregiontop =
                (commandGroup, message, result) =>
                {
                    if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Land))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    if (!Client.Network.CurrentSim.IsEstateManager)
                    {
                        throw new ScriptException(ScriptError.NO_LAND_RIGHTS);
                    }
                    Dictionary<UUID, EstateTask> topTasks = new Dictionary<UUID, EstateTask>();
                    switch (
                        wasGetEnumValueFromDescription<Type>(
                            wasInput(wasKeyValueGet(
                                wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.TYPE)), message))
                                .ToLowerInvariant()))
                    {
                        case Type.SCRIPTS:
                            ManualResetEvent TopScriptsReplyEvent = new ManualResetEvent(false);
                            EventHandler<TopScriptsReplyEventArgs> TopScriptsReplyEventHandler = (sender, args) =>
                            {
                                topTasks =
                                    args.Tasks.OrderByDescending(o => o.Value.Score)
                                        .ToDictionary(o => o.Key, o => o.Value);
                                TopScriptsReplyEvent.Set();
                            };
                            lock (ClientInstanceEstateLock)
                            {
                                Client.Estate.TopScriptsReply += TopScriptsReplyEventHandler;
                                Client.Estate.RequestTopScripts();
                                if (!TopScriptsReplyEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                                {
                                    Client.Estate.TopScriptsReply -= TopScriptsReplyEventHandler;
                                    throw new ScriptException(ScriptError.TIMEOUT_GETTING_TOP_SCRIPTS);
                                }
                                Client.Estate.TopScriptsReply -= TopScriptsReplyEventHandler;
                            }
                            break;
                        case Type.COLLIDERS:
                            ManualResetEvent TopCollidersReplyEvent = new ManualResetEvent(false);
                            EventHandler<TopCollidersReplyEventArgs> TopCollidersReplyEventHandler =
                                (sender, args) =>
                                {
                                    topTasks =
                                        args.Tasks.OrderByDescending(o => o.Value.Score)
                                            .ToDictionary(o => o.Key, o => o.Value);
                                    TopCollidersReplyEvent.Set();
                                };
                            lock (ClientInstanceEstateLock)
                            {
                                Client.Estate.TopCollidersReply += TopCollidersReplyEventHandler;
                                Client.Estate.RequestTopScripts();
                                if (
                                    !TopCollidersReplyEvent.WaitOne((int) corradeConfiguration.ServicesTimeout,
                                        false))
                                {
                                    Client.Estate.TopCollidersReply -= TopCollidersReplyEventHandler;
                                    throw new ScriptException(ScriptError.TIMEOUT_GETTING_TOP_SCRIPTS);
                                }
                                Client.Estate.TopCollidersReply -= TopCollidersReplyEventHandler;
                            }
                            break;
                        default:
                            throw new ScriptException(ScriptError.UNKNOWN_TOP_TYPE);
                    }
                    int amount;
                    if (
                        !int.TryParse(
                            wasInput(
                                wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.AMOUNT)), message)),
                            out amount))
                    {
                        amount = topTasks.Count;
                    }
                    List<string> data = new List<string>(topTasks.Take(amount).Select(o => new[]
                    {
                        o.Value.Score.ToString(CultureInfo.DefaultThreadCurrentCulture),
                        o.Value.TaskName,
                        o.Key.ToString(),
                        o.Value.OwnerName,
                        o.Value.Position.ToString()
                    }).SelectMany(o => o));
                    if (data.Any())
                    {
                        result.Add(wasGetDescriptionFromEnumValue(ResultKeys.DATA),
                            wasEnumerableToCSV(data));
                    }
                };
        }
    }
}