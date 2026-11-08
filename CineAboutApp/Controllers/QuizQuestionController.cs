using CineAboutApp.Interfaces.Controller;
using CineAboutApp.Models;
using CineAboutApp.Models.Requests.QuizQuestion;
using CineAboutApp.Models.Responses;
using CineAboutApp.Models.SearchObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CineAboutApp.WebAPI.Controllers
{
    public class QuizQuestionController(IQuizQuestionService service)
        : BaseCRUDController<QuizQuestionResponse, QuizQuestionSearchObject, QuizQuestionInsertOrUpdateRequest, QuizQuestionInsertOrUpdateRequest>(service)
    {
        [HttpGet]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Get([FromQuery] QuizQuestionSearchObject search, CancellationToken cancellationToken = default)
            => base.Get(search, cancellationToken);

        [HttpGet("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> GetById(int id, CancellationToken cancellationToken = default)
            => base.GetById(id, cancellationToken);

        [HttpPost]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Create([FromBody] QuizQuestionInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
            => base.Create(request, cancellationToken);

        [HttpPut("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Update([FromRoute] int id, [FromBody] QuizQuestionInsertOrUpdateRequest request, CancellationToken cancellationToken = default)
            => base.Update(id, request, cancellationToken);

        [HttpDelete("{id}")]
        [Authorize(Roles = AppRoles.Admin)]
        public override Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken = default)
            => base.Delete(id, cancellationToken);
    }
}
