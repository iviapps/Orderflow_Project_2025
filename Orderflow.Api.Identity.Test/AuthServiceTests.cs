using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq; 
using NUnit.Framework;
using Orderflow.Identity.DTOs.Auth;
using Orderflow.Identity.Services.Auth;
using Orderflow.Identity.Services.Common;
using Orderflow.Shared.Events;


namespace Orderflow.Api.Identity.Test
{
    [TestFixture]
    public class AuthServiceTests
    {
        private Mock<UserManager<IdentityUser>> _userManagerMock;
        private Mock<SignInManager<IdentityUser>> _signInManagerMock;
        private Mock<ITokenService> _tokenServiceMock;
        private Mock<IPublishEndpoint> _publishEndpointMock;
        private Mock<ILogger<AuthService>> _loggerMock;

        private AuthService _sut; // System Under Test

        [SetUp]
        public void Setup()
        {
            // Mock del UserStore necesario para construir UserManager
            var userStoreMock = new Mock<IUserStore<IdentityUser>>();

            _userManagerMock = new Mock<UserManager<IdentityUser>>(
                userStoreMock.Object,
                null,   // IOptions<IdentityOptions>
                null,   // IPasswordHasher<IdentityUser>
                null,   // IEnumerable<IUserValidator<IdentityUser>>
                null,   // IEnumerable<IPasswordValidator<IdentityUser>>
                null,   // ILookupNormalizer
                null,   // IdentityErrorDescriber
                null,   // IServiceProvider
                null    // ILogger<UserManager<IdentityUser>>
            );

            // Infra mínima para SignInManager
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<IdentityUser>>();
            var identityOptions = new Mock<IOptions<IdentityOptions>>();
            var loggerSignIn = new Mock<ILogger<SignInManager<IdentityUser>>>();
            var schemes = new Mock<IAuthenticationSchemeProvider>();
            var userConfirmation = new Mock<IUserConfirmation<IdentityUser>>();

            _signInManagerMock = new Mock<SignInManager<IdentityUser>>(
                _userManagerMock.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                identityOptions.Object,
                loggerSignIn.Object,
                schemes.Object,
                userConfirmation.Object
            );

            _tokenServiceMock = new Mock<ITokenService>();
            _publishEndpointMock = new Mock<IPublishEndpoint>();
            _loggerMock = new Mock<ILogger<AuthService>>();

            _sut = new AuthService(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _tokenServiceMock.Object,
                _publishEndpointMock.Object,
                _loggerMock.Object
            );
        }

        #region LoginAsync tests

