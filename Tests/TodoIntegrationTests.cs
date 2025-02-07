using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using TodoApp.Data;
using TodoApp.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

public class TodoIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public TodoIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove existing database context
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<TodoContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add in-memory database for testing
                services.AddDbContext<TodoContext>(options =>
                {
                    options.UseInMemoryDatabase("TodoTestDb");
                });

                // Build service provider and seed data
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<TodoContext>();
                    db.Database.EnsureCreated();
                    SeedTestData(db);
                }
            });
        });

        _client = _factory.CreateClient();
    }

    private void SeedTestData(TodoContext db)
    {

        db.TodoItems.RemoveRange(db.TodoItems);
        db.SaveChanges();



        db.TodoItems.AddRange(
            new TodoItem { Id = 1, Task = "Task 1", IsCompleted = false },
            new TodoItem { Id = 2, Task = "Task 2", IsCompleted = true }
        );

        db.SaveChanges();
    }

    [Fact]
    public async Task GetTodos_ReturnsSeededTasks()
    {
        var response = await _client.GetAsync("/Todo/Index");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();

        Assert.Contains("Task 1", content);
        Assert.Contains("Task 2", content);
    }

    [Fact]
    public async Task AddTodo_CreatesNewTodo()
    {
        var formData = new Dictionary<string, string>
        {
            { "task", "New Task" }
        };
        var content = new FormUrlEncodedContent(formData);

        var response = await _client.PostAsync("/Todo/Add", content);
        response.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync("/Todo/Index");
        var responseBody = await getResponse.Content.ReadAsStringAsync();

        Assert.Contains("New Task", responseBody);
    }

    [Fact]
    public async Task CompleteTodo_MarksTodoAsCompleted()
    {
        var response = await _client.GetAsync("/Todo/Complete/1");
        response.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync("/Todo/Index");
        var responseBody = await getResponse.Content.ReadAsStringAsync();

        Assert.Contains("Task 1", responseBody);
    }

    [Fact]
    public async Task DeleteTodo_RemovesTodoSuccessfully()
    {
        var response = await _client.GetAsync("/Todo/Delete/1");
        response.EnsureSuccessStatusCode();

        var getResponse = await _client.GetAsync("/Todo/Index");
        var responseBody = await getResponse.Content.ReadAsStringAsync();

        Assert.DoesNotContain("Task 1", responseBody);
    }
}
