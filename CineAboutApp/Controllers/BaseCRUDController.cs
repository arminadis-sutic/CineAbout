using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace CineAboutApp.WebAPI.Controllers
{
    public class BaseCRUDController<T, TSearch, TInsert, TUpdate>(ICRUDService<T, TSearch, TInsert, TUpdate> service) : BaseController<T, TSearch>(service)
        where TSearch : BaseSearchObject
        where TInsert : class
        where TUpdate : class
        where T : class
    {
        [HttpPost]
        [Authorize]
        public async virtual Task<IActionResult> Create([FromBody] TInsert request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null");
            }
            var result = await service.CreateAsync(request, cancellationToken);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async virtual Task<IActionResult> Update([FromRoute] int id, [FromBody] TUpdate request, CancellationToken cancellationToken = default)
        {
            if (request == null)
            {
                return BadRequest("Request cannot be null");
            }
            var result = await service.UpdateAsync(id, request, cancellationToken);

            if (result == null)
            {
                return BadRequest();
            }

            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async virtual Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
        {
            var result = await service.DeleteAsync(id, cancellationToken);
            return result ? Ok() : NotFound();
        }
    }
}
