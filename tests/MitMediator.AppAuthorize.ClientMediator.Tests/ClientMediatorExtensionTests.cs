using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using MitMediator.AutoApi.HttpMediator;
using Moq;

namespace MitMediator.AppAuthorize.ClientMediator.Tests
{
    public class ClientMediatorExtensionTests
    {
        [Fact]
        public void JwtTokenModel_ShouldReadClaimsAndExpiration()
        {
            var expires = DateTime.UtcNow.AddMinutes(10);
            var token = new JwtSecurityToken(
                claims: [new Claim(ClaimTypes.NameIdentifier, "user-id")],
                expires: expires);
            var model = new JwtTokenModel(
                new JwtSecurityTokenHandler().WriteToken(token),
                "refresh-token",
                expires.AddDays(1));

            Assert.Equal("user-id", model.UserId);
            Assert.Equal(expires, model.JwtTokenExpires, precision: TimeSpan.FromSeconds(1));
            Assert.Equal("refresh-token", model.RefreshToken);
            Assert.Equal(expires.AddDays(1), model.RefreshTokenExpires);
        }

        [Fact]
        public void JwtTokenModel_ShouldBeImmutable()
        {
            var properties = typeof(JwtTokenModel).GetProperties();

            Assert.All(properties, property => Assert.Null(property.SetMethod));
        }

        [Fact]
        public void JwtTokenModel_ShouldRejectMissingExpiration()
        {
            var token = new JwtSecurityToken(
                claims: [new Claim(ClaimTypes.NameIdentifier, "user-id")]);
            var exception = Assert.Throws<ArgumentException>(
                () => new JwtTokenModel(
                    new JwtSecurityTokenHandler().WriteToken(token)));

            Assert.Contains("expiration", exception.Message);
            Assert.Equal("jwtToken", exception.ParamName);
        }

        [Fact]
        public void JwtTokenModel_ShouldReportJwtExpiration()
        {
            var utcNow = DateTimeOffset.Parse(
                "2030-01-02T03:04:05Z");
            var expired = new JwtTokenModel(
                CreateJwtToken(
                    expires: utcNow.UtcDateTime.AddMinutes(-1)));
            var current = new JwtTokenModel(
                CreateJwtToken(
                    expires: utcNow.UtcDateTime.AddMinutes(10)));

            Assert.True(expired.IsJwtExpired(utcNow));
            Assert.False(current.IsJwtExpired(utcNow));
        }

        [Fact]
        public void JwtTokenModel_ShouldReportRefreshTokenExpiration()
        {
            var utcNow = DateTimeOffset.Parse(
                "2030-01-02T03:04:05Z");
            var expired = new JwtTokenModel(
                CreateJwtToken(),
                "expired-refresh",
                utcNow.AddMinutes(-1));
            var current = new JwtTokenModel(
                CreateJwtToken(),
                "current-refresh",
                utcNow.AddMinutes(10));
            var withoutRefreshToken = new JwtTokenModel(CreateJwtToken());

            Assert.True(expired.IsRefreshTokenExpired(utcNow));
            Assert.False(current.IsRefreshTokenExpired(utcNow));
            Assert.False(withoutRefreshToken.IsRefreshTokenExpired(utcNow));
        }

        [Fact]
        public void JwtTokenModel_ShouldRejectRepeatedUserId()
        {
            var token = new JwtSecurityToken(
                claims:
                [
                    new Claim(ClaimTypes.NameIdentifier, "first-user"),
                    new Claim(ClaimTypes.NameIdentifier, "second-user")
                ],
                expires: DateTime.UtcNow.AddMinutes(10));
            var exception = Assert.Throws<ArgumentException>(
                () => new JwtTokenModel(
                    new JwtSecurityTokenHandler().WriteToken(token)));

            Assert.Contains("exactly one", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("not-a-jwt")]
        public void JwtTokenModel_ShouldRejectInvalidToken(string? value)
        {
            Assert.ThrowsAny<ArgumentException>(
                () => new JwtTokenModel(value!));
        }

        [Fact]
        public void JwtTokenModel_ShouldRejectMissingUserId()
        {
            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddMinutes(10));

            var exception = Assert.Throws<ArgumentException>(
                () => new JwtTokenModel(
                    new JwtSecurityTokenHandler().WriteToken(token)));

            Assert.Contains("user identifier", exception.Message);
        }

