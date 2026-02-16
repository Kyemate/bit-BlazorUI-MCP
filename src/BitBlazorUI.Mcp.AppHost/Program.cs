var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BitBlazorUI_Mcp>("bitblazorui-mcp");

builder.Build().Run();
