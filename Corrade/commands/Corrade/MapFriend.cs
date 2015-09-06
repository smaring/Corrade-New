using System;
using System.Collections.Generic;
using System.Threading;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> mapfriend =
                (commandGroup, message, result) =>
                {
                    if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Friendship))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    UUID agentUUID;
                    if (
                        !UUID.TryParse(
                            wasInput(wasKeyValueGet(
                                wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.AGENT)), message)),
                            out agentUUID) && !AgentNameToUUID(
                                wasInput(
                                    wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.FIRSTNAME)),
                                        message)),
                                wasInput(
                                    wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.LASTNAME)),
                                        message)),
                                corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout,
                                ref agentUUID))
                    {
                        throw new ScriptException(ScriptError.AGENT_NOT_FOUND);
                    }
                    FriendInfo friend = Client.Friends.FriendList.Find(o => o.UUID.Equals(agentUUID));
                    if (friend == null)
                    {
                        throw new ScriptException(ScriptError.FRIEND_NOT_FOUND);
                    }
                    if (!friend.CanSeeThemOnMap)
                    {
                        throw new ScriptException(ScriptError.FRIEND_DOES_NOT_ALLOW_MAPPING);
                    }
                    ulong regionHandle = 0;
                    Vector3 position = Vector3.Zero;
                    ManualResetEvent FriendFoundEvent = new ManualResetEvent(false);
                    bool offline = false;
                    EventHandler<FriendFoundReplyEventArgs> FriendFoundEventHandler = (sender, args) =>
                    {
                        if (args.RegionHandle.Equals(0))
                        {
                            offline = true;
                            FriendFoundEvent.Set();
                            return;
                        }
                        regionHandle = args.RegionHandle;
                        position = args.Location;
                        FriendFoundEvent.Set();
                    };
                    lock (ClientInstanceFriendsLock)
                    {
                        Client.Friends.FriendFoundReply += FriendFoundEventHandler;
                        Client.Friends.MapFriend(agentUUID);
                        if (!FriendFoundEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                        {
                            Client.Friends.FriendFoundReply -= FriendFoundEventHandler;
                            throw new ScriptException(ScriptError.TIMEOUT_MAPPING_FRIEND);
                        }
                        Client.Friends.FriendFoundReply -= FriendFoundEventHandler;
                    }
                    if (offline)
                    {
                        throw new ScriptException(ScriptError.FRIEND_OFFLINE);
                    }
                    UUID parcelUUID = Client.Parcels.RequestRemoteParcelID(position, regionHandle, UUID.Zero);
                    ManualResetEvent ParcelInfoEvent = new ManualResetEvent(false);
                    string regionName = string.Empty;
                    EventHandler<ParcelInfoReplyEventArgs> ParcelInfoEventHandler = (sender, args) =>
                    {
                        regionName = args.Parcel.SimName;
                        ParcelInfoEvent.Set();
                    };
                    lock (ClientInstanceParcelsLock)
                    {
                        Client.Parcels.ParcelInfoReply += ParcelInfoEventHandler;
                        Client.Parcels.RequestParcelInfo(parcelUUID);
                        if (!ParcelInfoEvent.WaitOne((int) corradeConfiguration.ServicesTimeout, false))
                        {
                            Client.Parcels.ParcelInfoReply -= ParcelInfoEventHandler;
                            throw new ScriptException(ScriptError.TIMEOUT_GETTING_PARCELS);
                        }
                        Client.Parcels.ParcelInfoReply -= ParcelInfoEventHandler;
                    }
                    result.Add(wasGetDescriptionFromEnumValue(ResultKeys.DATA),
                        wasEnumerableToCSV(new[] {regionName, position.ToString()}));
                };
        }
    }
}