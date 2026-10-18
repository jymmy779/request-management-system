using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RequestManagementSystem.Api.Authorization;
using RequestManagementSystem.Api.Data;
using RequestManagementSystem.Api.DTOs;
using RequestManagementSystem.Api.Models;

namespace RequestManagementSystem.Tests;

public class AuthApiTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string TestEmail = "employee@test.local";
    private const string TestPassword = "Test123!";

    private readonly CustomWebApplicationFactory _factory;

    public AuthApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WhenPasswordIsInvalid_ReturnsUnauthorized()
    {
        await CreateUserAsync(
            TestEmail,
            AppRoles.Employee);

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = "Wrong123!"
            });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_SetsHttpOnlyCookie()
    {
        await CreateUserAsync(
            TestEmail,
            AppRoles.Employee);

        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = TestPassword
            });

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        var setCookie = Assert.Single(
            response.Headers.GetValues("Set-Cookie"));

        Assert.StartsWith(
            $"{AuthCookieNames.AccessToken}=",
            setCookie);
        Assert.Contains(
            "httponly",
            setCookie.ToLowerInvariant());
        Assert.Contains(
            "samesite=lax",
            setCookie.ToLowerInvariant());
    }

    [Fact]
    public async Task Me_WithoutAccessCookie_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Theory]
    [InlineData(TestEmail, AppRoles.Employee)]
    [InlineData("manager@test.local", AppRoles.Manager)]
    [InlineData("admin@test.local", AppRoles.Admin)]
    public async Task Me_WithValidAccessCookie_ReturnsCurrentUser(
        string email,
        string expectedRole)
    {
        await CreateUserAsync(
            email,
            expectedRole);

        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = email,
                Password = TestPassword
            });

        loginResponse.EnsureSuccessStatusCode();

        var response = await client.GetAsync("/api/auth/me");

        response.EnsureSuccessStatusCode();

        var currentUser =
            await response.Content
                .ReadFromJsonAsync<CurrentUserResponseDto>();

        Assert.NotNull(currentUser);
        Assert.Equal("Test User", currentUser.FullName);
        Assert.Equal(email, currentUser.Email);
        Assert.Equal(expectedRole, currentUser.Role);
        Assert.Equal("Test Department", currentUser.DepartmentName);
    }

    [Fact]
    public async Task Logout_AfterLogin_RemovesAccessCookie()
    {
        await CreateUserAsync(
            TestEmail,
            AppRoles.Employee);

        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = TestPassword
            });

        loginResponse.EnsureSuccessStatusCode();

        var logoutResponse = await client.PostAsync(
            "/api/auth/logout",
            content: null);

        Assert.Equal(
            HttpStatusCode.NoContent,
            logoutResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            meResponse.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordDto
            {
                CurrentPassword = TestPassword,
                NewPassword = "Changed123!"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWrongCurrentPassword_ReturnsBadRequest()
    {
        await CreateUserAsync(TestEmail, AppRoles.Employee);
        var client = await CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordDto
            {
                CurrentPassword = "Wrong123!",
                NewPassword = "Changed123!"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithWeakNewPassword_ReturnsBadRequest()
    {
        await CreateUserAsync(TestEmail, AppRoles.Employee);
        var client = await CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordDto
            {
                CurrentPassword = TestPassword,
                NewPassword = "weak"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WithValidInput_ChangesHashAndSignsOutCurrentSession()
    {
        const string newPassword = "Changed123!";

        await CreateUserAsync(TestEmail, AppRoles.Employee);
        var client = await CreateLoggedInClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/auth/change-password",
            new ChangePasswordDto
            {
                CurrentPassword = TestPassword,
                NewPassword = newPassword
            });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(TestEmail);

        Assert.NotNull(user);
        Assert.False(await userManager.CheckPasswordAsync(
            user,
            TestPassword));
        Assert.True(await userManager.CheckPasswordAsync(
            user,
            newPassword));
    }

    [Fact]
    public async Task PasswordResetTokenProvider_CanGenerateAndVerifyToken()
    {
        await CreateUserAsync(TestEmail, AppRoles.Employee);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(TestEmail);

        Assert.NotNull(user);

        var token = await userManager
            .GeneratePasswordResetTokenAsync(user);

        Assert.NotEmpty(token);
        Assert.True(await userManager.VerifyUserTokenAsync(
            user,
            userManager.Options.Tokens.PasswordResetTokenProvider,
            UserManager<ApplicationUser>.ResetPasswordTokenPurpose,
            token));
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailExists_SendsResetLinkAndReturnsAccepted()
    {
        await CreateUserAsync(TestEmail, AppRoles.Employee);
        var emailSender = GetTestEmailSender();
        emailSender.Clear();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordDto
            {
                Email = TestEmail
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());

        var sentEmail = Assert.Single(emailSender.SentEmails);
        Assert.Equal(TestEmail, sentEmail.RecipientEmail);
        Assert.Contains("/reset-password?", sentEmail.HtmlBody);
        Assert.Contains("token=", sentEmail.HtmlBody);
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailDoesNotExist_ReturnsSameResponseWithoutEmail()
    {
        var emailSender = GetTestEmailSender();
        emailSender.Clear();
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordDto
            {
                Email = "missing@test.local"
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        Assert.Empty(emailSender.SentEmails);
    }

    [Fact]
    public async Task ForgotPassword_WhenEmailDeliveryFails_DoesNotRevealAccount()
    {
        await CreateUserAsync(TestEmail, AppRoles.Employee);
        var emailSender = GetTestEmailSender();
        emailSender.Clear();
        emailSender.ThrowOnSend = true;
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordDto
            {
                Email = TestEmail
            });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());

        emailSender.Clear();
    }

    [Fact]
    public async Task ResetPassword_WithEmailedToken_ChangesPasswordAndRejectsReuse()
    {
        const string newPassword = "Reset123!";

        await CreateUserAsync(TestEmail, AppRoles.Employee);
        var client = _factory.CreateClient();
        var input = await CreateResetPasswordInputAsync(
            client,
            newPassword);

        var response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            input);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(TestEmail);

        Assert.NotNull(user);
        Assert.False(await userManager.CheckPasswordAsync(
            user,
            TestPassword));
        Assert.True(await userManager.CheckPasswordAsync(
            user,
            newPassword));

        input.NewPassword = "Another123!";

        var reuseResponse = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            input);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            reuseResponse.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
    {
        await CreateUserAsync(TestEmail, AppRoles.Employee);
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            new ResetPasswordDto
            {
                Email = TestEmail,
                Token = "invalid-token",
                NewPassword = "Reset123!"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithWeakPassword_ReturnsValidationError()
    {
        await CreateUserAsync(TestEmail, AppRoles.Employee);
        var client = _factory.CreateClient();
        var input = await CreateResetPasswordInputAsync(
            client,
            "weak");

        var response = await client.PostAsJsonAsync(
            "/api/auth/reset-password",
            input);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(TestEmail);

        Assert.NotNull(user);
        Assert.True(await userManager.CheckPasswordAsync(
            user,
            TestPassword));
    }

    private async Task<ResetPasswordDto> CreateResetPasswordInputAsync(
        HttpClient client,
        string newPassword)
    {
        var emailSender = GetTestEmailSender();
        emailSender.Clear();

        var forgotResponse = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordDto
            {
                Email = TestEmail
            });

        Assert.Equal(
            HttpStatusCode.Accepted,
            forgotResponse.StatusCode);

        var sentEmail = Assert.Single(emailSender.SentEmails);
        const string hrefPrefix = "href=\"";
        var hrefStart = sentEmail.HtmlBody.IndexOf(
            hrefPrefix,
            StringComparison.Ordinal) + hrefPrefix.Length;
        var hrefEnd = sentEmail.HtmlBody.IndexOf(
            '\"',
            hrefStart);

        Assert.True(hrefStart >= hrefPrefix.Length);
        Assert.True(hrefEnd > hrefStart);

        var resetUrl = WebUtility.HtmlDecode(
            sentEmail.HtmlBody[hrefStart..hrefEnd]);
        var resetUri = new Uri(resetUrl);
        var query = QueryHelpers.ParseQuery(resetUri.Query);

        return new ResetPasswordDto
        {
            Email = query["email"].ToString(),
            Token = query["token"].ToString(),
            NewPassword = newPassword
        };
    }

    private TestEmailSender GetTestEmailSender()
    {
        return _factory.Services
            .GetRequiredService<TestEmailSender>();
    }

    private async Task<HttpClient> CreateLoggedInClientAsync()
    {
        var client = _factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequestDto
            {
                Email = TestEmail,
                Password = TestPassword
            });

        loginResponse.EnsureSuccessStatusCode();

        return client;
    }

    private async Task CreateUserAsync(
        string email,
        string role)
    {
        using var scope = _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.EnsureCreatedAsync();
        await RoleSeeder.SeedAsync(scope.ServiceProvider);

        var department = await dbContext.Departments
            .SingleOrDefaultAsync(item => item.Code == "TEST");

        if (department is null)
        {
            department = new Department
            {
                Code = "TEST",
                Name = "Test Department"
            };

            dbContext.Departments.Add(department);
            await dbContext.SaveChangesAsync();
        }

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FullName = "Test User",
                DepartmentId = department.Id
            };

            var createResult =
                await userManager.CreateAsync(
                    user,
                    TestPassword);

            Assert.True(
                createResult.Succeeded,
                string.Join(
                    "; ",
                    createResult.Errors.Select(
                        error => error.Description)));
        }
        else if (user.DepartmentId != department.Id ||
                 user.FullName != "Test User")
        {
            user.DepartmentId = department.Id;
            user.FullName = "Test User";

            var updateResult = await userManager.UpdateAsync(user);

            Assert.True(updateResult.Succeeded);
        }

        if (!await userManager.CheckPasswordAsync(user, TestPassword))
        {
            var removeResult = await userManager.RemovePasswordAsync(user);
            Assert.True(removeResult.Succeeded);

            var resetResult = await userManager.AddPasswordAsync(
                user,
                TestPassword);

            Assert.True(resetResult.Succeeded);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult =
                await userManager.AddToRoleAsync(
                    user,
                    role);

            Assert.True(roleResult.Succeeded);
        }
    }
}
