using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Acme.UserManagement.Services
{
    /// <summary>
    /// Service responsible for managing user-related operations such as
    /// creation, retrieval, and deactivation.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly UserServiceOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        public UserService(
            IUserRepository userRepository,
            ILogger<UserService> logger,
            IOptions<UserServiceOptions> options)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Creates a new user in the system.
        /// </summary>
        /// <param name="request">User creation request payload.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The created user.</returns>
        public async Task<User> CreateUserAsync(
            CreateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            _logger.LogInformation("Creating user with email {Email}", request.Email);

            if (!request.Email.EndsWith(_options.AllowedEmailDomain))
                throw new InvalidOperationException("Email domain not allowed.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Name = request.Name,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);

            return user;
        }

        /// <summary>
        /// Retrieves a user by identifier.
        /// </summary>
        public async Task<User?> GetUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return await _userRepository.GetByIdAsync(userId, cancellationToken);
        }

        /// <summary>
        /// Deactivates an existing user.
        /// </summary>
        public async Task DeactivateUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Attempted to deactivate non-existing user {UserId}", userId);
                return;
            }

            user.IsActive = false;
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }

    /// <summary>
    /// Configuration options for <see cref="UserService"/>.
    /// </summary>
    public class UserServiceOptions
    {
        public string AllowedEmailDomain { get; set; } = "@example.com";
    }

    /// <summary>
    /// User creation request DTO.
    /// </summary>
    public class CreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Domain model representing a user.
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>
    /// Abstraction for user persistence.
    /// </summary>
    public interface IUserRepository
    {
        Task AddAsync(User user, CancellationToken cancellationToken);
        Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
        Task UpdateAsync(User user, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Public contract for user services.
    /// </summary>
    public interface IUserService
    {
        Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken);
        Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken);
        Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken);
    }
}
