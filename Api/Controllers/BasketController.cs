using Api.Logic;
using Api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Api.Controllers
{
    [ApiController]
    [Route("Order")]
    public class BasketController(ILogger<BasketController> logger, IBasketProcessor basketProcessor, IBasketQuerier querier) : Controller
    {
        private ILogger<BasketController> _logger => logger;
        private IBasketProcessor _basketProcessor => basketProcessor;
        private IBasketQuerier _basketQuerier => querier;


        [HttpPost("[action]")]
        public async Task<IActionResult> Make([FromBody] CreateBasketRequest basket)
        {
            _logger.LogDebug("Processing new basket: {@basketData}", JsonSerializer.Serialize(basket));
            return await _basketProcessor
                .ProcessBasket(basket)
                .ContinueWith(t =>
                {
                    t.GetAwaiter().GetResult();
                    return Ok();
                });
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Get([FromBody] BasketFilters filters)
        {
            return await _basketQuerier
                .GetBaskets(filters)
                .ContinueWith(d => Ok(d.Result));
        }


        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            return await _basketQuerier
                .GetBasket(id)
                .ContinueWith(d => Ok(d.Result));
        }
    }
}