        [Fact]
        public void JwtTokenModel_ShouldRejectBlankUserId()
        {
            var token = new JwtSecurityToken(
                claims: [new Claim(ClaimTypes.NameIdentifier, " ")],
                expires: DateTime.UtcNow.AddMinutes(10));

            var exception = Assert.Throws<ArgumentException>(
                () => new JwtTokenModel(
                    new JwtSecurityTokenHandler().WriteToken(token)));

            Assert.Contains("non-blank user identifier", exception.Message);
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        [InlineData("refresh-token", false)]
        public void JwtTokenModel_ShouldRejectIncompleteRefreshToken(
            string? refreshToken,
            bool hasExpiration)
        {
            var exception = Assert.Throws<ArgumentException>(
                () => new JwtTokenModel(
                    CreateJwtToken(),
                    refreshToken,
                    hasExpiration
                        ? DateTimeOffset.UtcNow.AddDays(1)
                        : null));

            Assert.Contains("refresh token", exception.Message);
        }

        [Fact]
        public void JwtTokenModel_ShouldDeserializeThroughImmutableConstructor()
        {
            var token = new JwtSecurityToken(
                claims: [new Claim(ClaimTypes.NameIdentifier, "user-id")],
                expires: DateTime.UtcNow.AddMinutes(10));
            var jwt = new JwtSecurityTokenHandler().WriteToken(token);
            var json = JsonSerializer.Serialize(new
            {
                jwtToken = jwt,
                refreshToken = "refresh-token",
                refreshTokenExpires = DateTimeOffset.UtcNow.AddDays(1)
            });

            var model = JsonSerializer.Deserialize<JwtTokenModel>(
                json,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            Assert.NotNull(model);
            Assert.Equal("user-id", model.UserId);
            Assert.Equal(jwt, model.JwtToken);
            Assert.Equal("refresh-token", model.RefreshToken);
        }

        [Fact]
        public void ClientCommands_ShouldBePublic()
        {
            Type[] commandTypes =
            [
                typeof(CreateJwtByPasswordCommand),
                typeof(CreateJwtByPasswordWithPrefixCommand),
                typeof(CreateJwtByCodeCommand),
                typeof(CreateJwtByCodeWithPrefixCommand),
                typeof(CreateJwtByRefreshTokenCommand),
                typeof(CreateJwtByRefreshTokenWithPrefixCommand)
            ];

            Assert.All(commandTypes, type => Assert.True(type.IsPublic));
        }

        [Fact]
        public async Task GetJwtByPasswordAsync_Calls_SendAsync_With_Command()
        {
            var mock = new Mock<IClientMediator>();
            var expected = new JwtTokenModel(
                CreateJwtToken(),
                "r",
                DateTime.UtcNow.AddDays(1));
            mock.Setup(m => m.SendAsync<CreateJwtByPasswordCommand, JwtTokenModel>(It.IsAny<ClientMediator.CreateJwtByPasswordCommand>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<JwtTokenModel>(expected));

            var result = await mock.Object.GetJwtByPasswordAsync("login", "pwd", CancellationToken.None);

            Assert.Same(expected, result);
            mock.Verify(m => m.SendAsync<CreateJwtByPasswordCommand, JwtTokenModel>(
                It.Is<CreateJwtByPasswordCommand>(c => c.Login == "login" && c.Password == "pwd"),
                CancellationToken.None), Times.Once);
        }

        [Theory]
        [InlineData(null, "password", "login")]
        [InlineData("", "password", "login")]
        [InlineData(" ", "password", "login")]
        [InlineData("login", null, "password")]
        [InlineData("login", "", "password")]
        [InlineData("login", " ", "password")]
        public void GetJwtByPasswordAsync_ShouldRejectInvalidCredentials(
            string? login,
            string? password,
            string parameterName)
        {
            var mock = new Mock<IClientMediator>();

            var exception = Assert.ThrowsAny<ArgumentException>(
                () => mock.Object.GetJwtByPasswordAsync(
                    login!,
                    password!));

            Assert.Equal(parameterName, exception.ParamName);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetJwtByPasswordAsync_ShouldDefaultCancellationToken()
        {
            var mock = new Mock<IClientMediator>();
            mock.Setup(m => m.SendAsync<CreateJwtByPasswordCommand, JwtTokenModel>(
                    It.IsAny<CreateJwtByPasswordCommand>(),
                    CancellationToken.None))
                .Returns(new ValueTask<JwtTokenModel>(
                    new JwtTokenModel(
                        CreateJwtToken(),
                        "refresh",
                        DateTime.UtcNow.AddDays(1))));

            await mock.Object.GetJwtByPasswordAsync("login", "password");

            mock.Verify(m => m.SendAsync<CreateJwtByPasswordCommand, JwtTokenModel>(
                It.IsAny<CreateJwtByPasswordCommand>(),
                CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task GetJwtByCodeAsync_Calls_SendAsync_With_Command()
        {
            var mock = new Mock<IClientMediator>();
            var expected = new JwtTokenModel(
                CreateJwtToken(),
                "r2",
                DateTime.UtcNow.AddDays(1));
            mock.Setup(m => m.SendAsync<CreateJwtByCodeCommand, JwtTokenModel>(It.IsAny<ClientMediator.CreateJwtByCodeCommand>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<JwtTokenModel>(expected));

            var result = await mock.Object.GetJwtByCodeAsync("login", "code", CancellationToken.None);

            Assert.Same(expected, result);
            mock.Verify(m => m.SendAsync<CreateJwtByCodeCommand, JwtTokenModel>(
                It.Is<ClientMediator.CreateJwtByCodeCommand>(c => c.Login == "login" && c.Code == "code"),
                CancellationToken.None), Times.Once);
        }

        [Theory]
        [InlineData(null, "code", "login")]
        [InlineData("", "code", "login")]
        [InlineData(" ", "code", "login")]
        [InlineData("login", null, "code")]
        [InlineData("login", "", "code")]
        [InlineData("login", " ", "code")]
        public void GetJwtByCodeAsync_ShouldRejectInvalidCredentials(
            string? login,
            string? code,
            string parameterName)
        {
            var mock = new Mock<IClientMediator>();

            var exception = Assert.ThrowsAny<ArgumentException>(
                () => mock.Object.GetJwtByCodeAsync(
                    login!,
                    code!));

            Assert.Equal(parameterName, exception.ParamName);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetJwtByRefreshAsync_Calls_SendAsync_With_Command()
        {
            var mock = new Mock<IClientMediator>();
            var expected = new JwtTokenModel(
                CreateJwtToken(),
                "r3",
                DateTime.UtcNow.AddDays(1));
            mock.Setup(m => m.SendAsync<CreateJwtByRefreshTokenCommand, JwtTokenModel>(It.IsAny<CreateJwtByRefreshTokenCommand>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<JwtTokenModel>(expected));

            var result = await mock.Object.GetJwtByRefreshAsync("user", "refreshKey", CancellationToken.None);

            Assert.Same(expected, result);
            mock.Verify(m => m.SendAsync<CreateJwtByRefreshTokenCommand, JwtTokenModel>(
                It.Is<CreateJwtByRefreshTokenCommand>(c => c.UserId == "user" && c.RefreshTokenKey == "refreshKey"),
                CancellationToken.None), Times.Once);
        }

        [Theory]
        [InlineData(null, "refresh-token", "userId")]
        [InlineData("", "refresh-token", "userId")]
        [InlineData(" ", "refresh-token", "userId")]
        [InlineData("user-id", null, "refreshTokenKey")]
        [InlineData("user-id", "", "refreshTokenKey")]
        [InlineData("user-id", " ", "refreshTokenKey")]
        public void GetJwtByRefreshAsync_ShouldRejectInvalidCredentials(
            string? userId,
            string? refreshTokenKey,
            string parameterName)
        {
            var mock = new Mock<IClientMediator>();

            var exception = Assert.ThrowsAny<ArgumentException>(
                () => mock.Object.GetJwtByRefreshAsync(
                    userId!,
                    refreshTokenKey!));

            Assert.Equal(parameterName, exception.ParamName);
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetJwtByPasswordAsync_ShouldSupportCustomRoutePrefix()
        {
            var mock = new Mock<IClientMediator>();
            mock.Setup(mediator => mediator.SendAsync<
                    CreateJwtByPasswordWithPrefixCommand, JwtTokenModel>(
                    It.IsAny<CreateJwtByPasswordWithPrefixCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JwtTokenModel(CreateJwtToken()));

            await mock.Object.GetJwtByPasswordAsync(
                "login",
                "password",
                " /api/identity/ ");

            mock.Verify(mediator => mediator.SendAsync<
                CreateJwtByPasswordWithPrefixCommand, JwtTokenModel>(
                It.Is<CreateJwtByPasswordWithPrefixCommand>(
                    command =>
                        command.Key == "api/identity" &&
                        command.Login == "login" &&
                        command.Password == "password"),
                CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task GetJwtByCodeAsync_ShouldSupportCustomRoutePrefix()
        {
            var mock = new Mock<IClientMediator>();
            mock.Setup(mediator => mediator.SendAsync<
                    CreateJwtByCodeWithPrefixCommand, JwtTokenModel>(
                    It.IsAny<CreateJwtByCodeWithPrefixCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JwtTokenModel(CreateJwtToken()));

            await mock.Object.GetJwtByCodeAsync(
                "login",
                "code",
                "identity");

            mock.Verify(mediator => mediator.SendAsync<
                CreateJwtByCodeWithPrefixCommand, JwtTokenModel>(
                It.Is<CreateJwtByCodeWithPrefixCommand>(
                    command =>
                        command.Key == "identity" &&
                        command.Login == "login" &&
                        command.Code == "code"),
                CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public async Task GetJwtByRefreshAsync_ShouldSupportCustomRoutePrefix()
        {
            var mock = new Mock<IClientMediator>();
            mock.Setup(mediator => mediator.SendAsync<
                    CreateJwtByRefreshTokenWithPrefixCommand, JwtTokenModel>(
                    It.IsAny<CreateJwtByRefreshTokenWithPrefixCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JwtTokenModel(CreateJwtToken()));

            await mock.Object.GetJwtByRefreshAsync(
                "user",
                "refresh",
                "identity");

            mock.Verify(mediator => mediator.SendAsync<
                CreateJwtByRefreshTokenWithPrefixCommand, JwtTokenModel>(
                It.Is<CreateJwtByRefreshTokenWithPrefixCommand>(
                    command =>
                        command.Key == "identity" &&
                        command.UserId == "user" &&
                        command.RefreshTokenKey == "refresh"),
                CancellationToken.None),
                Times.Once);
        }

        [Fact]
        public void CustomPrefixOverloads_ShouldRejectInvalidCredentials()
        {
            var mock = new Mock<IClientMediator>();

            Assert.ThrowsAny<ArgumentException>(
                () => mock.Object.GetJwtByPasswordAsync(
                    null!,
                    "password",
                    "identity"));
            Assert.ThrowsAny<ArgumentException>(
                () => mock.Object.GetJwtByCodeAsync(
                    "login",
                    null!,
                    "identity"));
            Assert.ThrowsAny<ArgumentException>(
                () => mock.Object.GetJwtByRefreshAsync(
                    "user-id",
                    null!,
                    "identity"));
            mock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(" / ")]
        public async Task GetJwtByPasswordAsync_ShouldRejectInvalidRoutePrefix(
            string routePrefix)
        {
            var mock = new Mock<IClientMediator>();

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => mock.Object.GetJwtByPasswordAsync(
                    "login",
                    "password",
                    routePrefix).AsTask());
            mock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData("api//identity")]
        [InlineData("{tenant}/identity")]
        [InlineData("identity?query")]
        [InlineData("identity#fragment")]
        [InlineData(@"api\identity")]
        [InlineData("api identity")]
        [InlineData("identity:*")]
        [InlineData(".")]
        [InlineData("api/..")]
        [InlineData("identity\n")]
        public async Task
            GetJwtByPasswordAsync_ShouldRejectUnsafeRoutePrefix(
                string routePrefix)
        {
            var mock = new Mock<IClientMediator>();

            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => mock.Object.GetJwtByPasswordAsync(
                    "login",
                    "password",
                    routePrefix).AsTask());
            mock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetJwtByRefreshAsync_ShouldNormalizeUserId()
        {
            var mock = new Mock<IClientMediator>();
            mock.Setup(mediator => mediator.SendAsync<
                    CreateJwtByRefreshTokenCommand, JwtTokenModel>(
                    It.IsAny<CreateJwtByRefreshTokenCommand>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new JwtTokenModel(CreateJwtToken()));

            await mock.Object.GetJwtByRefreshAsync(
                " user-id ",
                "refresh");

            mock.Verify(mediator => mediator.SendAsync<
                CreateJwtByRefreshTokenCommand, JwtTokenModel>(
                It.Is<CreateJwtByRefreshTokenCommand>(
                    command => command.UserId == "user-id"),
                CancellationToken.None),
                Times.Once);
        }

        private static string CreateJwtToken(
            string userId = "user-id",
            DateTime? expires = null)
        {
            var token = new JwtSecurityToken(
                claims: [new Claim(ClaimTypes.NameIdentifier, userId)],
                expires: expires ?? DateTime.UtcNow.AddMinutes(10));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}


