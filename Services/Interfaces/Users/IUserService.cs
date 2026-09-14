using Microsoft.AspNetCore.Mvc;
using MyApi.Models.Dtos.Users;

namespace MyApi.Services.Interfaces.Users;

public interface IUserService
{
    Task<ActionResult<UserDto>> GetUserByIdAsync(Guid id);
    Task<ActionResult<IEnumerable<UserDto>>> GetAllUsersAsync();
    Task<ActionResult<UserDto>> CreateUserAsync(CreateUserDto createUser);
    Task<ActionResult<UserDto>> UpdateUserAsync(Guid id, CreateUserDto updateUser);
    Task<ActionResult<UserDto>> DeleteUserAsync(Guid id);
}
