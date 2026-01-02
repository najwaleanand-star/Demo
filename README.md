```markdown
# Acme.UserManagement.Services - UserService

## 1. Overview

The `UserService` is a core component within the `Acme.UserManagement.Services` namespace, responsible for orchestrating and executing all primary user management operations. It acts as an abstraction layer over data persistence and integrates with various cross-cutting concerns such as logging and configuration.

## 2. Purpose

The primary purpose of the `UserService` is to provide a centralized and consistent interface for managing user entities throughout their lifecycle. This includes, but is not limited to, user creation, retrieval, updates, deletions (both soft and hard), and potentially authentication-related operations such as password management. It encapsulates the business logic associated with user operations, ensuring data integrity and adherence to application-specific rules, decoupling these concerns from presentation or API layers.

## 3. Key Components

The `UserService` relies on several injected dependencies to perform its operations. Based on typical service architecture, these would include:

*   **`IUserRepository`**: An interface responsible for data access operations related to user entities (e.g., CRUD operations against a database).
    *   *Assumption*: Provides methods like `AddAsync`, `GetByIdAsync`, `GetByEmailAsync`, `UpdateAsync`, `DeleteAsync`.
*   **`ILogger<UserService>`**: For logging operational events, warnings, and errors.
    *   *Assumption*: Utilizes standard logging frameworks (e.g., `Microsoft.Extensions.Logging`).
*   **`IMapper`**: (e.g., AutoMapper) For mapping between domain entities (e.g., `User` entity) and Data Transfer Objects (DTOs) used by the service's public interfaces (e.g., `UserDto`, `UserCreationDto`).
    *   *Assumption*: Facilitates separation of concerns and prevents direct exposure of persistence models.
*   **`IPasswordHasher`**: An interface responsible for securely hashing and verifying user passwords.
    *   *Assumption*: Implements a strong, configurable hashing algorithm (e.g., BCrypt, Argon2, PBKDF2).
*   **`IOptions<UserManagementSettings>`**: For accessing service-specific configuration settings.
    *   *Assumption*: Provides access to settings like default roles, password policies, etc.
*   **`IValidator<T>`**: (Optional, but good practice) Interfaces for validating input DTOs before processing.
    *   *Assumption*: Utilizes a library like FluentValidation for complex validation rules.

## 4. Public Interfaces / Functions

The `UserService` exposes the following public methods for managing user accounts.

*   **`Task<UserDto> CreateUserAsync(UserCreationDto userCreationDto)`**
    *   **Description**: Creates a new user account. Includes password hashing and initial role assignment.
    *   **Parameters**:
        *   `userCreationDto`: An object containing the necessary data for user creation (e.g., `Email`, `Password`, `FirstName`, `LastName`).
    *   **Returns**: A `UserDto` representing the newly created user.
    *   **Throws**: `ValidationException` (if input is invalid), `DuplicateUserException` (if user with same email already exists).

*   **`Task<UserDto?> GetUserByIdAsync(Guid userId)`**
    *   **Description**: Retrieves a user by their unique identifier.
    *   **Parameters**:
        *   `userId`: The `Guid` of the user to retrieve.
    *   **Returns**: A `UserDto` if found, otherwise `null`.

*   **`Task<UserDto?> GetUserByEmailAsync(string email)`**
    *   **Description**: Retrieves a user by their email address.
    *   **Parameters**:
        *   `email`: The email address of the user to retrieve.
    *   **Returns**: A `UserDto` if found, otherwise `null`.

*   **`Task<IEnumerable<UserDto>> SearchUsersAsync(UserSearchCriteria criteria)`**
    *   **Description**: Retrieves a paged and filtered list of users based on specified criteria.
    *   **Parameters**:
        *   `criteria`: An object containing search parameters (e.g., `EmailContains`, `NameContains`, `PageNumber`, `PageSize`, `SortBy`).
    *   **Returns**: A collection of `UserDto` matching the criteria.

*   **`Task<UserDto> UpdateUserAsync(Guid userId, UserUpdateDto userUpdateDto)`**
    *   **Description**: Updates an existing user's details.
    *   **Parameters**:
        *   `userId`: The `Guid` of the user to update.
        *   `userUpdateDto`: An object containing the fields to update (e.g., `FirstName`, `LastName`, `IsActive`, `Roles`).
    *   **Returns**: The `UserDto` with updated information.
    *   **Throws**: `UserNotFoundException` (if user does not exist), `ValidationException` (if input is invalid).

*   **`Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)`**
    *   **Description**: Allows a user to change their password.
    *   **Parameters**:
        *   `userId`: The `Guid` of the user.
        *   `currentPassword`: The user's current password for verification.
        *   `newPassword`: The new password.
    *   **Throws**: `UserNotFoundException`, `InvalidPasswordException` (if current password doesn't match), `ValidationException` (if new password doesn't meet policy).

*   **`Task DeleteUserAsync(Guid userId, bool hardDelete = false)`**
    *   **Description**: Deletes a user account. Supports both soft deletion (marking as inactive) and hard deletion (permanent removal).
    *   **Parameters**:
        *   `userId`: The `Guid` of the user to delete.
        *   `hardDelete`: If `true`, permanently removes the user; otherwise, marks the user as inactive.
    *   **Throws**: `UserNotFoundException`.

## 5. Configuration / Environment variables

The `UserService` expects certain configurations to be available, typically managed via `appsettings.json` and environment variables in production.

*   **`UserManagementSettings:DefaultRoleForNewUsers`**:
    *   **Type**: `string`
    *   **Description**: The default role assigned to a user upon creation.
    *   **Example**: `"User"`
*   **`UserManagementSettings:PasswordPolicy:MinimumLength`**:
    *   **Type**: `int`
    *   **Description**: Minimum allowed length for user passwords.
*   **`UserManagementSettings:PasswordPolicy:RequireUppercase`**:
    *   **Type**: `bool`
    *   **Description**: Whether passwords must contain at least one uppercase character.
*   **`UserManagementSettings:PasswordPolicy:RequireLowercase`**:
    *   **Type**: `bool`
    *   **Description**: Whether passwords must contain at least one lowercase character.
*   **`UserManagementSettings:PasswordPolicy:RequireDigit`**:
    *   **Type**: `bool`
    *   **Description**: Whether passwords must contain at least one digit.
*   **`UserManagementSettings:PasswordPolicy:RequireNonAlphanumeric`**:
    *   **Type**: `bool`
    *   **Description**: Whether passwords must contain at least one non-alphanumeric character.
*   **`ConnectionStrings:UserManagementDb`**:
    *   **Type**: `string`
    *   **Description**: Connection string for the user management database. This is typically consumed by the `IUserRepository` implementation.
    *   *Assumption*: The `UserService` indirectly relies on this via its repository dependency.

These settings are typically loaded into a `UserManagementSettings` class and injected via `IOptions<UserManagementSettings>`.

## 6. Usage Example

To use the `UserService`, it must be registered with the Dependency Injection (DI) container. Below is an example of how to register and then consume the service in a typical ASP.NET Core application.

**1. Service Registration (e.g., `Startup.cs` or `Program.cs` in .NET 6+)**

```csharp
// Assuming extensions for adding repository, mapper, password hasher, etc.
public void ConfigureServices(IServiceCollection services)
{
    // Register configuration options
    services.Configure<UserManagementSettings>(Configuration.GetSection("UserManagementSettings"));

    // Register dependencies
    services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>(); // Example
    services.AddScoped<IUserRepository, SqlUserRepository>();     // Example
    services.AddAutoMapper(typeof(Startup));                     // Example, for DTO mapping

    // Register the UserService itself
    services.AddScoped<UserService>();

    // Register DTO validators (if using)
    services.AddScoped<IValidator<UserCreationDto>, UserCreationDtoValidator>();
    services.AddScoped<IValidator<UserUpdateDto>, UserUpdateDtoValidator>();
}
```

**2. Service Consumption (e.g., in a Controller or other service)**

```csharp
using Acme.UserManagement.Services;
using Acme.UserManagement.Services.Models; // Assuming DTOs are here
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(UserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] UserCreationDto userDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var createdUser = await _userService.CreateUserAsync(userDto);
            return CreatedAtAction(nameof(GetUserById), new { userId = createdUser.Id }, createdUser);
        }
        catch (DuplicateUserException ex)
        {
            _logger.LogWarning(ex, "Attempted to create duplicate user with email: {Email}", userDto.Email);
            return Conflict(new { message = $"User with email '{userDto.Email}' already exists." });
        }
        catch (ValidationException ex)
        {
            _logger.LogError(ex, "User creation failed due to validation errors.");
            return BadRequest(new { message = "Input validation failed.", errors = ex.Errors });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during user creation.");
            return StatusCode(500, "An internal server error occurred.");
        }
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetUserById(Guid userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }

    // Other actions like UpdateUser, DeleteUser, ChangePassword would follow similar patterns.
}
```

## 7. Edge Cases / Notes

*   **Concurrency**: Multiple requests attempting to update the same user simultaneously could lead to race conditions. The `IUserRepository` implementation should handle optimistic concurrency control (e.g., using row versions/timestamps) for update operations.
*   **Data Validation**: All input DTOs (e.g., `UserCreationDto`, `UserUpdateDto`) must be thoroughly validated at the service boundary to prevent invalid data from reaching the persistence layer. This includes format validation (email, password strength), uniqueness checks (email), and business rule validation.
*   **Soft vs. Hard Delete**: The `DeleteUserAsync` method supports both. Soft deletion (`IsActive = false`) is generally preferred for auditing, referential integrity, and potential user recovery. Hard deletion should be used with caution and potentially require additional authorization.
*   **Password Security**: Passwords are never stored in plain text. They are hashed using `IPasswordHasher` before persistence and verified using the same hasher. The `IPasswordHasher` should use a strong, salt-based, and computationally intensive algorithm.
*   **Error Handling**: The service methods are designed to throw specific exceptions (e.g., `UserNotFoundException`, `DuplicateUserException`, `ValidationException`) to signal distinct failure conditions, allowing callers to handle them appropriately. Generic `Exception` catches should be avoided when more specific errors are possible.
*   **Logging**: Comprehensive logging is essential for auditing, debugging, and operational monitoring. Log entries should capture relevant user identifiers (e.g., `userId`, `email`) and operational outcomes without exposing sensitive data.
*   **Extensibility**: Consider the need for future extensions (e.g., adding more user attributes, integrating with external identity providers). The service's design should facilitate adding new features without major refactoring.
*   **Transaction Management**: Complex operations involving multiple data changes should be wrapped in database transactions to ensure atomicity. The `IUserRepository` or an external unit-of-work pattern would typically manage this.
*   **Sensitive Data Handling**: Ensure that `UserDto`s returned by the service do not expose sensitive information like password hashes or internal system IDs unless explicitly required and authorized.
```