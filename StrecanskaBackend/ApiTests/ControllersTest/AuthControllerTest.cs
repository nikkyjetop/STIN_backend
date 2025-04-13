using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using StrecanskaBackend.Controllers;

namespace StrecanskaBackend.ControllersTest
{
    public class AuthControllerTest
    {
        [Fact]
        public void ValidateCode_ShouldReturnOk_WhenCodeIsCorrect()
        {
            AuthController controller = new();
            string validCode = "TUL123";

            IActionResult result = controller.ValidateCode(validCode);

            result.Should().BeOfType<OkObjectResult>();
            OkObjectResult? okResult = result as OkObjectResult;
            okResult!.Value.Should().Be("OK");
        }

        [Fact]
        public void ValidateCode_ShouldReturnUnauthorized_WhenCodeIsIncorrect()
        {
            AuthController controller = new();
            string invalidCode = "wrong";

            IActionResult result = controller.ValidateCode(invalidCode);

            result.Should().BeOfType<UnauthorizedObjectResult>();
            UnauthorizedObjectResult? unauthorizedResult = result as UnauthorizedObjectResult;
            unauthorizedResult!.Value.Should().Be("Unauthorized");
        }
    }
}
