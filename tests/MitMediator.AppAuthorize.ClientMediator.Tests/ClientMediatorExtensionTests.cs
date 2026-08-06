using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
            var model = new JwtTokenModel
            {
                JwtToken = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = "refresh-token",
                RefreshTokenExpires = expires.AddDays(1)
            };

            Assert.Equal("user-id", model.UserId);
            Assert.Equal(expires, model.JwtTokenExpires, precision: TimeSpan.FromSeconds(1));
            Assert.Equal("refresh-token", model.RefreshToken);
            Assert.Equal(expires.AddDays(1), model.RefreshTokenExpires);
        }

        [Fact]
        public async Task GetJwtByPasswordAsync_Calls_SendAsync_With_Command()
        {
            var mock = new Mock<IClientMediator>();
            var expected = new JwtTokenModel { JwtToken = "t", RefreshToken = "r", RefreshTokenExpires = DateTime.UtcNow.AddDays(1) };
            mock.Setup(m => m.SendAsync<CreateJwtByPasswordCommand, JwtTokenModel>(It.IsAny<ClientMediator.CreateJwtByPasswordCommand>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<JwtTokenModel>(expected));

            var result = await mock.Object.GetJwtByPasswordAsync("login", "pwd", CancellationToken.None);

            Assert.Same(expected, result);
            mock.Verify(m => m.SendAsync<CreateJwtByPasswordCommand, JwtTokenModel>(
                It.Is<CreateJwtByPasswordCommand>(c => c.Login == "login" && c.Password == "pwd"),
                CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task GetJwtByCodeAsync_Calls_SendAsync_With_Command()
        {
            var mock = new Mock<IClientMediator>();
            var expected = new JwtTokenModel { JwtToken = "t2", RefreshToken = "r2", RefreshTokenExpires = DateTime.UtcNow.AddDays(1) };
            mock.Setup(m => m.SendAsync<CreateJwtByCodeCommand, JwtTokenModel>(It.IsAny<ClientMediator.CreateJwtByCodeCommand>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<JwtTokenModel>(expected));

            var result = await mock.Object.GetJwtByCodeAsync("login", "code", CancellationToken.None);

            Assert.Same(expected, result);
            mock.Verify(m => m.SendAsync<CreateJwtByCodeCommand, JwtTokenModel>(
                It.Is<ClientMediator.CreateJwtByCodeCommand>(c => c.Login == "login" && c.Code == "code"),
                CancellationToken.None), Times.Once);
        }

        [Fact]
        public async Task GetJwtByRefreshAsync_Calls_SendAsync_With_Command()
        {
            var mock = new Mock<IClientMediator>();
            var expected = new JwtTokenModel { JwtToken = "t3", RefreshToken = "r3", RefreshTokenExpires = DateTime.UtcNow.AddDays(1) };
            mock.Setup(m => m.SendAsync<CreateJwtByRefreshTokenCommand, JwtTokenModel>(It.IsAny<CreateJwtByRefreshTokenCommand>(), It.IsAny<CancellationToken>()))
                .Returns(new ValueTask<JwtTokenModel>(expected));

            var result = await mock.Object.GetJwtByRefreshAsync("user", "refreshKey", CancellationToken.None);

            Assert.Same(expected, result);
            mock.Verify(m => m.SendAsync<CreateJwtByRefreshTokenCommand, JwtTokenModel>(
                It.Is<CreateJwtByRefreshTokenCommand>(c => c.UserId == "user" && c.RefreshTokenKey == "refreshKey"),
                CancellationToken.None), Times.Once);
        }
    }
}


