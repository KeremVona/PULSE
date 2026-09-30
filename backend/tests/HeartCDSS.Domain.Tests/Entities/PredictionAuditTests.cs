using FluentAssertions;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Domain.Exceptions;
using Xunit;

namespace HeartCDSS.Domain.Tests.Entities;

public class PredictionAuditTests
{
    [Fact]
    public void RecordSuccess_ShouldMakeValidAuditRecord()
    {
        var encounterId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var reqPayload = "{\"clinical_indicators\": {\"age\": 58}}";
        var resPayload = "{\"risk_probability\": 0.6840}";

        var audit = PredictionAudit.RecordSuccess(
            encounterId: encounterId,
            userId: userId,
            correlationId: correlationId,
            requestPayload: reqPayload,
            responsePayload: resPayload,
            executionTimeMs: 18,
            clientIp: "127.0.0.1",
            userAgent: "PULSE-Client/1.0"
        );

        audit.EncounterId.Should().Be(encounterId);
        audit.UserId.Should().Be(userId);
        audit.CorrelationId.Should().Be(correlationId);
        audit.RequestPayload.Should().Be(reqPayload);
        audit.ResponsePayload.Should().Be(resPayload);
        audit.ExecutionTimeMs.Should().Be(18);
        audit.InferenceStatus.Should().Be(InferenceExecutionStatus.Success);
        audit.ErrorDetails.Should().BeNull();
        audit.ClientIp.Should().Be("127.0.0.1");
        audit.UserAgent.Should().Be("PULSE-Client/1.0");
    }

    [Fact]
    public void RecordFailure_ShouldSetStatusAndErrorDetails()
    {
        var encounterId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();

        var audit = PredictionAudit.RecordFailure(
            encounterId: encounterId,
            userId: userId,
            correlationId: correlationId,
            requestPayload: "{}",
            executionTimeMs: 5000,
            failureStatus: InferenceExecutionStatus.CircuitBreakerOpen,
            errorDetails: "ML microservice circuit breaker open after 3 consecutive failures."
        );

        audit.InferenceStatus.Should().Be(InferenceExecutionStatus.CircuitBreakerOpen);
        audit.ErrorDetails.Should().Contain("circuit breaker open");
        audit.ResponsePayload.Should().BeNull();
    }

    [Fact]
    public void RecordSuccess_WithEmptyPayload_ShouldThrow()
    {
        var act = () => PredictionAudit.RecordSuccess(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "",
            "{}",
            10
        );

        act.Should().Throw<DomainException>()
            .WithMessage("*Request payload cannot be empty*");
    }
}
