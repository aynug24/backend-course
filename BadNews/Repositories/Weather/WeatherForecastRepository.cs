using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
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

            var weatherForecast = openWeatherForecast != null
                ? WeatherForecast.CreateFrom(openWeatherForecast)
                : BuildRandomForecast();

            return weatherForecast;
        }

        private static async Task<OpenWeatherForecast> GetOpenWeatherForecastAsync(string apiKey)
        {
            throw new Exception();
            try
            {
                return await new OpenWeatherClient(apiKey).GetWeatherFromApiAsync();
            } // дохло от формата json-а, хочется необязательные компоненты прям оборачивать в трай-рендеры
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                Log.Error(ex, "Couldn't get open weather. Resorting to shaman-provided data.");
            }

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
