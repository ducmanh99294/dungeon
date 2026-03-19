using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;    // Kéo Prefab Enemy_Slime vào đây
    public float spawnRate = 2f;      // Tốc độ sinh (2 giây 1 con)
    public float spawnRadius = 5f;    // Bán kính sinh quái

    [Header("Cài đặt Giới hạn Quái")]
    public int maxEnemiesAtOnce = 5;      // Tối đa 5 con cùng lúc trên màn hình
    public int totalEnemiesToSpawn = 10;  // Tổng cộng màn này chỉ sinh đúng 10 con

    private int enemiesSpawnedSoFar = 0;  // Biến đếm số quái đã sinh (Không cần chỉnh)
    private float nextSpawnTime = 0f;

    void Update()
    {
        // 1. Nếu đã sinh đủ tổng số quái của màn chơi -> Dừng lại, không làm gì thêm
        if (enemiesSpawnedSoFar >= totalEnemiesToSpawn)
        {
            return;
        }

        // 2. Nếu chưa đủ tổng số, kiểm tra xem đã đến thời gian sinh quái chưa
        if (Time.time >= nextSpawnTime)
        {
            // Đếm xem hiện tại đang có bao nhiêu con quái còn sống trên màn hình (dựa vào Tag)
            int currentActiveEnemies = GameObject.FindGameObjectsWithTag("Enemy").Length;

            // 3. Nếu số quái hiện tại trên màn hình VẪN ÍT HƠN mức cho phép cùng lúc -> Mới sinh thêm
            if (currentActiveEnemies < maxEnemiesAtOnce)
            {
                SpawnEnemy();

                enemiesSpawnedSoFar++; // Tăng số lượng đếm tổng lên 1

                // Hẹn giờ cho lần sinh tiếp theo
                nextSpawnTime = Time.time + 1f / spawnRate;
            }
        }
    }

    void SpawnEnemy()
    {
        // Tính toán vị trí ngẫu nhiên
        Vector2 randomPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;

        // Tạo quái vật
        Instantiate(enemyPrefab, randomPos, Quaternion.identity);
    }

    void OnDrawGizmosSelected()
    {
        // Vẽ vòng tròn màu xanh để dễ nhìn trong lúc thiết kế
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}