using Bogus;
using Microsoft.AspNetCore.OpenApi;


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var api = app.MapGroup("/api");
var users = api.MapGroup("/users");

var usersDataGenerator = new Faker<LastSeenUser>()
  .CustomInstantiator(f => new LastSeenUser(
    f.Random.Guid().ToString(),
    f.Person.UserName,
    f.Person.FirstName,
    f.Person.LastName,
    f.Date.PastOffset(),
    f.Date.RecentOffset(),
    false
   ));

const int userCount = 217;
var userData = usersDataGenerator.GenerateLazy(userCount).ToList();
_ = UpdateUsersAsync(userData);

users.MapGet("/lastSeen", (int offset) => new Result<LastSeenUser>(userCount, userData.Skip(offset).Take(20)));

app.UseSwagger();
app.UseSwaggerUI();

app.Run();

async Task UpdateUsersAsync(List<LastSeenUser> data)
{
  while (true)
  {
    UpdateUsers(data);
    await Task.Delay(20_000);
  }
}

void UpdateUsers(List<LastSeenUser> data)
{
  for (int i = 0; i < data.Count; i++)
  {
    var r = Random.Shared.NextSingle();
    if (!data[i].IsOnline)
    {
      if (r < 0.05)
      {
        data[i] = data[i] with { LastSeenDate = null, IsOnline = true };
      }
    }
    else
    {
      if (r < 0.15)
      {
        data[i] = data[i] with { LastSeenDate = DateTimeOffset.Now, IsOnline = false };
      }
    }
  }
}

record Result<T>(int Total, IEnumerable<T> Data);

record LastSeenUser(
  string UserId, string Nickname, string FirstName, string LastName, 
  DateTimeOffset RegistrationDate, DateTimeOffset? LastSeenDate, bool IsOnline);