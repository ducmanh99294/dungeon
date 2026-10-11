// NetworkMonster.cs — gắn vào mỗi Monster prefab
using UnityEngine;

public class NetworkMonster : MonoBehaviour
{
    [Header("Network")]
    public string monsterId; // ID từ server, do MonsterSpawnManager gán khi spawn

    // Gọi khi player đánh trúng — server tính damage, client chỉ báo
    public void ReportDamage(string attackId = "basic_attack")
    {
        if (string.IsNullOrEmpty(monsterId)) return;
        NetworkManager.Instance?.SendMonsterAttack(monsterId, attackId);
    }
}
