using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Основные настройки атаки")]
    public int damage = 25;
    public LayerMask enemyLayers;

    [Header("Комбо система")]
    public int maxComboSteps = 4;
    public float resetComboAfter = 1.5f;
    public float attackCooldown = 0.3f; // Минимальная задержка между атаками

    [Header("Привязки анимаций")]
    public Animator anim;
    public string[] attackTriggers = { "attack1", "attack2", "attack3", "attack4" };

    [Header("Настройки меча для каждой анимации")]
    public SwordSettings[] swordSettings;

    [Header("Визуальные эффекты")]
    public LineRenderer slashLine;
    public GameObject hitEffect;
    public ParticleSystem slashParticle;

    [Header("Настройки линий")]
    public Color slashColor = new Color(0, 0.5f, 1f, 0.8f);
    public float slashDuration = 0.1f;
    public AnimationCurve slashWidthCurve = AnimationCurve.EaseInOut(0, 0.3f, 1, 0);

    [Header("Эффект призрака меча")]
    public bool enableGhostSword = true;
    public int ghostSwordCount = 5;
    public float ghostSwordSpacing = 0.05f;
    public float ghostSwordLifetime = 0.3f;
    public Color ghostSwordColor = new Color(0, 0.5f, 1f, 0.3f);

    [Header("Аудио")]
    public AudioClip[] attackSounds;
    public AudioClip hitSound;
    public AudioSource audioSource;

    [Header("Точки атаки")]
    public Transform attackStartPoint;
    public Transform attackEndPoint;
    public bool useTwoPoints = true;

    private int currentComboStep = 0;
    private float lastAttackTime = 0f;
    private float lastComboTime = 0f;
    private bool isAttacking = false;
    private bool pendingNextAttack = false; // Ожидание следующей атаки

    // Для управления корутинами
    private Coroutine currentDamageCoroutine;
    private Coroutine currentSlashCoroutine;
    private Coroutine currentGhostCoroutine;

    [System.Serializable]
    public class SwordSettings
    {
        public float swordLength = 1.5f;
        public float damageDelay = 0.2f;
        public float swordAngle = 0f;
        public Color swordColor = new Color(0, 0.5f, 1f, 0.8f);
        public Vector2 startPointOffset = Vector2.zero;
        public Vector2 endPointOffset = new Vector2(1.5f, 0.5f);
        public bool useCustomPoints = false;
    }

    void Start()
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (attackStartPoint == null || attackEndPoint == null)
        {
            SetupAttackPoints();
        }

        SetupSwordSettings();
        SetupLineRenderer();
    }

    void SetupAttackPoints()
    {
        if (attackStartPoint == null)
        {
            GameObject startObj = new GameObject("AttackStartPoint");
            startObj.transform.SetParent(transform);
            attackStartPoint = startObj.transform;
            attackStartPoint.localPosition = new Vector3(0.5f, 0.5f, 0);
        }

        if (attackEndPoint == null)
        {
            GameObject endObj = new GameObject("AttackEndPoint");
            endObj.transform.SetParent(transform);
            attackEndPoint = endObj.transform;
            attackEndPoint.localPosition = new Vector3(2f, 0.5f, 0);
        }
    }

    void SetupSwordSettings()
    {
        if (swordSettings == null || swordSettings.Length == 0)
        {
            swordSettings = new SwordSettings[maxComboSteps];
            for (int i = 0; i < maxComboSteps; i++)
            {
                swordSettings[i] = new SwordSettings();
                swordSettings[i].swordLength = 1.5f + (i * 0.2f);
                swordSettings[i].damageDelay = 0.15f + (i * 0.03f);
                swordSettings[i].swordAngle = i * 10f;
                swordSettings[i].useCustomPoints = false;
            }
        }
    }

    void SetupLineRenderer()
    {
        if (slashLine == null)
        {
            GameObject lineObj = new GameObject("SlashLine");
            lineObj.transform.SetParent(transform);
            slashLine = lineObj.AddComponent<LineRenderer>();
        }

        slashLine.positionCount = 2;
        slashLine.startWidth = 0.15f;
        slashLine.endWidth = 0.03f;
        slashLine.material = new Material(Shader.Find("Sprites/Default"));
        slashLine.enabled = false;
    }

    void Update()
    {
        // Проверяем нажатие атаки
        if (Input.GetButtonDown("Fire1"))
        {
            if (!isAttacking && Time.time - lastAttackTime >= attackCooldown)
            {
                // Если не атакуем - начинаем атаку
                Attack();
            }
            else if (isAttacking)
            {
                // Если атакуем - запоминаем, что нужно начать следующую атаку
                pendingNextAttack = true;
                Debug.Log("Attack buffered for next combo step");
            }
        }

        // Сброс комбо по таймеру
        if (Time.time - lastComboTime > resetComboAfter && currentComboStep > 0)
        {
            ResetCombo();
        }
    }

    void Attack()
    {
        // Останавливаем предыдущие корутины
        StopAllAttackCoroutines();

        isAttacking = true;
        lastAttackTime = Time.time;
        lastComboTime = Time.time;
        pendingNextAttack = false; // Сбрасываем ожидание

        // Увеличиваем шаг комбо
        if (currentComboStep < maxComboSteps)
        {
            currentComboStep++;
        }
        else
        {
            ResetCombo();
            currentComboStep = 1;
        }

        Debug.Log($"Attacking! Combo step: {currentComboStep}");

        // Запускаем анимацию
        string triggerName = attackTriggers[currentComboStep - 1];
        if (anim != null)
        {
            // Сбрасываем все триггеры атаки
            foreach (string trigger in attackTriggers)
            {
                anim.ResetTrigger(trigger);
            }
            anim.SetTrigger(triggerName);
        }

        PlayAttackSound();

        // Запускаем визуальные эффекты
        if (enableGhostSword)
        {
            currentGhostCoroutine = StartCoroutine(SpawnGhostSwords());
        }

        currentSlashCoroutine = StartCoroutine(ShowSlashLineWithAnimation());

        // Запускаем нанесение урона (через задержку или Animation Event)
        SwordSettings settings = GetCurrentSwordSettings();
        currentDamageCoroutine = StartCoroutine(DealDamageWithDelay(settings.damageDelay));
    }

    void StopAllAttackCoroutines()
    {
        if (currentDamageCoroutine != null)
            StopCoroutine(currentDamageCoroutine);
        if (currentSlashCoroutine != null)
            StopCoroutine(currentSlashCoroutine);
        if (currentGhostCoroutine != null)
            StopCoroutine(currentGhostCoroutine);
    }

    SwordSettings GetCurrentSwordSettings()
    {
        if (currentComboStep <= swordSettings.Length)
        {
            return swordSettings[currentComboStep - 1];
        }
        return swordSettings[swordSettings.Length - 1];
    }

    IEnumerator DealDamageWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        SwordSettings settings = GetCurrentSwordSettings();
        GetAttackPoints(out Vector3 startPoint, out Vector3 endPoint, settings);

        Vector3 attackDirection = (endPoint - startPoint).normalized;
        float attackLength = Vector3.Distance(startPoint, endPoint);

        if (slashParticle != null)
        {
            slashParticle.Play();
        }

        Collider[] hitEnemies = Physics.OverlapSphere(startPoint, attackLength, enemyLayers);

        foreach (Collider enemy in hitEnemies)
        {
            Vector3 closestPoint = GetClosestPointOnLine(startPoint, endPoint, enemy.transform.position);
            float distanceToLine = Vector3.Distance(enemy.transform.position, closestPoint);
            float enemyDot = Vector3.Dot(attackDirection, (enemy.transform.position - startPoint).normalized);

            if (distanceToLine <= 0.8f && enemyDot > 0.2f && enemyDot <= 1.2f)
            {
                HealthEnemy enemyHealth = enemy.GetComponent<HealthEnemy>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);

                    if (hitEffect != null)
                    {
                        Instantiate(hitEffect, enemy.transform.position, Quaternion.identity);
                    }

                    if (hitSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(hitSound);
                    }
                }
            }
        }
    }

    // ВАЖНО: Этот метод вызывается в КОНЦЕ анимации атаки
    // Добавьте Animation Event в конец каждой анимации атаки
    public void OnAttackAnimationEnd()
    {
        Debug.Log($"Attack animation ended! Combo step: {currentComboStep}, Pending: {pendingNextAttack}");

        // Анимация закончилась - снимаем флаг атаки
        isAttacking = false;

        // Если есть ожидающая атака - начинаем следующую
        if (pendingNextAttack && currentComboStep < maxComboSteps)
        {
            Debug.Log("Starting next combo attack!");
            Attack();
        }
        else if (currentComboStep >= maxComboSteps)
        {
            // Если это был последний удар - сбрасываем комбо
            ResetCombo();
        }
    }

    // Альтернативный метод: если не хотите использовать Animation Events
    // Вызывайте этот метод в корутине через время, равное длине анимации
    public void ForceEndAttack()
    {
        if (isAttacking)
        {
            isAttacking = false;

            if (pendingNextAttack && currentComboStep < maxComboSteps)
            {
                Attack();
            }
            else if (currentComboStep >= maxComboSteps)
            {
                ResetCombo();
            }
        }
    }

    void GetAttackPoints(out Vector3 startPoint, out Vector3 endPoint, SwordSettings settings)
    {
        bool useCustomTwoPoints = useTwoPoints && (settings.useCustomPoints || attackStartPoint != null && attackEndPoint != null);

        if (useCustomTwoPoints)
        {
            if (settings.useCustomPoints)
            {
                startPoint = attackStartPoint.position + (Vector3)settings.startPointOffset;
                endPoint = attackEndPoint.position + (Vector3)settings.endPointOffset;
            }
            else
            {
                startPoint = attackStartPoint.position;
                endPoint = attackEndPoint.position;
            }

            if (settings.swordAngle != 0)
            {
                Quaternion rotation = Quaternion.Euler(0, 0, settings.swordAngle);
                Vector3 center = (startPoint + endPoint) / 2;
                Vector3 direction = endPoint - startPoint;
                direction = rotation * direction;
                startPoint = center - direction / 2;
                endPoint = center + direction / 2;
            }
        }
        else
        {
            startPoint = attackStartPoint != null ? attackStartPoint.position : transform.position;
            Vector3 attackDirection = GetAttackDirection();

            if (settings.swordAngle != 0)
            {
                Quaternion rotation = Quaternion.Euler(0, 0, settings.swordAngle);
                attackDirection = rotation * attackDirection;
            }

            endPoint = startPoint + attackDirection * settings.swordLength;
        }
    }

    Vector3 GetClosestPointOnLine(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
    {
        Vector3 lineDirection = (lineEnd - lineStart).normalized;
        float lineLength = Vector3.Distance(lineStart, lineEnd);

        Vector3 pointToStart = point - lineStart;
        float projection = Vector3.Dot(pointToStart, lineDirection);
        projection = Mathf.Clamp(projection, 0, lineLength);

        return lineStart + lineDirection * projection;
    }

    IEnumerator ShowSlashLineWithAnimation()
    {
        if (slashLine == null) yield break;

        SwordSettings settings = GetCurrentSwordSettings();
        GetAttackPoints(out Vector3 startPoint, out Vector3 endPoint, settings);
        Vector3 fullEndPoint = endPoint;

        slashLine.enabled = true;
        slashLine.startColor = settings.swordColor;
        slashLine.endColor = new Color(settings.swordColor.r, settings.swordColor.g, settings.swordColor.b, 0);

        float elapsed = 0;
        while (elapsed < slashDuration)
        {
            float t = elapsed / slashDuration;
            Vector3 currentEndPoint = Vector3.Lerp(startPoint, fullEndPoint, t);

            slashLine.SetPosition(0, startPoint);
            slashLine.SetPosition(1, currentEndPoint);

            float width = slashWidthCurve.Evaluate(t) * 0.3f;
            slashLine.startWidth = width;
            slashLine.endWidth = width * 0.2f;

            elapsed += Time.deltaTime;
            yield return null;
        }

        slashLine.enabled = false;
    }

    IEnumerator SpawnGhostSwords()
    {
        SwordSettings settings = GetCurrentSwordSettings();

        for (int i = 0; i < ghostSwordCount; i++)
        {
            CreateGhostSword(settings, i);
            yield return new WaitForSeconds(ghostSwordSpacing);
        }
    }

    void CreateGhostSword(SwordSettings settings, int index)
    {
        GetAttackPoints(out Vector3 startPoint, out Vector3 endPoint, settings);

        float progress = (float)index / ghostSwordCount;
        Vector3 currentEndPoint = Vector3.Lerp(startPoint, endPoint, 0.3f + progress * 0.7f);

        GameObject ghostObj = new GameObject($"GhostSword_{index}");
        ghostObj.transform.SetParent(transform);

        LineRenderer ghostLine = ghostObj.AddComponent<LineRenderer>();
        ghostLine.positionCount = 2;
        ghostLine.SetPosition(0, startPoint);
        ghostLine.SetPosition(1, currentEndPoint);

        ghostLine.startWidth = 0.1f;
        ghostLine.endWidth = 0.02f;

        float alpha = ghostSwordColor.a * (1 - progress * 0.7f);
        Color fadedColor = new Color(ghostSwordColor.r, ghostSwordColor.g, ghostSwordColor.b, alpha);
        ghostLine.startColor = fadedColor;
        ghostLine.endColor = new Color(fadedColor.r, fadedColor.g, fadedColor.b, 0);

        ghostLine.material = new Material(Shader.Find("Sprites/Default"));

        StartCoroutine(FadeGhostSword(ghostLine, ghostObj));
        Destroy(ghostObj, ghostSwordLifetime);
    }

    IEnumerator FadeGhostSword(LineRenderer line, GameObject obj)
    {
        float startTime = Time.time;
        Color startColor = line.startColor;

        while (Time.time - startTime < ghostSwordLifetime)
        {
            float t = (Time.time - startTime) / ghostSwordLifetime;
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(startColor.a, 0, t);
            line.startColor = newColor;
            yield return null;
        }
    }

    Vector3 GetAttackDirection()
    {
        float mouseX = Input.GetAxis("Mouse X");

        if (Mathf.Abs(mouseX) > 0.1f)
        {
            return new Vector3(Mathf.Sign(mouseX), 0.2f, 0).normalized;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        if (horizontal != 0)
        {
            return new Vector3(horizontal, 0.2f, 0).normalized;
        }

        return new Vector3(1, 0.2f, 0).normalized;
    }

    void PlayAttackSound()
    {
        if (audioSource != null && attackSounds != null && attackSounds.Length > 0)
        {
            int soundIndex = Mathf.Min(currentComboStep - 1, attackSounds.Length - 1);
            if (attackSounds[soundIndex] != null)
            {
                audioSource.PlayOneShot(attackSounds[soundIndex]);
            }
        }
    }

    void ResetCombo()
    {
        Debug.Log("Combo reset!");
        currentComboStep = 0;
        pendingNextAttack = false;
        isAttacking = false;
    }
}