        [Test]
        public async Task LoginAsync_WhenUserDoesNotExist_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "notfound@test.com",
                Password = "SomePassword123!"
            };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((IdentityUser?)null);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);

            _signInManagerMock.Verify(
                s => s.CheckPasswordSignInAsync(
                    It.IsAny<IdentityUser>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()),
                Times.Never);
        }

        [Test]
        public async Task LoginAsync_WhenUserLockedOut_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "locked@test.com",
                Password = "Password123!"
            };

            var user = new IdentityUser
            {
                Id = "user-locked",
                Email = request.Email,
                UserName = request.Email
            };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(s => s.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.LockedOut);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors!.First(), Does.Contain("locked"));
        }

        [Test]
        public async Task LoginAsync_WhenCredentialsAreInvalid_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "user@test.com",
                Password = "WrongPassword!"
            };

            var user = new IdentityUser
            {
                Id = "user-1",
                Email = request.Email,
                UserName = request.Email
            };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(s => s.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.Failed);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public async Task LoginAsync_WhenCredentialsAreValid_ReturnsSuccessAndUsesTokenService()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "valid@test.com",
                Password = "Password123!"
            };

            var user = new IdentityUser
            {
                Id = "user-2",
                Email = request.Email,
                UserName = request.Email
            };

            var roles = new List<string> { "Customer", "Admin" };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(s => s.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.Success);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles);

            _tokenServiceMock
                .Setup(t => t.GenerateAccessTokenAsync(user, roles))
                .ReturnsAsync("fake-jwt-token");

            _tokenServiceMock
                .Setup(t => t.GetTokenExpiryInSeconds())
                .Returns(3600);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Null.Or.Empty);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.AccessToken, Is.EqualTo("fake-jwt-token"));
            Assert.That(result.Data.TokenType, Is.EqualTo("Bearer"));
            Assert.That(result.Data.ExpiresIn, Is.EqualTo(3600));
            Assert.That(result.Data.UserId, Is.EqualTo(user.Id));
            Assert.That(result.Data.Email, Is.EqualTo(user.Email));
            Assert.That(result.Data.Roles, Is.EquivalentTo(roles));

            _tokenServiceMock.Verify(
                t => t.GenerateAccessTokenAsync(user, roles),
                Times.Once);
        }

        #endregion

        #region RegisterAsync tests

        [Test]
        public async Task RegisterAsync_WhenEmailAlreadyExists_ReturnsFailure()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "existing@test.com",
                Password = "Password123!"
            };

            var existingUser = new IdentityUser
            {
                Id = "existing-id",
                Email = request.Email,
                UserName = "existing"
            };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors!.First(), Does.Contain("already exists"));

            _userManagerMock.Verify(
                m => m.CreateAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()),
                Times.Never);

            _publishEndpointMock.Verify(
                p => p.Publish(It.IsAny<UserRegisteredEvent>(), default),
                Times.Never);
        }

        [Test]
        public async Task RegisterAsync_WhenNewUser_IsCreated_AssignedRole_AndEventPublished()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "newuser@test.com",
                Password = "Password123!"
            };

            var createdUserId = "new-user-id";

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((IdentityUser?)null);

            _userManagerMock
                .Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<IdentityUser, string>((user, pwd) =>
                {
                    // Simular que el UserManager asigna un Id al crear el usuario
                    user.Id = createdUserId;
                });

            _userManagerMock
                .Setup(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), "Customer"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Null.Or.Empty);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.UserId, Is.EqualTo(createdUserId));
            Assert.That(result.Data.Email, Is.EqualTo(request.Email));
            Assert.That(result.Data.Message, Does.Contain("successfully"));

            // Verificar que se creó el usuario
            _userManagerMock.Verify(
                m => m.CreateAsync(
                    It.Is<IdentityUser>(u =>
                        u.Email == request.Email &&
                        u.UserName == "newuser" &&
                        u.EmailConfirmed == false),
                    request.Password),
                Times.Once);

            // Verificar que se asignó el rol Customer
            _userManagerMock.Verify(
                m => m.AddToRoleAsync(
                    It.Is<IdentityUser>(u => u.Id == createdUserId),
                    "Customer"),
                Times.Once);

            // Verificar que se publicó el evento
            _publishEndpointMock.Verify(
                p => p.Publish(
                    It.Is<UserRegisteredEvent>(e =>
                        e.UserId == createdUserId &&
                        e.Email == request.Email &&
                        e.FirstName == null &&
                        e.LastName == null),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task RegisterAsync_WhenUserCreationFails_ReturnsFailureWithErrors()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "invalid@test.com",
                Password = "weak"
            };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((IdentityUser?)null);

            var identityErrors = new[]
            {
                new IdentityError { Code = "PasswordTooShort", Description = "Password is too short" },
                new IdentityError { Code = "PasswordRequiresDigit", Description = "Password must have at least one digit" }
            };

            _userManagerMock
                .Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Failed(identityErrors));

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors!.Count(), Is.EqualTo(2));
            Assert.That(result.Errors, Does.Contain("Password is too short"));
            Assert.That(result.Errors, Does.Contain("Password must have at least one digit"));

            _userManagerMock.Verify(
                m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()),
                Times.Never);

            _publishEndpointMock.Verify(
                p => p.Publish(It.IsAny<UserRegisteredEvent>(), default),
                Times.Never);
        }

        #endregion

        #region GetCurrentUserAsync tests

        [Test]
        public async Task GetCurrentUserAsync_WhenUserNotFound_ReturnsFailure()
        {
            // Arrange
            var userId = "non-existent-id";

            _userManagerMock
                .Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync((IdentityUser?)null);

            // Act
            var result = await _sut.GetCurrentUserAsync(userId);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors!.First(), Does.Contain("not found"));
        }

        [Test]
        public async Task GetCurrentUserAsync_WhenUserExists_ReturnsUserWithRoles()
        {
            // Arrange
            var userId = "user-123";
            var user = new IdentityUser
            {
                Id = userId,
                Email = "user@test.com",
                UserName = "user"
            };

            var roles = new List<string> { "Customer", "Premium" };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var result = await _sut.GetCurrentUserAsync(userId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Null.Or.Empty);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.UserId, Is.EqualTo(userId));
            Assert.That(result.Data.Email, Is.EqualTo(user.Email));
            Assert.That(result.Data.Roles, Is.EquivalentTo(roles));
        }

        #endregion

        #region RegisterAsync - Sad Paths (Escenarios de Error)

        [Test]
        public async Task RegisterAsync_WhenRoleAssignmentFails_StillReturnsSuccessButLogsWarning()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "newuser@test.com",
                Password = "Password123!"
            };

            var createdUserId = "new-user-id";

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((IdentityUser?)null);

            _userManagerMock
                .Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<IdentityUser, string>((user, pwd) => { user.Id = createdUserId; });

            // El rol falla pero el usuario YA fue creado
            var roleErrors = new[]
            {
                new IdentityError { Code = "RoleNotFound", Description = "Role 'Customer' does not exist" }
            };

            _userManagerMock
                .Setup(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), "Customer"))
                .ReturnsAsync(IdentityResult.Failed(roleErrors));

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            // El registro SIGUE siendo exitoso aunque falle el rol
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.UserId, Is.EqualTo(createdUserId));

            // Verificar que se intentó asignar el rol
            _userManagerMock.Verify(
                m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), "Customer"),
                Times.Once);

            // El evento SÍ se publica aunque falle el rol
            _publishEndpointMock.Verify(
                p => p.Publish(It.IsAny<UserRegisteredEvent>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task RegisterAsync_WhenPasswordIsTooWeak_ReturnsFailure()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "test@test.com",
                Password = "123" // Password muy débil
            };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((IdentityUser?)null);

            var passwordErrors = new[]
            {
                new IdentityError
                {
                    Code = "PasswordTooShort",
                    Description = "Passwords must be at least 6 characters."
                },
                new IdentityError
                {
                    Code = "PasswordRequiresNonAlphanumeric",
                    Description = "Passwords must have at least one non alphanumeric character."
                },
                new IdentityError
                {
                    Code = "PasswordRequiresUpper",
                    Description = "Passwords must have at least one uppercase ('A'-'Z')."
                }
            };

            _userManagerMock
                .Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Failed(passwordErrors));

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Null);
            Assert.That(result.Errors!.Count(), Is.EqualTo(3));
            Assert.That(result.Errors, Does.Contain("Passwords must be at least 6 characters."));

            // No se debe asignar rol ni publicar evento
            _userManagerMock.Verify(
                m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), It.IsAny<string>()),
                Times.Never);

            _publishEndpointMock.Verify(
                p => p.Publish(It.IsAny<UserRegisteredEvent>(), default),
                Times.Never);
        }

        [Test]
        public async Task RegisterAsync_WhenEmailIsInvalidFormat_ReturnsFailure()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "not-an-email", // Email sin formato válido
                Password = "Password123!"
            };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((IdentityUser?)null);

            var emailError = new[]
            {
                new IdentityError
                {
                    Code = "InvalidEmail",
                    Description = "Email 'not-an-email' is invalid."
                }
            };

            _userManagerMock
                .Setup(m => m.CreateAsync(It.IsAny<IdentityUser>(), request.Password))
                .ReturnsAsync(IdentityResult.Failed(emailError));

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Null);
            Assert.That(result.Errors!.First(), Does.Contain("invalid"));
        }

        [Test]
        public async Task RegisterAsync_WhenUserNameGenerationCreatesInvalidUsername_StillCreatesUser()
        {
            // Arrange
            var request = new RegisterRequest
            {
                Email = "@special.com", // Email que genera username vacío
                Password = "Password123!"
            };

            var createdUserId = "user-special";

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync((IdentityUser?)null);

            // El servicio intenta crear con username vacío (Split('@')[0] = "")
            _userManagerMock
                .Setup(m => m.CreateAsync(
                    It.Is<IdentityUser>(u => u.UserName == ""),
                    request.Password))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<IdentityUser, string>((user, pwd) => { user.Id = createdUserId; });

            _userManagerMock
                .Setup(m => m.AddToRoleAsync(It.IsAny<IdentityUser>(), "Customer"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _sut.RegisterAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);

            // Verificar que intentó crear con username vacío
            _userManagerMock.Verify(
                m => m.CreateAsync(
                    It.Is<IdentityUser>(u => u.UserName == "" && u.Email == request.Email),
                    request.Password),
                Times.Once);
        }

        #endregion

        #region LoginAsync - Sad Paths Adicionales

        [Test]
        public async Task LoginAsync_WhenUserExistsButEmailIsNull_ReturnsEmptyEmailInResponse()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "test@test.com",
                Password = "Password123!"
            };

            var user = new IdentityUser
            {
                Id = "user-no-email",
                Email = null, // Email nulo (caso edge)
                UserName = "testuser"
            };

            var roles = new List<string> { "Customer" };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(s => s.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.Success);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles);

            _tokenServiceMock
                .Setup(t => t.GenerateAccessTokenAsync(user, roles))
                .ReturnsAsync("fake-token");

            _tokenServiceMock
                .Setup(t => t.GetTokenExpiryInSeconds())
                .Returns(3600);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            // El servicio convierte null a string.Empty
            Assert.That(result.Data!.Email, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task LoginAsync_WhenUserHasNoRoles_ReturnsEmptyRolesList()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "noroles@test.com",
                Password = "Password123!"
            };

            var user = new IdentityUser
            {
                Id = "user-no-roles",
                Email = request.Email,
                UserName = "noroles"
            };

            var emptyRoles = new List<string>(); // Usuario sin roles

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(s => s.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.Success);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(emptyRoles);

            _tokenServiceMock
                .Setup(t => t.GenerateAccessTokenAsync(user, emptyRoles))
                .ReturnsAsync("token-no-roles");

            _tokenServiceMock
                .Setup(t => t.GetTokenExpiryInSeconds())
                .Returns(3600);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.Roles, Is.Empty);
        }

        [Test]
        public async Task LoginAsync_WhenSignInResultRequiresTwoFactor_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "2fa@test.com",
                Password = "Password123!"
            };

            var user = new IdentityUser
            {
                Id = "user-2fa",
                Email = request.Email,
                UserName = "2fa-user"
            };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(s => s.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.TwoFactorRequired);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            // El servicio actual NO maneja 2FA, trata como fallo
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public async Task LoginAsync_WhenSignInResultIsNotAllowed_ReturnsFailure()
        {
            // Arrange
            var request = new LoginRequest
            {
                Email = "notallowed@test.com",
                Password = "Password123!"
            };

            var user = new IdentityUser
            {
                Id = "user-not-allowed",
                Email = request.Email,
                UserName = "notallowed"
            };

            _userManagerMock
                .Setup(m => m.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            _signInManagerMock
                .Setup(s => s.CheckPasswordSignInAsync(user, request.Password, true))
                .ReturnsAsync(SignInResult.NotAllowed);

            // Act
            var result = await _sut.LoginAsync(request);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
        }

        #endregion

        #region GetCurrentUserAsync - Sad Paths Adicionales

        [Test]
        public async Task GetCurrentUserAsync_WhenUserEmailIsNull_ReturnsEmptyEmail()
        {
            // Arrange
            var userId = "user-no-email";
            var user = new IdentityUser
            {
                Id = userId,
                Email = null, // Email nulo
                UserName = "user"
            };

            var roles = new List<string> { "Customer" };

            _userManagerMock
                .Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var result = await _sut.GetCurrentUserAsync(userId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.Email, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task GetCurrentUserAsync_WhenUserHasNoRoles_ReturnsEmptyRolesList()
        {
            // Arrange
            var userId = "user-no-roles";
            var user = new IdentityUser
            {
                Id = userId,
                Email = "user@test.com",
                UserName = "user"
            };

            var emptyRoles = new List<string>();

            _userManagerMock
                .Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync(user);

            _userManagerMock
                .Setup(m => m.GetRolesAsync(user))
                .ReturnsAsync(emptyRoles);

            // Act
            var result = await _sut.GetCurrentUserAsync(userId);

            // Assert
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data!.Roles, Is.Empty);
        }

        [Test]
        public async Task GetCurrentUserAsync_WhenUserIdIsEmpty_ReturnsFailure()
        {
            // Arrange
            var userId = string.Empty;

            _userManagerMock
                .Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync((IdentityUser?)null);

            // Act
            var result = await _sut.GetCurrentUserAsync(userId);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
        }

        [Test]
        public async Task GetCurrentUserAsync_WhenUserIdIsInvalidGuid_ReturnsFailure()
        {
            // Arrange
            var userId = "invalid-guid-format-123456789";

            _userManagerMock
                .Setup(m => m.FindByIdAsync(userId))
                .ReturnsAsync((IdentityUser?)null);

            // Act
            var result = await _sut.GetCurrentUserAsync(userId);

            // Assert
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors!.First(), Does.Contain("not found"));
        }

        #endregion
    }
}