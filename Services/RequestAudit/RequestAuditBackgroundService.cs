using TenantService.Api.Repositories;

namespace TenantService.Api.Services.RequestAudit;

public sealed class RequestAuditBackgroundService : BackgroundService
{
    private readonly IRequestAuditQueue _queue;
    private readonly IRequestAuditRepository _repository;
    private readonly ILogger<RequestAuditBackgroundService> _logger;

    public RequestAuditBackgroundService(
        IRequestAuditQueue queue,
        IRequestAuditRepository repository,
        ILogger<RequestAuditBackgroundService> logger)
    {
        _queue = queue;
        _repository = repository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken){
        await foreach (var entry in _queue.Reader.ReadAllAsync(stoppingToken)){
            try{
                await _repository.InsertAsync(entry, stoppingToken);
            }
            catch (Exception ex){
                _logger.LogError(ex, "Failed to persist request audit entry {AuditId}", entry.Id);
            }
        }
    }
}