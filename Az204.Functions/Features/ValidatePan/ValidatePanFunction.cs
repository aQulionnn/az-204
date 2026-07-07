using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Az204.Functions.Features.ValidatePan;

public class ValidatePanFunction
{
    [Function("ValidatePan")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "validate-pan")] HttpRequest req)
    {
        var data = await req.ReadFromJsonAsync<ValidatePanRequest>();
        if (data is null)
            return new BadRequestObjectResult("Request body is required.");

        var pan = data.Pan;
        if (string.IsNullOrEmpty(pan))
            return new BadRequestObjectResult("Primary account number is required.");

        if (pan.Length is < 13 or > 19)
            return new BadRequestObjectResult("Primary account number must contain between 13 and 19 digits.");

        Span<byte> products = stackalloc byte[pan.Length];
        for (int i = 0; i < products.Length; i++)
        {
            if (!char.IsAsciiDigit(pan[i]))
                return new BadRequestObjectResult("Primary account number must contain only digits.");

            int weight = i % 2 == 0 ? 2 : 1, product = weight * (pan[i] - '0');

            if (product > 9)
                product = product / 10 + product % 10;

            products[i] = (byte)product;
        }

        int sum = 0;
        for (int i = 0; i < products.Length; i++)
            sum += products[i];

        var response = new ValidatePanResponse(sum % 10 == 0);
        return new OkObjectResult(response);
    }
}