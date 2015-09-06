using System;
using System.Collections.Generic;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> cache = (commandGroup, message, result) =>
            {
                if (!HasCorradePermission(commandGroup.Name, (int) Permissions.System))
                {
                    throw new ScriptException(ScriptError.NO_CORRADE_PERMISSIONS);
                }
                switch (wasGetEnumValueFromDescription<Action>(wasInput(
                    wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.ACTION)), message))
                    .ToLowerInvariant()))
                {
                    case Action.PURGE:
                        Client.Assets.Cache.BeginPrune();
                        Cache.Purge();
                        break;
                    case Action.SAVE:
                        SaveCorradeCache.Invoke();
                        break;
                    case Action.LOAD:
                        LoadCorradeCache.Invoke();
                        break;
                    default:
                        throw new ScriptException(ScriptError.UNKNOWN_ACTION);
                }
            };
        }
    }
}