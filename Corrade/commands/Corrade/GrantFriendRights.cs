///////////////////////////////////////////////////////////////////////////
//  Copyright (C) Wizardry and Steamworks 2013 - License: GNU GPLv3      //
//  Please see: http://www.gnu.org/licenses/gpl.html for legal details,  //
//  rights of fair usage, the disclaimer and warranty conditions.        //
///////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CorradeConfiguration;
using OpenMetaverse;
using wasOpenMetaverse;
using wasSharp;
using Parallel = System.Threading.Tasks.Parallel;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<CorradeCommandParameters, Dictionary<string, string>> grantfriendrights =
                (corradeCommandParameters, result) =>
                {
                    if (
                        !HasCorradePermission(corradeCommandParameters.Group.Name,
                            (int) Configuration.Permissions.Friendship))
                    {
                        throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                    }
                    UUID agentUUID;
                    if (
                        !UUID.TryParse(
                            wasInput(KeyValue.Get(
                                wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.AGENT)),
                                corradeCommandParameters.Message)),
                            out agentUUID) && !Resolvers.AgentNameToUUID(Client,
                                wasInput(
                                    KeyValue.Get(
                                        wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.FIRSTNAME)),
                                        corradeCommandParameters.Message)),
                                wasInput(
                                    KeyValue.Get(
                                        wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.LASTNAME)),
                                        corradeCommandParameters.Message)),
                                corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout,
                                new Time.DecayingAlarm(corradeConfiguration.DataDecayType),
                                ref agentUUID))
                    {
                        throw new ScriptException(ScriptError.AGENT_NOT_FOUND);
                    }
                    FriendInfo friend = Client.Friends.FriendList.Find(o => o.UUID.Equals(agentUUID));
                    if (friend == null)
                    {
                        throw new ScriptException(ScriptError.FRIEND_NOT_FOUND);
                    }
                    int rights = 0;
                    Parallel.ForEach(CSV.ToEnumerable(
                        wasInput(
                            KeyValue.Get(wasOutput(Reflection.GetNameFromEnumValue(ScriptKeys.RIGHTS)),
                                corradeCommandParameters.Message))).AsParallel().Where(o => !string.IsNullOrEmpty(o)),
                        o =>
                            Parallel.ForEach(
                                typeof (FriendRights).GetFields(BindingFlags.Public | BindingFlags.Static)
                                    .AsParallel().Where(p => string.Equals(o, p.Name, StringComparison.Ordinal)),
                                q => { rights |= ((int) q.GetValue(null)); }));
                    Client.Friends.GrantRights(agentUUID, (FriendRights) rights);
                };
        }
    }
}