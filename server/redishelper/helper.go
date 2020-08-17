package redishelper

import (
	"shisanshui/config"
	"sync"
	"time"

	"github.com/gomodule/redigo/redis"
)

var (
	pool *redis.Pool

	once sync.Once
)

// newPool 新建redis连接池
func newPool(addr string) *redis.Pool {
	return &redis.Pool{
		MaxIdle:     3,
		IdleTimeout: 240 * time.Second,
		Dial:        func() (redis.Conn, error) { return redis.Dial("tcp", addr) },
	}
}

// GetConn get a redis connection
func GetConn() redis.Conn {
	if pool == nil {
		once.Do(func() {
			newPool(config.RedisServer)
		})
	}

	return pool.Get()
}
