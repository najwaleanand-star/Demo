```markdown
# Acme.UserManagement.Services - UserService

## 1. Overview

The `UserService` is a core component within the `Acme.UserManagement.Services` namespace, responsible for orchestrating and executing all primary user management operations. It acts as an abstraction layer over data persistence and integrates with various cross-cutting concerns such as logging and configuration.

## 2. Purpose

The primary purpose of the `UserService` is to provide a centralized and consistent interface for managing user-related operations within the `Acme` ecosystem. It encapsulates business logic related to user creation, retrieval, modification, and deletion, ensuring data integrity and adherence to application policies. This service aims to decouple user-specific business rules from lower-level data access mechanisms and higher-level API controllers or application services.

## 3. Key Components

The `UserService` leverages several internal and external components to fulfill its responsibilities. *(Assumption: The UserService follows standard dependency injection patterns for its dependencies.)*

*   **`IUserRepository`**: An interface abstracting data access for user entities. It provides methods for CRUD (Create, Read, Update, Delete) operations against the persistent user store (e.g., database).
*   **`ILogger<UserService>`**: Utilized for logging operational events, warnings, and errors to provide visibility into the service's runtime behavior.
*   **`IConfiguration`** (or specific options classes like `IOptions<UserManagementOptions>`): Provides access to application-wide or `UserService`-specific configuration settings, such as password policies or default user roles.
*   **User DTOs/Models**: Data Transfer Objects (DTOs) and domain models (`User`, `UserRegistrationDto`, `UserProfileUpdateDto`, etc.) used for input and output across the service boundary, ensuring clean separation of concerns.

## 4. Public Interfaces / Functions

The `UserService` exposes the following public methods for interaction. All methods are designed to be asynchronous where appropriate for non-blocking I/O operations. *(Assumption: Methods are typically asynchronous and use DTOs for data transfer, and throw specific exceptions for error conditions, following common .NET patterns.)*

*   **`Task<UserDto> RegisterUserAsync(UserRegistrationDto registrationData)`**:
    *   Creates a new user account with the provided `registrationData`.
    *   Performs input validation, password hashing, and assigns default roles based on configuration.
    *   **Throws**: `DuplicateUserException` if a user with the same email already exists.
    *   **Returns**: A `UserDto` representing the newly created user.
*   **`Task<UserDto?> GetUserByIdAsync(Guid userId)`**:
    *   Retrieves a user by their unique identifier.
    *   **Returns**: A `UserDto` if found, otherwise `null`.
*   **`Task<UserDto?> GetUserByEmailAsync(string email)`**:
    *   Retrieves a user by their email address.
    *   **Returns**: A `UserDto` if found, otherwise `null`.
*   **`Task<UserDto> UpdateUserProfileAsync(Guid userId, UserProfileUpdateDto updateData)`**:
    *   Updates the profile information for an existing user identified by `userId` with the `updateData` provided.
    *   Performs validation on `updateData`.
    *   **Throws**: `UserNotFoundException` if the `userId` does not correspond to an existing user.
    *   **Returns**: The updated `UserDto`.
*   **`Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)`**:
    *   Allows a user to change their password, verifying the `currentPassword` before applying the `newPassword`.
    *   **Throws**: `UserNotFoundException`, `InvalidCredentialsException` (if `currentPassword` is incorrect), or `ValidationException` (if `newPassword` fails policy).
*   **`Task DeleteUserAsync(Guid userId)`**:
    *   Permanently removes a user account and associated data from the system.
    *   **Throws**: `UserNotFoundException` if the user does not exist.

## 5. Configuration / Environment variables

The `UserService` relies on configuration settings which are typically loaded via `IConfiguration` at application startup. While direct environment variables are usually consumed by `IConfiguration`, the service itself consumes the resulting structured configuration.

Key configuration aspects include:

*   **`UserManagement:PasswordPolicy:MinLength`**: Integer specifying the minimum allowed length for user passwords (e.g., `8`).
*   **`UserManagement:PasswordPolicy:RequiresDigit`**: Boolean indicating if passwords must contain at least one digit (e.g., `true`).
*   **`UserManagement:PasswordPolicy:RequiresUppercase`**: Boolean indicating if passwords must contain at least one uppercase letter (e.g., `true`).
*   **`UserManagement:DefaultRolesForNewUsers`**: A string array or comma-separated string of roles automatically assigned to newly registered users (e.g., `["User", "Customer"]`).
*   **`UserManagement:AccountLockoutEnabled`**: Boolean to enable or disable account lockout functionality after a configurable number of failed login attempts.

These settings are typically defined in `appsettings.json`, `appsettings.{Environment}.json`, or overridden by environment variables (e.g., `UserManagement__PasswordPolicy__MinLength` for .NET applications).

## 6. Usage Example

Here's a simplified example demonstrating how the `UserService` would be integrated and used within a typical .NET application, assuming dependency injection is configured. *(Assumption: The example uses common ASP.NET Core controller patterns with `IActionResult` and DTOs.)*

```csharp
using Acme.UserManagement.Services.Models; // Assuming DTOs are here
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

