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
            public static Action<Group, string, Dictionary<string, string>> getobjectmediadata =
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
                    // if the primitive is not an object (the root) or the primitive
                    // is not an object as an avatar attachment then bail out
                    if (!primitive.ParentID.Equals(0) && !primitive.ParentID.Equals(Client.Self.LocalID))
                    {
                        throw new ScriptException(ScriptError.ITEM_IS_NOT_AN_OBJECT);
                    }
                    List<string> data = new List<string>();
                    Client.Objects.RequestObjectMedia(primitive.ID,
                        Client.Network.Simulators.FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                        (succeeded, version, faceMedia) =>
                        {
                            switch (succeeded)
                            {
                                case true:
                                    data.AddRange(GetStructuredData(faceMedia,
                                        wasInput(
                                            wasKeyValueGet(
                                                wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.DATA)),
                                                message))));
                                    break;
                                default:
                                    throw new ScriptException(ScriptError.COULD_NOT_RETRIEVE_OBJECT_MEDIA);
                            }
                        });
                    result.Add(wasGetDescriptionFromEnumValue(ResultKeys.DATA),
                        wasEnumerableToCSV(data));
                };
        }
    }
}