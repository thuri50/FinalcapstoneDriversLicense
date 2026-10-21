using System.Security.Claims;
using FinalcapstoneDriversLicense.Dtos.Application;
using FinalcapstoneDriversLicense.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DriversLicenseApi.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _service;

    public ApplicationsController(IApplicationService service) => _service = service;

    private string CurrentUserId =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User identifier was not found in token.");

    [HttpPost]
    [Authorize(Roles = "Applicant")]
    [ProducesResponseType(typeof(ApplicationResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApplicationResponse>> Create(CreateApplicationRequest request)
    {
        var result = await _service.CreateAsync(Guid.Parse(CurrentUserId), request); 
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("my-application")]
    [Authorize(Roles = "Applicant")]
    public async Task<ActionResult<ApplicationResponse>> GetMine()
        => Ok(await _service.GetMineAsync(Guid.Parse(CurrentUserId)));

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Applicant,Staff,Admin")]
    public async Task<ActionResult<ApplicationResponse>> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (User.IsInRole("Applicant") && result.ApplicantEmail != User.FindFirstValue(ClaimTypes.Email))
            return Forbid();
        return Ok(result);
    }

    [HttpGet]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<IReadOnlyList<ApplicationResponse>>> GetAll([FromQuery] string? status)
        => Ok(await _service.GetAllAsync(status));

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApplicationResponse>> UpdateStatus(int id, UpdateApplicationStatusRequest request)
        => Ok(await _service.UpdateStatusAsync(id, Guid.Parse(CurrentUserId), request));

    [HttpPut("{id:int}/collect")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApplicationResponse>> Collect(int id)
        => Ok(await _service.CollectAsync(id, Guid.Parse(CurrentUserId)));

    [HttpGet("{id:int}/history")]
    [Authorize(Roles = "Applicant,Staff,Admin")]
    public async Task<ActionResult<IReadOnlyList<StatusHistoryResponse>>> GetHistory(int id)
    {
        // Applicants can only see history for their own application.
        var application = await _service.GetByIdAsync(id);
        if (User.IsInRole("Applicant") && application.ApplicantEmail != User.FindFirstValue(ClaimTypes.Email))
            return Forbid();

        return Ok(await _service.GetHistoryAsync(id));
    }
}
