using LifeManagementApp.Models;
using LifeManagementApp.Services;
using LifeManagementApp.ViewModels;

namespace LifeManagementApp.Tests;

public class TaskFlowTests
{
    [Fact]
    public async Task AddRejectsBlankThenSavesTrimmedTitleAndReturns()
    {
        var repository = new FakeRepository(); var navigation = new FakeNavigation();
        var vm = new AddTodoViewModel(repository, navigation) { Title = "   " };
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.NotEmpty(vm.ErrorMessage); Assert.Null(repository.Saved); Assert.False(navigation.WentBack);
        vm.Title = "  Study  ";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("Study", repository.Saved!.Title); Assert.True(navigation.WentBack);
    }

    [Fact]
    public async Task EditLoadsByIdentifierAndSavesChanges()
    {
        var repository = new FakeRepository { Saved = new TodoItem { Id = 42, Title = "Old" } };
        var navigation = new FakeNavigation(); var vm = new EditTodoViewModel(repository, navigation);
        await vm.LoadAsync(42); Assert.Equal("Old", vm.Title);
        vm.Title = "Updated"; await vm.SaveCommand.ExecuteAsync(null);
        Assert.Equal("Updated", repository.Saved!.Title); Assert.True(navigation.WentBack);
    }

    [Fact]
    public async Task ListRefreshOrdersPriorityThenDate()
    {
        var repository = new FakeRepository { Items = [new() { Title = "Later", Priority = 5, DueDate = new(2026, 12, 3) }, new() { Title = "Low", Priority = 1 }, new() { Title = "Soon", Priority = 5, DueDate = new(2026, 12, 1) }] };
        var vm = new TodoListViewModel(repository, new FakeNavigation());
        await vm.RefreshAsync(); Assert.Equal(new[] { "Soon", "Later", "Low" }, vm.Items.Select(x => x.Title));
    }

    private sealed class FakeNavigation : INavigationService
    {
        public bool WentBack { get; private set; }
        public Task GoBackAsync() { WentBack = true; return Task.CompletedTask; }
        public Task GoToAddTodoAsync() => Task.CompletedTask;
        public Task GoToEditTodoAsync(int id) => Task.CompletedTask;
    }

    private sealed class FakeRepository : ITodoRepository
    {
        public TodoItem? Saved { get; set; }
        public List<TodoItem> Items { get; set; } = [];
        public Task InitializeAsync() => Task.CompletedTask;
        public Task<IReadOnlyList<TodoItem>> GetAllAsync() => Task.FromResult<IReadOnlyList<TodoItem>>(Items);
        public Task<TodoItem?> GetByIdAsync(int id) => Task.FromResult(Saved?.Id == id ? Saved : null);
        public Task AddAsync(TodoItem item) { Saved = item; return Task.CompletedTask; }
        public Task UpdateAsync(TodoItem item) { Saved = item; return Task.CompletedTask; }
        public Task DeleteAsync(int id) => Task.CompletedTask;
        public Task<IReadOnlyList<Category>> GetCategoriesAsync() => Task.FromResult<IReadOnlyList<Category>>([]);
        public Task AddCategoryAsync(Category category) => Task.CompletedTask;
    }
}
