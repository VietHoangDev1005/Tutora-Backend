using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MV.ApplicationLayer.Interfaces;
using MV.ApplicationLayer.ServiceInterfaces;
using MV.DomainLayer.Constants;
using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.DTO.ResponseModel;
using MV.DomainLayer.Entities;
using MV.DomainLayer.Exceptions;
using MV.DomainLayer.Helpers;

namespace MV.ApplicationLayer.Services;

/// <summary>
/// Danh bạ học sinh ngoài nền tảng + nhật ký buổi dạy. Chỉ đọc/ghi schema
/// recorder; không tạo user, booking hay class_session nào.
/// </summary>
public class RecorderService(IAppDbContext db) : IRecorderService
{
    // ── Học sinh ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RecorderStudentResponse>> ListStudentsAsync(string tutorId, CancellationToken ct = default)
    {
        var rows = await db.RecorderStudents
            .Where(s => s.Tutorid == tutorId && s.Archivedat == null)
            .Select(s => new
            {
                Student = s,
                LessonCount = db.RecorderLessons.Count(l => l.Studentid == s.Studentid
                    && l.Status != SessionRecordingStatus.Discarded),
                LastLessonAt = db.RecorderLessons
                    .Where(l => l.Studentid == s.Studentid && l.Status != SessionRecordingStatus.Discarded)
                    .Max(l => (DateTime?)(l.Startedat ?? l.Scheduledstart)),
                Parent = db.RecorderParents.FirstOrDefault(p => p.Parentid == s.Parentid),
                InviteExpiresAt = db.RecorderParentInvites
                    .Where(i => i.Studentid == s.Studentid && i.Usedat == null && i.Revokedat == null)
                    .Max(i => (DateTime?)i.Expiresat)
            })
            .ToListAsync(ct);

        return rows
            .OrderByDescending(r => r.LastLessonAt ?? r.Student.Createdat)
            .Select(r => ToResponse(r.Student, r.LessonCount, r.LastLessonAt, r.Parent, r.InviteExpiresAt))
            .ToList();
    }

    public async Task<RecorderStudentResponse> GetStudentAsync(Guid studentId, string tutorId, CancellationToken ct = default)
    {
        var s = await LoadStudentAsync(studentId, tutorId, ct);
        var lessons = db.RecorderLessons.Where(l => l.Studentid == studentId && l.Status != SessionRecordingStatus.Discarded);
        var parent = s.Parentid == null ? null
            : await db.RecorderParents.FirstOrDefaultAsync(p => p.Parentid == s.Parentid, ct);
        var inviteExpiresAt = await db.RecorderParentInvites
            .Where(i => i.Studentid == studentId && i.Usedat == null && i.Revokedat == null)
            .MaxAsync(i => (DateTime?)i.Expiresat, ct);
        return ToResponse(s, await lessons.CountAsync(ct),
            await lessons.MaxAsync(l => (DateTime?)(l.Startedat ?? l.Scheduledstart), ct),
            parent, inviteExpiresAt);
    }

    public async Task<RecorderStudentResponse> CreateStudentAsync(string tutorId, RecorderStudentRequest request, CancellationToken ct = default)
    {
        var now = TimeZoneHelper.UtcNow;
        var s = new RecorderStudent
        {
            Studentid = Guid.NewGuid(),
            Tutorid = tutorId,
            Consentstatus = RecorderConsentStatus.Unknown,
            Createdat = now,
            Updatedat = now
        };
        var consentChange = Apply(s, request, now);
        db.RecorderStudents.Add(s);
        await db.SaveChangesAsync(ct);
        await LogConsentChangeAsync(s, consentChange, now, ct);
        if (request.Schedule != null)
            await ReplaceScheduleAsync(s, request.Schedule, request.ScheduleFrom, request.ScheduleUntil, ct);
        return ToResponse(s, 0, null);
    }

    public async Task<RecorderStudentResponse> UpdateStudentAsync(Guid studentId, string tutorId, RecorderStudentRequest request, CancellationToken ct = default)
    {
        var s = await LoadStudentAsync(studentId, tutorId, ct);
        var now = TimeZoneHelper.UtcNow;
        var consentChange = Apply(s, request, now);
        await db.SaveChangesAsync(ct);
        await LogConsentChangeAsync(s, consentChange, now, ct);
        if (request.Schedule != null)
            await ReplaceScheduleAsync(s, request.Schedule, request.ScheduleFrom, request.ScheduleUntil, ct);
        return await GetStudentAsync(studentId, tutorId, ct);
    }

