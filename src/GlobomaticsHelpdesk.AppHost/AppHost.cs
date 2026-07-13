var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.GlobomaticsHelpdesk_Api>("api")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.GlobomaticsHelpdesk_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();

