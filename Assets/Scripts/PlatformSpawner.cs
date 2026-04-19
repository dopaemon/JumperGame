using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class PlatformSpawner : MonoBehaviour
{
    private const float PlatformWidth = 1.35f;
    private const float PlatformHeight = 0.28f;

	[SerializeField] private LevelData levelData;
	[SerializeField] private float minStepY = 1.1f;
    [SerializeField] private float maxStepY = 2.1f;
    [SerializeField] private float earlyGameMaxStepY = 1.75f;
    [SerializeField] private float horizontalPadding = 0.9f;
    [SerializeField] private float laneEdgePadding = 0.55f;
    [SerializeField] private float laneClusterWidthFactor = 0.72f;
    [SerializeField] private float aheadDistance = 15f;
    [SerializeField] private float despawnDistance = 0.75f;
    [SerializeField] private int guaranteedStaticPlatforms = 10;
    [SerializeField] private float enemySpawnChance = 0.08f;
    [SerializeField] private float powerUpSpawnChance = 0.05f;
	[SerializeField] private GameObject coinPrefab;
	[SerializeField] private float coinSpawnChance = 0.35f;
	[SerializeField] private float enemyAvoidPlatformCenter = 1.2f;
    [SerializeField] private float enemyRowClearance = 0.9f;
    [SerializeField] private float itemSeparationRadius = 1.1f;
    [SerializeField] private int spawnPlacementAttempts = 8;

    private readonly List<PlatformBase> activePlatforms = new();
    private readonly List<EnemyHazard> activeEnemies = new();
    private readonly List<FlightPowerUp> activePowerUps = new();
    private readonly Stack<StaticPlatform> staticPlatformPool = new();
    private readonly Stack<MovingPlatform> movingPlatformPool = new();
    private readonly Stack<BreakablePlatform> breakablePlatformPool = new();
    private readonly Stack<SpringPlatform> springPlatformPool = new();
    private readonly Stack<EnemyHazard> enemyPool = new();
    private readonly Stack<FlightPowerUp> powerUpPool = new();

    private Camera gameplayCamera;
    private int lastAnchorLane = 1;
    private float lastSpawnY;
    private bool initialized;
    private int bonusRouteRowsRemaining;

    public void Initialize(Camera targetCamera)
    {
        gameplayCamera = targetCamera;
        initialized = true;
    }

    // Expose the LevelData so other bootstrap code can access sprites (player/enemy/background)
    public LevelData LevelData => levelData;

    // Allow setting LevelData at runtime (used by menu selection)
    public void SetLevelData(LevelData data)
    {
        levelData = data;
    }

    public void SeedInitialPlatforms()
    {
        if (!initialized)
        {
            return;
        }

        Vector2 startPosition = new Vector2(0f, -3.4f);
        
        CreatePlatform(startPosition, PlatformKind.Static);
        lastSpawnY = startPosition.y;

        // Create two additional static rows above the central platform so the next platforms are separated vertically.
        int initialSideRows = Mathf.Min(2, guaranteedStaticPlatforms - 1);
        for (int i = 0; i < initialSideRows; i++)
        {
            SpawnNextRow(forceStatic: true);
        }

        // Remaining guaranteed static platforms
        int alreadyCreated = 1 + initialSideRows;
        for (int i = 0; i < Mathf.Max(0, guaranteedStaticPlatforms - alreadyCreated); i++)
        {
            SpawnNextRow(forceStatic: true);
        }
    }

    private void Update()
    {
        if (!initialized || gameplayCamera == null)
        {
            return;
        }

        float cameraTop = gameplayCamera.transform.position.y + gameplayCamera.orthographicSize;
        while (lastSpawnY < cameraTop + aheadDistance)
        {
            SpawnNextRow(forceStatic: false);
        }

        float despawnLine = gameplayCamera.transform.position.y - gameplayCamera.orthographicSize - despawnDistance;
        for (int i = activePlatforms.Count - 1; i >= 0; i--)
        {
            PlatformBase platform = activePlatforms[i];
            if (platform == null)
            {
                activePlatforms.RemoveAt(i);
                continue;
            }

            if (!platform.gameObject.activeSelf || platform.transform.position.y < despawnLine)
            {
                RecyclePlatform(i);
            }
        }

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyHazard enemy = activeEnemies[i];
            if (enemy == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            if (!enemy.gameObject.activeSelf || enemy.transform.position.y < despawnLine)
            {
                RecycleEnemy(i);
            }
        }

        for (int i = activePowerUps.Count - 1; i >= 0; i--)
        {
            FlightPowerUp powerUp = activePowerUps[i];
            if (powerUp == null)
            {
                activePowerUps.RemoveAt(i);
                continue;
            }

            if (!powerUp.gameObject.activeSelf || powerUp.transform.position.y < despawnLine)
            {
                RecyclePowerUp(i);
            }
        }
    }

    private void SpawnNextRow(bool forceStatic)
    {
        float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
        float minX = -halfWidth + horizontalPadding;
        float maxX = halfWidth - horizontalPadding;
        float upperStepY = lastSpawnY < 12f ? earlyGameMaxStepY : maxStepY;
        float nextY = ChooseNextRowY(upperStepY);
        float[] laneXs = BuildLaneCenters(minX, maxX);
        int anchorLane = ChooseAnchorLane(laneXs.Length);
        bool shouldSpawnEnemy = !forceStatic && nextY > 28f && Random.value < enemySpawnChance;
        int rowPlatformCount = shouldSpawnEnemy ? 1 : GetRowPlatformCount(nextY, forceStatic);
        List<float> rowXs = BuildRowPositions(laneXs, anchorLane, rowPlatformCount);

        for (int i = 0; i < rowXs.Count; i++)
        {
            PlatformKind kind = i == 0
                ? (forceStatic ? PlatformKind.Static : ChoosePlatformKind(nextY))
                : (forceStatic ? PlatformKind.Static : ChooseSecondaryPlatformKind(nextY));

            CreatePlatform(new Vector2(rowXs[i], nextY), kind);
        }

        lastAnchorLane = anchorLane;
        lastSpawnY = nextY;

        if (shouldSpawnEnemy)
        {
            TryCreateEnemy(new Vector2(laneXs[anchorLane], nextY + 1.15f));
            bonusRouteRowsRemaining = Mathf.Max(bonusRouteRowsRemaining, 2);
        }

    }

    private float ChooseNextRowY(float upperStepY)
    {
        for (int attempt = 0; attempt < spawnPlacementAttempts; attempt++)
        {
            float candidateY = lastSpawnY + Random.Range(minStepY, upperStepY);
            if (!IsNearEnemyRow(candidateY))
            {
                return candidateY;
            }
        }

        return lastSpawnY + upperStepY;
    }

    private bool IsNearEnemyRow(float rowY)
    {
        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyHazard enemy = activeEnemies[i];
            if (enemy == null || !enemy.gameObject.activeSelf)
            {
                continue;
            }

            if (Mathf.Abs(enemy.transform.position.y - rowY) <= enemyRowClearance)
            {
                return true;
            }
        }

        return false;
    }

    private float[] BuildLaneCenters(float minX, float maxX)
    {
        float usableWidth = Mathf.Max(1f, maxX - minX);
        float laneSpan = Mathf.Max(usableWidth * laneClusterWidthFactor, PlatformWidth * 2.6f);
        float centerX = (minX + maxX) * 0.5f;
        float laneMin = Mathf.Max(minX + laneEdgePadding, centerX - (laneSpan * 0.5f));
        float laneMax = Mathf.Min(maxX - laneEdgePadding, centerX + (laneSpan * 0.5f));

        if (laneMax <= laneMin)
        {
            float center = (minX + maxX) * 0.5f;
            return new[] { center - 1.2f, center, center + 1.2f };
        }

        int laneCount = usableWidth > 5.6f ? 4 : 3;
        float[] lanes = new float[laneCount];
        for (int i = 0; i < laneCount; i++)
        {
            lanes[i] = Mathf.Lerp(laneMin, laneMax, laneCount == 1 ? 0.5f : i / (float)(laneCount - 1));
        }

        return lanes;
    }

    private int ChooseAnchorLane(int laneCount)
    {
        int minLane = Mathf.Max(0, lastAnchorLane - 1);
        int maxLane = Mathf.Min(laneCount - 1, lastAnchorLane + 1);
        return Random.Range(minLane, maxLane + 1);
    }

    private int GetRowPlatformCount(float rowY, bool forceStatic)
    {
        return 1;
    }

    private List<float> BuildRowPositions(float[] laneXs, int anchorLane, int requestedCount)
    {
        List<float> positions = new List<float>(requestedCount) { laneXs[anchorLane] };
        if (requestedCount <= 1)
        {
            return positions;
        }

        List<int> branchCandidates = new List<int>(2);
        if (anchorLane > 0)
        {
            branchCandidates.Add(anchorLane - 1);
        }

        if (anchorLane < laneXs.Length - 1)
        {
            branchCandidates.Add(anchorLane + 1);
        }

        if (branchCandidates.Count == 0)
        {
            return positions;
        }

        int branchLane = branchCandidates[Random.Range(0, branchCandidates.Count)];
        positions.Add(laneXs[branchLane]);
        positions.Sort();
        return positions;
    }

    private PlatformKind ChoosePlatformKind(float height)
    {
        float roll = Random.value;
        if (height < 20f)
        {
            return roll < 0.78f ? PlatformKind.Static : PlatformKind.Moving;
        }

        if (height < 45f)
        {
            if (roll < 0.48f)
            {
                return PlatformKind.Static;
            }

            if (roll < 0.74f)
            {
                return PlatformKind.Moving;
            }

            return roll < 0.9f ? PlatformKind.Breakable : PlatformKind.Spring;
        }

        if (roll < 0.38f)
        {
            return PlatformKind.Static;
        }

        if (roll < 0.64f)
        {
            return PlatformKind.Moving;
        }

        return roll < 0.86f ? PlatformKind.Breakable : PlatformKind.Spring;
    }

    private PlatformKind ChooseSecondaryPlatformKind(float height)
    {
        float roll = Random.value;
        if (height < 20f)
        {
            return roll < 0.9f ? PlatformKind.Static : PlatformKind.Moving;
        }

        if (height < 45f)
        {
            if (roll < 0.58f)
            {
                return PlatformKind.Static;
            }

            if (roll < 0.82f)
            {
                return PlatformKind.Moving;
            }

            return roll < 0.94f ? PlatformKind.Breakable : PlatformKind.Spring;
        }

        if (roll < 0.5f)
        {
            return PlatformKind.Static;
        }

        if (roll < 0.78f)
        {
            return PlatformKind.Moving;
        }

        return roll < 0.92f ? PlatformKind.Breakable : PlatformKind.Spring;
    }

    private void CreatePlatform(Vector2 position, PlatformKind kind)
    {

        PlatformBase platform = RentPlatform(kind);
        GameObject platformObject = platform.gameObject;
        platformObject.name = $"{kind} Platform";
        platformObject.transform.SetParent(transform, false);
        platformObject.transform.position = position;
        platformObject.SetActive(true);
		platformObject.tag = "Platform";

		foreach (Transform child in platform.transform)
		{
			if (child.CompareTag("Coin"))
			{
				Destroy(child.gameObject);
			}
		}
		Vector2 size = new Vector2(PlatformWidth, PlatformHeight);
        Color tint = kind switch
        {
            PlatformKind.Moving => new Color(0.43f, 0.85f, 0.96f),
            PlatformKind.Breakable => new Color(0.96f, 0.62f, 0.41f),
            PlatformKind.Spring => new Color(0.53f, 0.87f, 0.56f),
            _ => new Color(0.52f, 0.88f, 0.46f)
        };

      if (platform is SpringPlatform spring)
        {
            // Use separate platform sprite and spring-top sprite
            spring.Initialize(
                levelData.springPlatform,
                levelData.springTop,
                size
            );
        }
		else
		{
			platform.Initialize(GetSpriteByKind(kind), size);
		}

		if (platform is MovingPlatform movingPlatform)
        {
            movingPlatform.Configure(1.45f, Random.Range(1.1f, 1.8f), Random.Range(0f, Mathf.PI * 2f));
        }

		activePlatforms.Add(platform);

		bool spawnedPowerUp = false;

		if (kind != PlatformKind.Breakable && Random.value < powerUpSpawnChance)
		{
			spawnedPowerUp = TryCreatePowerUp(
				new Vector2(position.x, position.y + 0.85f)
			);
		}

		if (!spawnedPowerUp &&
	        Random.value < coinSpawnChance)
		{
			Vector2 coinPos = position + Vector2.up * 0.75f;

			if (!IsCoinPositionBlocked(coinPos, 0.7f))
			{
				SpawnCoin(platform.transform, position);
			}
		}
	}

	private void SpawnCoin(Transform parentPlatform, Vector2 platformPosition)
	{
		if (coinPrefab == null)
			return;

		GameObject coin = Instantiate(
			coinPrefab,
			parentPlatform
		);

		coin.transform.localPosition = new Vector3(0f, 0.75f, 0f);
	}

	private bool IsCoinPositionBlocked(Vector2 position, float radius)
	{
		Collider2D hit = Physics2D.OverlapCircle(
			position,
			radius
		);

		if (hit == null)
			return false;

		return hit.GetComponent<FlightPowerUp>() != null;
	}

	private void TryCreateEnemy(Vector2 anchorPosition)
    {
        Vector2? spawnPosition = FindEnemySpawnPosition(anchorPosition);
        if (!spawnPosition.HasValue)
        {
            return;
        }

        EnemyHazard enemy = RentEnemy();
        GameObject enemyObject = enemy.gameObject;
        enemyObject.name = "Enemy";
        enemyObject.transform.SetParent(transform, false);
        enemyObject.transform.position = spawnPosition.Value;
        // Assign enemy sprite from level data if available
        if (levelData != null && levelData.enemySprite != null)
        {
            enemy.SetSprite(levelData.enemySprite);
        }
        enemyObject.SetActive(true);
        activeEnemies.Add(enemy);
    }

	private bool TryCreatePowerUp(Vector2 anchorPosition)
	{
		Vector2? spawnPosition = FindPowerUpSpawnPosition(anchorPosition);

		if (!spawnPosition.HasValue)
		{
			return false;
		}

		FlightPowerUp powerUp = RentPowerUp();
		GameObject powerUpObject = powerUp.gameObject;

		powerUpObject.SetActive(false);

		powerUpObject.name = "Flight PowerUp";

		powerUpObject.transform.SetParent(transform, false);
		powerUpObject.transform.position = spawnPosition.Value;

		if (levelData != null && levelData.powerUpSprite != null)
		{
			powerUp.SetSprite(levelData.powerUpSprite);
		}

		powerUpObject.SetActive(true);

		activePowerUps.Add(powerUp);

		return true;
	}

	private Vector2? FindEnemySpawnPosition(Vector2 anchorPosition)
    {
        float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
        float minX = -halfWidth + horizontalPadding;
        float maxX = halfWidth - horizontalPadding;

        for (int attempt = 0; attempt < spawnPlacementAttempts; attempt++)
        {
            float direction = Random.value < 0.5f ? -1f : 1f;
            float extraOffset = Random.Range(enemyAvoidPlatformCenter, enemyAvoidPlatformCenter + 1.1f);
            Vector2 candidate = new Vector2(
                Mathf.Clamp(anchorPosition.x + (direction * extraOffset), minX, maxX),
                anchorPosition.y);

            if (Mathf.Abs(candidate.x - anchorPosition.x) < enemyAvoidPlatformCenter * 0.85f)
            {
                continue;
            }

            if (IsSpawnAreaFree(candidate, itemSeparationRadius))
            {
                return candidate;
            }
        }

        return null;
    }

    private Vector2? FindPowerUpSpawnPosition(Vector2 anchorPosition)
    {
        float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
        float minX = -halfWidth + horizontalPadding;
        float maxX = halfWidth - horizontalPadding;

        for (int attempt = 0; attempt < spawnPlacementAttempts; attempt++)
        {
            float offsetX = Random.Range(-0.45f, 0.45f);
            float offsetY = Random.Range(-0.1f, 0.2f);
            Vector2 candidate = new Vector2(
                Mathf.Clamp(anchorPosition.x + offsetX, minX, maxX),
                anchorPosition.y + offsetY);

            if (IsSpawnAreaFree(candidate, itemSeparationRadius))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool IsSpawnAreaFree(Vector2 candidate, float requiredRadius)
    {
        float requiredRadiusSqr = requiredRadius * requiredRadius;

        for (int i = 0; i < activeEnemies.Count; i++)
        {
            EnemyHazard enemy = activeEnemies[i];
            if (enemy == null)
            {
                continue;
            }

            if (!enemy.gameObject.activeSelf)
            {
                continue;
            }

            if (((Vector2)enemy.transform.position - candidate).sqrMagnitude < requiredRadiusSqr)
            {
                return false;
            }
        }

        for (int i = 0; i < activePowerUps.Count; i++)
        {
            FlightPowerUp powerUp = activePowerUps[i];
            if (powerUp == null)
            {
                continue;
            }

            if (!powerUp.gameObject.activeSelf)
            {
                continue;
            }

            if (((Vector2)powerUp.transform.position - candidate).sqrMagnitude < requiredRadiusSqr)
            {
                return false;
            }
        }

        return true;
    }

    private PlatformBase RentPlatform(PlatformKind kind)
    {
        PlatformBase platform = kind switch
        {
            PlatformKind.Moving => movingPlatformPool.Count > 0 ? movingPlatformPool.Pop() : CreatePlatformComponent<MovingPlatform>(),
            PlatformKind.Breakable => breakablePlatformPool.Count > 0 ? breakablePlatformPool.Pop() : CreatePlatformComponent<BreakablePlatform>(),
            PlatformKind.Spring => springPlatformPool.Count > 0 ? springPlatformPool.Pop() : CreatePlatformComponent<SpringPlatform>(),
            _ => staticPlatformPool.Count > 0 ? staticPlatformPool.Pop() : CreatePlatformComponent<StaticPlatform>()
        };

        return platform;
    }

    private static T CreatePlatformComponent<T>() where T : PlatformBase
    {
        GameObject platformObject = new GameObject(typeof(T).Name);
        return platformObject.AddComponent<T>();
    }

    private EnemyHazard RentEnemy()
    {
        if (enemyPool.Count > 0)
        {
            return enemyPool.Pop();
        }

        return new GameObject("Enemy").AddComponent<EnemyHazard>();
    }

    private FlightPowerUp RentPowerUp()
    {
        if (powerUpPool.Count > 0)
        {
            return powerUpPool.Pop();
        }

        return new GameObject("Flight PowerUp").AddComponent<FlightPowerUp>();
    }

    private void RecyclePlatform(int index)
    {
        PlatformBase platform = activePlatforms[index];
        activePlatforms.RemoveAt(index);
        ReturnPlatform(platform);
    }

    private void RecycleEnemy(int index)
    {
        EnemyHazard enemy = activeEnemies[index];
        activeEnemies.RemoveAt(index);
        ReturnEnemy(enemy);
    }

    private void RecyclePowerUp(int index)
    {
        FlightPowerUp powerUp = activePowerUps[index];
        activePowerUps.RemoveAt(index);
        ReturnPowerUp(powerUp);
    }

    private void ReturnPlatform(PlatformBase platform)
    {
        if (platform == null)
        {
            return;
        }

        platform.Deactivate();

        switch (platform)
        {
            case MovingPlatform movingPlatform:
                movingPlatformPool.Push(movingPlatform);
                break;
            case BreakablePlatform breakablePlatform:
                breakablePlatformPool.Push(breakablePlatform);
                break;
            case SpringPlatform springPlatform:
                springPlatformPool.Push(springPlatform);
                break;
            case StaticPlatform staticPlatform:
                staticPlatformPool.Push(staticPlatform);
                break;
        }
    }

    private void ReturnEnemy(EnemyHazard enemy)
    {
        if (enemy == null)
        {
            return;
        }

        enemy.gameObject.SetActive(false);
        enemyPool.Push(enemy);
    }

    private void ReturnPowerUp(FlightPowerUp powerUp)
    {
        if (powerUp == null)
        {
            return;
        }

        powerUp.gameObject.SetActive(false);
        powerUpPool.Push(powerUp);
    }

    private enum PlatformKind
    {
        Static,
        Moving,
        Breakable,
        Spring
    }

	private Sprite GetSpriteByKind(PlatformKind kind)
	{
		return kind switch
		{
			PlatformKind.Moving => levelData.movingPlatform,
			PlatformKind.Breakable => levelData.breakablePlatform,
			PlatformKind.Spring => levelData.springPlatform,
			_ => levelData.staticPlatform
		};
	}

}
