using Microsoft.EntityFrameworkCore;
using MV.ApplicationLayer.Services;
using MV.DomainLayer.Constants;
using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Exceptions;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.DBContext;
using Xunit;

namespace MV.ApplicationLayer.Tests;

/// <summary>
/// Validation của luồng "Thêm / sửa học sinh" trên app ghi âm: đồng ý của phụ huynh,
/// lịch tuần (giờ, trùng giờ, trùng với học sinh khác) và khoảng ngày của lịch.
/// </summary>
public class RecorderStudentValidationTests
{
    private const string Tutor = "tutor-1";

    [Fact]
    public async Task Create_WithoutParentConsent_IsRejectedAndNothingSaved()
    {
        await using var db = CreateContext();
        var request = Request("An");
        request.ParentConsent = false;

        var ex = await Assert.ThrowsAsync<RecorderNotReadyException>(
            () => Service(db).CreateStudentAsync(Tutor, request));

        Assert.Contains("đồng ý", ex.Message);
        Assert.Empty(db.RecorderStudents);
    }

    [Fact]
    public async Task Create_WithConsent_SavesTutorConfirmedWithCurrentVersion()
    {
        await using var db = CreateContext();

        var saved = await Service(db).CreateStudentAsync(Tutor, Request("An"));

        var s = Assert.Single(db.RecorderStudents);
        Assert.Equal(saved.StudentId, s.Studentid);
        Assert.Equal(RecorderConsentStatus.TutorConfirmed, s.Consentstatus);
        Assert.Equal(RecorderConsentText.CurrentVersion, s.Consentversion);
    }

    [Fact]
    public async Task Create_EndTimeNotAfterStart_IsRejected()
    {
        await using var db = CreateContext();

        await Assert.ThrowsAsync<RecorderNotReadyException>(() => Service(db).CreateStudentAsync(
            Tutor, Request("An", Slot(1, "18:00", "18:00"))));
        await Assert.ThrowsAsync<RecorderNotReadyException>(() => Service(db).CreateStudentAsync(
            Tutor, Request("An", Slot(1, "19:00", "18:00"))));
        Assert.Empty(db.RecorderStudents);
    }

    [Fact]
    public async Task Create_OverlappingSlotsInOwnSchedule_IsRejected()
    {
        await using var db = CreateContext();

        var ex = await Assert.ThrowsAsync<RecorderNotReadyException>(() => Service(db).CreateStudentAsync(
            Tutor, Request("An", Slot(2, "18:00", "19:30"), Slot(2, "19:00", "20:00"))));

        Assert.Contains("trùng giờ", ex.Message);
        Assert.Empty(db.RecorderStudents);
    }

    [Fact]
    public async Task Create_BackToBackSlots_AreAllowed()
    {
        await using var db = CreateContext();

        await Service(db).CreateStudentAsync(
            Tutor, Request("An", Slot(2, "18:00", "19:00"), Slot(2, "19:00", "20:00")));

        Assert.Single(db.RecorderStudents);
    }

    [Fact]
    public async Task Create_ConflictWithAnotherStudentOfSameTutor_IsRejected()
    {
        await using var db = CreateContext();
        var service = Service(db);
        await service.CreateStudentAsync(Tutor, Request("An", Slot(3, "18:00", "19:30")));

        var ex = await Assert.ThrowsAsync<RecorderNotReadyException>(
            () => service.CreateStudentAsync(Tutor, Request("Bình", Slot(3, "19:00", "20:00"))));

        Assert.Contains("An", ex.Message);
        Assert.Single(db.RecorderStudents);
    }

    [Fact]
    public async Task Create_SameSlotAsAnotherTutorsStudent_IsAllowed()
    {
        await using var db = CreateContext();
        var service = Service(db);
        await service.CreateStudentAsync("tutor-other", Request("An", Slot(3, "18:00", "19:30")));

        await service.CreateStudentAsync(Tutor, Request("Bình", Slot(3, "18:00", "19:30")));

        Assert.Equal(2, db.RecorderStudents.Count());
    }

    [Fact]
    public async Task Create_SameSlotOnDifferentDay_IsAllowed()
    {
        await using var db = CreateContext();
        var service = Service(db);
        await service.CreateStudentAsync(Tutor, Request("An", Slot(3, "18:00", "19:30")));

        await service.CreateStudentAsync(Tutor, Request("Bình", Slot(4, "18:00", "19:30")));

        Assert.Equal(2, db.RecorderStudents.Count());
    }

    [Fact]
    public async Task Create_SameSlotAsLegacyArchivedStudent_IsAllowed()
    {
        await using var db = CreateContext();
        var service = Service(db);
        var an = await service.CreateStudentAsync(Tutor, Request("An", Slot(3, "18:00", "19:30")));
        // Học sinh đã ẩn từ bản cũ (tính năng ẩn đã bỏ, dữ liệu cũ vẫn còn Archivedat).
        (await db.RecorderStudents.SingleAsync(s => s.Studentid == an.StudentId)).Archivedat = TimeZoneHelper.UtcNow;
        await db.SaveChangesAsync();

        await service.CreateStudentAsync(Tutor, Request("Bình", Slot(3, "18:00", "19:30")));

        Assert.Equal(2, db.RecorderStudents.Count());
    }

