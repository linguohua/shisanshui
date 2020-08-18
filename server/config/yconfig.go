package config

import (
	"fmt"
	"io/ioutil"

	log "github.com/sirupsen/logrus"
	"gopkg.in/yaml.v2"
)

type yexternServer struct {
	Name string `yaml:"name"`
	Addr string `yaml:"addr"`
}

type yconfig struct {
	ServerPort    int              `yaml:"serverPort"`
	ServerID      string           `yaml:"serverID"`
	ExternServers []*yexternServer `yaml:"externServers"`
}

func loadFromFile(filePath string) error {
	content, err := ioutil.ReadFile(filePath)
	if err != nil {
		return err
	}

	var yc = yconfig{}
	err = yaml.Unmarshal(content, &yc)
	if err != nil {
		return err
	}

	ServerPort = yc.ServerPort
	if ServerPort == 0 {
		return fmt.Errorf("no ServerPort found in yaml config file")
	}

	if !IsInK8SCluster {
		// set server id from config file when not in k8s cluster
		ServerID = yc.ServerID
	}

	for _, es := range yc.ExternServers {
		switch es.Name {
		case "redis-server":
			RedisServer = es.Addr
		case "game-lobby-server":
			GameLobbyServer = es.Addr
		case "business-lobby-server":
			BusinessLobbyServer = es.Addr
		case "config-server":
			ConfigServer = es.Addr
		default:
			log.Warnln("unsupport external server name:", es.Name)
		}
	}

	if RedisServer == "" {
		return fmt.Errorf("no redis-server found in yaml config file")
	}

	if GameLobbyServer == "" {
		return fmt.Errorf("game-lobby-server found in ENV")
	}

	if BusinessLobbyServer == "" {
		return fmt.Errorf("no business-lobby-server found in yaml config file")
	}

	if ConfigServer == "" {
		return fmt.Errorf("no config-server found in ENV")
	}

	return nil
}
