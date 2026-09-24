using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MV.ApplicationLayer.Services;
using MV.DomainLayer.Configuration;
using MV.DomainLayer.Constants;
using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Exceptions;
using MV.DomainLayer.Helpers;
using MV.InfrastructureLayer.DBContext;
using Xunit;

namespace MV.ApplicationLayer.Tests;

/// <summary>
/// Những gì Google Play cần trước khi gửi duyệt: xoá hẳn dữ liệu học sinh, báo nội dung AI sai,
/// và tài khoản demo cho người duyệt không gửi Zalo thật.
/// </summary>
public class RecorderPlayReadinessTests
{
    private const string Tutor = "tutor-1";
    private const string DemoTutor = "tutor-demo";

    // ── Xoá vĩnh viễn học sinh ─────────────────────────────────────────────

    [Fact]
    public async Task DeleteStudentPermanently_RemovesStudentLessonsAndFiles()
    {
        await using var db = CreateContext();
        var storage = new FakeStorage();
        var student = AddStudent(db, Tutor);
        var recorded = AddLesson(db, Tutor, student.Studentid, SessionRecordingStatus.Sent);
        recorded.Storagekey = "app/tutor-1/lesson-a";
        recorded.Transcriptkey = "lesson-transcripts/lesson-a.json";
        AddLesson(db, Tutor, student.Studentid, SessionRecordingStatus.Scheduled);
        var other = AddStudent(db, Tutor, "Bình");
        AddLesson(db, Tutor, other.Studentid, SessionRecordingStatus.Sent);
        await db.SaveChangesAsync();

        await new RecorderService(db, storage).DeleteStudentPermanentlyAsync(student.Studentid, Tutor);

        Assert.Equal("Bình", Assert.Single(db.RecorderStudents).Fullname);
        Assert.All(db.RecorderLessons, l => Assert.Equal(other.Studentid, l.Studentid));
        Assert.Equal(["app/tutor-1/lesson-a"], storage.DeletedPrefixes);
        Assert.Equal(["lesson-transcripts/lesson-a.json"], storage.DeletedTranscripts);
    }

