package config

import (
	"fmt"
	"os"

	log "github.com/sirupsen/logrus"
)

const (
	// EnvNameGameSeverPort env game server port
	EnvNameGameSeverPort = "GAME_SERVER_PORT"
	// EnvK8sServiceHost k8s expose service host
	EnvK8sServiceHost = "KUBERNETES_SERVICE_HOST"
	// EnvServiceName service name
	EnvServiceName = "SERVICE_NAME"
	// EnvHostName host name
	EnvHostName = "HOSTNAME"
	// EnvServerID server ID
	EnvServerID = "ServerID"
	// EnvRedisServer redis server address
	EnvRedisServer = "REDIS_SERVER"
)

var (
	// RequiredAppModuleVer client module version that can access this server
	RequiredAppModuleVer = 0
	// RedisServer sredis server address
	RedisServer = ""

	// ServerID server unique id
	ServerID = ""

	// ServerPort server listen port
	ServerPort = ""
)

// Init init config
func Init() error {
	// is run in k8s cluster?
	if os.Getenv(EnvK8sServiceHost) != "" {
		serviceName := os.Getenv(EnvServiceName)
		ServerID = os.Getenv(EnvHostName) + "." + serviceName

		if serviceName == "" {
			return fmt.Errorf("no SERVICE_NAME found in ENV")
		}
	} else {
		ServerID = os.Getenv(EnvServerID)
	}

	if ServerID == "" {
		return fmt.Errorf("no ServerID be specified")
	}

	RedisServer = os.Getenv(EnvRedisServer)
	if RedisServer == "" {
		return fmt.Errorf("no REDIS_SERVER found in ENV")
	}

	ServerPort = os.Getenv(EnvNameGameSeverPort)
	if ServerPort == "" {
		return fmt.Errorf("no GAME_SERVER_PORT found in ENV")
	}

	log.Printf("ServerID:%s, ServerPort:%s, RedisServer:%s", ServerID, ServerPort, RedisServer)

	return nil
}
