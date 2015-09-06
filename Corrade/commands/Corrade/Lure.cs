using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> lure = (commandGroup, message, result) =>
            {
                if (!HasCorradePermission(commandGroup.Name,
                    (int) Permissions.Movement))
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
                Client.Self.SendTeleportLure(agentUUID,
                    wasInput(wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.MESSAGE)),
                        message)));
            };
        }
    }
}