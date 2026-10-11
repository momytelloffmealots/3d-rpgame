using UnityEngine;
using HealthSystem;

/// <summary>
/// Giao diện HUD hiển thị máu, hiệu ứng chớp đỏ khi bị thương, màn hình tử trận và phím tắt test.
/// Sử dụng OnGUI với Texture tự sinh (Texture2D) đảm bảo chạy mượt và hiển thị 100% không lo lỗi Canvas.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("=== THAM CHIẾU ===")]
    [SerializeField] private PlayerHealth playerHealth;

    [Header("=== CẤU HÌNH GIAO DIỆN ===")]
    [Tooltip("Hiển thị bảng hướng dẫn phím tắt ở góc màn hình")]
    public bool showGuide = true;

    [Tooltip("Phím ẩn/hiện bảng phím tắt")]
    public KeyCode toggleGuideKey = KeyCode.F1;

    // Màu sắc
    private Color healthyColor = new Color(0.18f, 0.8f, 0.44f);  // Xanh lá (#2ECC71)
    private Color warningColor = new Color(0.95f, 0.61f, 0.07f); // Vàng (#F39C12)
    private Color criticalColor = new Color(0.91f, 0.3f, 0.24f); // Đỏ (#E74C3C)

    // Trạng thái hiển thị
    private float displayedHealth = 100f;
    private float damageFlashAlpha = 0f;

    // Texture tự sinh
    private Texture2D whiteTexture;
    private Texture2D darkBgTexture;

    // GUIStyles
    private GUIStyle hpTextStyle;
    private GUIStyle deathTitleStyle;
    private GUIStyle deathSubtitleStyle;
    private GUIStyle guideStyle;
    private GUIStyle guideHeaderStyle;
    private bool stylesInitialized = false;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        }
    }

    private void Start()
    {
        if (playerHealth != null)
        {
            displayedHealth = playerHealth.startingHealth;
            playerHealth.OnDamageTaken += OnPlayerDamageTaken;
            playerHealth.OnRespawnEvent += OnPlayerRespawn;
        }

        CreateTextures();
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDamageTaken -= OnPlayerDamageTaken;
            playerHealth.OnRespawnEvent -= OnPlayerRespawn;
        }
    }

    private void OnPlayerDamageTaken(float damage, DamageSource source)
    {
        // Kích hoạt chớp đỏ màn hình
        damageFlashAlpha = Mathf.Clamp(damageFlashAlpha + 0.5f, 0f, 0.75f);
    }

    private void OnPlayerRespawn()
    {
        damageFlashAlpha = 0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleGuideKey))
        {
            showGuide = !showGuide;
        }

        // Làm mượt thanh máu
        if (playerHealth != null)
        {
            displayedHealth = Mathf.Lerp(displayedHealth, playerHealth.CurrentHealth, Time.deltaTime * 10f);
        }

        // Giảm dần hiệu ứng đỏ màn hình
        if (damageFlashAlpha > 0f)
        {
            damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, Time.deltaTime * 1.5f);
        }
    }

    private void CreateTextures()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();

        darkBgTexture = new Texture2D(1, 1);
        darkBgTexture.SetPixel(0, 0, new Color(0.08f, 0.08f, 0.08f, 0.85f));
        darkBgTexture.Apply();
    }

    private void InitStyles()
    {
        if (stylesInitialized) return;

        hpTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        hpTextStyle.normal.textColor = Color.white;

        deathTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 42,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        deathTitleStyle.normal.textColor = new Color(0.95f, 0.2f, 0.2f, 1f);

        deathSubtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        deathSubtitleStyle.normal.textColor = Color.white;

        guideHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        guideHeaderStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

        guideStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperLeft
        };
        guideStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

        stylesInitialized = true;
    }

    private void OnGUI()
    {
        InitStyles();

        // 1. Vẽ hiệu ứng chớp đỏ khi bị thương
        DrawDamageFlash();

        // 2. Vẽ thanh máu
        DrawHealthBar();

        // 3. Vẽ màn hình chết nếu nhân vật tử trận
        if (playerHealth != null && playerHealth.IsDead)
        {
            DrawDeathScreen();
        }

        // 4. Vẽ bảng phím tắt hướng dẫn
        if (showGuide)
        {
            DrawGuideBox();
        }
    }

    private void DrawDamageFlash()
    {
        if (damageFlashAlpha > 0.01f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0.85f, 0.1f, 0.1f, damageFlashAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);
            GUI.color = oldColor;
        }

        // Cảnh báo viền đỏ nhấp nháy khi máu dưới 25%
        if (playerHealth != null && !playerHealth.IsDead && playerHealth.CurrentHealth <= playerHealth.maxHealth * 0.25f)
        {
            float pulse = Mathf.PingPong(Time.time * 2.5f, 0.35f);
            Color oldColor = GUI.color;
            GUI.color = new Color(0.8f, 0f, 0f, pulse);
            // Viền 4 cạnh
            float borderThickness = 12f;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, borderThickness), whiteTexture);
            GUI.DrawTexture(new Rect(0, Screen.height - borderThickness, Screen.width, borderThickness), whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, borderThickness, Screen.height), whiteTexture);
            GUI.DrawTexture(new Rect(Screen.width - borderThickness, 0, borderThickness, Screen.height), whiteTexture);
            GUI.color = oldColor;
        }
    }

    private void DrawHealthBar()
    {
        if (playerHealth == null) return;

        float maxHp = Mathf.Max(1f, playerHealth.maxHealth);
        float currentHp = Mathf.Max(0f, playerHealth.CurrentHealth);
        float hpPercent = Mathf.Clamp01(displayedHealth / maxHp);

        float barWidth = 280f;
        float barHeight = 28f;
        float posX = 30f;
        float posY = Screen.height - 60f;

        // Khung nền đen
        Color oldColor = GUI.color;
        GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
        GUI.DrawTexture(new Rect(posX - 4, posY - 4, barWidth + 8, barHeight + 8), whiteTexture);

        // Màu thanh máu tùy theo % máu
        Color barColor = healthyColor;
        if (hpPercent <= 0.25f)
        {
            barColor = criticalColor;
        }
        else if (hpPercent <= 0.5f)
        {
            barColor = warningColor;
        }

        // Thanh máu
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(posX, posY, barWidth * hpPercent, barHeight), whiteTexture);
        GUI.color = oldColor;

        // Chữ HP
        string hpText = $"HP: {Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
        // Bóng chữ đen
        GUI.color = Color.black;
        GUI.Label(new Rect(posX + 1, posY + 1, barWidth, barHeight), hpText, hpTextStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(posX, posY, barWidth, barHeight), hpText, hpTextStyle);
    }

    private void DrawDeathScreen()
    {
        // Màn hình đen mờ
        Color oldColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), whiteTexture);
        GUI.color = oldColor;

        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;

        // Chữ BẠN ĐÃ TỬ TRẬN
        GUI.Label(new Rect(centerX - 300, centerY - 60, 600, 60), "BẠN ĐÃ TỬ TRẬN", deathTitleStyle);

        // Hướng dẫn hồi sinh
        GUI.Label(new Rect(centerX - 300, centerY + 20, 600, 40), "Nhấn phím [R] để Hồi Sinh", deathSubtitleStyle);
    }

    private void DrawGuideBox()
    {
        float width = 310f;
        float height = 210f;
        float posX = Screen.width - width - 20f;
        float posY = 20f;

        // Nền đen mờ
        Color oldColor = GUI.color;
        GUI.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
        GUI.DrawTexture(new Rect(posX, posY, width, height), whiteTexture);
        GUI.color = oldColor;

        // Tiêu đề
        GUI.Label(new Rect(posX + 15, posY + 10, width - 30, 25), "THỬ NGHIỆM HỆ THỐNG SÁT THƯƠNG", guideHeaderStyle);

        string content =
            "• [H] : Mất 15 HP (Test Hurt Sound)\n" +
            "• [J] : Hồi 25 HP (Test Heal)\n" +
            "• [K] : Tự tử (Test Die Sound)\n" +
            "• [R] : Hồi sinh (Sau khi chết)\n" +
            "• [T] : Bật / Tắt trụ súng (Bullet Damage)\n" +
            "• [E] : Kích nổ thùng bom đỏ khi lại gần\n" +
            "• Nhảy từ mái/thang xuống : Sát thương rơi\n" +
            "• [F1] : Ẩn / Hiện bảng hướng dẫn này";

        GUI.Label(new Rect(posX + 15, posY + 35, width - 30, height - 45), content, guideStyle);
    }
}
