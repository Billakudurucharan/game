using System.Collections.Generic;
using UnityEngine;

/// <summary>A self-contained, procedural-art endless runner for the Runner scene.</summary>
[DisallowMultipleComponent]
public sealed class RunnerGame : MonoBehaviour
{
    [Header("Runner")]
    public float startSpeed = 5.5f;
    public float maxSpeed = 10f;
    public float speedIncrease = 0.025f;
    public float jumpVelocity = 11f;
    public float gravity = -24f;
    [Range(0.8f, 1.8f)]
    [Tooltip("Medium-sized character, pickups, and hazards.")]
    public float visualScale = 1.25f;

    enum State { Ready, Running, GameOver }
    enum HazardType { Stump, ThornVines, Rock, Macaw }

    sealed class Hazard
    {
        public Transform transform;
        public HazardType type;
        public float width;
        public float height;
        public float centerY;
    }

    sealed class Pickup
    {
        public Transform transform;
        public float phase;
        public float baseY;
    }

    const float GroundTop = -3.2f;
    const string BestKey = "RunnerBest";
    const float PlayerX = -3.2f;

    State state;
    bool paused;
    bool crouching;
    bool grounded;
    int score;
    int best;
    int bonusScore;
    int shownScore = -1;
    float distance;
    float speed;
    float playerY;
    float velocityY;
    float scroll;
    float spawnDistance;
    float gameOverTimer;
    float jumpBufferTimer;
    float halfWidth = 8f;
    float animationTimer;
    float lightningCooldown = 3f;
    float lightningDuration;

    Camera gameCamera;
    Vector3 cameraBase;
    Transform world;
    Transform hazardRoot;
    Transform pickupRoot;
    Transform readyRoot;
    Transform gameOverRoot;
    Transform playerTransform;
    SpriteRenderer playerRenderer;
    SpriteRenderer skyRenderer;
    SpriteRenderer lightningRenderer;
    SpriteRenderer flashRenderer;
    SpriteRenderer scoreRenderer;
    SpriteRenderer bestRenderer;
    SpriteRenderer readyRootRenderer;
    SpriteRenderer readyTitleRenderer;
    SpriteRenderer readyHintRenderer;
    SpriteRenderer readyControlsRenderer;
    SpriteRenderer gameOverRootRenderer;
    SpriteRenderer gameOverTitleRenderer;
    SpriteRenderer finalScoreRenderer;
    SpriteRenderer finalBestRenderer;
    SpriteRenderer retryRenderer;
    SpriteRenderer pausedRenderer;

