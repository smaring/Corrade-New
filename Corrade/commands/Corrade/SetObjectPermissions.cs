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
            public static Action<Group, string, Dictionary<string, string>> setobjectpermissions =
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
                    string itemPermissions =
                        wasInput(
                            wasKeyValueGet(
                                wasOutput(wasGetDescriptionFromEnumValue(ScriptKeys.PERMISSIONS)), message));
                    if (string.IsNullOrEmpty(itemPermissions))
                    {
                        throw new ScriptException(ScriptError.NO_PERMISSIONS_PROVIDED);
                    }
                    OpenMetaverse.Permissions permissions = wasStringToPermissions(itemPermissions);
                    Client.Objects.SetPermissions(
                        Client.Network.Simulators.FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                        new List<uint> {primitive.LocalID},
                        PermissionWho.Base, permissions.BaseMask, true);
                    Client.Objects.SetPermissions(
                        Client.Network.Simulators.FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                        new List<uint> {primitive.LocalID},
                        PermissionWho.Owner, permissions.OwnerMask, true);
                    Client.Objects.SetPermissions(
                        Client.Network.Simulators.FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                        new List<uint> {primitive.LocalID},
                        PermissionWho.Group, permissions.GroupMask, true);
                    Client.Objects.SetPermissions(
                        Client.Network.Simulators.FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                        new List<uint> {primitive.LocalID},
                        PermissionWho.Everyone, permissions.EveryoneMask, true);
                    Client.Objects.SetPermissions(
                        Client.Network.Simulators.FirstOrDefault(o => o.Handle.Equals(primitive.RegionHandle)),
                        new List<uint> {primitive.LocalID},
                        PermissionWho.NextOwner, permissions.NextOwnerMask, true);
                };
        }
    }
}