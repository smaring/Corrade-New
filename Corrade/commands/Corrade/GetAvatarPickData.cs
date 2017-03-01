﻿///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2016 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using CorradeConfigurationSharp;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using wasOpenMetaverse;
using wasSharp;
using wasSharp.Timers;
using Reflection = wasSharp.Reflection;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static readonly Action<Command.CorradeCommandParameters, Dictionary<string, string>>
                getavatarpickdata =
                    (corradeCommandParameters, result) =>
                    {
                        if (
                            !HasCorradePermission(corradeCommandParameters.Group.UUID,
                                (int)Configuration.Permissions.Interact))
                        {
                            throw new Command.ScriptException(Enumerations.ScriptError.NO_CORRADE_PERMISSIONS);
                        }
                        UUID agentUUID;
                        if (
                            !UUID.TryParse(
                                wasInput(KeyValue.Get(
                                    wasOutput(Reflection.GetNameFromEnumValue(Command.ScriptKeys.AGENT)),
                                    corradeCommandParameters.Message)),
                                out agentUUID) && !Resolvers.AgentNameToUUID(Client,
                                    wasInput(
                                        KeyValue.Get(
                                            wasOutput(Reflection.GetNameFromEnumValue(Command.ScriptKeys.FIRSTNAME)),
                                            corradeCommandParameters.Message)),
                                    wasInput(
                                        KeyValue.Get(
                                            wasOutput(Reflection.GetNameFromEnumValue(Command.ScriptKeys.LASTNAME)),
                                            corradeCommandParameters.Message)),
                                    corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout,
                                    new DecayingAlarm(corradeConfiguration.DataDecayType),
                                    ref agentUUID))
                        {
                            throw new Command.ScriptException(Enumerations.ScriptError.AGENT_NOT_FOUND);
                        }

                        UUID pickUUID;
                        if (
                            !UUID.TryParse(
                                wasInput(
                                    KeyValue.Get(wasOutput(Reflection.GetNameFromEnumValue(Command.ScriptKeys.ITEM)),
                                        corradeCommandParameters.Message)), out pickUUID))
                            throw new Command.ScriptException(Enumerations.ScriptError.NO_ITEM_SPECIFIED);

                        var AvatarPicksReplyEvent = new ManualResetEvent(false);
                        var picks = new Dictionary<UUID, string>();
                        EventHandler<AvatarPicksReplyEventArgs> AvatarPicksReplyEventHandler =
                            (sender, args) =>
                            {
                                picks = args.Picks;
                                AvatarPicksReplyEvent.Set();
                            };
                        lock (Locks.ClientInstanceAvatarsLock)
                        {
                            Client.Avatars.AvatarPicksReply += AvatarPicksReplyEventHandler;
                            Client.Avatars.RequestAvatarPicks(agentUUID);
                            if (!AvatarPicksReplyEvent.WaitOne((int)corradeConfiguration.ServicesTimeout, false))
                            {
                                Client.Avatars.AvatarPicksReply -= AvatarPicksReplyEventHandler;
                                throw new Command.ScriptException(Enumerations.ScriptError.TIMEOUT_GETTING_AVATAR_DATA);
                            }
                            Client.Avatars.AvatarPicksReply -= AvatarPicksReplyEventHandler;
                        }

                        if (!picks.ContainsKey(pickUUID))
                            throw new Command.ScriptException(Enumerations.ScriptError.PICK_NOT_FOUND);

                        var AvatarPickInfoReplyEvent = new ManualResetEvent(false);
                        var profilePick = new ProfilePick();
                        EventHandler<PickInfoReplyEventArgs> AvatarPickInfoReplyEventHandler = (sender, args) =>
                        {
                            profilePick = args.Pick;
                            AvatarPickInfoReplyEvent.Set();
                        };
                        lock (Locks.ClientInstanceAvatarsLock)
                        {
                            Client.Avatars.PickInfoReply += AvatarPickInfoReplyEventHandler;
                            Client.Avatars.RequestPickInfo(agentUUID, pickUUID);
                            if (!AvatarPickInfoReplyEvent.WaitOne((int)corradeConfiguration.ServicesTimeout, false))
                            {
                                Client.Avatars.PickInfoReply -= AvatarPickInfoReplyEventHandler;
                                throw new Command.ScriptException(Enumerations.ScriptError.TIMEOUT_GETTING_PROFILE_PICK);
                            }
                            Client.Avatars.PickInfoReply -= AvatarPickInfoReplyEventHandler;
                        }

                        if (profilePick.Equals(default(ProfilePick)))
                            throw new Command.ScriptException(Enumerations.ScriptError.COULD_NOT_RETRIEVE_PICK);

                        var data =
                            profilePick.GetStructuredData(
                                wasInput(
                                    KeyValue.Get(wasOutput(Reflection.GetNameFromEnumValue(Command.ScriptKeys.DATA)),
                                        corradeCommandParameters.Message))).ToList();
                        if (data.Any())
                        {
                            result.Add(Reflection.GetNameFromEnumValue(Command.ResultKeys.DATA),
                                CSV.FromEnumerable(data));
                        }
                    };
        }
    }
}
