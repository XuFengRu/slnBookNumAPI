using Microsoft.AspNetCore.Mvc;
using BookNumAPI.Models;
using System.Linq;

namespace BookNumAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceController : ControllerBase
    {
        private readonly BookNumApiContext _db;

        public ServiceController(BookNumApiContext db)
        {
            _db = db;
        }

        // GET: api/Service
        [HttpGet]
        public IActionResult GetServices()
        {
            var services = _db.Services
                              .Where(s => s.IsActive) // 只取啟用的方案
                              .ToList();
            return Ok(services);
        }

        // GET: api/Service/5
        [HttpGet("{id}")]
        public IActionResult GetService(int id)
        {
            var service = _db.Services.Find(id);
            if (service == null)
            {
                return NotFound();
            }
            return Ok(service);
        }

        // POST: api/Service
        [HttpPost]
        public IActionResult CreateService([FromBody] Service service)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _db.Services.Add(service);
            _db.SaveChanges();
            return CreatedAtAction(nameof(GetService), new { id = service.ServiceId }, service);
        }

        // PUT: api/Service/5
        [HttpPut("{id}")]
        public IActionResult UpdateService(int id, [FromBody] Service service)
        {
            if (id != service.ServiceId)
            {
                return BadRequest("Service ID mismatch");
            }

            var existing = _db.Services.Find(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.ServiceName = service.ServiceName;
            existing.Hour = service.Hour;
            existing.Price = service.Price;
            existing.Description = service.Description;
            existing.IsActive = service.IsActive;

            _db.SaveChanges();
            return NoContent();
        }

        // DELETE: api/Service/5
        [HttpDelete("{id}")]
        public IActionResult DeleteService(int id)
        {
            var service = _db.Services.Find(id);
            if (service == null)
            {
                return NotFound();
            }

            _db.Services.Remove(service);
            _db.SaveChanges();
            return NoContent();
        }
    }
}