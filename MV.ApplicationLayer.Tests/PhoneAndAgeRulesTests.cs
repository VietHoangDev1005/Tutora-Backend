using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using MV.DomainLayer.Entities;
using MV.InfrastructureLayer.DBContext;
using MV.InfrastructureLayer.Repositories;
using MV.DomainLayer.DTO.RequestModel;
using MV.DomainLayer.Helpers;
using Xunit;

namespace MV.ApplicationLayer.Tests;

/// <summary>
/// Quy tắc dùng chung: SĐT lưu một dạng duy nhất +84…, mật khẩu tối thiểu 8 ký tự,
/// gia sư từ 18 tuổi.
/// </summary>
public class PhoneAndAgeRulesTests
{
    [Theory]
    [InlineData("0901234567", "+84901234567")]
    [InlineData("84901234567", "+84901234567")]
    [InlineData("+84901234567", "+84901234567")]
    [InlineData(" 090 123 4567 ", "+84901234567")]
    [InlineData("090.123.4567", "+84901234567")]
    [InlineData("090-123-4567", "+84901234567")]
    [InlineData("+84 90 123 4567", "+84901234567")]
    [InlineData("02812345678", "+842812345678")] // số bàn 11 chữ số
    public void ToE164_NormalizesEveryInputFormToPlus84(string input, string expected)
    {
        Assert.Equal(expected, PhoneNumberHelper.ToE164(input));
        Assert.True(PhoneNumberHelper.IsValidVietnamPhone(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ToE164_EmptyIsNull(string? input) => Assert.Null(PhoneNumberHelper.ToE164(input));

    [Theory]
    [InlineData("12345")]
    [InlineData("090123")]
    [InlineData("1901234567")]
    [InlineData("0901234567890")]
    [InlineData("abc")]
    public void InvalidPhones_AreNotGuessed(string input)
    {
        Assert.False(PhoneNumberHelper.IsValidVietnamPhone(input));
        // Không "đoán" thành một số +84 hợp lệ.
        Assert.False(PhoneNumberHelper.IsValidVietnamPhone(PhoneNumberHelper.ToE164(input)));
    }

    [Fact]
    public void LookupVariants_CoverLegacyStoredForms()
    {
        Assert.Equal(new[] { "+84901234567", "84901234567", "0901234567" },
            PhoneNumberHelper.LookupVariants("090 123 4567"));
    }

    [Theory]
    [InlineData("0901", "901")]
    [InlineData("+84901", "901")]
    [InlineData("84901234567", "901234567")]
    [InlineData("nguyen", "nguyen")]
    public void SearchFragment_MatchesAnyStoredForm(string term, string expected) =>
        Assert.Equal(expected, PhoneNumberHelper.SearchFragment(term));

    [Theory]
    [InlineData("0901234567", true)]
    [InlineData("84901234567", true)]
    [InlineData("+84901234567", true)]
    [InlineData("090 123 4567", true)]
    [InlineData("090.123.4567", true)]
    [InlineData("12345", false)]
    [InlineData("1901234567", false)]
    public void DtoPattern_AcceptsFreeFormInput(string input, bool ok) =>
        Assert.Equal(ok, System.Text.RegularExpressions.Regex.IsMatch(input, PhoneNumberHelper.VietnamPhonePattern));

    [Theory]
    [InlineData("abc1234", false)]
    [InlineData("abcd1234", true)]
    public void RegisterAndResetPassword_RequireAtLeast8(string password, bool ok)
    {
        var register = new SimpleRegisterRequest { Phone = "0901234567", Password = password, FullName = "Nguyễn Văn A" };
        var reset = new ResetPasswordRequest { Phone = "0901234567", Otp = "123456", NewPassword = password };
        Assert.Equal(ok, IsValid(register));
        Assert.Equal(ok, IsValid(reset));
    }

    [Fact]
    public void TutorAge_BoundaryIsExactly18thBirthday()
    {
        var today = DateOnly.FromDateTime(TimeZoneHelper.UtcNow);
        Assert.True(AgeHelper.IsOldEnoughToTutor(today.AddYears(-18)));
        Assert.False(AgeHelper.IsOldEnoughToTutor(today.AddYears(-18).AddDays(1)));
        Assert.False(AgeHelper.IsOldEnoughToTutor(today.AddDays(1))); // ngày ở tương lai
        Assert.True(AgeHelper.IsOldEnoughToTutor(new DateOnly(1990, 1, 1)));
    }

    [Theory]
    [InlineData("0901234567")]    // dữ liệu cũ chưa chuẩn hoá
    [InlineData("84901234567")]
    [InlineData("+84901234567")]  // dạng chuẩn
    public async Task GetUserByPhone_FindsUserWhicheverWayItWasStoredOrTyped(string stored)
    {
        var options = new DbContextOptionsBuilder<AgoraDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var db = new PhoneTestDbContext(options);
        db.Users.Add(new User
        {
            Userid = "u1", Username = "u1", Password = "x", Email = "u1@test.local",
            Fullname = "A", Phone = stored, Status = 1, Createdat = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var repo = new UserRepository(db, null!);

        foreach (var typed in new[] { "0901234567", "84901234567", "+84901234567", "090 123 4567" })
            Assert.Equal("u1", (await repo.GetUserByPhoneAsync(typed))?.Userid);
        Assert.False(await repo.IsPhoneUniqueAsync("+84 901 234 567"));
        Assert.Null(await repo.GetUserByPhoneAsync("0909999999"));
    }

    private sealed class PhoneTestDbContext(DbContextOptions<AgoraDbContext> options) : AgoraDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<QuestionBank>().Ignore(q => q.Embedding);
            modelBuilder.Entity<TutoraKbChunk>().Ignore(c => c.Embedding);
        }
    }

    private static bool IsValid(object model) =>
        Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);
}
