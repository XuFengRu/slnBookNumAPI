using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookNumAPI.Models;

namespace BookNumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivityCategoryController : ControllerBase
    {
        private readonly BookNumApiContext _context;

        public ActivityCategoryController(BookNumApiContext context)
        {
            _context = context;
        }

        // 取得所有活動分類
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _context.ActivityCategories
                .Select(c => new
                {
                    c.CategoryId,
                    c.CategoryName
                })
                .ToListAsync();

            return Ok(categories);
        }
    }
}