    Sprite[] runFrames;
    Sprite crouchSprite;
    Sprite coinSprite;
    Sprite stumpSprite;
    Sprite thornVinesSprite;
    Sprite rockSprite;
    Sprite macawSprite;
    Sprite rainSprite;
    Transform[] rainDrops;
    ScrollLayer cloudLayer;
    ScrollLayer duneLayer;
    ScrollLayer platformLayer;
    ScrollLayer groundLayer;
    readonly List<Hazard> hazards = new List<Hazard>();
    readonly List<Pickup> pickups = new List<Pickup>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoStart()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Runner"
            && FindAnyObjectByType<RunnerGame>() == null)
            new GameObject("RunnerGame").AddComponent<RunnerGame>();
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        best = PlayerPrefs.GetInt(BestKey, 0);
        SetupCamera();
        BuildWorld();
        BuildUI();
        Restart();
    }

    void SetupCamera()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            gameCamera = cameraObject.AddComponent<Camera>();
        }

        if (FindAnyObjectByType<AudioListener>() == null)
            gameCamera.gameObject.AddComponent<AudioListener>();

        gameCamera.orthographic = true;
        gameCamera.orthographicSize = 4.7f;
        gameCamera.transform.position = new Vector3(0f, 0f, -10f);
        gameCamera.transform.rotation = Quaternion.identity;
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.backgroundColor = new Color32(32, 54, 67, 255);
        cameraBase = gameCamera.transform.position;
        halfWidth = gameCamera.orthographicSize * gameCamera.aspect;
    }

    SpriteRenderer NewRenderer(string objectName, Transform parent, Sprite sprite, int order, Vector3 position)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;
        return renderer;
    }

    void BuildWorld()
    {
        world = new GameObject("Runner World").transform;
        hazardRoot = new GameObject("Hazards").transform;
        hazardRoot.SetParent(world, false);
        pickupRoot = new GameObject("Pickups").transform;
        pickupRoot.SetParent(world, false);

        Sprite sky = PixelArt.Sky(PixelCanvas.C(38, 67, 80), PixelCanvas.C(94, 123, 128));
        skyRenderer = NewRenderer("Sky", world, sky, -100, Vector3.zero);
        cloudLayer = new ScrollLayer("Storm Clouds", world, PixelArt.Clouds(), 3.1f, -80, 0.02f);
        cloudLayer.SetColor(new Color32(105, 121, 127, 255));
        duneLayer = new ScrollLayer("Jungle Canopy", world, MakeJungleCanopy(), GroundTop, -70, 0.055f);
        platformLayer = new ScrollLayer("Jungle Platforms", world, MakeJunglePlatforms(),
            GroundTop - 0.1f, -50, 0.18f);
        groundLayer = new ScrollLayer("Jungle Ground", world, MakeJungleGround(), GroundTop, 10, 1f);

        lightningRenderer = NewRenderer("Lightning Bolt", world, MakeLightningBolt(), -60,
            new Vector3(0f, 1.65f, 0f));
        lightningRenderer.enabled = false;
        flashRenderer = NewRenderer("Lightning Flash", world, PixelArt.White(), 100, Vector3.zero);
        flashRenderer.transform.localScale = new Vector3(halfWidth * 2f, 9.4f, 1f);
        flashRenderer.color = new Color(0.75f, 0.86f, 0.92f, 0f);
        rainSprite = MakeRainDrop();
        rainDrops = new Transform[72];
        for (int i = 0; i < rainDrops.Length; i++)
        {
            SpriteRenderer rainRenderer = NewRenderer("Rain", world, rainSprite, 95,
                new Vector3(Random.Range(-halfWidth, halfWidth), Random.Range(-4.7f, 4.7f), 0f));
            rainRenderer.color = new Color32(169, 207, 220, 190);
            rainRenderer.transform.localScale = Vector3.one * Random.Range(0.75f, 1.3f);
            rainDrops[i] = rainRenderer.transform;
        }

        playerTransform = new GameObject("Runner").transform;
        playerTransform.SetParent(world, false);
        playerRenderer = playerTransform.gameObject.AddComponent<SpriteRenderer>();
        playerRenderer.sortingOrder = 20;
        playerTransform.localScale = Vector3.one * visualScale;
        runFrames = new[] { MakeRunner(false), MakeRunner(true) };
        crouchSprite = MakeRunnerCrouch();
        coinSprite = MakeCoin();
        stumpSprite = MakeStump();
        thornVinesSprite = MakeThornVines();
        rockSprite = MakeRock();
        macawSprite = MakeMacaw();
    }

    void BuildUI()
    {
        Transform ui = new GameObject("Runner UI").transform;
        scoreRenderer = NewRenderer("Score", ui, null, 120, new Vector3(-halfWidth + 2.1f, 4.05f, 0f));
        bestRenderer = NewRenderer("Best Score", ui, null, 120, new Vector3(-halfWidth + 2.1f, 3.58f, 0f));

        readyRoot = new GameObject("Ready Screen").transform;
        readyRoot.SetParent(ui, false);
        readyRootRenderer = NewRenderer("Ready Background", readyRoot, PixelArt.Panel(), 110, new Vector3(0f, 0.25f, 0f));
        readyRootRenderer.transform.localScale = new Vector3(1.05f, 1.1f, 1f);
        readyTitleRenderer = NewRenderer("Ready Title", readyRoot, null, 112, new Vector3(0f, 2.15f, 0f));
        readyHintRenderer = NewRenderer("Ready Hint", readyRoot, null, 112, new Vector3(0f, -0.75f, 0f));
        readyControlsRenderer = NewRenderer("Ready Controls", readyRoot, null, 112, new Vector3(0f, -1.42f, 0f));

        gameOverRoot = new GameObject("Game Over Screen").transform;
        gameOverRoot.SetParent(ui, false);
        gameOverRootRenderer = NewRenderer("Game Over Background", gameOverRoot, PixelArt.Panel(), 110, new Vector3(0f, 0.15f, 0f));
        gameOverRootRenderer.transform.localScale = new Vector3(1.05f, 1.1f, 1f);
        gameOverTitleRenderer = NewRenderer("Game Over Title", gameOverRoot, null, 112, new Vector3(0f, 2.12f, 0f));
        finalScoreRenderer = NewRenderer("Final Score", gameOverRoot, null, 112, new Vector3(0f, 0.45f, 0f));
        finalBestRenderer = NewRenderer("Final Best", gameOverRoot, null, 112, new Vector3(0f, -0.45f, 0f));
        retryRenderer = NewRenderer("Retry Hint", gameOverRoot, null, 112, new Vector3(0f, -1.78f, 0f));

        pausedRenderer = NewRenderer("Paused", ui, null, 130, new Vector3(0f, 0.2f, 0f));
        pausedRenderer.enabled = false;

        SetText(readyTitleRenderer, "PIXEL RUNNER", 0.055f, TextStyle.Title);
        SetText(readyHintRenderer, "SPACE TO START", 0.04f, TextStyle.White);
        SetText(readyControlsRenderer, "SPACE JUMP DOWN DUCK P PAUSE", 0.023f, TextStyle.Label);
        SetText(gameOverTitleRenderer, "GAME OVER", 0.06f, TextStyle.Red);
        SetText(retryRenderer, "SPACE OR R TO RETRY", 0.03f, TextStyle.White);
        SetText(pausedRenderer, "PAUSED P ESC RESUME", 0.03f, TextStyle.White);
        gameOverRoot.gameObject.SetActive(false);

        UpdateHud();
    }

    void SetText(SpriteRenderer renderer, string text, float pixelSize, TextStyle style)
    {
        renderer.sprite = PixelFont.Render(text, pixelSize, style);
    }

    void Restart()
    {
        for (int i = 0; i < hazards.Count; i++)
            if (hazards[i].transform != null) Destroy(hazards[i].transform.gameObject);
        for (int i = 0; i < pickups.Count; i++)
            if (pickups[i].transform != null) Destroy(pickups[i].transform.gameObject);
        hazards.Clear();
        pickups.Clear();

        state = State.Ready;
        paused = false;
        crouching = false;
        grounded = true;
        score = 0;
        bonusScore = 0;
        distance = 0f;
        speed = startSpeed;
        playerY = GroundTop;
        velocityY = 0f;
        scroll = 0f;
        spawnDistance = 5f;
        gameOverTimer = 0f;
        jumpBufferTimer = 0f;
        animationTimer = 0f;
        shownScore = -1;

        playerRenderer.sprite = runFrames[0];
        playerTransform.position = new Vector3(PlayerX, GroundTop, 0f);
        readyRoot.gameObject.SetActive(true);
        readyHintRenderer.enabled = true;
        gameOverRoot.gameObject.SetActive(false);
        pausedRenderer.enabled = false;
        UpdateHud();
    }

    void BeginRun()
    {
        state = State.Running;
        readyRoot.gameObject.SetActive(false);
        Jump();
    }

    void Jump()
    {
        if (!grounded || crouching) return;
        grounded = false;
        velocityY = jumpVelocity;
    }

    void EndRun()
    {
        if (state != State.Running) return;
        state = State.GameOver;
        paused = false;
        pausedRenderer.enabled = false;
        gameOverTimer = 0f;
        crouching = false;

        if (score > best)
        {
            best = score;
            PlayerPrefs.SetInt(BestKey, best);
            PlayerPrefs.Save();
        }

        SetText(finalScoreRenderer, "SCORE " + score, 0.05f, TextStyle.White);
        SetText(finalBestRenderer, "BEST " + best, 0.05f, TextStyle.White);
        gameOverRoot.gameObject.SetActive(true);
        UpdateHud();
    }

    void Update()
    {
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        halfWidth = gameCamera.orthographicSize * gameCamera.aspect;

        bool jumpPressed = GameInput.FlapDown();
        bool restartPressed = GameInput.RestartDown();
        if (GameInput.PauseDown() && (state == State.Running || paused))
        {
            paused = !paused;
            pausedRenderer.enabled = paused;
        }

        if (paused)
        {
            UpdateVisuals(dt);
            return;
        }

        switch (state)
        {
            case State.Ready:
                scroll += startSpeed * 0.35f * dt;
                animationTimer += dt;
                playerY = GroundTop + Mathf.Sin(Time.time * 4f) * 0.04f;
                playerTransform.position = new Vector3(PlayerX, playerY, 0f);
                playerRenderer.sprite = runFrames[(int)(animationTimer * 8f) % runFrames.Length];
                readyHintRenderer.enabled = Mathf.Repeat(Time.time, 1f) < 0.72f;
                if (jumpPressed) BeginRun();
                break;

            case State.Running:
                TickRun(dt, jumpPressed);
                break;

            case State.GameOver:
                gameOverTimer += dt;
                if (gameOverTimer > 0.6f && (jumpPressed || restartPressed))
                    Restart();
                break;
        }

        UpdateVisuals(dt);
    }

    void TickRun(float dt, bool jumpPressed)
    {
        speed = Mathf.Min(maxSpeed, startSpeed + distance * speedIncrease);
        distance += speed * dt;
        scroll += speed * dt;
        score = Mathf.FloorToInt(distance * 2f) + bonusScore;

        if (jumpPressed) jumpBufferTimer = 0.18f;
        else jumpBufferTimer = Mathf.Max(0f, jumpBufferTimer - dt);

        crouching = grounded && GameInput.CrouchHeld();
        if (grounded && !crouching && jumpBufferTimer > 0f)
        {
            Jump();
            jumpBufferTimer = 0f;
        }

        if (!grounded)
        {
            velocityY += gravity * dt;
            playerY += velocityY * dt;
            if (playerY <= GroundTop)
            {
                playerY = GroundTop;
                velocityY = 0f;
                grounded = true;
            }
        }

        animationTimer += dt;
        if (crouching) playerRenderer.sprite = crouchSprite;
        else playerRenderer.sprite = runFrames[(int)(animationTimer * Mathf.Lerp(8f, 13f, (speed - startSpeed) / Mathf.Max(0.01f, maxSpeed - startSpeed))) % runFrames.Length];
        playerTransform.position = new Vector3(PlayerX, playerY, 0f);

        spawnDistance -= speed * dt;
        if (spawnDistance <= 0f)
        {
            SpawnHazard(halfWidth + 1.5f);
            spawnDistance = Random.Range(8.5f, 10f);
        }

        float playerHeight = (crouching ? 0.56f : 0.96f) * visualScale;
        float playerCenterY = playerY + playerHeight * 0.5f;
        for (int i = hazards.Count - 1; i >= 0; i--)
        {
            Hazard hazard = hazards[i];
            hazard.transform.position += Vector3.left * speed * dt;
            float hazardX = hazard.transform.position.x;

            if (Overlaps(PlayerX, playerCenterY, 0.62f * visualScale, playerHeight,
                    hazardX, hazard.centerY, hazard.width, hazard.height))
            {
                EndRun();
                return;
            }

            if (hazardX < -halfWidth - 2f)
            {
                Destroy(hazard.transform.gameObject);
                hazards.RemoveAt(i);
            }
        }

        for (int i = pickups.Count - 1; i >= 0; i--)
        {
            Pickup pickup = pickups[i];
            pickup.transform.position += Vector3.left * speed * dt;
            pickup.phase += dt * 5f;
            pickup.transform.position = new Vector3(pickup.transform.position.x,
                pickup.baseY + Mathf.Sin(pickup.phase) * 0.08f, 0f);

            if (Overlaps(PlayerX, playerCenterY, 0.7f * visualScale, playerHeight, pickup.transform.position.x,
                    pickup.transform.position.y, 0.65f * visualScale, 0.65f * visualScale))
            {
                bonusScore += 5;
                score += 5;
                Destroy(pickup.transform.gameObject);
                pickups.RemoveAt(i);
                continue;
            }

            if (pickup.transform.position.x < -halfWidth - 1f)
            {
                Destroy(pickup.transform.gameObject);
                pickups.RemoveAt(i);
            }
        }

        UpdateHud();
    }

    static bool Overlaps(float ax, float ay, float aw, float ah, float bx, float by, float bw, float bh)
    {
        return Mathf.Abs(ax - bx) * 2f < aw + bw && Mathf.Abs(ay - by) * 2f < ah + bh;
    }

    void SpawnHazard(float x)
    {
        float roll = Random.value;
        HazardType type;
        if (roll < 0.36f) type = HazardType.Stump;
        else if (roll < 0.61f) type = HazardType.ThornVines;
        else if (roll < 0.82f) type = HazardType.Rock;
        else type = HazardType.Macaw;

        Sprite sprite;
        float width;
        float height;
        float y;
        switch (type)
        {
            case HazardType.Stump:
                sprite = stumpSprite;
                width = 0.95f * visualScale;
                height = 1.05f * visualScale;
                y = GroundTop + height * 0.5f;
                break;
            case HazardType.ThornVines:
                sprite = thornVinesSprite;
                width = 1.25f * visualScale;
                height = 0.58f * visualScale;
                y = GroundTop + height * 0.5f;
                break;
            case HazardType.Rock:
                sprite = rockSprite;
                width = 0.95f * visualScale;
                height = 0.78f * visualScale;
                y = GroundTop + height * 0.5f;
                break;
            default:
                sprite = macawSprite;
                width = 1.12f * visualScale;
                height = 0.58f * visualScale;
                y = GroundTop + 1.05f * visualScale;
                break;
        }

        GameObject go = new GameObject(type.ToString());
        go.transform.SetParent(hazardRoot, false);
        go.transform.position = new Vector3(x, y, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 15;
        renderer.transform.localScale = new Vector3(width / sprite.bounds.size.x, height / sprite.bounds.size.y, 1f);
        hazards.Add(new Hazard { transform = go.transform, type = type, width = width, height = height, centerY = y });

        if (Random.value < 0.45f)
            SpawnCoin(x + Random.Range(1.7f, 3f), GroundTop + Random.Range(1.25f, 2.3f));
    }

    void SpawnCoin(float x, float y)
    {
        GameObject go = new GameObject("Bonus Coin");
        go.transform.SetParent(pickupRoot, false);
        go.transform.position = new Vector3(x, y, 0f);
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = coinSprite;
        renderer.sortingOrder = 18;
        go.transform.localScale = Vector3.one * visualScale * 0.75f;
        pickups.Add(new Pickup { transform = go.transform, phase = Random.Range(0f, 6.28f), baseY = y });
    }

    void UpdateHud()
    {
        if (score != shownScore)
        {
            shownScore = score;
            SetText(scoreRenderer, "SCORE " + score, 0.05f, TextStyle.White);
        }

        SetText(bestRenderer, "BEST " + best, 0.05f, TextStyle.White);
        scoreRenderer.transform.localPosition = new Vector3(-halfWidth + 2.1f, 4.05f, 0f);
        bestRenderer.transform.localPosition = new Vector3(-halfWidth + 2.1f, 3.58f, 0f);
    }

    void UpdateVisuals(float dt)
    {
        float w = halfWidth;
        skyRenderer.transform.localScale = new Vector3((2f * w + 2f) * 64f, 10.6f, 1f);
        cloudLayer.Update(scroll, w);
        duneLayer.Update(scroll, w);
        platformLayer.Update(scroll, w);
        groundLayer.Update(scroll, w);
        flashRenderer.transform.localScale = new Vector3(w * 2f, 9.4f, 1f);
        UpdateStorm(dt, w);
        if (gameOverTimer > 0.6f && state == State.GameOver)
            retryRenderer.enabled = Mathf.Repeat(Time.time, 1f) < 0.72f;
        else
            retryRenderer.enabled = state == State.GameOver;
    }

    void UpdateStorm(float dt, float w)
    {
        for (int i = 0; i < rainDrops.Length; i++)
        {
            Transform drop = rainDrops[i];
            Vector3 position = drop.position;
            position.x -= 2.2f * dt;
            position.y -= 9f * dt;
            if (position.y < -4.8f)
            {
                position.y = 4.8f;
                position.x = Random.Range(-w, w);
            }
            if (position.x < -w - 0.5f)
                position.x = w + 0.5f;
            drop.position = position;
        }

        if (lightningDuration > 0f)
        {
            lightningDuration -= dt;
            bool lit = Mathf.Repeat(Time.time * 18f, 1f) < 0.45f;
            lightningRenderer.enabled = lit;
            flashRenderer.enabled = lit;
            return;
        }

        lightningRenderer.enabled = false;
        flashRenderer.enabled = false;
        lightningCooldown -= dt;
        if (lightningCooldown <= 0f)
        {
            lightningDuration = 0.28f;
            lightningCooldown = Random.Range(4f, 8f);
            lightningRenderer.transform.position = new Vector3(Random.Range(-w * 0.5f, w * 0.5f), 1.65f, 0f);
        }
    }

    static Sprite MakeRainDrop()
    {
        PixelCanvas canvas = new PixelCanvas(4, 14);
        canvas.FillRect(2, 1, 3, 5, PixelCanvas.C(220, 241, 247));
        canvas.FillRect(1, 6, 2, 11, PixelCanvas.C(180, 218, 231));
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0.5f));
    }

    static Sprite MakeLightningBolt()
    {
        PixelCanvas canvas = new PixelCanvas(32, 96);
        Color32 edge = PixelCanvas.C(220, 229, 200);
        Color32 core = PixelCanvas.C(255, 252, 208);
        FillTriangle(canvas, 20, 92, 12, 57, 21, 61, edge);
        FillTriangle(canvas, 21, 62, 9, 37, 19, 41, edge);
        FillTriangle(canvas, 19, 42, 8, 8, 16, 23, edge);
        FillTriangle(canvas, 18, 83, 14, 58, 18, 61, core);
        FillTriangle(canvas, 18, 60, 12, 39, 17, 41, core);
        FillTriangle(canvas, 17, 40, 10, 17, 15, 25, core);
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0.5f));
    }

    static void FillTriangle(PixelCanvas canvas, int x0, int y0, int x1, int y1, int x2, int y2, Color32 color)
    {
        int minY = Mathf.Min(y0, Mathf.Min(y1, y2));
        int maxY = Mathf.Max(y0, Mathf.Max(y1, y2));
        for (int y = minY; y <= maxY; y++)
        {
            float t0 = y1 == y0 ? 0f : Mathf.Clamp01((y - y0) / (float)(y1 - y0));
            float t1 = y2 == y1 ? 0f : Mathf.Clamp01((y - y1) / (float)(y2 - y1));
            float t2 = y2 == y0 ? 0f : Mathf.Clamp01((y - y0) / (float)(y2 - y0));
            float[] intersections = new float[3];
            int count = 0;
            if ((y >= Mathf.Min(y0, y1)) && (y <= Mathf.Max(y0, y1))) intersections[count++] = Mathf.Lerp(x0, x1, t0);
            if ((y >= Mathf.Min(y1, y2)) && (y <= Mathf.Max(y1, y2))) intersections[count++] = Mathf.Lerp(x1, x2, t1);
            if ((y >= Mathf.Min(y0, y2)) && (y <= Mathf.Max(y0, y2))) intersections[count++] = Mathf.Lerp(x0, x2, t2);

            if (count < 2) continue;
            float left = intersections[0];
            float right = intersections[0];
            for (int i = 1; i < count; i++)
            {
                left = Mathf.Min(left, intersections[i]);
                right = Mathf.Max(right, intersections[i]);
            }
            canvas.FillRect(Mathf.CeilToInt(left), y, Mathf.FloorToInt(right), y, color);
        }
    }

    static Sprite MakeJungleCanopy()
    {
        const int width = 480;
        const int height = 160;
        PixelCanvas canvas = new PixelCanvas(width, height);
        Color32 far = PixelCanvas.C(77, 117, 91);
        Color32 mid = PixelCanvas.C(50, 93, 70);
        Color32 near = PixelCanvas.C(33, 70, 53);
        Color32 leafLight = PixelCanvas.C(87, 136, 73);
        Color32 trunk = PixelCanvas.C(79, 63, 45);

        canvas.FillRect(0, 0, width - 1, 30, far);
        FillTriangle(canvas, 0, 0, 85, 86, 190, 0, far);
        FillTriangle(canvas, 145, 0, 285, 105, 430, 0, far);
        FillTriangle(canvas, 350, 0, 425, 76, 480, 24, far);
        FillTriangle(canvas, 0, 0, 115, 64, 230, 0, mid);
        FillTriangle(canvas, 190, 0, 335, 83, 480, 0, mid);
        FillTriangle(canvas, 0, 0, 75, 43, 165, 0, near);
        FillTriangle(canvas, 270, 0, 380, 52, 480, 0, near);

        int[] treeX = { 24, 113, 218, 326, 448 };
        int[] treeTop = { 78, 66, 92, 71, 84 };
        for (int i = 0; i < treeX.Length; i++)
        {
            int x = treeX[i];
            int top = treeTop[i];
            canvas.FillRect(x - 4, 0, x + 3, top, trunk);
            canvas.FillEllipse(x, top, 34, 21, near);
            canvas.FillEllipse(x - 18, top - 8, 24, 16, mid);
            canvas.FillEllipse(x + 17, top - 7, 23, 16, mid);
            canvas.FillEllipse(x - 8, top + 8, 16, 8, leafLight);
        }
        canvas.FillRect(0, 0, width - 1, 3, PixelCanvas.C(27, 55, 42));

        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0f));
    }

    static void DrawJungleFruit(PixelCanvas canvas, int x, int y)
    {
        canvas.FillEllipse(x, y, 5, 8, PixelCanvas.C(148, 48, 35));
        canvas.FillEllipse(x, y, 3, 6, PixelCanvas.C(232, 75, 48));
        canvas.FillRect(x - 1, y + 2, x, y + 4, PixelCanvas.C(255, 179, 80));
        canvas.FillRect(x + 1, y + 7, x + 3, y + 9, PixelCanvas.C(75, 154, 55));
    }

    static void DrawJungleTree(PixelCanvas canvas, int x, int y, int scale)
    {
        Color32 trunk = PixelCanvas.C(111, 75, 43);
        Color32 bark = PixelCanvas.C(156, 108, 57);
        Color32 canopy = PixelCanvas.C(28, 93, 52);
        Color32 leafLight = PixelCanvas.C(64, 130, 61);
        canvas.FillRect(x - scale, y, x + scale, y + 6 * scale, trunk);
        canvas.FillRect(x - scale / 2, y + 2 * scale, x, y + 6 * scale, bark);
        canvas.FillEllipse(x, y + 15 * scale, 12 * scale, 9 * scale, canopy);
        canvas.FillEllipse(x - 9 * scale, y + 11 * scale, 8 * scale, 7 * scale, leafLight);
        canvas.FillEllipse(x + 8 * scale, y + 12 * scale, 8 * scale, 7 * scale, leafLight);
    }

    static void DrawLadder(PixelCanvas canvas, int centerX, int bottom, int top)
    {
        Color32 wood = PixelCanvas.C(70, 92, 52);
        Color32 light = PixelCanvas.C(120, 146, 75);
        canvas.FillRect(centerX - 9, bottom, centerX - 5, top, wood);
        canvas.FillRect(centerX + 5, bottom, centerX + 9, top, wood);
        canvas.FillRect(centerX - 8, bottom, centerX - 7, top, light);
        canvas.FillRect(centerX + 6, bottom, centerX + 7, top, light);
        for (int y = bottom + 5; y < top; y += 9)
        {
            canvas.FillRect(centerX - 6, y, centerX + 6, y + 2, wood);
            canvas.FillRect(centerX - 5, y + 1, centerX + 5, y + 1, light);
        }
    }

    static void DrawFloatingPlatform(PixelCanvas canvas, int left, int right, int top)
    {
        Color32 soil = PixelCanvas.C(91, 67, 45);
        Color32 soilLight = PixelCanvas.C(120, 83, 50);
        Color32 soilShadow = PixelCanvas.C(58, 63, 42);
        Color32 topColor = PixelCanvas.C(54, 112, 47);
        Color32 topLight = PixelCanvas.C(105, 151, 62);

        canvas.FillRect(left + 8, top - 15, right - 7, top - 5, soil);
        FillTriangle(canvas, left + 10, top - 15, (left + right) / 2, top - 34, right - 8, top - 15, soilShadow);
        canvas.FillRect(left + 10, top - 12, right - 10, top - 8, soilLight);
        for (int x = left + 16; x < right - 12; x += 17)
        {
            int y = top - 11 - (x % 3);
            canvas.FillEllipse(x, y, 2, 2, soilShadow);
            canvas.FillRect(x + 6, y + 4, x + 8, y + 5, PixelCanvas.C(77, 112, 52));
        }

        float center = (left + right) * 0.5f;
        float radius = (right - left) * 0.5f;
        canvas.FillEllipse(center, top, radius, 8, PixelCanvas.C(34, 79, 41));
        canvas.FillEllipse(center, top + 2, radius - 4, 5, topColor);
        canvas.FillEllipse(center - radius * 0.22f, top + 4, radius * 0.48f, 2,
            topLight);
    }

    static Sprite MakeJunglePlatforms()
    {
        const int width = 480;
        const int height = 168;
        PixelCanvas canvas = new PixelCanvas(width, height);

        DrawFloatingPlatform(canvas, 16, 112, 76);
        DrawLadder(canvas, 42, 5, 71);
        DrawJungleTree(canvas, 85, 82, 2);
        DrawJungleTree(canvas, 101, 82, 1);
        DrawJungleFruit(canvas, 69, 102);
        DrawJungleFruit(canvas, 82, 104);
        DrawJungleFruit(canvas, 95, 102);

        DrawFloatingPlatform(canvas, 164, 292, 119);
        DrawLadder(canvas, 196, 43, 114);
        DrawJungleTree(canvas, 251, 125, 1);
        DrawJungleTree(canvas, 267, 125, 2);
        DrawJungleFruit(canvas, 219, 148);
        DrawJungleFruit(canvas, 234, 153);
        DrawJungleFruit(canvas, 249, 156);
        DrawJungleFruit(canvas, 264, 153);

        DrawFloatingPlatform(canvas, 337, 456, 84);
        DrawLadder(canvas, 420, 7, 79);
        DrawJungleTree(canvas, 367, 90, 2);
        DrawJungleTree(canvas, 389, 90, 1);
        DrawJungleFruit(canvas, 360, 108);
        DrawJungleFruit(canvas, 375, 111);
        DrawJungleFruit(canvas, 390, 108);
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0f));
    }

    static Sprite MakeJungleGround()
    {
        const int width = 192;
        const int height = 48;
        PixelCanvas canvas = new PixelCanvas(width, height);
        canvas.FillRect(0, 0, width - 1, height - 1, PixelCanvas.C(82, 57, 39));
        canvas.FillRect(0, 0, width - 1, 35, PixelCanvas.C(103, 70, 43));
        canvas.FillRect(0, 35, width - 1, 37, PixelCanvas.C(39, 91, 44));

        for (int x = 0; x < width; x += 3)
        {
            canvas.Set(x, 47, PixelCanvas.C(77, 130, 48));
            canvas.Set(x + 1, 46, PixelCanvas.C(123, 157, 59));
        }
        canvas.FillRect(0, 38, width - 1, 45, PixelCanvas.C(53, 108, 44));
        canvas.FillRect(0, 40, width - 1, 43, PixelCanvas.C(77, 130, 48));

        System.Random random = new System.Random(57);
        for (int i = 0; i < 145; i++)
        {
            int x = random.Next(width);
            int y = random.Next(3, 33);
            Color32 speckle = i % 3 == 0 ? PixelCanvas.C(124, 87, 54) : PixelCanvas.C(73, 94, 52);
            canvas.FillRect(x, y, x + random.Next(1, 4), y + random.Next(1, 3), speckle);
        }

        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 1f));
    }

    Sprite MakeRunner(bool alternate)
    {
        PixelCanvas canvas = new PixelCanvas(24, 32);
        Color32 outline = PixelCanvas.C(48, 57, 35);
        Color32 body = PixelCanvas.C(70, 113, 56);
        Color32 bodyLight = PixelCanvas.C(109, 145, 66);
        Color32 pants = PixelCanvas.C(137, 103, 58);
        Color32 shoe = PixelCanvas.C(67, 55, 39);
        Color32 pack = PixelCanvas.C(111, 66, 42);
        Color32 packLight = PixelCanvas.C(157, 94, 51);
        Color32 hat = PixelCanvas.C(113, 74, 39);
        Color32 hatLight = PixelCanvas.C(183, 142, 79);
        canvas.FillRect(3, 10, 7, 21, pack);
        canvas.FillRect(4, 11, 6, 19, packLight);
        canvas.FillRoundRect(6, 8, 17, 22, 3, body);
        canvas.FillRect(7, 18, 16, 20, bodyLight);
        canvas.FillRect(15, 13, 16, 18, PixelCanvas.C(50, 81, 44));
        canvas.FillRect(8, 13, 9, 14, PixelCanvas.C(196, 172, 117));
        canvas.FillEllipse(12, 24, 6, 5, PixelCanvas.C(207, 159, 116));
        canvas.FillRect(14, 24, 15, 26, outline);
        canvas.FillRect(14, 25, 14, 26, PixelCanvas.C(255, 255, 255));
        canvas.FillRect(3, 13, 6, 17, body);
        canvas.FillRect(3, 15, 4, 18, PixelCanvas.C(201, 127, 55));
        canvas.FillRect(7, 21, 16, 22, PixelCanvas.C(137, 92, 48));
        canvas.FillRect(7, 27, 16, 28, PixelCanvas.C(42, 88, 132));
        canvas.FillRect(6, 28, 17, 29, PixelCanvas.C(35, 54, 75));

        if (alternate)
        {
            canvas.FillRect(8, 3, 11, 9, pants);
            canvas.FillRect(14, 5, 17, 9, pants);
            canvas.FillRect(6, 2, 12, 2, shoe);
            canvas.FillRect(14, 4, 19, 4, shoe);
        }
        else
        {
            canvas.FillRect(8, 5, 11, 9, pants);
            canvas.FillRect(13, 3, 16, 9, pants);
            canvas.FillRect(6, 4, 12, 4, shoe);
            canvas.FillRect(12, 2, 18, 2, shoe);
        }

        canvas.FillRect(7, 27, 16, 29, shoe);
        canvas.FillRect(9, 28, 14, 28, PixelCanvas.C(126, 91, 53));
        canvas.FillRect(11, 29, 13, 29, PixelCanvas.C(255, 255, 255));
        canvas.FillRect(5, 29, 19, 29, hat);
        canvas.FillRect(7, 30, 17, 30, hatLight);
        canvas.FillRect(8, 31, 16, 31, PixelCanvas.C(115, 77, 42));
        canvas.Outline(outline, true);
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0f));
    }

    Sprite MakeRunnerCrouch()
    {
        PixelCanvas canvas = new PixelCanvas(30, 20);
        Color32 outline = PixelCanvas.C(48, 57, 35);
        canvas.FillRoundRect(7, 5, 23, 12, 3, PixelCanvas.C(70, 113, 56));
        canvas.FillRect(9, 10, 13, 14, PixelCanvas.C(111, 66, 42));
        canvas.FillRect(9, 10, 12, 13, PixelCanvas.C(157, 94, 51));
        canvas.FillEllipse(21, 14, 5, 4, PixelCanvas.C(207, 159, 116));
        canvas.FillRect(23, 14, 24, 16, outline);
        canvas.FillRect(23, 15, 23, 16, PixelCanvas.C(255, 255, 255));
        canvas.FillRect(8, 11, 22, 12, PixelCanvas.C(137, 92, 48));
        canvas.FillRect(9, 2, 14, 5, PixelCanvas.C(137, 103, 58));
        canvas.FillRect(18, 2, 23, 5, PixelCanvas.C(137, 103, 58));
        canvas.FillRect(6, 1, 15, 1, PixelCanvas.C(67, 55, 39));
        canvas.FillRect(16, 1, 25, 1, PixelCanvas.C(67, 55, 39));
        canvas.FillRect(16, 16, 24, 18, PixelCanvas.C(67, 55, 39));
        canvas.FillRect(18, 17, 22, 17, PixelCanvas.C(126, 91, 53));
        canvas.FillRect(6, 16, 27, 16, PixelCanvas.C(113, 74, 39));
        canvas.FillRect(9, 17, 23, 18, PixelCanvas.C(161, 119, 62));
        canvas.FillRect(5, 16, 28, 16, PixelCanvas.C(78, 56, 38));
        canvas.FillRect(19, 18, 21, 18, PixelCanvas.C(255, 255, 255));
        canvas.Outline(outline, true);
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0f));
    }

    Sprite MakeStump()
    {
        PixelCanvas canvas = new PixelCanvas(26, 34);
        canvas.FillRect(4, 7, 21, 28, PixelCanvas.C(100, 65, 40));
        canvas.FillRect(7, 3, 19, 8, PixelCanvas.C(141, 96, 54));
        canvas.FillEllipse(13, 7, 9, 4, PixelCanvas.C(184, 137, 71));
        canvas.FillEllipse(13, 8, 4, 2, PixelCanvas.C(105, 72, 43));
        canvas.FillRect(5, 16, 7, 20, PixelCanvas.C(64, 107, 48));
        canvas.FillRect(19, 12, 21, 16, PixelCanvas.C(67, 128, 51));
        canvas.FillRect(8, 25, 9, 29, PixelCanvas.C(59, 113, 46));
        canvas.Outline(PixelCanvas.C(61, 45, 32), true);
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0.5f));
    }

    Sprite MakeThornVines()
    {
        PixelCanvas canvas = new PixelCanvas(30, 16);
        Color32 vine = PixelCanvas.C(36, 82, 42);
        Color32 thorn = PixelCanvas.C(191, 157, 89);
        canvas.FillRect(0, 0, 29, 3, PixelCanvas.C(69, 111, 50));
        for (int x = 1; x < 29; x += 7)
        {
            FillTriangle(canvas, x, 3, x + 3, 14, x + 6, 3, vine);
            FillTriangle(canvas, x, 5, x + 2, 1, x + 4, 5, thorn);
            FillTriangle(canvas, x + 2, 8, x + 4, 12, x + 6, 8, thorn);
        }
        canvas.Outline(PixelCanvas.C(31, 65, 39), true);
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0.5f));
    }

    Sprite MakeRock()
    {
        PixelCanvas canvas = new PixelCanvas(30, 24);
        canvas.FillEllipse(15, 12, 11, 10, PixelCanvas.C(94, 91, 72));
        canvas.FillEllipse(12, 16, 6, 4, PixelCanvas.C(139, 122, 82));
        canvas.FillEllipse(18, 8, 6, 4, PixelCanvas.C(67, 72, 59));
        canvas.FillRect(17, 4, 21, 6, PixelCanvas.C(159, 140, 91));
        canvas.Outline(PixelCanvas.C(53, 61, 47), true);
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0.5f));
    }

    Sprite MakeMacaw()
    {
        PixelCanvas canvas = new PixelCanvas(32, 24);
        canvas.FillEllipse(15, 11, 10, 8, PixelCanvas.C(196, 56, 46));
        canvas.FillEllipse(15, 9, 7, 6, PixelCanvas.C(238, 198, 82));
        canvas.FillEllipse(7, 14, 8, 4, PixelCanvas.C(47, 126, 77));
        canvas.FillEllipse(19, 14, 8, 4, PixelCanvas.C(47, 91, 148));
        canvas.FillEllipse(11, 13, 3, 3, PixelCanvas.C(255, 255, 255));
        canvas.FillEllipse(18, 13, 3, 3, PixelCanvas.C(255, 255, 255));
        canvas.Set(12, 13, PixelCanvas.C(39, 43, 35));
        canvas.Set(19, 13, PixelCanvas.C(39, 43, 35));
        canvas.FillRect(14, 9, 16, 10, PixelCanvas.C(218, 143, 57));
        canvas.FillRect(10, 18, 11, 21, PixelCanvas.C(47, 126, 77));
        canvas.FillRect(19, 18, 20, 21, PixelCanvas.C(47, 91, 148));
        canvas.Outline(PixelCanvas.C(69, 53, 41), true);
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0.5f));
    }

    Sprite MakeCoin()
    {
        PixelCanvas canvas = new PixelCanvas(16, 16);
        canvas.FillEllipse(8, 8, 7, 7, PixelCanvas.C(153, 57, 34));
        canvas.FillEllipse(8, 8, 5, 5, PixelCanvas.C(218, 84, 48));
        canvas.FillRect(7, 4, 8, 11, PixelCanvas.C(242, 172, 73));
        canvas.Outline(PixelCanvas.C(99, 50, 31), true);
        return canvas.ToSprite(PixelArt.PPU, new Vector2(0.5f, 0.5f));
    }
}
