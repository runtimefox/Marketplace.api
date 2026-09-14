using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApi.Models.Dtos.Users;
using MyApi.Models.Entities;
using MyApi.Services.Interfaces.Users;

namespace MyApi.Controllers;

[Route("api/user")]
[ApiController]
[Authorize(Roles = nameof(UserRole.Admin))]

public class UserController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> Get()
    {
        return await userService.GetAllUsersAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> Get(Guid id)
    {
        return await userService.GetUserByIdAsync(id);
    }
}
