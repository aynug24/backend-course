using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;

namespace BadNews.Repositories.Weather
{
    public class WeatherForecastRepository : IWeatherForecastRepository
    {
        private const string defaultWeatherImageUrl = "/images/cloudy.png";

        private readonly IOptions<OpenWeatherOptions> weatherOptions;
        private readonly Random random = new Random();

        public WeatherForecastRepository(IOptions<OpenWeatherOptions> weatherOptions)
        {
            this.weatherOptions = weatherOptions;
        }

        public async Task<WeatherForecast> GetWeatherForecastAsync()
        {
            var apiKey = weatherOptions?.Value.ApiKey;

            var openWeatherForecast = await GetOpenWeatherForecastAsync(apiKey);

            return openWeatherForecast != null
                ? WeatherForecast.CreateFrom(openWeatherForecast)
                : BuildRandomForecast();
        }

        private const string resortToRngMsg = "Resorting to shaman-provided data.";

        private static async Task<OpenWeatherForecast> GetOpenWeatherForecastAsync(string apiKey)
        {
            try
            {
                return await new OpenWeatherClient(apiKey).GetWeatherFromApiAsync();
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                Log.Error(ex, $"Couldn't get open weather. {resortToRngMsg}");
            }
            catch (JsonException ex)
            {
                Log.Error(ex, $"Couldn't parse open weather. {resortToRngMsg}");
            }

            // дохло от формата json-а (int -> decimal поля изменились)
            // можно, кстати, оборачивать <vc:... /> в @try/catch, чтобы необязательный комп не валил весь сайт
            // красота такая себе)
            return null;
        }

        private WeatherForecast BuildRandomForecast()
        {
            var temperature = random.Next(-20, 20 + 1);
            return new WeatherForecast
            {
                TemperatureInCelsius = temperature,
                IconUrl = defaultWeatherImageUrl
            };
        }
    }
}
