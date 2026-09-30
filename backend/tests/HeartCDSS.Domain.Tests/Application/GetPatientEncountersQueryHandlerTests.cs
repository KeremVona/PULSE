using FluentAssertions;
using HeartCDSS.Application.Encounters.Queries.GetPatientEncounters;
using HeartCDSS.Domain.Entities;
using HeartCDSS.Domain.Enums;
using HeartCDSS.Infrastructure.Persistence;
using HeartCDSS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HeartCDSS.Domain.Tests.Application;

public class GetPatientEncountersQueryHandlerTests
{
    private static HeartCDSSDbContext MakeInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<HeartCDSSDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new HeartCDSSDbContext(options);
    }

    [Fact]
    public async Task Handle_WhenPatientHasMultipleEncounters_ReturnsAllInDescendingDateOrder()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var madePatient = Patient.Make(
            mrn: "MRN-MULTI-01",
            firstName: "Dana",
            lastName: "Scully",
            dateOfBirth: new DateOnly(1964, 2, 23),
            biologicalSex: BiologicalSex.Female
        );

        var madeEncounter1 = ClinicalEncounter.Make(
            patientId: madePatient.Id,
            clinicianId: Guid.NewGuid(),
            encounterNumber: "ENC-MULTI-001",
            encounterType: EncounterType.OutpatientScreening,
            encounterDate: DateTimeOffset.UtcNow.AddDays(-30)
        );

        var madeEncounter2 = ClinicalEncounter.Make(
            patientId: madePatient.Id,
            clinicianId: Guid.NewGuid(),
            encounterNumber: "ENC-MULTI-002",
            encounterType: EncounterType.CardiologyFollowUp,
            encounterDate: DateTimeOffset.UtcNow
        );

        await using (var seedContext = MakeInMemoryDbContext(dbName))
        {
            await seedContext.Patients.AddAsync(madePatient);
            await seedContext.Encounters.AddRangeAsync(madeEncounter1, madeEncounter2);
            await seedContext.SaveChangesAsync();
        }

        // Act
        await using var actContext = MakeInMemoryDbContext(dbName);
        var repo = new ClinicalEncounterRepository(actContext);
        var madeHandler = new GetPatientEncountersQueryHandler(repo);

        var madeQuery = new GetPatientEncountersQuery(madePatient.Id);
        var madeResults = await madeHandler.Handle(madeQuery, CancellationToken.None);

        // Assert
        madeResults.Should().HaveCount(2);
        madeResults[0].EncounterNumber.Should().Be("ENC-MULTI-002"); // Most recent first
        madeResults[1].EncounterNumber.Should().Be("ENC-MULTI-001");
    }
}
