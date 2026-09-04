// WeatherSystem.cs — gắn vào WeatherSystem object
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum WeatherType { None, Rain, Snow, Fog, Storm }
public enum Season { Spring, Summer, Autumn, Winter }

public class WeatherSystem : MonoBehaviour
{
    public static WeatherSystem Instance;

    [Header("References")]
    public ParticleSystem rainParticle;
    public ParticleSystem snowParticle;
    public ParticleSystem fogParticle;
    public Image fogOverlay;

    [Header("Weather Text")]
    public TMP_Text weatherText;
    public float notifyDuration = 2f;   // hiện bao lâu
    public float fadeDuration = 0.5f; // fade out bao lâu

    [Header("Weather Settings")]
    public float minWeatherDuration = 60f;  // thời gian thời tiết tối thiểu (giây)
    public float maxWeatherDuration = 180f; // tối đa

    [Header("Rain Overlay")]
    public Color rainOverlayColor = new Color(0.1f, 0.1f, 0.2f, 0.2f); // tối nhẹ khi mưa
    public Color lightFogColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
    public Color heavyFogColor = new Color(0.6f, 0.6f, 0.7f, 0.5f);

    // State
    private WeatherType currentWeather = WeatherType.None;
    private Season currentSeason = Season.Spring;
    private float weatherTimer = 0f;
    private Coroutine notifyRoutine;

    // Weather names
    static readonly string[] weatherNames =
    {
        "", "🌧 Mưa", "❄ Tuyết", "🌫 Sương mù", "⛈ Bão"
    };

    [System.Serializable]
    public class SeasonWeatherChance
    {
        public string seasonName;
        [Range(0f, 1f)] public float chanceNone;
        [Range(0f, 1f)] public float chanceRain;
        [Range(0f, 1f)] public float chanceSnow;
        [Range(0f, 1f)] public float chanceFog;
        [Range(0f, 1f)] public float chanceStorm;
    }

    [Header("Season Weather Chances")]
    public SeasonWeatherChance[] seasonChances = new SeasonWeatherChance[]
    {
        new SeasonWeatherChance { seasonName="Spring", chanceNone=0.3f, chanceRain=0.4f, chanceSnow=0f,   chanceFog=0.3f, chanceStorm=0f   },
        new SeasonWeatherChance { seasonName="Summer", chanceNone=0.5f, chanceRain=0.3f, chanceSnow=0f,   chanceFog=0f,   chanceStorm=0.2f },
        new SeasonWeatherChance { seasonName="Autumn", chanceNone=0.3f, chanceRain=0.3f, chanceSnow=0f,   chanceFog=0.4f, chanceStorm=0f   },
        new SeasonWeatherChance { seasonName="Winter", chanceNone=0.3f, chanceRain=0f,   chanceSnow=0.5f, chanceFog=0f,   chanceStorm=0.2f },
    };

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (weatherText != null)
        {
            weatherText.alpha = 0f;
            weatherText.gameObject.SetActive(false);
        }

        // Lắng nghe ngày mới để đổi mùa
        if (DayNightSystem.Instance != null)
            DayNightSystem.Instance.OnNewDay += OnNewDay;

        // Random thời tiết đầu game
        ChangeWeather(GetRandomWeather());
    }

    void Update()
    {
        weatherTimer -= Time.deltaTime;
        if (weatherTimer <= 0f)
            ChangeWeather(GetRandomWeather());
    }

    void OnNewDay(int day)
    {
        // Đổi mùa mỗi 7 ngày
        currentSeason = (Season)(((day - 1) / 7) % 4);
        Debug.Log($"[Weather] Mùa: {currentSeason}");
    }

    WeatherType GetRandomWeather()
    {
        var s = seasonChances[(int)currentSeason];
        float[] chances = { s.chanceNone, s.chanceRain, s.chanceSnow, s.chanceFog, s.chanceStorm };

        float roll = Random.value;
        float cumulative = 0f;
        for (int i = 0; i < chances.Length; i++)
        {
            cumulative += chances[i];
            if (roll <= cumulative) return (WeatherType)i;
        }
        return WeatherType.None;
    }

    public void ChangeWeather(WeatherType newWeather)
    {
        currentWeather = newWeather;
        weatherTimer = Random.Range(minWeatherDuration, maxWeatherDuration);

        // Tắt tất cả
        StopAllWeatherEffects();

        // Bật effect tương ứng
        switch (newWeather)
        {
            case WeatherType.Rain:
                if (rainParticle != null) rainParticle.Play();
                SetFog(rainOverlayColor); // tối nhẹ khi mưa
                break;

            case WeatherType.Snow:
                if (snowParticle != null) snowParticle.Play();
                SetFog(Color.clear);
                break;

            case WeatherType.Fog:
                SetFog(lightFogColor);
                break;

            case WeatherType.Storm:
                if (rainParticle != null)
                {
                    var emission = rainParticle.emission;
                    emission.rateOverTime = 300f; // mưa nhiều hơn
                    rainParticle.Play();
                }
                SetFog(heavyFogColor);
                break;

            case WeatherType.None:
                SetFog(Color.clear);
                break;
        }

        // Bật/tắt lightning
        bool hasLightning = newWeather == WeatherType.Rain || newWeather == WeatherType.Storm;
        LightningSystem.Instance?.SetActive(hasLightning);
        ShowWeatherNotify(weatherNames[(int)newWeather]);

        Debug.Log($"[Weather] Thời tiết: {newWeather} | Mùa: {currentSeason} | Kéo dài: {weatherTimer:F0}s");
    }

    void StopAllWeatherEffects()
    {
        if (rainParticle != null)
        {
            var emission = rainParticle.emission;
            emission.rateOverTime = 100f;
            rainParticle.Stop();
        }
        if (snowParticle != null) snowParticle.Stop();
        if (fogParticle != null) fogParticle.Stop();
    }

    void SetFog(Color color)
    {
        if (fogOverlay != null)
            fogOverlay.color = color;
    }

    void ShowWeatherNotify(string msg)
    {
        if (weatherText == null) return;
        if (notifyRoutine != null) StopCoroutine(notifyRoutine);
        notifyRoutine = StartCoroutine(NotifyRoutine(msg));
    }

    IEnumerator NotifyRoutine(string msg)
    {
        weatherText.text = msg;
        weatherText.alpha = 1f;
        weatherText.gameObject.SetActive(true);

        // Hiện trong notifyDuration giây
        yield return new WaitForSeconds(notifyDuration);

        // Fade out
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            weatherText.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        weatherText.alpha = 0f;
        weatherText.gameObject.SetActive(false);
    }

    public WeatherType CurrentWeather => currentWeather;
    public Season CurrentSeason => currentSeason;
}