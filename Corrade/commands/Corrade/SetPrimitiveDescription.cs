using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class CorradeCommands
        {
            public static Action<Group, string, Dictionary<string, string>> setprimitivedescription =
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
                    string description =
                        wasInput(
                            wasKeyValueGet(wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.DESCRIPTION)),
                                message));
                    if (string.IsNullOrEmpty(description))
                    {
                        throw new ScriptException(ScriptError.NO_DESCRIPTION_PROVIDED);
                    }
                    if (IsSecondLife() &&
                        Encoding.UTF8.GetByteCount(description) >
                        LINDEN_CONSTANTS.PRIMITIVES.MAXIMUM_DESCRIPTION_SIZE)
                    {
                        throw new ScriptException(ScriptError.DESCRIPTION_TOO_LARGE);
                    }
                    Client.Objects.SetDescription(
                        Client.Network.Simulators.FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                        primitive.LocalID, description);
                };
        }
    }
}