using System.Diagnostics;
using System.Text;
using TenantService.Api.Common;
using TenantService.Api.Models;
using TenantService.Api.Services.RequestAudit;

namespace TenantService.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private const int MaxBodyLength = 8000;
    private readonly RequestDelegate _next;
    private readonly IRequestAuditQueue _auditQueue;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, IRequestAuditQueue auditQueue, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _auditQueue = auditQueue;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context.Request);
        var originalBodyStream = context.Response.Body;
        await using var captureStream = new ResponseCaptureStream(originalBodyStream, MaxBodyLength);
        context.Response.Body = captureStream;

        Exception? exception = null;

        try{
            await _next(context);
        }
        catch (Exception ex){
            exception = ex;
            if (!context.Response.HasStarted){
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            }

            throw;
        }
        finally{
            stopwatch.Stop();
            context.Response.Body = originalBodyStream;

            var auditEntry = new RequestAuditEntry(
                Guid.NewGuid(),
                DateTime.UtcNow,
                context.User.GetTenantId(),
                context.User.GetTenantName(),
                context.Request.Method,
                context.Request.Path.ToString(),
                RequestAuditSanitizer.SanitizeAndTruncate(requestBody, MaxBodyLength),
                RequestAuditSanitizer.SanitizeAndTruncate(captureStream.GetCapturedText(), MaxBodyLength),
                context.Response.StatusCode,
                (int)stopwatch.ElapsedMilliseconds);

            try{
                _auditQueue.Enqueue(auditEntry);
            }
            catch (Exception enqueueException){
                _logger.LogError(enqueueException, "Failed to enqueue request audit entry {AuditId}", auditEntry.Id);
            }

            if (exception is not null){
                _logger.LogDebug(exception, "Request {Method} {Path} failed after audit capture", context.Request.Method, context.Request.Path);
            }
        }
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request){
        if (request.ContentLength is 0 || request.Body is null)
            return null;

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var buffer = new char[4096];
        var builder = new StringBuilder();
        var totalChars = 0;

        while (true){
            var read = await reader.ReadAsync(buffer);
            if (read == 0)
                break;

            var remaining = MaxBodyLength - totalChars;
            if (remaining > 0){
                var take = Math.Min(remaining, read);
                builder.Append(buffer, 0, take);
                totalChars += take;
            }
        }

        request.Body.Position = 0;
        return builder.Length == 0 ? null : builder.ToString();
    }

    private sealed class ResponseCaptureStream : Stream{
        private readonly Stream _inner;
        private readonly MemoryStream _buffer = new();
        private readonly int _maxBytes;
        private bool _truncated;

        public ResponseCaptureStream(Stream inner, int maxBytes){
            _inner = inner;
            _maxBytes = maxBytes;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void Flush() => _inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count){
            _inner.Write(buffer, offset, count);
            Capture(buffer.AsSpan(offset, count));
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken){
            await _inner.WriteAsync(buffer.AsMemory(offset, count), cancellationToken);
            Capture(buffer.AsSpan(offset, count));
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default){
            await _inner.WriteAsync(buffer, cancellationToken);
            Capture(buffer.Span);
        }

        protected override void Dispose(bool disposing){}

        public override ValueTask DisposeAsync(){
            return ValueTask.CompletedTask;
        }

        public string? GetCapturedText(){
            if (_buffer.Length == 0)
                return null;

            var text = Encoding.UTF8.GetString(_buffer.ToArray());
            return _truncated ? text + "...[truncated]" : text;
        }

        private void Capture(ReadOnlySpan<byte> bytes){
            if (_buffer.Length >= _maxBytes){
                _truncated = true;
                return;
            }

            var remaining = _maxBytes - (int)_buffer.Length;
            var take = Math.Min(remaining, bytes.Length);
            _buffer.Write(bytes[..take]);
            if (take < bytes.Length)
                _truncated = true;
        }
    }
}