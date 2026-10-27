using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace SupplierSelectionDSS.Tools.DemoSeeder;

public class DemoHostEnvironment : IWebHostEnvironment
{
    public DemoHostEnvironment(string contentRootPath)
    {
        ContentRootPath = contentRootPath;
        WebRootPath = Path.Combine(contentRootPath, "wwwroot");
        EnvironmentName = "Development";
        ApplicationName = "SupplierSelectionDSS";
        ContentRootFileProvider = new PhysicalFileProvider(Directory.Exists(contentRootPath) ? contentRootPath : AppContext.BaseDirectory);
        WebRootFileProvider = new NullFileProvider();
    }

    public string EnvironmentName { get; set; }
    public string ApplicationName { get; set; }
    public string WebRootPath { get; set; }
    public IFileProvider WebRootFileProvider { get; set; }
    public string ContentRootPath { get; set; }
    public IFileProvider ContentRootFileProvider { get; set; }
}
