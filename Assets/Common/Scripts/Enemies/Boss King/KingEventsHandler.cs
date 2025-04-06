// KingEventsHandler.cs
using UnityEngine;

namespace OctoberStudio.Enemy
{
    public class KingEventsHandler : MonoBehaviour
    {
        [SerializeField] private EnemyKingBehavior king;

        public void FireBomb()
        {
            king?.FireBombFromEvent();
        }
    }
}