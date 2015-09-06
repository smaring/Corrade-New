using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OpenMetaverse;
using Parallel = System.Threading.Tasks.Parallel;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> getavatarsdata =
                (commandGroup, message, result) =>
                {
                    if (
                        !HasCorradePermission(commandGroup.Name,
                            (int) Permissions.Interact))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    float range;
                    if (
                        !float.TryParse(
                            wasInput(wasKeyValueGet(
                                wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.RANGE)), message)),
                            out range))
                    {
                        range = corradeConfiguration.Range;
                    }
                    HashSet<Avatar> avatars = new HashSet<Avatar>();
                    object LockObject = new object();
                    switch (wasGetEnumValueFromDescription<Entity>(
                        wasInput(
                            wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.ENTITY)), message))
                            .ToLowerInvariant()))
                    {
                        case Entity.RANGE:
                            Parallel.ForEach(
                                GetAvatars(range, corradeConfiguration.ServicesTimeout,
                                    corradeConfiguration.DataTimeout)
                                    .AsParallel()
                                    .Where(o => Vector3.Distance(o.Position, Client.Self.SimPosition) <= range),
                                o =>
                                {
                                    lock (LockObject)
                                    {
                                        avatars.Add(o);
                                    }
                                });
                            break;
                        case Entity.PARCEL:
                            Vector3 position;
                            if (
                                !Vector3.TryParse(
                                    wasInput(
                                        wasKeyValueGet(
                                            wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.POSITION)),
                                            message)),
                                    out position))
                            {
                                position = Client.Self.SimPosition;
                            }
                            Parcel parcel = null;
                            if (
                                !GetParcelAtPosition(Client.Network.CurrentSim, position, ref parcel))
                            {
                                throw new ScriptException(ScriptError.COULD_NOT_FIND_PARCEL);
                            }
                            Parallel.ForEach(GetAvatars(new[]
                            {
                                Vector3.Distance(Client.Self.SimPosition, parcel.AABBMin),
                                Vector3.Distance(Client.Self.SimPosition, parcel.AABBMax),
                                Vector3.Distance(Client.Self.SimPosition,
                                    new Vector3(parcel.AABBMin.X, parcel.AABBMax.Y, 0)),
                                Vector3.Distance(Client.Self.SimPosition,
                                    new Vector3(parcel.AABBMax.X, parcel.AABBMin.Y, 0))
                            }.Max(), corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout)
                                .AsParallel()
                                .Where(o => IsVectorInParcel(o.Position, parcel)), o =>
                                {
                                    lock (LockObject)
                                    {
                                        avatars.Add(o);
                                    }
                                });
                            break;
                        case Entity.REGION:
                            // Get all sim parcels
                            ManualResetEvent SimParcelsDownloadedEvent = new ManualResetEvent(false);
                            EventHandler<SimParcelsDownloadedEventArgs> SimParcelsDownloadedEventHandler =
                                (sender, args) => SimParcelsDownloadedEvent.Set();
                            lock (ClientInstanceParcelsLock)
                            {
                                Client.Parcels.SimParcelsDownloaded += SimParcelsDownloadedEventHandler;
                                Client.Parcels.RequestAllSimParcels(Client.Network.CurrentSim);
                                if (Client.Network.CurrentSim.IsParcelMapFull())
                                {
                                    SimParcelsDownloadedEvent.Set();
                                }
                                if (
                                    !SimParcelsDownloadedEvent.WaitOne((int) corradeConfiguration.ServicesTimeout,
                                        false))
                                {
                                    Client.Parcels.SimParcelsDownloaded -= SimParcelsDownloadedEventHandler;
                                    throw new ScriptException(ScriptError.TIMEOUT_GETTING_PARCELS);
                                }
                                Client.Parcels.SimParcelsDownloaded -= SimParcelsDownloadedEventHandler;
                            }
                            HashSet<Parcel> regionParcels =
                                new HashSet<Parcel>(Client.Network.CurrentSim.Parcels.Copy().Values);
                            Parallel.ForEach(
                                GetAvatars(
                                    regionParcels.AsParallel().Select(o => new[]
                                    {
                                        Vector3.Distance(Client.Self.SimPosition, o.AABBMin),
                                        Vector3.Distance(Client.Self.SimPosition, o.AABBMax),
                                        Vector3.Distance(Client.Self.SimPosition,
                                            new Vector3(o.AABBMin.X, o.AABBMax.Y, 0)),
                                        Vector3.Distance(Client.Self.SimPosition,
                                            new Vector3(o.AABBMax.X, o.AABBMin.Y, 0))
                                    }.Max()).Max(), corradeConfiguration.ServicesTimeout,
                                    corradeConfiguration.DataTimeout)
                                    .AsParallel()
                                    .Where(o => regionParcels.AsParallel().Any(p => IsVectorInParcel(o.Position, p))),
                                o =>
                                {
                                    lock (LockObject)
                                    {
                                        avatars.Add(o);
                                    }
                                });
                            break;
                        case Entity.AVATAR:
                            UUID agentUUID = UUID.Zero;
                            if (
                                !UUID.TryParse(
                                    wasInput(
                                        wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.AGENT)),
                                            message)), out agentUUID) && !AgentNameToUUID(
                                                wasInput(
                                                    wasKeyValueGet(
                                                        wasOutput(
                                                            wasGetDescriptionFromEnumValue(ScriptKeys.FIRSTNAME)),
                                                        message)),
                                                wasInput(
                                                    wasKeyValueGet(
                                                        wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.LASTNAME)),
                                                        message)),
                                                corradeConfiguration.ServicesTimeout,
                                                corradeConfiguration.DataTimeout,
                                                ref agentUUID))
                            {
                                throw new ScriptException(ScriptError.AGENT_NOT_FOUND);
                            }
                            Avatar avatar = GetAvatars(range, corradeConfiguration.ServicesTimeout,
                                corradeConfiguration.DataTimeout)
                                .AsParallel()
                                .FirstOrDefault(o => o.ID.Equals(agentUUID));
                            if (avatar == null)
                                throw new ScriptException(ScriptError.AVATAR_NOT_IN_RANGE);
                            avatars.Add(avatar);
                            break;
                        default:
                            throw new ScriptException(ScriptError.UNKNOWN_ENTITY);
                    }

                    // allow partial results
                    UpdateAvatars(ref avatars, corradeConfiguration.ServicesTimeout,
                        corradeConfiguration.DataTimeout);

                    List<string> data = new List<string>();

                    Parallel.ForEach(avatars, o =>
                    {
                        IEnumerable<string> avatarData = GetStructuredData(o,
                            wasInput(
                                wasKeyValueGet(
                                    wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.DATA)),
                                    message)));
                        if (avatarData.ToList().Any())
                        {
                            lock (LockObject)
                            {
                                data.AddRange(avatarData);
                            }
                        }
                    });
                    if (data.Any())
                    {
                        result.Add(wasGetDescriptionFromEnumValue(ResultKeys.DATA),
                            wasEnumerableToCSV(data));
                    }
                };
        }
    }
}