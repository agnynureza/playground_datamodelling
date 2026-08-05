using System.Threading.Channels;
using TenantService.Api.Models;

namespace TenantService.Api.Services.RequestAudit;

public sealed class RequestAuditQueue : IRequestAuditQueue{
    private readonly Channel<RequestAuditEntry> _channel = Channel.CreateUnbounded<RequestAuditEntry>(new UnboundedChannelOptions{
        SingleReader = true,
        SingleWriter = false
    });

    public ChannelReader<RequestAuditEntry> Reader => _channel.Reader;

    public void Enqueue(RequestAuditEntry entry){
        _channel.Writer.TryWrite(entry);
    }
}