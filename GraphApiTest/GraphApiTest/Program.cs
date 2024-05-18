using Azure.Identity;
using Microsoft.Graph;

var groupDisplayName = "MyTestGRoup";

using var graphClient = new GraphServiceClient(
    new ClientSecretCredential(tenantId, clientId, clientSecret),
    [ "https://graph.microsoft.com/.default" ],
    baseUrl: null);

var getGroupRequest = await graphClient.Groups.GetAsync((config) =>
{
    config.QueryParameters.Filter = $"displayName eq '{groupDisplayName}'";
    config.QueryParameters.Select = [ "id" ];
});

var groupId = getGroupRequest!.Value!.Single().Id!;

Console.WriteLine($"Found group with displayName: {groupDisplayName} - it has id: {groupId}.");

string[] shouldInclude = [ emailAddress ];

var getUsersResponse = await graphClient.Users.GetAsync(config =>
{
    config.QueryParameters.Top = 10;
    config.QueryParameters.Filter = $"mail in ('{string.Join("','", shouldInclude)}')";
    config.QueryParameters.Select = [ "id", "mail" ];
});

var usersToInclude = getUsersResponse.Value.Select(u => new { UserId = u.Id, Mail = u.Mail }).ToArray();

async Task ShowCurrentUsers()
{
    var listMembers = await graphClient.Groups[groupId].Members.GraphUser.GetAsync(config =>
    {
        config.QueryParameters.Top = 10;
        config.QueryParameters.Select = ["id", "mail"];
    });

    Console.WriteLine(listMembers.Value!.Aggregate("Found members: ",
        (s, u) => s = $"{s} {{ Id: {u.Id}, Mail: {u.Mail} }}"));
}

await ShowCurrentUsers();

await graphClient.Groups[groupId].PatchAsync(new Microsoft.Graph.Models.Group
{
    AdditionalData = new Dictionary<string, object>
    {
        { "members@odata.bind", new List<string> { $"https://graph.microsoft.com/v1.0/directoryObjects/{usersToInclude.First().UserId}" } }
    }
});

await ShowCurrentUsers();

await graphClient.Groups[groupId].Members[usersToInclude.First().UserId].Ref.DeleteAsync();

await ShowCurrentUsers();