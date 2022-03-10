using TeamDev.Redis;

public interface IRedisAutoconnect
{ 
    void RegisterListeners(RedisDataAccessProvider redis);
}