// Define custom exceptions used by the UserService (defined elsewhere)
public class DuplicateUserException : Exception { public DuplicateUserException(string message) : base(message) { } }
public class UserNotFoundException : Exception { public UserNotFoundException(string message) : base(message) { } }
public class InvalidCredentialsException : Exception { public InvalidCredentialsException(string message) : base(message) { } }
public class ValidationException : Exception { public ValidationException(string message) : base(message) { } public object ValidationErrors { get; set; } }

// Example Controller (e.g., in Acme.UserManagement.Api)
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="registrationDto">User registration details.</param>
    /// <returns>The newly created user's DTO.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> Register([FromBody] UserRegistrationDto registrationDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var newUser = await _userService.RegisterUserAsync(registrationDto);
            _logger.LogInformation("User {UserId} registered successfully.", newUser.Id);
            return Ok(newUser);
        }
        catch (DuplicateUserException ex)
        {
            _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Registration validation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message, errors = ex.ValidationErrors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during user registration.");
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    /// <summary>
    /// Retrieves a user's profile by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the user.</param>
    /// <returns>The user's DTO.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserProfile(Guid id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = $"User with ID '{id}' not found." });
        }
        return Ok(user);
    }

    /// <summary>
    /// Allows a user to change their password.
    /// </summary>
    /// <param name="userId">The ID of the user changing the password.</param>
    /// <param name="changePasswordDto">Current and new password details.</param>
    /// <returns>No content on successful password change.</returns>
    [HttpPut("{userId:guid}/password")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ChangePassword(Guid userId, [FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _userService.ChangePasswordAsync(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            _logger.LogInformation("Password changed for user {UserId}.", userId);
            return NoContent();
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "Password change failed: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidCredentialsException ex)
        {
            _logger.LogWarning(ex, "Password change failed: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "New password validation failed: {Message}", ex.Message);
            return BadRequest(new { message = ex.Message, errors = ex.ValidationErrors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during password change for user {UserId}.", userId);
            return StatusCode(500, "An internal server error occurred.");
        }
    }
}

// --- Example DTOs (typically defined in a separate Models or DTOs project/folder) ---
public interface IUserService // Minimal interface for DI example
{
    Task<UserDto> RegisterUserAsync(UserRegistrationDto registrationData);
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    // ... other methods as defined in Section 4
}

public class UserRegistrationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [MinLength(8)] // Example validation
    public string Password { get; set; }
    [Required]
    public string FirstName { get; set; }
    [Required]
    public string LastName { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public DateTime CreatedDate { get; set; }
    public IEnumerable<string> Roles { get; set; } = new List<string>();
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; }
    [Required]
    [MinLength(8)] // Example validation
    public string NewPassword { get; set; }
}
```

## 7. Edge Cases / Notes

*   **Concurrency**: For operations like `UpdateUserProfileAsync`, where multiple requests might modify the same user concurrently, consider implementing optimistic concurrency control (e.g., using ETag or version fields on the user entity/DTO) within the `IUserRepository` layer to prevent lost updates.
*   **Data Validation**: Robust input validation is crucial. It is performed at the service entry point (e.g., via DTO validation attributes, FluentValidation) to ensure data integrity before reaching core business logic or the persistence layer.
*   **Error Handling**: The service is designed to throw specific, descriptive exceptions (e.g., `DuplicateUserException`, `UserNotFoundException`, `InvalidCredentialsException`) that higher layers can catch and translate into appropriate API responses or user feedback. Generic exceptions are caught and logged as critical errors.
*   **Password Security**: Passwords are never stored in plain text. Secure hashing algorithms (e.g., Argon2, bcrypt, or PBKDF2 with strong salts) are applied during user registration and verification during login/password changes.
*   **Authorization vs. Authentication**: The `UserService` primarily handles user *authentication* (verifying identity) and managing user data. *Authorization* (what an authenticated user is allowed to do) is typically handled at a higher layer (e.g., API gateway, controllers, middleware) using roles or claims managed or provided by `UserService` functionality. For instance, `DeleteUserAsync` would often require prior administrator role checks.
*   **Performance**: For very large user bases, operations involving extensive searching or filtering of users might require optimized data access patterns (e.g., database indexing, pagination, eventual consistency models for read operations). The `IUserRepository` implementation should support these optimizations.
*   **Soft Deletion**: While `DeleteUserAsync` implies permanent removal, some applications might prefer "soft deletion" (marking a user as inactive rather than physically deleting) to retain historical data, simplify recovery, or manage regulatory compliance. If soft deletion is desired, the `UserService` would need an explicit method or a configuration setting to control this behavior, with the implementation residing in `IUserRepository`.
```