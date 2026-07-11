using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OctopusEx.Sample.WebApi.Models;
using OctopusEx.WebCore.MultiTenancy;

namespace OctopusEx.Sample.WebApi.Controllers;

/// <summary>
/// 待办事项 API 控制器。演示多租户自动隔离 + JWT 认证。
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TodosController : ControllerBase
{
    private readonly TodoService _service;
    private readonly ICurrentTenant _currentTenant;

    public TodosController(TodoService service, ICurrentTenant currentTenant)
    {
        _service = service;
        _currentTenant = currentTenant;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var item = await _service.GetByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTodoRequest request)
    {
        var item = await _service.CreateAsync(request, _currentTenant.TenantId);
        return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTodoRequest request)
    {
        var item = await _service.UpdateAsync(id, request);
        if (item == null) return NotFound();
        return Ok(item);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
