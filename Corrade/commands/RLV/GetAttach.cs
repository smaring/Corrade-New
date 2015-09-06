using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenMetaverse;

namespace Corrade
{
    public partial class Corrade
    {
        public partial class RLVBehaviours
        {
            public static Action<string, RLVRule, UUID> getattach = (message, rule, senderUUID) =>
            {
                int channel;
                if (!int.TryParse(rule.Param, out channel) || channel < 1)
                {
                    return;
                }
                HashSet<Primitive> attachments = new HashSet<Primitive>(
                    GetAttachments(corradeConfiguration.ServicesTimeout, corradeConfiguration.DataTimeout)
                        .AsParallel()
                        .Select(o => o.Key));
                StringBuilder response = new StringBuilder();
                if (!attachments.Any())
                {
                    Client.Self.Chat(response.ToString(), channel, ChatType.Normal);
                    return;
                }
                HashSet<AttachmentPoint> attachmentPoints =
                    new HashSet<AttachmentPoint>(attachments.AsParallel()
                        .Select(o => o.PrimData.AttachmentPoint));
                switch (!string.IsNullOrEmpty(rule.Option))
                {
                    case true:
                        RLVAttachment RLVattachment = RLVAttachments.AsParallel().FirstOrDefault(
                            o => o.Name.Equals(rule.Option, StringComparison.InvariantCultureIgnoreCase));
                        switch (!RLVattachment.Equals(default(RLVAttachment)))
                        {
                            case true:
                                if (!attachmentPoints.Contains(RLVattachment.AttachmentPoint))
                                    goto default;
                                response.Append(RLV_CONSTANTS.TRUE_MARKER);
                                break;
                            default:
                                response.Append(RLV_CONSTANTS.FALSE_MARKER);
                                break;
                        }
                        break;
                    default:
                        string[] data = new string[RLVAttachments.Count];
                        Parallel.ForEach(Enumerable.Range(0, RLVAttachments.Count), o =>
                        {
                            if (!attachmentPoints.Contains(RLVAttachments[o].AttachmentPoint))
                            {
                                data[o] = RLV_CONSTANTS.FALSE_MARKER;
                                return;
                            }
                            data[o] = RLV_CONSTANTS.TRUE_MARKER;
                        });
                        response.Append(string.Join("", data.ToArray()));
                        break;
                }
                Client.Self.Chat(response.ToString(), channel, ChatType.Normal);
            };
        }
    }
}