    public async Task ArchiveStudentAsync(Guid studentId, string tutorId, CancellationToken ct = default)
    {
        var s = await LoadStudentAsync(studentId, tutorId, ct);
        s.Archivedat = TimeZoneHelper.UtcNow;
        s.Updatedat = s.Archivedat.Value;
        await db.SaveChangesAsync(ct);
    }

    // ── Buổi dạy ─────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<RecorderLessonResponse>> ListLessonsAsync(
        string tutorId, DateTime? from, DateTime? to, Guid? studentId, CancellationToken ct = default)
    {
        // Sinh sẵn buổi theo thời khoá biểu tới cuối khoảng đang xem (tối đa ~6 tháng tới).
        var horizon = TimeZoneHelper.UtcNow.AddDays(GenerateAheadDays);
        var until = to is DateTime tt ? ToUtc(tt) : TimeZoneHelper.UtcNow.AddDays(DefaultAheadDays);
        await EnsureGeneratedAsync(tutorId, until < horizon ? until : horizon, ct);

        var q = db.RecorderLessons
            .Where(l => l.Tutorid == tutorId && l.Status != SessionRecordingStatus.Discarded);
        if (studentId is Guid sid) q = q.Where(l => l.Studentid == sid);
        if (from is DateTime f) { var fu = ToUtc(f); q = q.Where(l => (l.Scheduledstart ?? l.Startedat) >= fu); }
        if (to is DateTime t) { var tu = ToUtc(t); q = q.Where(l => (l.Scheduledstart ?? l.Startedat) <= tu); }

        var rows = await q
            .OrderByDescending(l => l.Scheduledstart ?? l.Startedat)
            .Select(l => new
            {
                Lesson = l,
                StudentName = l.Student != null ? l.Student.Fullname
                    : l.ClassSession != null && l.ClassSession.Student != null ? l.ClassSession.Student.Fullname : null,
                Grade = l.Student != null ? l.Student.Grade : null
            })
            .Take(500)
            .ToListAsync(ct);

        return rows.Select(r => ToResponse(r.Lesson, r.StudentName, r.Grade)).ToList();
    }

    public async Task<RecorderLessonResponse> CreateLessonAsync(string tutorId, RecorderLessonCreateRequest request, CancellationToken ct = default)
    {
        var s = await LoadStudentAsync(request.StudentId, tutorId, ct);
        if (request.ScheduledStart is DateTime a && request.ScheduledEnd is DateTime b && b <= a)
            throw new RecorderNotReadyException("Giờ kết thúc phải sau giờ bắt đầu.");

        var now = TimeZoneHelper.UtcNow;
        var lesson = new RecorderLesson
        {
            Lessonid = Guid.NewGuid(),
            Tutorid = tutorId,
            Studentid = s.Studentid,
            Scheduledstart = request.ScheduledStart is DateTime st ? ToUtc(st) : null,
            Scheduledend = request.ScheduledEnd is DateTime en ? ToUtc(en) : null,
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? s.Subject : request.Subject.Trim(),
            Status = SessionRecordingStatus.Scheduled,
            Aistatus = RecorderAiStatus.None,
            Createdat = now,
            Updatedat = now
        };
        db.RecorderLessons.Add(lesson);
        await db.SaveChangesAsync(ct);
        return ToResponse(lesson, s.Fullname, s.Grade);
    }

    public async Task DeleteLessonAsync(Guid lessonId, string tutorId, CancellationToken ct = default)
    {
        var lesson = await db.RecorderLessons.FirstOrDefaultAsync(l => l.Lessonid == lessonId && l.Tutorid == tutorId, ct)
            ?? throw new RecorderNotFoundException("Không tìm thấy buổi học.");
        if (lesson.Status != SessionRecordingStatus.Scheduled)
            throw new RecorderNotReadyException("Buổi đã có bản ghi, không xoá được. Hãy huỷ bản ghi thay vì xoá buổi.");
        // Đánh dấu huỷ thay vì xoá dòng: nếu xoá, lần sinh lịch sau sẽ tạo lại đúng buổi đó.
        lesson.Status = SessionRecordingStatus.Discarded;
        lesson.Updatedat = TimeZoneHelper.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    // ── Thời khoá biểu ───────────────────────────────────────────────────────

    /// <summary>Đổi lịch ngay → sinh sẵn 8 tuần; tab Lịch xem xa hơn thì sinh thêm tới đó.</summary>
    private const int DefaultAheadDays = 56;
    private const int GenerateAheadDays = 190;

    private static readonly JsonSerializerOptions ScheduleJson = new(JsonSerializerDefaults.Web);

    internal static List<RecorderScheduleSlot> ParseSchedule(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<RecorderScheduleSlot>>(json, ScheduleJson) ?? new();
        }
        catch (JsonException)
        {
            return new();
        }
    }

    /// <summary>
    /// Lưu lịch mới, bỏ các buổi CHƯA ghi từ hôm nay trở đi của lịch cũ rồi sinh lại.
    /// Buổi đã ghi / đã gửi báo cáo không bao giờ bị đụng.
    /// </summary>
    private async Task ReplaceScheduleAsync(
        RecorderStudent s, List<RecorderScheduleSlot> slots, DateOnly? from, DateOnly? until, CancellationToken ct)
    {
        foreach (var slot in slots)
        {
            if (!TryParseTime(slot.Start, out var a) || !TryParseTime(slot.End, out var b) || b <= a)
                throw new RecorderNotReadyException("Giờ kết thúc phải sau giờ bắt đầu.");
        }

        var todayVn = DateOnly.FromDateTime(TimeZoneHelper.ToVietnamTime(TimeZoneHelper.UtcNow));
        var startDay = from ?? todayVn;
        if (until is DateOnly u)
        {
            if (u < startDay)
                throw new RecorderNotReadyException("Ngày kết thúc phải sau ngày bắt đầu.");
            if (u > startDay.AddYears(1))
                throw new RecorderNotReadyException("Lịch học tối đa 1 năm.");
        }
        var fromUtc = VnToUtc(todayVn, TimeOnly.MinValue);

        var stale = await db.RecorderLessons
            .Where(l => l.Studentid == s.Studentid
                && l.Status == SessionRecordingStatus.Scheduled
                && l.Scheduledstart >= fromUtc)
            .ToListAsync(ct);
        db.RecorderLessons.RemoveRange(stale);

        s.Schedule = slots.Count == 0 ? null : JsonSerializer.Serialize(
            slots.OrderBy(x => x.DayOfWeek).ThenBy(x => x.Start).ToList(), ScheduleJson);
        s.Schedulefrom = slots.Count == 0 ? null : startDay;
        s.Scheduleuntil = slots.Count == 0 ? null : until;
        s.Updatedat = TimeZoneHelper.UtcNow;
        await db.SaveChangesAsync(ct);

        if (slots.Count > 0)
            await GenerateForStudentAsync(s, TimeZoneHelper.UtcNow.AddDays(DefaultAheadDays), ct);
    }

    private async Task EnsureGeneratedAsync(string tutorId, DateTime untilUtc, CancellationToken ct)
    {
        var students = await db.RecorderStudents
            .Where(s => s.Tutorid == tutorId && s.Archivedat == null && s.Schedule != null)
            .ToListAsync(ct);
        foreach (var s in students)
            await GenerateForStudentAsync(s, untilUtc, ct);
    }

    /// <summary>Tạo các buổi 'scheduled' còn thiếu từ ngày áp dụng lịch tới <paramref name="untilUtc"/>.</summary>
    private async Task GenerateForStudentAsync(RecorderStudent s, DateTime untilUtc, CancellationToken ct)
    {
        var slots = ParseSchedule(s.Schedule);
        if (slots.Count == 0 || s.Schedulefrom is not DateOnly scheduleFrom) return;

        // Không sinh buổi trong quá khứ: lịch đặt hôm nay thì bắt đầu từ hôm nay.
        var todayVn = DateOnly.FromDateTime(TimeZoneHelper.ToVietnamTime(TimeZoneHelper.UtcNow));
        var fromDay = scheduleFrom > todayVn ? scheduleFrom : todayVn;

        var untilDay = DateOnly.FromDateTime(TimeZoneHelper.ToVietnamTime(untilUtc));
        if (s.Scheduleuntil is DateOnly end && end < untilDay) untilDay = end;
        if (untilDay < fromDay) return;

        var fromUtc = VnToUtc(fromDay, TimeOnly.MinValue);
        var existing = (await db.RecorderLessons
                .Where(l => l.Studentid == s.Studentid && l.Scheduledstart >= fromUtc)
                .Select(l => l.Scheduledstart)
                .ToListAsync(ct))
            .Where(d => d.HasValue).Select(d => d!.Value).ToHashSet();

        var now = TimeZoneHelper.UtcNow;
        var added = 0;
        for (var day = fromDay; day <= untilDay; day = day.AddDays(1))
        {
            var dow = day.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)day.DayOfWeek;
            foreach (var slot in slots.Where(x => x.DayOfWeek == dow))
            {
                if (!TryParseTime(slot.Start, out var st) || !TryParseTime(slot.End, out var en)) continue;
                var startUtc = VnToUtc(day, st);
                if (existing.Contains(startUtc)) continue;
                db.RecorderLessons.Add(new RecorderLesson
                {
                    Lessonid = Guid.NewGuid(),
                    Tutorid = s.Tutorid,
                    Studentid = s.Studentid,
                    Scheduledstart = startUtc,
                    Scheduledend = VnToUtc(day, en),
                    Subject = s.Subject,
                    Status = SessionRecordingStatus.Scheduled,
                    Aistatus = RecorderAiStatus.None,
                    Createdat = now,
                    Updatedat = now
                });
                existing.Add(startUtc);
                added++;
            }
        }

        if (added == 0) return;
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Hai request cùng sinh lịch một lúc: unique index chặn bản trùng. Bỏ phần
            // chưa lưu của lượt này — lượt kia đã sinh đủ.
            foreach (var e in db.RecorderLessons.Local.Where(l => l.Studentid == s.Studentid).ToList())
            {
                var entry = db.RecorderLessons.Entry(e);
                if (entry.State == EntityState.Added) entry.State = EntityState.Detached;
            }
        }
    }

    private static bool TryParseTime(string? hhmm, out TimeOnly t) =>
        TimeOnly.TryParseExact(hhmm, "HH:mm", out t);

    private static DateTime VnToUtc(DateOnly day, TimeOnly time) =>
        TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(day.ToDateTime(time), DateTimeKind.Unspecified),
            TimeZoneHelper.VietnamTimeZone);

    // ── nội bộ ────────────────────────────────────────────────────────────────

    private async Task<RecorderStudent> LoadStudentAsync(Guid studentId, string tutorId, CancellationToken ct) =>
        await db.RecorderStudents.FirstOrDefaultAsync(
            s => s.Studentid == studentId && s.Tutorid == tutorId && s.Archivedat == null, ct)
        ?? throw new RecorderNotFoundException("Không tìm thấy học sinh.");

    /// <summary>
    /// Ghi nhật ký đồng ý (recorder.consent_events) — bằng chứng gia sư xác nhận phụ huynh
    /// đã đồng ý nội dung ghi âm phiên bản nào, lúc nào. Lưu sau khi học sinh đã có trong DB (FK).
    /// </summary>
    private async Task LogConsentChangeAsync(RecorderStudent s, string? action, DateTime now, CancellationToken ct)
    {
        if (action == null) return;
        db.RecorderConsentEvents.Add(new RecorderConsentEvent
        {
            Eventid = Guid.NewGuid(),
            Studentid = s.Studentid,
            Action = action,
            Method = RecorderConsentMethod.Tutor,
            Consentversion = s.Consentversion,
            Createdat = now
        });
        await db.SaveChangesAsync(ct);
    }

    /// <returns>Hành động đồng ý cần ghi nhật ký (granted/withdrawn) hoặc null nếu không đổi.</returns>
    private static string? Apply(RecorderStudent s, RecorderStudentRequest r, DateTime now)
    {
        s.Fullname = r.FullName.Trim();
        s.Grade = r.Grade;
        s.Subject = Clean(r.Subject);
        s.Parentname = Clean(r.ParentName);
        s.Parentphone = NormalizePhone(r.ParentPhone);
        s.Note = Clean(r.Note);
        s.Updatedat = now;

        // Gia sư chỉ nâng được lên tutor_confirmed; không hạ parent_confirmed/declined
        // (hai trạng thái đó do phụ huynh tự quyết qua ZNS).
        var version = string.IsNullOrWhiteSpace(r.ConsentVersion) ? RecorderConsentText.LegacyVersion : r.ConsentVersion.Trim();
        if (r.ParentConsent && s.Consentstatus == RecorderConsentStatus.Unknown)
        {
            s.Consentstatus = RecorderConsentStatus.TutorConfirmed;
            s.Consentat = now;
            s.Consentversion = version;
            return RecorderConsentAction.Granted;
        }
        if (r.ParentConsent && s.Consentstatus == RecorderConsentStatus.TutorConfirmed
            && !string.Equals(s.Consentversion, version, StringComparison.Ordinal))
        {
            // Phụ huynh đồng ý lại theo nội dung phiên bản mới.
            s.Consentat = now;
            s.Consentversion = version;
            return RecorderConsentAction.Granted;
        }
        if (!r.ParentConsent && s.Consentstatus == RecorderConsentStatus.TutorConfirmed)
        {
            s.Consentstatus = RecorderConsentStatus.Unknown;
            s.Consentat = null;
            return RecorderConsentAction.Withdrawn;
        }
        return null;
    }

    private static string? Clean(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    /// <summary>+84 / 84 → 0 để một số điện thoại chỉ có một cách viết.</summary>
    private static string? NormalizePhone(string? phone)
    {
        var p = Clean(phone)?.Replace(" ", "").Replace(".", "");
        if (p == null) return null;
        if (p.StartsWith("+84")) return "0" + p[3..];
        if (p.StartsWith("84") && p.Length >= 11) return "0" + p[2..];
        return p;
    }

    private static DateTime ToUtc(DateTime d) => d.Kind switch
    {
        DateTimeKind.Utc => d,
        DateTimeKind.Local => d.ToUniversalTime(),
        _ => DateTime.SpecifyKind(d, DateTimeKind.Utc)
    };

    private static RecorderStudentResponse ToResponse(
        RecorderStudent s, int lessonCount, DateTime? lastLessonAt,
        RecorderParent? parent = null, DateTime? inviteExpiresAt = null)
    {
        var r = ToBaseResponse(s, lessonCount, lastLessonAt);
        if (s.Parentid != null && parent != null)
        {
            r.ParentLinkStatus = parent.Isfollower ? RecorderParentLinkStatus.Linked : RecorderParentLinkStatus.Unfollowed;
            r.ParentLinkedAt = s.Parentlinkedat;
            r.ParentZaloName = parent.Displayname;
        }
        else if (inviteExpiresAt != null && inviteExpiresAt > TimeZoneHelper.UtcNow)
        {
            r.ParentLinkStatus = RecorderParentLinkStatus.Invited;
            r.InviteExpiresAt = inviteExpiresAt;
        }
        return r;
    }

    private static RecorderStudentResponse ToBaseResponse(RecorderStudent s, int lessonCount, DateTime? lastLessonAt) => new()
    {
        StudentId = s.Studentid,
        FullName = s.Fullname,
        Grade = s.Grade,
        Subject = s.Subject,
        ParentName = s.Parentname,
        ParentPhone = s.Parentphone,
        ConsentStatus = s.Consentstatus,
        ConsentVersion = s.Consentversion,
        ConsentAt = s.Consentat,
        Note = s.Note,
        Schedule = ParseSchedule(s.Schedule),
        ScheduleFrom = s.Schedulefrom,
        ScheduleUntil = s.Scheduleuntil,
        LessonCount = lessonCount,
        LastLessonAt = lastLessonAt,
        CreatedAt = s.Createdat
    };

    private static RecorderLessonResponse ToResponse(RecorderLesson l, string? studentName, short? grade) => new()
    {
        LessonId = l.Lessonid,
        StudentId = l.Studentid,
        ClassSessionId = l.Classsessionid,
        StudentName = studentName ?? "Học sinh",
        Grade = grade,
        Subject = l.Subject,
        ScheduledStart = l.Scheduledstart,
        ScheduledEnd = l.Scheduledend,
        Status = l.Status,
        AiStatus = l.Aistatus,
        DurationSec = l.Durationsec,
        StartedAt = l.Startedat,
        ApprovedAt = l.Approvedat,
        DeliveryChannel = l.Deliverychannel,
        DeliveryStatus = l.Deliverystatus
    };
}
