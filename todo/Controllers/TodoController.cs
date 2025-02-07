using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Data;
using TodoApp.Models;

namespace TodoApp.Controllers
{
    public class TodoController : Controller
    {
        private readonly TodoContext _context;

        public TodoController(TodoContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var todos = await _context.TodoItems.ToListAsync();
            return View(todos);
        }

        [HttpPost]
        public async Task<IActionResult> Add(string task)
        {
            if (!string.IsNullOrEmpty(task))
            {
                _context.TodoItems.Add(new TodoItem { Task = task, IsCompleted = false });
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Complete(int id)
        {
            var todo = await _context.TodoItems.FindAsync(id);
            if (todo != null)
            {
                todo.IsCompleted = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var todo = await _context.TodoItems.FindAsync(id);
            if (todo != null)
            {
                _context.TodoItems.Remove(todo);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
