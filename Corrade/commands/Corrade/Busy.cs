using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> busy = (commandGroup, message, result) =>
            {
                if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Grooming))
                {
                    throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                }
                switch (wasGetEnumValueFromDescription<Action>(
                    wasInput(
                        wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.ACTION)), message))
                        .ToLowerInvariant()))
                {
                    case Action.ENABLE:
                        Client.Self.AnimationStart(Animations.BUSY, true);
                        break;
                    case Action.DISABLE:
                        Client.Self.AnimationStop(Animations.BUSY, true);
                        break;
                    case Action.GET:
                        result.Add(wasGetDescriptionFromEnumValue(ScriptKeys.DATA),
                            Client.Self.SignaledAnimations.ContainsKey(Animations.BUSY).ToString());
                        break;
                    default:
                        throw new ScriptException(ScriptError.UNKNOWN_ACTION);
                }
            };
        }
    }
}