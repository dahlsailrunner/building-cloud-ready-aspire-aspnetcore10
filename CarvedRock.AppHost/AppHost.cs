var builder = DistributedApplication.CreateBuilder(args);

var db = builder.AddPostgres("db").AddDatabase("CarvedRockPostgres");

builder.AddProject<Projects.CarvedRock_Api>("api")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
