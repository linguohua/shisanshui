package config

import (
	"fmt"
	"os"

	log "github.com/sirupsen/logrus"
)

const (
	// EnvK8sServiceHost k8s expose service host
	EnvK8sServiceHost = "KUBERNETES_SERVICE_HOST"
	// EnvServiceName service name
	EnvServiceName = "SERVICE_NAME"
	// EnvHostName host name
	EnvHostName = "HOSTNAME"
	// EnvServerID server ID
	EnvServerID = "ServerID"
)

var (
	// RequiredAppModuleVer client module version that can access this server
	RequiredAppModuleVer = 0

	// ServerID server unique id
	ServerID = ""

	// ServerPort server listen port
	ServerPort = 0

	// RedisServer sredis server address
	RedisServer = ""

	// ConfigServer config server
	ConfigServer = ""

	// GameLobbyServer the game lobby
	GameLobbyServer = ""
	// BusinessLobbyServer bussiness lobby server
	BusinessLobbyServer = ""

	// IsInK8SCluster is in k8s cluster or not
	IsInK8SCluster = false
)

// Init init config
func Init(filepath string) error {
	// is run in k8s cluster?
	if os.Getenv(EnvK8sServiceHost) != "" {
		log.Println("server run in k8s cluster")
		IsInK8SCluster = true
		serviceName := os.Getenv(EnvServiceName)
		ServerID = os.Getenv(EnvHostName) + "." + serviceName

		if serviceName == "" {
			return fmt.Errorf("no EnvServiceName found in ENV")
		}
	}

	err := loadFromFile(filepath)
	if err != nil {
		return err
	}

	if ServerID == "" {
		return fmt.Errorf("no ServerID be specified")
	}

	log.Printf("ServerID:%s, RedisServer:%s, ConfigServer:%s", ServerID, RedisServer, ConfigServer)
	log.Printf("GameLobbyServer:%s, BusinessLobbyServer:%s", GameLobbyServer, BusinessLobbyServer)
	return nil
}
