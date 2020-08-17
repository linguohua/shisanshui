package main

import (
	"flag"
	"fmt"
	"os"

	log "github.com/sirupsen/logrus"

	"shisanshui/config"
	"shisanshui/gameserver"
	"shisanshui/wait"
)

var (
	listenPort = ""
	daemon     = ""

	version     string
	longVersion string
)

const (
	monkeyServerID    = "monkey-game-server-id"
	monkeyRedisServer = "127.0.0.1:6379"
)

func init() {
	flag.StringVar(&listenPort, "l", "4443", "specify the listen address")
	flag.StringVar(&daemon, "d", "yes", "specify daemon mode")
}

// getVersion get version
func getVersion() string {
	return version
}

func getLongVersion() string {
	return longVersion
}

func main() {
	version := flag.Bool("v", false, "show version")
	lversion := flag.Bool("lv", false, "show long version")
	flag.Parse()

	if *version {
		fmt.Printf("%s\n", getVersion())
		os.Exit(0)
	}

	if *lversion {
		fmt.Printf("%s\n", getLongVersion())
		os.Exit(0)
	}

	log.Println("try to start  shisanshui server, version:", getVersion())

	// not in k8s cluster
	if os.Getenv(config.EnvK8sServiceHost) == "" {
		// inject monkey env
		os.Setenv(config.EnvNameGameSeverPort, listenPort)
		os.Setenv(config.EnvServerID, monkeyServerID)
		os.Setenv(config.EnvRedisServer, monkeyRedisServer)
	}

	err := config.Init()
	if err != nil {
		log.Panicln("config faield:", err)
	}

	params := &gameserver.Params{
		ListenAddr: fmt.Sprintf(":%s", config.ServerPort),
	}

	// start http server
	go gameserver.StartHTTPServer(params)
	log.Println("start shisanshui server ok!")

	if daemon == "yes" {
		wait.GetSignal()
	} else {
		wait.GetInput()
	}
	return
}
