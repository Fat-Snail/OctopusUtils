namespace OctopusEx.WebCore.Tests.Interceptors;

using Microsoft.EntityFrameworkCore;
using Moq;
using OctopusEx.WebCore.Interceptors;
using OctopusEx.WebCore.Interceptors.Auditing;
using System.Text.Json;

#region Test Helpers

/// <summary>
/// Simple test entity with a primary key and two string properties.
/// </summary>
public class TestAuditEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}

/// <summary>
/// Test DbContext that registers <see cref="TestAuditEntity"/> and <see cref="AuditLog"/>.
/// Uses non-generic <see cref="DbContextOptions"/> so that <c>AddInterceptors</c> can be
/// chained fluently without losing the builder type.
/// </summary>
public class TestAuditDbContext : DbContext
{
    public TestAuditDbContext(DbContextOptions options) : base(options) { }

    public DbSet<TestAuditEntity> TestEntities { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
}

#endregion

#region AuditEntryStore Tests

/// <summary>
/// Unit tests for the static <see cref="AuditEntryStore"/> class, which uses
/// <see cref="ConditionalWeakTable{DbContext, List{AuditEntry}}"/> for per-context storage.
/// </summary>
public class AuditEntryStoreTests
{
    /// <summary>
    /// Creates a minimal InMemory DbContext for use as a ConditionalWeakTable key.
    /// </summary>
    private static TestAuditDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestAuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestAuditDbContext(options);
    }

    /// <summary>
    /// Creates a list with a single <see cref="AuditEntry"/> backed by a real <see cref="EntityEntry"/>.
    /// </summary>
    private static List<AuditEntry> CreateSampleEntries(TestAuditDbContext context, string name = "Test")
    {
        var entity = new TestAuditEntity { Name = name };
        context.TestEntities.Add(entity);
        return new List<AuditEntry> { new AuditEntry(context.Entry(entity)) };
    }

    [Fact]
    public void SetAuditEntries_ThenGet_ReturnsSameList()
    {
        // Arrange
        using var context = CreateContext();
        var entries = CreateSampleEntries(context);

        // Act
        AuditEntryStore.SetAuditEntries(context, entries);
        var result = AuditEntryStore.GetAuditEntries(context);

        // Assert
        result.Should().BeSameAs(entries);
        result.Should().HaveCount(1);
    }

    [Fact]
    public void GetAuditEntries_NotSet_ReturnsEmptyList()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var result = AuditEntryStore.GetAuditEntries(context);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void ClearAuditEntries_RemovesEntries()
    {
        // Arrange
        using var context = CreateContext();
        var entries = CreateSampleEntries(context);
        AuditEntryStore.SetAuditEntries(context, entries);
        AuditEntryStore.GetAuditEntries(context).Should().HaveCount(1);

        // Act
        AuditEntryStore.ClearAuditEntries(context);

        // Assert
        AuditEntryStore.GetAuditEntries(context).Should().BeEmpty();
    }

    [Fact]
    public void SetAuditEntries_OverwriteExisting_ReplacesList()
    {
        // Arrange
        using var context = CreateContext();
        var firstList = CreateSampleEntries(context, "First");
        var secondList = CreateSampleEntries(context, "Second");

        AuditEntryStore.SetAuditEntries(context, firstList);
        AuditEntryStore.GetAuditEntries(context).Should().BeSameAs(firstList);

        // Act
        AuditEntryStore.SetAuditEntries(context, secondList);
        var result = AuditEntryStore.GetAuditEntries(context);

        // Assert
        result.Should().BeSameAs(secondList);
        result.Should().NotBeSameAs(firstList);
        result.Should().HaveCount(1);
    }
}

#endregion

#region AuditInterceptor Tests

/// <summary>
/// Integration tests for <see cref="AuditInterceptor"/> using EF Core InMemory provider.
/// Verifies that audit entries are collected during <c>SavingChanges</c> and persisted as
/// <see cref="AuditLog"/> records during <c>SavedChanges</c>.
/// </summary>
public class AuditInterceptorTests
{
    /// <summary>
    /// Creates a Moq-based <see cref="IAuditConfiguration"/> with sensible defaults.
    /// All parameters are optional and default to an enabled, permissive configuration.
    /// </summary>
    private static Mock<IAuditConfiguration> CreateMockConfig(
        bool enabled = true,
        IReadOnlyCollection<string>? globalIgnoredProperties = null,
        string? disabledDomain = null)
    {
        var mock = new Mock<IAuditConfiguration>();
        mock.Setup(c => c.Enabled).Returns(enabled);
        mock.Setup(c => c.GlobalIgnoredProperties)
            .Returns(globalIgnoredProperties ?? new List<string>());

        var defaultDomain = new DomainAuditConfiguration { Enabled = true };
        var disabledDomainConfig = new DomainAuditConfiguration { Enabled = false };

        if (disabledDomain is not null)
        {
            mock.Setup(c => c.GetDomainConfiguration(disabledDomain))
                .Returns(disabledDomainConfig);
            mock.Setup(c => c.GetDomainConfiguration(It.Is<string>(s => s != disabledDomain)))
                .Returns(defaultDomain);
        }
        else
        {
            mock.Setup(c => c.GetDomainConfiguration(It.IsAny<string>()))
                .Returns(defaultDomain);
        }

        mock.Setup(c => c.GetCurrentUser())
            .Returns(new AuditUserInfo
            {
                UserId = "test-user",
                UserName = "Test User",
                IpAddress = "127.0.0.1",
                UserAgent = "TestAgent"
            });

        return mock;
    }

