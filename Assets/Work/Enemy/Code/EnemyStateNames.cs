namespace Work.Enemy.Code
{
    /// <summary>
    /// 적 FSM 상태 이름 상수.
    /// </summary>
    public static class EnemyStateNames
    {
        public const string IDLE = "Idle";
        public const string PATROL = "Patrol";
        public const string CHASE = "Chase";
        public const string ATTACK = "Attack";
        public const string RETURN = "Return";
        public const string DEAD = "Dead";
    }
}
