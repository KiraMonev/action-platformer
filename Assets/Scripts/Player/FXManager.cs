using UnityEngine;

public class FXManager : MonoBehaviour
{
    public static FXManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<FXManager>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("FXManager");
                    _instance = obj.AddComponent<FXManager>();
                }
            }
            return _instance;
        }
    }
    private static FXManager _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Эффект крови при получении урона.
    /// direction — направление разлёта частиц (обычно от атакующего к цели).
    /// </summary>
    public void PlayHitBlood(Vector2 position, Vector2 direction)
    {
        var ps = CreateFX("HitBlood", position, direction);

        var main = ps.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.startColor = new Color(0.55f, 0.02f, 0.02f, 0.85f);
        main.gravityModifier = 1.4f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 26) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.05f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(0.55f, 0.02f, 0.02f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.01f, 0.01f), 1f) },
            new[] { new GradientAlphaKey(0.85f, 0f),
                    new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        SetShrinkOverLifetime(ps, 0.05f);
        ps.Play();
    }

    /// <summary>
    /// Эффект искр (например, удар мечом по твёрдой поверхности).
    /// </summary>
    public void PlayHitSparks(Vector2 position, Vector2 direction)
    {
        var ps = CreateFX("HitSparks", position, direction);

        var main = ps.main;
        main.duration = 0.4f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.0f, 7.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
        main.startColor = new Color(1f, 0.9f, 0.2f, 1f);
        main.gravityModifier = 0.8f;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 10, 15) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.02f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0f),
                    new GradientColorKey(new Color(1f, 0.35f, 0f), 0.7f) },
            new[] { new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        SetShrinkOverLifetime(ps, 0.1f);
        ps.Play();
    }

    private ParticleSystem CreateFX(string name, Vector2 position, Vector2 direction)
    {
        var go = new GameObject(name);
        go.transform.position = position;
        if (direction != Vector2.zero)
            go.transform.right = direction.normalized;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); // Остановить авто-воспроизведение перед настройкой

        var psr = go.GetComponent<ParticleSystemRenderer>();
        psr.material = new Material(Shader.Find("Sprites/Default"));
        psr.sortingOrder = 110;

        go.AddComponent<ParticleAutoDestroy>();
        return ps;
    }

    private void SetShrinkOverLifetime(ParticleSystem ps, float endSize)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var curve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, endSize));
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }
}

public class ParticleAutoDestroy : MonoBehaviour
{
    private ParticleSystem _ps;

    private void Start() => _ps = GetComponent<ParticleSystem>();

    private void Update()
    {
        if (_ps != null && !_ps.IsAlive(true))
            Destroy(gameObject);
    }
}
