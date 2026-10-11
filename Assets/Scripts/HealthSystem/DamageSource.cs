namespace HealthSystem
{
    /// <summary>
    /// Phân loại nguồn gốc sát thương
    /// </summary>
    public enum DamageSource
    {
        Generic,    // Sát thương chung
        Bullet,     // Đạn từ súng / turret
        Explosion,  // Bom / lựu đạn / thùng nổ
        Fall        // Rơi từ trên cao xuống đất
    }
}
