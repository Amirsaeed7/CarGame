using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class NightEnvironmentController : MonoBehaviour
{
    [SerializeField] private PlayerLaneController player;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Volume environmentVolume;
    [SerializeField] private RoadSegmentPool roadManager;
    [SerializeField] private Camera gameCamera;

    private readonly Color[] fireworkColors =
    {
        new Color(0.2f, 0.9f, 1f),
        new Color(1f, 0.25f, 0.85f),
        new Color(1f, 0.72f, 0.2f),
        new Color(0.55f, 0.35f, 1f)
    };

    private Material roadMaterial;
    private Material laneMaterial;
    private Material cyanMaterial;
    private Material magentaMaterial;
    private Material headlightMaterial;
    private Material playerBodyMaterial;
    private Material rivalBodyMaterial;
    private Material windowMaterial;
    private Material tireMaterial;
    private Material hubMaterial;
    private Material taillightMaterial;
    private Material coinMaterial;
    private Material coinRimMaterial;
    private Material fuelBodyMaterial;
    private Material fuelAccentMaterial;
    private Material metalMaterial;
    private Material fireworkMaterial;
    private ParticleSystem fireworks;
    private float nextFireworkTime;
    private readonly List<Transform> spinningCollectibles = new List<Transform>();
    private readonly List<Transform> floatingCollectibles = new List<Transform>();

    private void Awake()
    {
        if (player == null || directionalLight == null || environmentVolume == null || roadManager == null)
        {
            Debug.LogError("Assign the player, directional light, global volume, and road manager.", this);
            enabled = false;
            return;
        }

        if (gameCamera == null)
        {
            gameCamera = Camera.main;
        }

        if (gameCamera == null)
        {
            Debug.LogError("A MainCamera is required for the night environment.", this);
            enabled = false;
            return;
        }

        if (!SetNightLighting())
        {
            enabled = false;
            return;
        }

        CreateMaterials();
        if (!enabled)
        {
            return;
        }

        CreateHeadlights();
        CreateFireworks();
    }

    private IEnumerator Start()
    {
        yield return null;
        ApplyRoadVisuals();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying || fireworks == null)
        {
            AnimateCollectibles();
            return;
        }

        AnimateCollectibles();

        if (Time.time >= nextFireworkTime)
        {
            LaunchFirework();
            nextFireworkTime = Time.time + Random.Range(2.5f, 4.5f);
        }
    }

    private void AnimateCollectibles()
    {
        float deltaTime = Time.deltaTime;
        for (int i = spinningCollectibles.Count - 1; i >= 0; i--)
        {
            Transform item = spinningCollectibles[i];
            if (item == null)
            {
                spinningCollectibles.RemoveAt(i);
                continue;
            }

            if (item.gameObject.activeInHierarchy)
            {
                item.Rotate(Vector3.up, 100f * deltaTime, Space.World);
            }
        }

        for (int i = floatingCollectibles.Count - 1; i >= 0; i--)
        {
            Transform item = floatingCollectibles[i];
            if (item == null)
            {
                floatingCollectibles.RemoveAt(i);
                continue;
            }

            if (item.gameObject.activeInHierarchy)
            {
                Vector3 position = item.localPosition;
                position.y = Mathf.Sin(Time.time * 2.4f + i) * 0.08f;
                item.localPosition = position;
                item.Rotate(Vector3.up, 45f * deltaTime, Space.Self);
            }
        }
    }

    private void OnDestroy()
    {
        Destroy(roadMaterial);
        Destroy(laneMaterial);
        Destroy(cyanMaterial);
        Destroy(magentaMaterial);
        Destroy(headlightMaterial);
        Destroy(playerBodyMaterial);
        Destroy(rivalBodyMaterial);
        Destroy(windowMaterial);
        Destroy(tireMaterial);
        Destroy(hubMaterial);
        Destroy(taillightMaterial);
        Destroy(coinMaterial);
        Destroy(coinRimMaterial);
        Destroy(fuelBodyMaterial);
        Destroy(fuelAccentMaterial);
        Destroy(metalMaterial);
        Destroy(fireworkMaterial);
    }

    private bool SetNightLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.035f, 0.05f, 0.11f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.018f, 0.025f, 0.065f);
        RenderSettings.fogDensity = 0.006f;

        directionalLight.color = new Color(0.28f, 0.38f, 0.72f);
        directionalLight.intensity = 0.2f;

        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color(0.008f, 0.012f, 0.04f);

        VolumeProfile profile = environmentVolume.profile;
        if (profile == null)
        {
            Debug.LogError("The global volume must have a profile assigned.", environmentVolume);
            return false;
        }

        ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(-0.45f);
        colorAdjustments.contrast.Override(12f);
        colorAdjustments.saturation.Override(-12f);
        colorAdjustments.colorFilter.Override(new Color(0.76f, 0.84f, 1f));

        if (profile.TryGet(out Bloom bloom))
        {
            bloom.threshold.Override(0.9f);
            bloom.intensity.Override(0.55f);
        }

        return true;
    }

    private void CreateMaterials()
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null)
        {
            Debug.LogError("The Universal Render Pipeline/Lit shader could not be found.", this);
            enabled = false;
            return;
        }

        roadMaterial = CreateMaterial(litShader, new Color(0.018f, 0.022f, 0.04f), Color.black);
        laneMaterial = CreateMaterial(litShader, new Color(0.04f, 0.35f, 0.55f), new Color(0.12f, 1.5f, 2.4f));
        cyanMaterial = CreateMaterial(litShader, new Color(0.02f, 0.32f, 0.42f), new Color(0.08f, 1.2f, 1.8f));
        magentaMaterial = CreateMaterial(litShader, new Color(0.35f, 0.04f, 0.24f), new Color(1.7f, 0.12f, 1.1f));
        headlightMaterial = CreateMaterial(litShader, Color.white, new Color(3f, 3.2f, 2.7f));
        playerBodyMaterial = CreateMaterial(litShader, new Color(0.035f, 0.12f, 0.28f), new Color(0.015f, 0.13f, 0.48f));
        rivalBodyMaterial = CreateMaterial(litShader, new Color(0.19f, 0.035f, 0.14f), new Color(0.38f, 0.018f, 0.16f));
        windowMaterial = CreateMaterial(litShader, new Color(0.018f, 0.06f, 0.12f), new Color(0.01f, 0.12f, 0.2f));
        tireMaterial = CreateMaterial(litShader, new Color(0.012f, 0.017f, 0.03f), Color.black);
        hubMaterial = CreateMaterial(litShader, new Color(0.18f, 0.32f, 0.48f), new Color(0.04f, 0.16f, 0.3f));
        taillightMaterial = CreateMaterial(litShader, new Color(0.65f, 0.015f, 0.025f), new Color(2.2f, 0.012f, 0.02f));
        coinMaterial = CreateMaterial(litShader, new Color(1f, 0.48f, 0.025f), new Color(2.4f, 0.68f, 0.025f));
        coinRimMaterial = CreateMaterial(litShader, new Color(1f, 0.78f, 0.18f), new Color(1.8f, 1.1f, 0.12f));
        fuelBodyMaterial = CreateMaterial(litShader, new Color(0.045f, 0.48f, 0.3f), new Color(0.02f, 0.36f, 0.15f));
        fuelAccentMaterial = CreateMaterial(litShader, new Color(0.85f, 0.95f, 0.85f), new Color(0.55f, 1.3f, 0.7f));
        metalMaterial = CreateMaterial(litShader, new Color(0.2f, 0.32f, 0.42f), new Color(0.06f, 0.22f, 0.3f));

        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
        {
            Debug.LogError("The URP particle shader could not be found; fireworks cannot be rendered.", this);
            enabled = false;
            return;
        }

        fireworkMaterial = new Material(particleShader);
        fireworkMaterial.SetColor("_BaseColor", Color.white);
    }

    private Material CreateMaterial(Shader shader, Color baseColor, Color emissionColor)
    {
        Material material = new Material(shader);
        material.SetColor("_BaseColor", baseColor);
        if (emissionColor.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
        }

        return material;
    }

    private void CreateHeadlights()
    {
        for (int i = 0; i < 2; i++)
        {
            float side = i == 0 ? -0.3f : 0.3f;
            GameObject headlight = new GameObject($"Headlight_{i + 1}");
            headlight.transform.SetParent(player.transform, false);
            headlight.transform.localPosition = new Vector3(side, 0.3f, 0.48f);
            headlight.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

            Light beam = headlight.AddComponent<Light>();
            beam.type = LightType.Spot;
            beam.color = new Color(0.78f, 0.88f, 1f);
            beam.intensity = 1600f;
            beam.range = 38f;
            beam.spotAngle = 52f;
            beam.innerSpotAngle = 34f;
            beam.shadows = LightShadows.None;

            GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lens.name = "GlowingLens";
            lens.transform.SetParent(headlight.transform, false);
            lens.transform.localPosition = new Vector3(0f, 0f, 0.08f);
            lens.transform.localScale = new Vector3(0.22f, 0.1f, 0.04f);
            lens.GetComponent<Renderer>().sharedMaterial = headlightMaterial;
            Destroy(lens.GetComponent<Collider>());
        }
    }

    private void CreateFireworks()
    {
        GameObject fireworkObject = new GameObject("Fireworks");
        fireworks = fireworkObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = fireworks.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 256;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.gravityModifier = 0.35f;
        main.startColor = Color.white;

        ParticleSystem.EmissionModule emission = fireworks.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = fireworks.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = fireworks.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

        ParticleSystemRenderer particleRenderer = fireworkObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = fireworkMaterial;
        fireworks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void LaunchFirework()
    {
        if (GameAudioController.Instance != null)
        {
            GameAudioController.Instance.PlayFirework();
        }

        float side = Random.value < 0.5f ? -1f : 1f;
        fireworks.transform.position = new Vector3(
            side * Random.Range(10f, 17f),
            Random.Range(8f, 15f),
            player.transform.position.z + Random.Range(35f, 55f));

        Color color = fireworkColors[Random.Range(0, fireworkColors.Length)];
        int particleCount = Random.Range(36, 52);
        fireworks.Play(true);
        for (int i = 0; i < particleCount; i++)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                startColor = color * Random.Range(0.75f, 1.25f)
            };
            fireworks.Emit(emitParams, 1);
        }
    }

    private void ApplyRoadVisuals()
    {
        foreach (Transform segment in roadManager.GetComponentsInChildren<Transform>())
        {
            if (!segment.name.StartsWith("RoadSegment_"))
            {
                continue;
            }

            Transform surface = segment.Find("RoadSurface");
            if (surface != null)
            {
                surface.GetComponent<Renderer>().sharedMaterial = roadMaterial;
            }

            Transform leftGuide = segment.Find("LeftLaneGuide");
            if (leftGuide != null)
            {
                leftGuide.GetComponent<Renderer>().sharedMaterial = laneMaterial;
            }

            Transform rightGuide = segment.Find("RightLaneGuide");
            if (rightGuide != null)
            {
                rightGuide.GetComponent<Renderer>().sharedMaterial = laneMaterial;
            }

            CreateRoadsideMarker(segment, -1, cyanMaterial);
            CreateRoadsideMarker(segment, 1, magentaMaterial);
        }

        BuildGameplayVisuals();
    }

    private void BuildGameplayVisuals()
    {
        BuildVehicle(player.transform, playerBodyMaterial, true);

        Interactable[] interactables = FindObjectsByType<Interactable>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Interactable item in interactables)
        {
            if (item is StationaryCarObstacle || item is MovingCarObstacle)
            {
                BuildVehicle(item.transform, rivalBodyMaterial, false);
            }
            else if (item is CoinCollectible)
            {
                BuildCoin(item.transform);
            }
            else if (item is FuelCanCollectible)
            {
                BuildFuelCan(item.transform);
            }
        }

        GameManager.Instance?.RefreshPlayerRenderers();
    }

    private void BuildVehicle(Transform vehicle, Material bodyMaterial, bool isPlayer)
    {
        if (vehicle == null || vehicle.Find("GeneratedVehicleVisuals") != null)
        {
            return;
        }

        Renderer rootRenderer = vehicle.GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        Transform visualRoot = CreateVisualRoot(vehicle, "GeneratedVehicleVisuals");
        CreateBox(visualRoot, "LowerChassis", new Vector3(1.26f, 0.28f, 2.65f),
            new Vector3(0f, 0.24f, 0f), bodyMaterial);
        CreateBox(visualRoot, "SculptedBody", new Vector3(1.48f, 0.32f, 2.25f),
            new Vector3(0f, 0.4f, -0.04f), bodyMaterial);
        CreateBox(visualRoot, "FrontHood", new Vector3(1.35f, 0.14f, 0.7f),
            new Vector3(0f, 0.58f, 0.77f), bodyMaterial);
        CreateBox(visualRoot, "Cabin", new Vector3(1.02f, 0.52f, 1.28f),
            new Vector3(0f, 0.77f, -0.12f), bodyMaterial);
        CreateBox(visualRoot, "Roof", new Vector3(0.87f, 0.12f, 0.82f),
            new Vector3(0f, 1.08f, -0.12f), cyanMaterial);
        CreateBox(visualRoot, "FrontWindshield", new Vector3(0.85f, 0.3f, 0.06f),
            new Vector3(0f, 0.78f, 0.48f), windowMaterial, new Vector3(22f, 0f, 0f));
        CreateBox(visualRoot, "RearWindow", new Vector3(0.82f, 0.28f, 0.06f),
            new Vector3(0f, 0.78f, -0.72f), windowMaterial, new Vector3(-22f, 0f, 0f));
        CreateBox(visualRoot, "FrontBumper", new Vector3(1.38f, 0.12f, 0.14f),
            new Vector3(0f, 0.23f, 1.32f), metalMaterial);
        CreateBox(visualRoot, "RearBumper", new Vector3(1.38f, 0.12f, 0.14f),
            new Vector3(0f, 0.23f, -1.32f), metalMaterial);
        CreateBox(visualRoot, "NeonSideSkirtLeft", new Vector3(0.045f, 0.07f, 1.35f),
            new Vector3(-0.7f, 0.29f, 0f), cyanMaterial);
        CreateBox(visualRoot, "NeonSideSkirtRight", new Vector3(0.045f, 0.07f, 1.35f),
            new Vector3(0.7f, 0.29f, 0f), magentaMaterial);

        for (int side = -1; side <= 1; side += 2)
        {
            float x = side * 0.59f;
            CreateBox(visualRoot, $"Headlamp_{side}", new Vector3(0.22f, 0.11f, 0.045f),
                new Vector3(x, 0.48f, 1.19f), headlightMaterial);
            CreateBox(visualRoot, $"Taillamp_{side}", new Vector3(0.23f, 0.12f, 0.045f),
                new Vector3(x, 0.45f, -1.19f), taillightMaterial);

            for (int axle = -1; axle <= 1; axle += 2)
            {
                float z = axle * 0.91f;
                CreateWheel(visualRoot, x, z);
            }
        }

        if (isPlayer)
        {
            CreateBox(visualRoot, "NeonFrontTrim", new Vector3(0.78f, 0.035f, 0.045f),
                new Vector3(0f, 0.32f, 1.37f), cyanMaterial);
        }
    }

    private void CreateWheel(Transform parent, float x, float z)
    {
        Transform wheel = CreatePrimitive(parent, "NeonTyre", PrimitiveType.Cylinder,
            new Vector3(x, 0.28f, z), Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), tireMaterial);
        Vector3 rootScale = GetAbsoluteScale(parent);
        wheel.localScale = new Vector3(0.52f / rootScale.x, 0.08f / rootScale.y, 0.54f / rootScale.z);
        wheel.localRotation = Quaternion.Euler(0f, 0f, 90f);

        for (int side = -1; side <= 1; side += 2)
        {
            Transform hub = CreatePrimitive(parent, "WheelHub", PrimitiveType.Cylinder,
                new Vector3(x + side * 0.07f, 0.28f, z), Vector3.zero, Vector3.one, hubMaterial);
            hub.localScale = new Vector3(0.25f / rootScale.x, 0.012f / rootScale.y, 0.27f / rootScale.z);
            hub.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }
    }

    private void BuildCoin(Transform coin)
    {
        if (coin == null || coin.Find("GeneratedCoinVisuals") != null)
        {
            return;
        }

        Renderer rootRenderer = coin.GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        Transform visualRoot = CreateVisualRoot(coin, "GeneratedCoinVisuals");
        Transform spinner = CreateVisualRoot(visualRoot, "CoinSpinner");
        CreatePrimitive(spinner, "CoinDisk", PrimitiveType.Cylinder,
            Vector3.zero, new Vector3(90f, 0f, 0f), new Vector3(0.72f, 0.12f, 0.72f), coinMaterial);
        spinningCollectibles.Add(spinner);

        CreatePrimitive(spinner, "CoinFaceFront", PrimitiveType.Cylinder,
            new Vector3(0f, 0f, 0.032f), new Vector3(90f, 0f, 0f),
            new Vector3(0.56f, 0.045f, 0.56f), coinRimMaterial);
        CreatePrimitive(spinner, "CoinFaceBack", PrimitiveType.Cylinder,
            new Vector3(0f, 0f, -0.032f), new Vector3(90f, 0f, 0f),
            new Vector3(0.56f, 0.045f, 0.56f), coinRimMaterial);

        CreateBox(spinner, "CoinMarkVertical", new Vector3(0.09f, 0.31f, 0.035f),
            new Vector3(0f, 0f, 0.07f), coinMaterial);
        CreateBox(spinner, "CoinMarkUpper", new Vector3(0.23f, 0.07f, 0.035f),
            new Vector3(0.03f, 0.11f, 0.07f), coinMaterial);
        CreateBox(spinner, "CoinMarkLower", new Vector3(0.23f, 0.07f, 0.035f),
            new Vector3(-0.03f, -0.11f, 0.07f), coinMaterial);
    }

    private void BuildFuelCan(Transform fuelCan)
    {
        if (fuelCan == null || fuelCan.Find("GeneratedFuelCanVisuals") != null)
        {
            return;
        }

        Renderer rootRenderer = fuelCan.GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            rootRenderer.enabled = false;
        }

        Transform visualRoot = CreateVisualRoot(fuelCan, "GeneratedFuelCanVisuals");
        floatingCollectibles.Add(visualRoot);

        CreateBox(visualRoot, "CanBody", new Vector3(0.68f, 0.82f, 0.36f),
            new Vector3(0f, 0f, 0f), fuelBodyMaterial);
        CreateBox(visualRoot, "CanShoulder", new Vector3(0.56f, 0.16f, 0.36f),
            new Vector3(0f, 0.45f, 0f), fuelBodyMaterial);
        CreateBox(visualRoot, "CanNeck", new Vector3(0.22f, 0.16f, 0.22f),
            new Vector3(0.2f, 0.56f, 0f), fuelBodyMaterial, new Vector3(0f, 0f, -18f));
        CreateBox(visualRoot, "CanSpout", new Vector3(0.3f, 0.1f, 0.18f),
            new Vector3(0.32f, 0.67f, 0f), fuelBodyMaterial, new Vector3(0f, 0f, -18f));
        CreateBox(visualRoot, "CanCap", new Vector3(0.2f, 0.07f, 0.2f),
            new Vector3(0.2f, 0.69f, 0f), metalMaterial);
        CreateBox(visualRoot, "HandleTop", new Vector3(0.28f, 0.065f, 0.07f),
            new Vector3(-0.13f, 0.6f, 0f), metalMaterial);
        CreateBox(visualRoot, "HandleSideLeft", new Vector3(0.065f, 0.23f, 0.07f),
            new Vector3(-0.26f, 0.51f, 0f), metalMaterial);
        CreateBox(visualRoot, "HandleSideRight", new Vector3(0.065f, 0.23f, 0.07f),
            new Vector3(0f, 0.51f, 0f), metalMaterial);
        CreateBox(visualRoot, "FuelStripe", new Vector3(0.08f, 0.54f, 0.035f),
            new Vector3(0f, -0.04f, 0.2f), fuelAccentMaterial);
        CreateBox(visualRoot, "FuelCrossHorizontal", new Vector3(0.34f, 0.09f, 0.04f),
            new Vector3(0.13f, -0.13f, 0.22f), fuelAccentMaterial);
        CreateBox(visualRoot, "FuelCrossVertical", new Vector3(0.09f, 0.34f, 0.04f),
            new Vector3(0.13f, -0.13f, 0.22f), fuelAccentMaterial);
    }

    private Transform CreateVisualRoot(Transform parent, string rootName)
    {
        GameObject visualRoot = new GameObject(rootName);
        visualRoot.transform.SetParent(parent, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;
        return visualRoot.transform;
    }

    private void CreateBox(
        Transform parent,
        string objectName,
        Vector3 worldSize,
        Vector3 worldPosition,
        Material material,
        Vector3 eulerAngles = default)
    {
        Transform box = CreatePrimitive(parent, objectName, PrimitiveType.Cube,
            worldPosition, eulerAngles, Vector3.one, material);
        Vector3 rootScale = GetAbsoluteScale(parent.parent);
        box.localScale = new Vector3(
            worldSize.x / rootScale.x,
            worldSize.y / rootScale.y,
            worldSize.z / rootScale.z);
    }

    private Transform CreatePrimitive(
        Transform parent,
        string objectName,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 eulerAngles,
        Vector3 scale,
        Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = objectName;
        primitive.transform.SetParent(parent, false);
        Vector3 rootScale = GetAbsoluteScale(parent.parent);
        primitive.transform.localPosition = new Vector3(
            localPosition.x / rootScale.x,
            localPosition.y / rootScale.y,
            localPosition.z / rootScale.z);
        primitive.transform.localRotation = Quaternion.Euler(eulerAngles);
        primitive.transform.localScale = scale;

        Renderer renderer = primitive.GetComponent<Renderer>();
        renderer.sharedMaterial = material;

        Collider primitiveCollider = primitive.GetComponent<Collider>();
        if (primitiveCollider != null)
        {
            Destroy(primitiveCollider);
        }

        return primitive.transform;
    }

    private static Vector3 GetAbsoluteScale(Transform target)
    {
        Vector3 scale = target.lossyScale;
        return new Vector3(
            Mathf.Max(Mathf.Abs(scale.x), 0.0001f),
            Mathf.Max(Mathf.Abs(scale.y), 0.0001f),
            Mathf.Max(Mathf.Abs(scale.z), 0.0001f));
    }

    private void CreateRoadsideMarker(Transform segment, int side, Material material)
    {
        string markerName = side < 0 ? "NeonMarker_Left" : "NeonMarker_Right";
        if (segment.Find(markerName) != null)
        {
            return;
        }

        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = markerName;
        marker.transform.SetParent(segment, false);
        marker.transform.localPosition = new Vector3(side * 5.85f, 0.65f, 15f);
        marker.transform.localScale = new Vector3(0.12f, 1.3f, 0.12f);
        marker.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(marker.GetComponent<Collider>());
    }
}