    [Fact]
    public async Task Create_SameSlotButDateRangesDoNotOverlap_IsAllowed()
    {
        await using var db = CreateContext();
        var service = Service(db);
        var today = Today();
        await service.CreateStudentAsync(Tutor,
            Request("An", today, today.AddMonths(1), Slot(3, "18:00", "19:30")));

        await service.CreateStudentAsync(Tutor,
            Request("Bình", today.AddMonths(1).AddDays(1), today.AddMonths(3), Slot(3, "18:00", "19:30")));

        Assert.Equal(2, db.RecorderStudents.Count());
    }

    [Fact]
    public async Task Create_EndDateBeforeStartDate_IsRejectedAndNothingSaved()
    {
        await using var db = CreateContext();
        var today = Today();

        await Assert.ThrowsAsync<RecorderNotReadyException>(() => Service(db).CreateStudentAsync(Tutor,
            Request("An", today.AddDays(10), today.AddDays(5), Slot(1, "18:00", "19:00"))));

        Assert.Empty(db.RecorderStudents);
    }

    [Fact]
    public async Task Create_ScheduleLongerThanSixMonths_IsRejectedAndNothingSaved()
    {
        await using var db = CreateContext();
        var today = Today();

        await Assert.ThrowsAsync<RecorderNotReadyException>(() => Service(db).CreateStudentAsync(Tutor,
            Request("An", today, today.AddMonths(6).AddDays(1), Slot(1, "18:00", "19:00"))));

        Assert.Empty(db.RecorderStudents);
    }

    [Fact]
    public async Task Update_KeepingOwnSlot_DoesNotConflictWithItself()
    {
        await using var db = CreateContext();
        var service = Service(db);
        var an = await service.CreateStudentAsync(Tutor, Request("An", Slot(3, "18:00", "19:30")));

        await service.UpdateStudentAsync(an.StudentId, Tutor, Request("An (đổi tên)", Slot(3, "18:00", "19:30")));

        Assert.Equal("An (đổi tên)", Assert.Single(db.RecorderStudents).Fullname);
    }

    [Fact]
    public async Task Update_MovingIntoAnotherStudentsSlot_IsRejected()
    {
        await using var db = CreateContext();
        var service = Service(db);
        await service.CreateStudentAsync(Tutor, Request("An", Slot(3, "18:00", "19:30")));
        var binh = await service.CreateStudentAsync(Tutor, Request("Bình", Slot(4, "18:00", "19:30")));

        await Assert.ThrowsAsync<RecorderNotReadyException>(() => service.UpdateStudentAsync(
            binh.StudentId, Tutor, Request("Bình", Slot(3, "19:00", "20:00"))));
    }

    [Fact]
    public async Task Update_AnotherTutorsStudent_IsNotFound()
    {
        await using var db = CreateContext();
        var service = Service(db);
        var an = await service.CreateStudentAsync("tutor-other", Request("An"));

        await Assert.ThrowsAnyAsync<Exception>(
            () => service.UpdateStudentAsync(an.StudentId, Tutor, Request("An")));
        Assert.Equal("An", Assert.Single(db.RecorderStudents).Fullname);
    }

    private static RecorderService Service(AgoraDbContext db) => new(db, new FakeStorage());

    private static DateOnly Today() =>
        DateOnly.FromDateTime(TimeZoneHelper.ToVietnamTime(TimeZoneHelper.UtcNow));

    private static RecorderScheduleSlot Slot(int day, string start, string end) =>
        new() { DayOfWeek = day, Start = start, End = end };

    private static RecorderStudentRequest Request(string name, params RecorderScheduleSlot[] slots) =>
        Request(name, null, slots.Length == 0 ? null : Today().AddMonths(2), slots);

    private static RecorderStudentRequest Request(
        string name, DateOnly? from, DateOnly? until, params RecorderScheduleSlot[] slots) => new()
    {
        FullName = name,
        Grade = 7,
        Subject = "Toán",
        ParentName = "Chị Hương",
        ParentPhone = "0901234567",
        ParentConsent = true,
        ConsentVersion = RecorderConsentText.CurrentVersion,
        Schedule = slots.Length == 0 ? null : slots.ToList(),
        ScheduleFrom = slots.Length == 0 ? null : from,
        ScheduleUntil = slots.Length == 0 ? null : until
    };

    private static AgoraDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AgoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new RecorderTestDbContext(options);
    }

    private sealed class RecorderTestDbContext(DbContextOptions<AgoraDbContext> options)
        : AgoraDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<QuestionBank>().Ignore(question => question.Embedding);
            modelBuilder.Entity<TutoraKbChunk>().Ignore(chunk => chunk.Embedding);
        }
    }
}
