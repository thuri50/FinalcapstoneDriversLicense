using FinalcapstoneDriversLicense.Data;
using FinalcapstoneDriversLicense.Dtos.Application;
using FinalcapstoneDriversLicense.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinalcapstoneDriversLicense.Services;

public class ApplicationService : IApplicationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ApplicationService> _logger;

    public ApplicationService(ApplicationDbContext db, ILogger<ApplicationService> logger)
    {
        _db = db;
        _logger = logger;
    }
    public async Task<ApplicationResponse> CreateAsync(Guid userId, CreateApplicationRequest request)
    {
        if (request.DateOfBirth.Date >= DateTime.UtcNow.Date)
            throw new ArgumentException("Date of birth must be in the past.");

        var hasActiveApplication = await _db.LicenseApplications.AnyAsync(a =>
            a.UserId == userId && a.Status != ApplicationStatus.Collected && a.Status != ApplicationStatus.Rejected);
        if (hasActiveApplication)
            throw new InvalidOperationException("You already have an active license application.");

        var now = DateTime.UtcNow;
        var application = new LicenseApplication
        {
            ApplicationNumber = $"DL-{now:yyyy}-{Guid.NewGuid().ToString("N")[..8]}",
            UserId = userId,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            LicenseType = request.LicenseType.Trim(),
            Status = ApplicationStatus.Submitted,
            SubmittedDate = now,
            LastUpdated = now
        };

        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            Status = ApplicationStatus.Submitted,
            Comment = "Application submitted",
            DateChanged = now,
            ChangedByUserId = userId // Update this as well if ChangedByUserId is a Guid
        });

        _db.LicenseApplications.Add(application);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Application {ApplicationNumber} created by user {UserId}", application.ApplicationNumber, userId);
        return await BuildResponseAsync(application.Id);
    }

    public async Task<ApplicationResponse> GetMineAsync(Guid userId)
    {
        var application = await _db.LicenseApplications
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.SubmittedDate)
            .FirstOrDefaultAsync();

        if (application == null)
            throw new KeyNotFoundException("No application was found for this user.");

        return Map(application, null, null);
    }

   

    public async Task<ApplicationResponse> GetByIdAsync(int id)
    {
        var application = await _db.LicenseApplications
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (application is null)
            throw new KeyNotFoundException("Application was not found.");

        return Map(application, application.User?.FullName, application.User?.Email);
    }

    public async Task<IReadOnlyList<ApplicationResponse>> GetAllAsync(string? status)
    {
        var query = _db.LicenseApplications.AsNoTracking().Include(a => a.User).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!ApplicationStatus.All.Contains(status, StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException("Invalid application status filter.");
            query = query.Where(a => a.Status.ToLower() == status.ToLower());
        }

        var applications = await query.OrderByDescending(a => a.SubmittedDate).ToListAsync();
        return applications.Select(a => Map(a, a.User?.FullName, a.User?.Email)).ToList();
    }

    public async Task<ApplicationResponse> UpdateStatusAsync(int id, string staffUserId, UpdateApplicationStatusRequest request)
    {
        var application = await _db.LicenseApplications.FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
            throw new KeyNotFoundException("Application was not found.");

        var requestedStatus = ApplicationStatus.All.FirstOrDefault(s =>
            s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (requestedStatus is null)
            throw new ArgumentException("Invalid application status.");

        if (requestedStatus == ApplicationStatus.Rejected && string.IsNullOrWhiteSpace(request.RejectionReason))
            throw new ArgumentException("A rejection reason is required when rejecting an application.");

        if (application.Status == ApplicationStatus.Collected)
            throw new InvalidOperationException("A collected application cannot be changed.");

        if (requestedStatus == ApplicationStatus.Collected)
            throw new InvalidOperationException("Use the collect endpoint to record collection.");

        application.Status = requestedStatus;
        application.LastUpdated = DateTime.UtcNow;
        application.RejectionReason = requestedStatus == ApplicationStatus.Rejected
            ? request.RejectionReason!.Trim()
            : null;

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            Status = requestedStatus,
            Comment = request.Comment ?? application.RejectionReason ?? $"Status changed to {requestedStatus}",
            DateChanged = application.LastUpdated,
            ChangedByUserId = Guid.Parse(staffUserId)
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Application {ApplicationId} status changed to {Status} by {StaffUserId}",
            id, requestedStatus, staffUserId);
        return await BuildResponseAsync(id);
    }

    public async Task<ApplicationResponse> CollectAsync(int id, string staffUserId)
    {
        var application = await _db.LicenseApplications.FirstOrDefaultAsync(a => a.Id == id);
        if (application is null)
            throw new KeyNotFoundException("Application was not found.");
        if (application.Status != ApplicationStatus.ReadyForCollection)
            throw new InvalidOperationException("Only applications marked Ready for Collection can be collected.");

        application.Status = ApplicationStatus.Collected;
        application.CollectionDate = DateTime.UtcNow;
        application.LastUpdated = application.CollectionDate.Value;

        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            Status = ApplicationStatus.Collected,
            Comment = "License collected",
            DateChanged = application.LastUpdated,
            ChangedByUserId = Guid.Parse(staffUserId)
        });

        await _db.SaveChangesAsync();
        _logger.LogInformation("Application {ApplicationId} marked collected by {StaffUserId}", id, staffUserId);
        return await BuildResponseAsync(id);
    }

    public async Task<IReadOnlyList<StatusHistoryResponse>> GetHistoryAsync(int id)
    {
        var exists = await _db.LicenseApplications.AnyAsync(a => a.Id == id);
        if (!exists)
            throw new KeyNotFoundException("Application was not found.");

        return await _db.ApplicationStatusHistories
            .Where(h => h.ApplicationId == id)
            .OrderBy(h => h.DateChanged)
            .Select(h => new StatusHistoryResponse
            {
                Id = h.Id,
                Status = h.Status,
                Comment = h.Comment,
                DateChanged = h.DateChanged
            }).ToListAsync();
    }

    private async Task<ApplicationResponse> BuildResponseAsync(int id)
    {
        var application = await _db.LicenseApplications
            .Include(a => a.User)
            .FirstAsync(a => a.Id == id);
        return Map(application, application.User?.FullName, application.User?.Email);
    }

    private static ApplicationResponse Map(LicenseApplication a, string? name, string? email) => new()
    {
        Id = a.Id,
        ApplicationNumber = a.ApplicationNumber,
        DateOfBirth = a.DateOfBirth,
        Address = a.Address,
        PhoneNumber = a.PhoneNumber,
        LicenseType = a.LicenseType,
        Status = a.Status,
        SubmittedDate = a.SubmittedDate,
        LastUpdated = a.LastUpdated,
        CollectionDate = a.CollectionDate,
        RejectionReason = a.RejectionReason,
        ApplicantName = name,
        ApplicantEmail = email,
        TrackingMessage = a.Status == ApplicationStatus.ReadyForCollection
            ? "Your driver's license is ready for collection."
            : $"Your application is currently {a.Status}."
    };

     Task<ApplicationResponse> IApplicationService.UpdateStatusAsync(int id, Guid staffUserId, UpdateApplicationStatusRequest request)
    {

       var application= _db.ApplicationStatusHistories.FirstOrDefault(a => a.ApplicationId == id);
        if (application is null)
            throw new KeyNotFoundException("Application was not found.");
        var requestedStatus = ApplicationStatus.All.FirstOrDefault(s =>
            s.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (requestedStatus is null)
            throw new ArgumentException("Invalid application status.");
        if (requestedStatus == ApplicationStatus.Rejected && string.IsNullOrWhiteSpace(request.RejectionReason))
            throw new ArgumentException("A rejection reason is required when rejecting an application.");
        if (application.Status == ApplicationStatus.Collected)
            throw new InvalidOperationException("A collected application cannot be changed.");
        if (requestedStatus == ApplicationStatus.Collected)
            throw new InvalidOperationException("Use the collect endpoint to record collection.");
        application.Status = requestedStatus;
        application.LastUpdated = DateTime.UtcNow;
        application.RejectionReason = requestedStatus == ApplicationStatus.Rejected
            ? request.RejectionReason!.Trim()
            : null;
        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            Status = requestedStatus,
            Comment = request.Comment ?? application.RejectionReason ?? $"Status changed to {requestedStatus}",
            DateChanged = application.LastUpdated,
            ChangedByUserId = staffUserId
        });
        _db.SaveChanges();
        _logger.LogInformation("Application {ApplicationId} status changed to {Status} by {StaffUserId}",
            id, requestedStatus, staffUserId);
        return BuildResponseAsync(id);

    }

    Task<ApplicationResponse> IApplicationService.CollectAsync(int id, Guid staffUserId)
    {
       var application = _db.ApplicationStatusHistories.FirstOrDefault(a => a.ApplicationId == id);
        if (application is null)
            throw new KeyNotFoundException("Application was not found.");
        if (application.Status != ApplicationStatus.ReadyForCollection)
            throw new InvalidOperationException("Only applications marked Ready for Collection can be collected.");
        application.Status = ApplicationStatus.Collected;
        application.CollectionDate = DateTime.UtcNow;
        application.LastUpdated = application.CollectionDate;
        _db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            Status = ApplicationStatus.Collected,
            Comment = "License collected",
            DateChanged = application.LastUpdated,
            ChangedByUserId = staffUserId
        });
        _db.SaveChanges();
        _logger.LogInformation("Application {ApplicationId} marked collected by {StaffUserId}", id, staffUserId);
        return BuildResponseAsync(id);
    }

    Task IApplicationService.UpdateStatusAsync(int id, ApplicationStatus requestedStatus, Guid staffUserId, string? comment)
    {

       throw new NotImplementedException("This method is not implemented. Use UpdateStatusAsync with UpdateApplicationStatusRequest instead.");

    }
}

    

