var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.MudBlazor_Mcp>("mudblazor-mcp");

builder.Build().Run();
