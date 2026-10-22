using Xunit;
using Xunit.Abstractions;
using BCrypt.Net;

namespace SupplierSelectionDSS.Tests.Services;

public class BCryptGeneratorTest
{
    private readonly ITestOutputHelper _output;

    public BCryptGeneratorTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void GenerateDemoPasswordHashes()
    {
        var admin = BCrypt.Net.BCrypt.HashPassword("Admin@123456");
        var staff = BCrypt.Net.BCrypt.HashPassword("Staff@123456");
        var expert = BCrypt.Net.BCrypt.HashPassword("Expert@123456");
        var manager = BCrypt.Net.BCrypt.HashPassword("Manager@123456");

        _output.WriteLine($"ADMIN_HASH: {admin}");
        _output.WriteLine($"STAFF_HASH: {staff}");
        _output.WriteLine($"EXPERT_HASH: {expert}");
        _output.WriteLine($"MANAGER_HASH: {manager}");

        Assert.True(BCrypt.Net.BCrypt.Verify("Admin@123456", admin));
        Assert.True(BCrypt.Net.BCrypt.Verify("Staff@123456", staff));
        Assert.True(BCrypt.Net.BCrypt.Verify("Expert@123456", expert));
        Assert.True(BCrypt.Net.BCrypt.Verify("Manager@123456", manager));
    }
}