    [Fact]
    public async Task DeleteStudentPermanently_OtherTutorsStudent_IsRejectedAndKept()
    {
        await using var db = CreateContext();
        var student = AddStudent(db, "tutor-other");
        await db.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            new RecorderService(db, new FakeStorage()).DeleteStudentPermanentlyAsync(student.Studentid, Tutor));
        Assert.Single(db.RecorderStudents);
    }

    [Fact]
    public async Task DeleteStudentPermanently_StorageDown_KeepsEverythingToRetry()
    {
        await using var db = CreateContext();
        var student = AddStudent(db, Tutor);
        AddLesson(db, Tutor, student.Studentid, SessionRecordingStatus.Sent).Storagekey = "app/x";
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<RecorderNotReadyException>(() =>
            new RecorderService(db, new FakeStorage { Enabled = false })
                .DeleteStudentPermanentlyAsync(student.Studentid, Tutor));
        Assert.Single(db.RecorderStudents);
        Assert.Single(db.RecorderLessons);
    }

    // ── Báo nội dung AI sai ────────────────────────────────────────────────

    [Fact]
    public async Task ReportAiFeedback_SavesReasonAndNote()
    {
        await using var db = CreateContext();
        var lesson = AddLesson(db, Tutor, null, SessionRecordingStatus.AwaitingApproval);
        lesson.Airesult = "{\"summary\":\"...\"}";
        await db.SaveChangesAsync();

        await AppRecording(db).ReportAiFeedbackAsync(lesson.Lessonid, Tutor,
            new RecorderAiFeedbackRequest { Reason = RecorderAiFeedbackReason.WrongContent, Note = "  Sai bài tập  " });

        var fb = Assert.Single(db.RecorderAiFeedbacks);
        Assert.Equal(lesson.Lessonid, fb.Lessonid);
        Assert.Equal("wrong_content", fb.Reason);
        Assert.Equal("Sai bài tập", fb.Note);
    }

    [Fact]
    public async Task ReportAiFeedback_WithoutAiContent_IsRejected()
    {
        await using var db = CreateContext();
        var lesson = AddLesson(db, Tutor, null, SessionRecordingStatus.Processing);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<RecorderNotReadyException>(() => AppRecording(db).ReportAiFeedbackAsync(
            lesson.Lessonid, Tutor, new RecorderAiFeedbackRequest { Reason = RecorderAiFeedbackReason.Other }));
        Assert.Empty(db.RecorderAiFeedbacks);
    }

    [Fact]
    public async Task ReportAiFeedback_OtherTutorsLesson_IsRejected()
    {
        await using var db = CreateContext();
        var lesson = AddLesson(db, "tutor-other", null, SessionRecordingStatus.AwaitingApproval);
        lesson.Airesult = "{}";
        await db.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<Exception>(() => AppRecording(db).ReportAiFeedbackAsync(
            lesson.Lessonid, Tutor, new RecorderAiFeedbackRequest { Reason = RecorderAiFeedbackReason.Other }));
        Assert.Empty(db.RecorderAiFeedbacks);
    }

    // ── Tài khoản demo cho người duyệt Google Play ─────────────────────────

    [Theory]
    [InlineData(DemoTutor, RecorderDeliveryStatus.Sent)]   // demo: coi như đã gửi, không gửi Zalo
    [InlineData(Tutor, RecorderDeliveryStatus.Pending)]    // thật: chờ job gửi Zalo
    public async Task Approve_DemoTutorIsMarkedSentWithoutZalo(string tutorId, string expectedDelivery)
    {
        await using var db = CreateContext();
        var student = AddStudent(db, tutorId);
        var lesson = AddLesson(db, tutorId, student.Studentid, SessionRecordingStatus.AwaitingApproval);
        await db.SaveChangesAsync();

        await AppRecording(db).ApproveAsync(lesson.Lessonid, tutorId,
            new RecorderApproveRequest { LessonContent = "Ôn phân số", ZaloContent = "Ôn phân số" });

        var saved = await db.RecorderLessons.SingleAsync(l => l.Lessonid == lesson.Lessonid);
        Assert.Equal(SessionRecordingStatus.Sent, saved.Status);
        Assert.Equal(expectedDelivery, saved.Deliverystatus);
        Assert.Equal(RecorderDeliveryChannel.Zns, saved.Deliverychannel);
    }

    [Fact]
    public void AppReviewSettings_OnlyListedIdsAreDemo()
    {
        var s = new AppReviewSettings { DemoTutorUserIds = [DemoTutor] };
        Assert.True(s.IsDemoTutor(DemoTutor));
        Assert.False(s.IsDemoTutor(Tutor));
        Assert.False(s.IsDemoTutor(null));
        Assert.False(new AppReviewSettings().IsDemoTutor(DemoTutor));
    }

    // ── helpers ────────────────────────────────────────────────────────────

    private static AppRecordingService AppRecording(AgoraDbContext db) => new(
        db, new FakeStorage(), null!, null!,
        Options.Create(new AppReviewSettings { DemoTutorUserIds = [DemoTutor] }),
        NullLogger<AppRecordingService>.Instance);

    private static RecorderStudent AddStudent(AgoraDbContext db, string tutorId, string name = "An")
    {
        var s = new RecorderStudent
        {
            Studentid = Guid.NewGuid(),
            Tutorid = tutorId,
            Fullname = name,
            Parentphone = "+84901234567",
            Consentstatus = RecorderConsentStatus.TutorConfirmed,
            Consentversion = RecorderConsentText.CurrentVersion,
            Createdat = TimeZoneHelper.UtcNow,
            Updatedat = TimeZoneHelper.UtcNow
        };
        db.RecorderStudents.Add(s);
        return s;
    }

    private static RecorderLesson AddLesson(AgoraDbContext db, string tutorId, Guid? studentId, string status)
    {
        var l = new RecorderLesson
        {
            Lessonid = Guid.NewGuid(),
            Tutorid = tutorId,
            Studentid = studentId,
            Status = status,
            Aistatus = RecorderAiStatus.None,
            Createdat = TimeZoneHelper.UtcNow,
            Updatedat = TimeZoneHelper.UtcNow
        };
        db.RecorderLessons.Add(l);
        return l;
    }

    private static AgoraDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AgoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestDbContext(options);
    }

    private sealed class TestDbContext(DbContextOptions<AgoraDbContext> options) : AgoraDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<QuestionBank>().Ignore(q => q.Embedding);
            modelBuilder.Entity<TutoraKbChunk>().Ignore(c => c.Embedding);
        }
    }
}
