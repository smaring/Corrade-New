using System;
using System.Collections.Generic;
using System.Linq;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> setprimitiveposition =
                (commandGroup, message, result) =>
                {
                    if (!HasCorradePermission(commandGroup.Name, (int) Permissions.Interact))
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
                    Primitive primitive = null;
                    if (
                        !FindPrimitive(
                            StringOrUUID(wasInput(wasKeyValueGet(
                                wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.ITEM)), message))),
                            range,
                            ref primitive, corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout))
                    {
                        throw new ScriptException(ScriptError.PRIMITIVE_NOT_FOUND);
                    }
                    Vector3 position;
                    if (
                        !Vector3.TryParse(
                            wasInput(
                                wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.POSITION)),
                                    message)),
                            out position))
                    {
                        throw new ScriptException(ScriptError.INVALID_POSITION);
                    }
                    Client.Objects.SetPosition(
                        Client.Network.Simulators.FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                        primitive.LocalID, position, true);
                };
        }
    }
}