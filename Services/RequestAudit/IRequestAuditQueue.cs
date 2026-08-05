using System.Threading.Channels;
using TenantService.Api.Models;

namespace TenantService.Api.Services.RequestAudit;

public interface IRequestAuditQueue
{
    void Enqueue(RequestAuditEntry entry);

    ChannelReader<RequestAuditEntry> Reader { get; }
}