    /// <summary>
    /// Creates a <see cref="TestAuditDbContext"/> with the InMemory provider and the
    /// <see cref="AuditInterceptor"/> registered.
    /// </summary>
    private static TestAuditDbContext CreateContext(IAuditConfiguration config)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestAuditDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString());
        // AddInterceptors returns non-generic DbContextOptionsBuilder, so call it
        // as a separate statement to preserve the generic Options property.
        optionsBuilder.AddInterceptors(new AuditInterceptor(config));
        var context = new TestAuditDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // B. AuditInterceptor Integration Tests
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SavingChanges_Disabled_DoesNotCollectAuditEntries()
    {
        // Arrange
        var mockConfig = CreateMockConfig(enabled: false);
        using var context = CreateContext(mockConfig.Object);
        context.TestEntities.Add(new TestAuditEntity { Name = "Test" });

        // Act
        context.SaveChanges();

        // Assert
        context.AuditLogs.Should().BeEmpty();
    }

    [Fact]
    public void SavingChanges_Enabled_CollectsAuditEntriesForAddedEntity()
    {
        // Arrange
        var mockConfig = CreateMockConfig();
        using var context = CreateContext(mockConfig.Object);
        context.TestEntities.Add(new TestAuditEntity { Name = "NewEntity", Description = "Desc" });

        // Act
        context.SaveChanges();

        // Assert
        context.AuditLogs.Should().HaveCount(1);
        var log = context.AuditLogs.First();
        log.Action.Should().Be("INSERT");
        log.TableName.Should().Be("TestAuditEntity");
    }

    [Fact]
    public void SavingChanges_Enabled_CollectsAuditEntriesForModifiedEntity()
    {
        // Arrange
        var mockConfig = CreateMockConfig();
        using var context = CreateContext(mockConfig.Object);
        var entity = new TestAuditEntity { Name = "Original", Description = "Desc" };
        context.TestEntities.Add(entity);
        context.SaveChanges(); // INSERT

        // Act
        entity.Name = "Modified";
        context.SaveChanges(); // UPDATE

        // Assert
        context.AuditLogs.Should().HaveCount(2);
        context.AuditLogs.Should().Contain(a => a.Action == "INSERT");
        var updateLog = context.AuditLogs.First(a => a.Action == "UPDATE");
        updateLog.TableName.Should().Be("TestAuditEntity");
    }

    [Fact]
    public void SavingChanges_Enabled_CollectsAuditEntriesForDeletedEntity()
    {
        // Arrange
        var mockConfig = CreateMockConfig();
        using var context = CreateContext(mockConfig.Object);
        var entity = new TestAuditEntity { Name = "ToDelete", Description = "Desc" };
        context.TestEntities.Add(entity);
        context.SaveChanges(); // INSERT

        // Act
        context.TestEntities.Remove(entity);
        context.SaveChanges(); // DELETE

        // Assert
        context.AuditLogs.Should().HaveCount(2);
        context.AuditLogs.Should().Contain(a => a.Action == "INSERT");
        var deleteLog = context.AuditLogs.First(a => a.Action == "DELETE");
        deleteLog.TableName.Should().Be("TestAuditEntity");
    }

    [Fact]
    public void SavingChanges_SkipsAuditLogEntity()
    {
        // Arrange — add an AuditLog entity directly; the interceptor should NOT
        // create an additional audit record for the AuditLog itself.
        var mockConfig = CreateMockConfig();
        using var context = CreateContext(mockConfig.Object);
        var directLog = new AuditLog
        {
            TableName = "ManualLog",
            Action = "MANUAL",
            EntityId = "42"
        };
        context.AuditLogs.Add(directLog);

        // Act
        context.SaveChanges();

        // Assert — only the manually-added AuditLog exists; no recursive audit record.
        context.AuditLogs.Should().HaveCount(1);
        context.AuditLogs.First().Action.Should().Be("MANUAL");
    }

    [Fact]
    public void SavingChanges_RespectsGlobalIgnoredProperties()
    {
        // Arrange — "Description" is globally ignored; "Name" is not.
        var mockConfig = CreateMockConfig(
            globalIgnoredProperties: new List<string> { "Description" });
        using var context = CreateContext(mockConfig.Object);
        context.TestEntities.Add(new TestAuditEntity
        {
            Name = "VisibleName",
            Description = "IgnoredDesc"
        });

        // Act
        context.SaveChanges();

        // Assert
        context.AuditLogs.Should().HaveCount(1);
        var log = context.AuditLogs.First();
        log.NewValues.Should().NotBeNull();
        var newValues = JsonDocument.Parse(log.NewValues!);
        var propertyNames = newValues.RootElement.EnumerateObject()
            .Select(p => p.Name)
            .ToList();
        propertyNames.Should().Contain("Name");
        propertyNames.Should().NotContain("Description");
    }

    [Fact]
    public void SavingChanges_DisabledDomain_SkipsEntity()
    {
        // Arrange — domain configuration for TestAuditEntity is disabled.
        var mockConfig = CreateMockConfig(disabledDomain: "TestAuditEntity");
        using var context = CreateContext(mockConfig.Object);
        context.TestEntities.Add(new TestAuditEntity { Name = "Test" });

        // Act
        context.SaveChanges();

        // Assert
        context.AuditLogs.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // C. BuildAuditLogs Indirect Verification
    // ─────────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AuditLog_ContainsCorrectTableName()
    {
        // Arrange
        var mockConfig = CreateMockConfig();
        using var context = CreateContext(mockConfig.Object);
        context.TestEntities.Add(new TestAuditEntity { Name = "Test" });

        // Act
        context.SaveChanges();

        // Assert
        context.AuditLogs.Should().HaveCount(1);
        var log = context.AuditLogs.First();
        log.TableName.Should().Be("TestAuditEntity");
        log.DomainName.Should().Be("TestAuditEntity");
    }

    [Fact]
    public void AuditLog_ContainsUserInfo()
    {
        // Arrange
        var mockConfig = CreateMockConfig();
        using var context = CreateContext(mockConfig.Object);
        context.TestEntities.Add(new TestAuditEntity { Name = "Test" });

        // Act
        context.SaveChanges();

        // Assert
        context.AuditLogs.Should().HaveCount(1);
        var log = context.AuditLogs.First();
        log.UserId.Should().Be("test-user");
        log.UserName.Should().Be("Test User");
        log.IpAddress.Should().Be("127.0.0.1");
        log.UserAgent.Should().Be("TestAgent");
    }

    [Fact]
    public void AuditLog_ForAddedEntity_NewValuesContainPropertyData()
    {
        // Arrange
        var mockConfig = CreateMockConfig();
        using var context = CreateContext(mockConfig.Object);
        context.TestEntities.Add(new TestAuditEntity
        {
            Name = "Alice",
            Description = "Created"
        });

        // Act
        context.SaveChanges();

        // Assert — NewValues should contain Name and Description (Id is key or temporary).
        var log = context.AuditLogs.First(a => a.Action == "INSERT");
        log.NewValues.Should().NotBeNull();
        var newValues = JsonDocument.Parse(log.NewValues!);
        newValues.RootElement.GetProperty("Name").GetString().Should().Be("Alice");
        newValues.RootElement.GetProperty("Description").GetString().Should().Be("Created");
    }

    [Fact]
    public void AuditLog_ForModifiedEntity_ChangesContainOldAndNewValues()
    {
        // Arrange
        var mockConfig = CreateMockConfig();
        using var context = CreateContext(mockConfig.Object);
        var entity = new TestAuditEntity { Name = "Before", Description = "Unchanged" };
        context.TestEntities.Add(entity);
        context.SaveChanges();

        // Act
        entity.Name = "After";
        context.SaveChanges();

        // Assert
        var updateLog = context.AuditLogs.First(a => a.Action == "UPDATE");
        updateLog.OldValues.Should().NotBeNull();
        updateLog.NewValues.Should().NotBeNull();

        var oldValues = JsonDocument.Parse(updateLog.OldValues!);
        var newValues = JsonDocument.Parse(updateLog.NewValues!);
        oldValues.RootElement.GetProperty("Name").GetString().Should().Be("Before");
        newValues.RootElement.GetProperty("Name").GetString().Should().Be("After");

        // Changes JSON should also reflect the modification
        updateLog.Changes.Should().NotBeNullOrEmpty();
        var changes = JsonDocument.Parse(updateLog.Changes);
        changes.RootElement.GetArrayLength().Should().BeGreaterThan(0);
        var changeEntry = changes.RootElement.EnumerateArray()
            .First(e => e.GetProperty("PropertyName").GetString() == "Name");
        changeEntry.GetProperty("OldValue").GetString().Should().Be("Before");
        changeEntry.GetProperty("NewValue").GetString().Should().Be("After");
    }

    [Fact]
    public void AuditLog_ForDeletedEntity_OldValuesContainPropertyData()
    {
        // Arrange
        var mockConfig = CreateMockConfig();
        using var context = CreateContext(mockConfig.Object);
        var entity = new TestAuditEntity { Name = "Doomed", Description = "Goodbye" };
        context.TestEntities.Add(entity);
        context.SaveChanges();

        // Act
        context.TestEntities.Remove(entity);
        context.SaveChanges();

        // Assert
        var deleteLog = context.AuditLogs.First(a => a.Action == "DELETE");
        deleteLog.OldValues.Should().NotBeNull();
        var oldValues = JsonDocument.Parse(deleteLog.OldValues!);
        oldValues.RootElement.GetProperty("Name").GetString().Should().Be("Doomed");
        oldValues.RootElement.GetProperty("Description").GetString().Should().Be("Goodbye");
        deleteLog.NewValues.Should().BeNull();
    }
}

#endregion
